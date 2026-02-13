using LangExtract.Core;
using LangExtract.Logic.Tokenizers;
using LangExtract.Core.Exceptions;

namespace LangExtract.Logic;

public class Resolver
{
    private readonly FormatHandler _formatHandler;

    public Resolver(FormatHandler? formatHandler = null)
    {
        _formatHandler = formatHandler ?? new FormatHandler();
    }

    public List<Extraction> Resolve(string llmOutput, bool suppressParseErrors = false)
    {
        try 
        {
            var extractionData = _formatHandler.ParseOutputToList(llmOutput);
            return ExtractOrderedExtractions(extractionData);
        }
        catch (Exception ex) when (ex is FormatError || ex is ResolverParsingError)
        {
            if (suppressParseErrors)
            {
                Console.WriteLine($"Parse Error: {ex.Message}");
                return [];
            }
            throw new ResolverParsingError(ex.Message, ex);
        }
        catch (Exception ex)
        {
            if (suppressParseErrors)
            {
                Console.WriteLine($"Unexpected Error: {ex.Message}");
                return [];
            }
            throw;
        }
    }

    private List<Extraction> ExtractOrderedExtractions(List<Dictionary<string, object>> extractionData)
    {
        var extractions = new List<Extraction>();
        int extractionIndex = 0;
        string indexSuffix = "_index"; // Default hardcoded for now, pass in ctor if needed
        string attributeSuffix = _formatHandler.AttributeSuffix;

        for (int groupIndex = 0; groupIndex < extractionData.Count; groupIndex++)
        {
            var group = extractionData[groupIndex];
            foreach (var kvp in group)
            {
                string key = kvp.Key;
                object value = kvp.Value;

                // Skip attribute keys or index keys
                if (key.EndsWith(indexSuffix)) continue;
                if (key.EndsWith(attributeSuffix)) continue;

                // Basic type check
                if (!(value is string || value is int || value is float || value is double || value is long))
                {
                    // value usually converts to specific types in FormatHandler
                    continue; 
                }
                    
                string text = value.ToString() ?? "";
                    
                // Logic to get attributes if present
                Dictionary<string, object>? attributes = null;
                string attrKey = key + attributeSuffix;
                if (group.TryGetValue(attrKey, out var attrObj) && attrObj is Dictionary<string, object> attrDict)
                {
                    attributes = attrDict;
                }

                // Logic to get index if present
                // string idxKey = key + indexSuffix; ...

                extractions.Add(new Extraction(
                    extractionClass: key,
                    extractionText: text,
                    extractionIndex: extractionIndex++, 
                    groupIndex: groupIndex,
                    attributes: attributes
                ));
            }
        }

        return extractions;
    }

    public IEnumerable<Extraction> Align(
        IEnumerable<Extraction> extractions, 
        string sourceText, 
        int tokenOffset, 
        int charOffset,
        ITokenizer? tokenizer = null)
    {
        var aligner = new WordAligner();
        return aligner.AlignExtractions(extractions, sourceText, tokenOffset, charOffset, tokenizer);
    }
}

public class WordAligner
{
    public IEnumerable<Extraction> AlignExtractions(
        IEnumerable<Extraction> extractions,
        string sourceText,
        int tokenOffset,
        int charOffset,
        ITokenizer? tokenizer = null)
    {
        tokenizer ??= new RegexTokenizer(); 
        var tokenizedSource = tokenizer.Tokenize(sourceText);
        var sourceTokens = tokenizedSource.Tokens;
        var sourceTokenStrings = sourceTokens.Select(t => ExtractTokenString(sourceText, t)).ToList();
        var sourceTokenStringsLower = sourceTokenStrings.Select(s => s.ToLowerInvariant()).ToList();

        var alignedExtractions = new List<Extraction>();

        foreach (var extraction in extractions)
        {
            var extractionTokText = tokenizer.Tokenize(extraction.ExtractionText);
            var extTokenStrings = extractionTokText.Tokens.Select(t => ExtractTokenString(extraction.ExtractionText, t).ToLowerInvariant()).ToList();

            if (extTokenStrings.Count == 0) continue;

            var startIdx = FindSubSequence(sourceTokenStringsLower, extTokenStrings);

            if (startIdx != -1)
            {
                var endIdx = startIdx + extTokenStrings.Count; 
                    
                var startToken = sourceTokens[startIdx];
                var endToken = sourceTokens[endIdx - 1]; 

                extraction.TokenInterval = new TokenInterval(startIdx + tokenOffset, endIdx + tokenOffset);
                extraction.CharInterval = new CharInterval(
                    (startToken.CharInterval.StartPos ?? 0) + charOffset,
                    (endToken.CharInterval.EndPos ?? 0) + charOffset
                );
                extraction.AlignmentStatus = AlignmentStatus.MatchExact;
            }
                
            alignedExtractions.Add(extraction);
        }

        return alignedExtractions;
    }

    private string ExtractTokenString(string text, Token token)
    {
        var start = token.CharInterval.StartPos ?? 0;
        var end = token.CharInterval.EndPos ?? 0;
        if (start >= text.Length) return "";
        if (end > text.Length) end = text.Length;
        return text.Substring(start, end - start);
    }

    private int FindSubSequence(List<string> source, List<string> target)
    {
        // Simple naive search
        for (var i = 0; i <= source.Count - target.Count; i++)
        {
            var match = !target.Where((t, j) => source[i + j] != t).Any();
            if (match) return i;
        }
        return -1;
    }
}