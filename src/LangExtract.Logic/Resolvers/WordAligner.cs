using LangExtract.Core;
using LangExtract.Logic.Tokenizers;
using Microsoft.VisualBasic;

namespace LangExtract.Logic.Resolvers;

/// <summary>
    /// Aligns words between two sequences of tokens using sequence matching algorithms.
    /// </summary>
    public class WordAligner
    {
        private SequenceMatcher _matcher;
        private IReadOnlyList<string> _sourceTokens;
        private IReadOnlyList<string> _extractionTokens;

        public WordAligner()
        {
            _matcher = new SequenceMatcher();
        }

        /// <summary>
        /// Sets the source and extraction tokens for alignment.
        /// </summary>
        /// <param name="sourceTokens">A nonempty sequence of word-level tokens from source text.</param>
        /// <param name="extractionTokens">A nonempty sequence of extraction tokens in order for matching.</param>
        /// <exception cref="ArgumentException">If either sequence is empty.</exception>
        private void SetSeqs(IEnumerable<string> sourceTokens, IEnumerable<string> extractionTokens)
        {
            var sourceList = sourceTokens.ToList();
            var extractionList = extractionTokens.ToList();

            if (sourceList.Count == 0 || extractionList.Count == 0)
            {
                throw new ArgumentException("Source tokens and extraction tokens cannot be empty.");
            }

            _sourceTokens = sourceList;
            _extractionTokens = extractionList;
            _matcher.SetSequences(sourceList, extractionList);
        }

        /// <summary>
        /// Gets matching blocks of tokens using sequence matching.
        /// </summary>
        /// <returns>Sequence of matching blocks between source and extraction tokens.</returns>
        /// <exception cref="InvalidOperationException">If sequences haven't been set.</exception>
        private IReadOnlyList<MatchingBlock> GetMatchingBlocks()
        {
            if (_sourceTokens == null || _extractionTokens == null)
            {
                throw new InvalidOperationException(
                    "Source tokens and extraction tokens must be set before getting matching blocks.");
            }
            return _matcher.GetMatchingBlocks();
        }

        /// <summary>
        /// Fuzzy-align an extraction using sequence matching on tokens.
        /// 
        /// The algorithm scans every candidate window in sourceTokens and selects
        /// the window with the highest similarity ratio.
        /// </summary>
        /// <param name="extraction">The extraction to align.</param>
        /// <param name="sourceTokens">The tokens from the source text.</param>
        /// <param name="tokenizedText">The tokenized source text.</param>
        /// <param name="tokenOffset">The token offset of the current chunk.</param>
        /// <param name="charOffset">The character offset of the current chunk.</param>
        /// <param name="fuzzyAlignmentThreshold">The minimum ratio for a fuzzy match.</param>
        /// <param name="tokenizerImpl">Optional tokenizer instance.</param>
        /// <returns>The aligned Extraction if successful, null otherwise.</returns>
        private Extraction? FuzzyAlignExtraction(
            Extraction extraction,
            List<string> sourceTokens,
            TokenizedText tokenizedText,
            int tokenOffset,
            int charOffset,
            float fuzzyAlignmentThreshold = Constants.FuzzyAlignmentMinThreshold,
            ITokenizer tokenizerImpl = null)
        {
            var extractionTokens = TokenizeWithLowercase(extraction.ExtractionText, tokenizerImpl).ToList();
            var extractionTokensNorm = extractionTokens.Select(NormalizeToken).ToList();

            if (extractionTokens.Count == 0)
            {
                return null;
            }

            var bestRatio = 0.0f;
            (int startIdx, int windowSize)? bestSpan = null;

            var lenE = extractionTokens.Count;
            var maxWindow = sourceTokens.Count;

            var extractionCounts = CountTokens(extractionTokensNorm);
            var minOverlap = (int)(lenE * fuzzyAlignmentThreshold);

            var matcher = new SequenceMatcher();
            matcher.SetSequence2(extractionTokensNorm);

            for (int windowSize = lenE; windowSize <= maxWindow; windowSize++)
            {
                if (windowSize > sourceTokens.Count)
                    break;

                // Initialize sliding window
                var windowDeque = new Queue<string>(sourceTokens.Take(windowSize));
                var windowCounts = CountTokens(windowDeque.Select(NormalizeToken));

                for (int startIdx = 0; startIdx <= sourceTokens.Count - windowSize; startIdx++)
                {
                    // Fast pre-check: count overlapping tokens
                    int overlap = CountOverlap(extractionCounts, windowCounts);
                    
                    if (overlap >= minOverlap)
                    {
                        var windowTokensNorm = windowDeque.Select(NormalizeToken).ToList();
                        matcher.SetSequence1(windowTokensNorm);
                        
                        int matches = matcher.GetMatchingBlocks()
                            .Where(b => b.Size > 0)
                            .Sum(b => b.Size);
                        
                        float ratio = lenE > 0 ? (float)matches / lenE : 0.0f;
                        
                        if (ratio > bestRatio)
                        {
                            bestRatio = ratio;
                            bestSpan = (startIdx, windowSize);
                        }
                    }

                    // Slide the window to the right
                    if (startIdx + windowSize < sourceTokens.Count)
                    {
                        // Remove leftmost token
                        string oldToken = windowDeque.Dequeue();
                        string oldTokenNorm = NormalizeToken(oldToken);
                        DecrementCount(windowCounts, oldTokenNorm);

                        // Add new rightmost token
                        string newToken = sourceTokens[startIdx + windowSize];
                        windowDeque.Enqueue(newToken);
                        string newTokenNorm = NormalizeToken(newToken);
                        IncrementCount(windowCounts, newTokenNorm);
                    }
                }
            }

            if (bestSpan.HasValue && bestRatio >= fuzzyAlignmentThreshold)
            {
                var (startIdx, windowSize) = bestSpan.Value;

                try
                {
                    extraction.TokenInterval = new TokenInterval(
                        startIndex: startIdx + tokenOffset,
                        endIndex: startIdx + windowSize + tokenOffset);

                    var startToken = tokenizedText.Tokens[startIdx];
                    var endToken = tokenizedText.Tokens[startIdx + windowSize - 1];
                    
                    extraction.CharInterval = new CharInterval(
                        startPos: charOffset + startToken.CharInterval.StartPos,
                        endPos: charOffset + endToken.CharInterval.EndPos);

                    extraction.AlignmentStatus = AlignmentStatus.MatchFuzzy;
                    return extraction;
                }
                catch (IndexOutOfRangeException)
                {
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// Aligns extractions with their positions in the source text.
        /// </summary>
        /// <param name="allExtractions">Sequences of extractions to align.</param>
        /// <param name="sourceText">The source text against which extractions are to be aligned.</param>
        /// <param name="tokenOffset">The offset to add to token indices.</param>
        /// <param name="charOffset">The offset to add to character positions.</param>
        /// <param name="delim">Token used to separate multi-token extractions.</param>
        /// <param name="enableFuzzyAlignment">Whether to use fuzzy alignment when exact matching fails.</param>
        /// <param name="fuzzyAlignmentThreshold">Minimum token overlap ratio for fuzzy alignment.</param>
        /// <param name="acceptMatchLesser">Whether to accept partial exact matches.</param>
        /// <param name="tokenizerImpl">Optional tokenizer instance.</param>
        /// <returns>Sequences of aligned extractions.</returns>
        public IReadOnlyList<Extraction> AlignExtractions(
            IEnumerable<Extraction> allExtractions,
            string sourceText,
            int tokenOffset = 0,
            int charOffset = 0,
            ITokenizer? tokenizerImpl = null,
            string delim = "\u241F",
            bool enableFuzzyAlignment = true,
            float fuzzyAlignmentThreshold = Constants.FuzzyAlignmentMinThreshold,
            bool acceptMatchLesser = true)
        {
            var sourceTokens = TokenizeWithLowercase(sourceText, tokenizerImpl).ToList();

            var delimTokens = TokenizeWithLowercase(delim, tokenizerImpl).ToList();
            int delimLen = delimTokens.Count;
            
            if (delimLen != 1)
            {
                throw new ArgumentException($"Delimiter '{delim}' must be a single token.");
            }

            // Join all extraction texts with delimiter
            var joinedText = string.Join($" {delim} ", allExtractions.Select(e => e.ExtractionText));
            var extractionTokens = TokenizeWithLowercase(joinedText, tokenizerImpl).ToList();

            SetSeqs(sourceTokens, extractionTokens);

            // Build index mapping
            var indexToExtractionGroup = new Dictionary<int, Extraction>();
            int extractionIndex = 0;
            
            foreach (var extraction in allExtractions)
            {
                // Validate delimiter doesn't appear in extraction text
                if (extraction.ExtractionText.Contains(delim))
                {
                    throw new ArgumentException(
                        $"Delimiter '{delim}' appears inside extraction text '{extraction.ExtractionText}'. " +
                        "This would corrupt alignment mapping.");
                }

                indexToExtractionGroup[extractionIndex] = extraction;
                    
                var extractionTextTokens = TokenizeWithLowercase(extraction.ExtractionText, tokenizerImpl).ToList();
                extractionIndex += extractionTextTokens.Count + delimLen;
            }

            var tokenizedText = tokenizerImpl != null
                ? tokenizerImpl.Tokenize(sourceText)
                : new RegexTokenizer().Tokenize(sourceText);

            // Track aligned extractions
            var alignedExtractions = new HashSet<Extraction>();
            int exactMatches = 0;
            int lesserMatches = 0;

            // Exact matching phase
            foreach (var block in GetMatchingBlocks().Where(b => b.Size > 0))
            {
                int i = block.IndexA;
                int j = block.IndexB;
                int n = block.Size;

                if (!indexToExtractionGroup.TryGetValue(j, out var extraction))
                {
                    continue;
                }

                extraction.TokenInterval = new TokenInterval(
                    startIndex: i + tokenOffset,
                    endIndex: i + n + tokenOffset);

                try
                {
                    var startToken = tokenizedText.Tokens[i];
                    var endToken = tokenizedText.Tokens[i + n - 1];
                    
                    extraction.CharInterval = new CharInterval(
                        startPos: charOffset + startToken.CharInterval.StartPos,
                        endPos: charOffset + endToken.CharInterval.EndPos);
                }
                catch (IndexOutOfRangeException e)
                {
                    throw new IndexOutOfRangeException(
                        $"Failed to align extraction with source text. Extraction token interval " +
                        $"{extraction.TokenInterval} does not match source text tokens.", e);
                }

                int extractionTextLen = TokenizeWithLowercase(extraction.ExtractionText, tokenizerImpl).Count();
                
                if (extractionTextLen < n)
                {
                    throw new InvalidOperationException(
                        $"Delimiter prevents blocks greater than extraction length: " +
                        $"extractionTextLen={extractionTextLen}, blockSize={n}");
                }
                
                if (extractionTextLen == n)
                {
                    extraction.AlignmentStatus = AlignmentStatus.MatchExact;
                    exactMatches++;
                    alignedExtractions.Add(extraction);
                }
                else
                {
                    // Partial match
                    if (acceptMatchLesser)
                    {
                        extraction.AlignmentStatus = AlignmentStatus.MatchLesser;
                        lesserMatches++;
                        alignedExtractions.Add(extraction);
                    }
                    else
                    {
                        // Reset intervals when not accepting lesser matches
                        extraction.TokenInterval = null;
                        extraction.CharInterval = null;
                        extraction.AlignmentStatus = null;
                    }
                }
            }

            // Collect unaligned extractions
            var unalignedExtractions = new List<Extraction>();
            foreach (var extraction in indexToExtractionGroup.Values)
            {
                if (!alignedExtractions.Contains(extraction))
                {
                    unalignedExtractions.Add(extraction);
                }
            }

            // Apply fuzzy alignment to remaining extractions
            if (enableFuzzyAlignment && unalignedExtractions.Count > 0)
            {
                foreach (var extraction in unalignedExtractions)
                {
                    var alignedExtraction = FuzzyAlignExtraction(
                        extraction,
                        sourceTokens,
                        tokenizedText,
                        tokenOffset,
                        charOffset,
                        fuzzyAlignmentThreshold,
                        tokenizerImpl);
                    
                    if (alignedExtraction != null)
                    {
                        alignedExtractions.Add(alignedExtraction);
                    }
                }
            }

            return alignedExtractions.ToList().AsReadOnly();
        }

        /// <summary>
        /// Tokenize text with lowercase transformation.
        /// </summary>
        private static IEnumerable<string> TokenizeWithLowercase(string text, ITokenizer tokenizerInst = null)
        {
            var tokenizedPb = tokenizerInst != null
                ? tokenizerInst.Tokenize(text)
                : new RegexTokenizer().Tokenize(text);

            string originalText = tokenizedPb.Text;
            
            foreach (var token in tokenizedPb.Tokens)
            {
                var start = token.CharInterval.StartPos ?? 0;
                var end = token.CharInterval.EndPos ?? 0;
                var tokenStr = originalText.Substring(start, end - start);
                yield return tokenStr.ToLowerInvariant();
            }
        }

        /// <summary>
        /// Normalize token with lowercase and light stemming.
        /// </summary>
        private static string NormalizeToken(string token)
        {
            token = token.ToLowerInvariant();
            
            // Light pluralization stemming
            if (token.Length > 3 && token.EndsWith("s") && !token.EndsWith("ss"))
            {
                token = token.Substring(0, token.Length - 1);
            }
            
            return token;
        }

        /// <summary>
        /// Count tokens in a sequence.
        /// </summary>
        private static Dictionary<string, int> CountTokens(IEnumerable<string> tokens)
        {
            var counts = new Dictionary<string, int>();
            foreach (var token in tokens)
            {
                if (counts.ContainsKey(token))
                    counts[token]++;
                else
                    counts[token] = 1;
            }
            return counts;
        }

        /// <summary>
        /// Count overlapping tokens between two dictionaries.
        /// </summary>
        private static int CountOverlap(Dictionary<string, int> dict1, Dictionary<string, int> dict2)
        {
            int overlap = 0;
            foreach (var kvp in dict1)
            {
                if (dict2.TryGetValue(kvp.Key, out int count2))
                {
                    overlap += Math.Min(kvp.Value, count2);
                }
            }
            return overlap;
        }

        /// <summary>
        /// Decrement count in dictionary, removing key if count reaches zero.
        /// </summary>
        private static void DecrementCount(Dictionary<string, int> dict, string key)
        {
            if (dict.ContainsKey(key))
            {
                dict[key]--;
                if (dict[key] == 0)
                {
                    dict.Remove(key);
                }
            }
        }

        /// <summary>
        /// Increment count in dictionary.
        /// </summary>
        private static void IncrementCount(Dictionary<string, int> dict, string key)
        {
            if (dict.ContainsKey(key))
                dict[key]++;
            else
                dict[key] = 1;
        }
    }