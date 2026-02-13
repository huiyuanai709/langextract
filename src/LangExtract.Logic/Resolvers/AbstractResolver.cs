using LangExtract.Core;
using LangExtract.Core.Schema;

namespace LangExtract.Logic.Resolvers;

/// <summary>
    /// Abstract base class for resolvers that transform LLM text outputs into structured data.
    /// </summary>
    public abstract class AbstractResolver
    {
        private bool _fenceOutput;
        private Constraint _constraint;
        private FormatMode _formatType;

        /// <summary>
        /// Initializes the AbstractResolver.
        /// 
        /// Delimiters are used for parsing text blocks, and are used primarily for
        /// models that do not have constrained-decoding support.
        /// </summary>
        /// <param name="fenceOutput">Whether to expect/generate fenced output (```json or ```yaml).
        /// When true, the model is prompted to generate fenced output and the resolver expects it.
        /// When false, raw JSON/YAML is expected.</param>
        /// <param name="constraint">Applies constraint when decoding the output. Defaults to no constraint.</param>
        /// <param name="formatType">The format type for the output (JSON or YAML).</param>
        protected AbstractResolver(
            bool fenceOutput = true,
            Constraint constraint = null,
            FormatMode formatType = FormatMode.Json)
        {
            _fenceOutput = fenceOutput;
            _constraint = constraint ?? new Constraint();
            _formatType = formatType;
        }

        public bool FenceOutput
        {
            get => _fenceOutput;
            set => _fenceOutput = value;
        }

        public FormatMode FormatType
        {
            get => _formatType;
            set => _formatType = value;
        }

        public Constraint Constraint
        {
            get => _constraint;
            protected set => _constraint = value;
        }

        /// <summary>
        /// Run resolve function on input text.
        /// </summary>
        /// <param name="inputText">The input text to be processed.</param>
        /// <param name="kwargs">Additional arguments for subclass implementations.</param>
        /// <returns>Annotated text in the form of Extractions.</returns>
        public abstract IReadOnlyList<Extraction> Resolve(string inputText, Dictionary<string, object> kwargs = null);

        /// <summary>
        /// Aligns extractions with source text, setting token/char intervals and alignment status.
        /// 
        /// Uses exact matching first (difflib equivalent), then fuzzy alignment fallback if enabled.
        /// 
        /// Alignment Status Results:
        /// - MATCH_EXACT: Perfect token-level match
        /// - MATCH_LESSER: Partial exact match (extraction longer than matched text)
        /// - MATCH_FUZZY: Best overlap window meets threshold (≥ fuzzyAlignmentThreshold)
        /// - null: No alignment found
        /// </summary>
        /// <param name="extractions">Annotated extractions to align with the source text.</param>
        /// <param name="sourceText">The text in which to align the extractions.</param>
        /// <param name="tokenOffset">The token_offset corresponding to the starting token index of the chunk.</param>
        /// <param name="charOffset">The char_offset corresponding to the starting character index of the chunk.</param>
        /// <param name="tokenizer"></param>
        /// <param name="enableFuzzyAlignment">Whether to use fuzzy alignment when exact matching fails.</param>
        /// <param name="fuzzyAlignmentThreshold">Minimum token overlap ratio for fuzzy alignment (0-1).</param>
        /// <param name="acceptMatchLesser">Whether to accept partial exact matches (MATCH_LESSER status).</param>
        /// <returns>Aligned extractions with updated token intervals and alignment status.</returns>
        public abstract IEnumerable<Extraction> Align(
            IReadOnlyList<Extraction> extractions,
            string sourceText,
            int tokenOffset,
            int? charOffset,
            ITokenizer tokenizer,
            bool enableFuzzyAlignment = true,
            float fuzzyAlignmentThreshold = Constants.FuzzyAlignmentMinThreshold,
            bool acceptMatchLesser = true);
    }
