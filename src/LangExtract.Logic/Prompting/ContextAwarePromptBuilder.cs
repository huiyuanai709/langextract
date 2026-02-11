using System.Collections.Generic;

namespace LangExtract.Logic.Prompting
{
    public class ContextAwarePromptBuilder : PromptBuilder
    {
        private const string ContextPrefix = "[Previous text]: ...";
        private readonly int? _contextWindowChars;
        private readonly Dictionary<string, string> _prevChunkByDocId = new Dictionary<string, string>();

        public ContextAwarePromptBuilder(QAPromptGenerator generator, int? contextWindowChars = null)
            : base(generator)
        {
            _contextWindowChars = contextWindowChars;
        }

        public override string BuildPrompt(string chunkText, string documentId, string? additionalContext = null)
        {
            string? effectiveContext = BuildEffectiveContext(documentId, additionalContext);
            string prompt = _generator.Render(chunkText, effectiveContext);
            UpdateState(documentId, chunkText);
            return prompt;
        }

        private string? BuildEffectiveContext(string documentId, string? additionalContext)
        {
            var parts = new List<string>();

            if (_contextWindowChars.HasValue && _prevChunkByDocId.TryGetValue(documentId, out string? prevText))
            {
                int len = prevText.Length;
                int windowSize = _contextWindowChars.Value;
                string window = len > windowSize ? prevText.Substring(len - windowSize) : prevText;
                parts.Add($"{ContextPrefix}{window}");
            }

            if (!string.IsNullOrEmpty(additionalContext))
            {
                parts.Add(additionalContext);
            }

            return parts.Count > 0 ? string.Join("\n\n", parts) : null;
        }

        private void UpdateState(string documentId, string chunkText)
        {
            if (_contextWindowChars.HasValue)
            {
                _prevChunkByDocId[documentId] = chunkText;
            }
        }
    }
}
