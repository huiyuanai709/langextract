using LangExtract.Core;

namespace LangExtract.Logic.Chunking
{
    public class TextChunk
    {
        public TokenInterval TokenInterval { get; set; }
        public Document? Document { get; set; }

        private string? _chunkText;

        public TextChunk(TokenInterval tokenInterval, Document? document = null)
        {
            TokenInterval = tokenInterval;
            Document = document;
        }

        public string ChunkText
        {
            get
            {
                if (_chunkText == null)
                {
                    if (Document?.TokenizedText == null)
                    {
                        return "";
                    }
                    // Simple reconstruction
                    // In real implementation, this should use the source text indices
                    // Accessing TokenizedText logic here or helper method
                    _chunkText = GetTokenIntervalText(Document.TokenizedText, TokenInterval);
                }
                return _chunkText;
            }
        }

        private string GetTokenIntervalText(TokenizedText tokenizedText, TokenInterval interval)
        {
            if (interval.StartIndex >= interval.EndIndex) return "";
            if (interval.StartIndex >= tokenizedText.Tokens.Count) return "";
            
            var startToken = tokenizedText.Tokens[interval.StartIndex];
            // End index is exclusive
            int endIdx = interval.EndIndex - 1;
            if (endIdx >= tokenizedText.Tokens.Count) endIdx = tokenizedText.Tokens.Count - 1;
            
            if (endIdx < 0) return ""; // Should not happen if start < end

            var endToken = tokenizedText.Tokens[endIdx];

            int startPos = startToken.CharInterval.StartPos ?? 0;
            int endPos = endToken.CharInterval.EndPos ?? 0;

            if (startPos >= tokenizedText.Text.Length) return "";
            if (endPos > tokenizedText.Text.Length) endPos = tokenizedText.Text.Length;

            return tokenizedText.Text.Substring(startPos, endPos - startPos);
        }
    }
}
