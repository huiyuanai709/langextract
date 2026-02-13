using LangExtract.Core;

namespace LangExtract.Logic.Chunking;

public class ChunkIterator
{
    private readonly TokenizedText _tokenizedText;
    private readonly int _maxCharBuffer;
    private readonly Document _document;

    public ChunkIterator(Document document, int maxCharBuffer, ITokenizer tokenizer)
    {
        _document = document;
        _maxCharBuffer = maxCharBuffer;
        if (_document.TokenizedText == null)
        {
            _document.TokenizedText = tokenizer.Tokenize(_document.Text ?? "");
        }
        _tokenizedText = _document.TokenizedText;
    }

    public IEnumerable<TextChunk> Iterate()
    {
        if (_tokenizedText.Tokens.Count == 0) yield break;

        int currentTokenIdx = 0;
        while (currentTokenIdx < _tokenizedText.Tokens.Count)
        {
            // Simplified Logic: 
            // Just take as many tokens as possible to fit in maxCharBuffer
            // Ignoring sentence boundaries for simplicity in this port, 
            // but highlighting where sophisticated logic would go.

            int endTokenIdx = currentTokenIdx + 1;
            while (endTokenIdx <= _tokenizedText.Tokens.Count)
            {
                int length = GetLength(_tokenizedText, currentTokenIdx, endTokenIdx);
                if (length > _maxCharBuffer)
                {
                    // Backtrack one token if we exceeded buffer
                    if (endTokenIdx > currentTokenIdx + 1)
                    {
                        endTokenIdx--;
                    }
                    break;
                }
                    
                if (endTokenIdx == _tokenizedText.Tokens.Count) 
                {
                    break; 
                }
                    
                endTokenIdx++;
            }
                
            // Create chunk
            var interval = new TokenInterval(currentTokenIdx, endTokenIdx);
            yield return new TextChunk(interval, _document);

            currentTokenIdx = endTokenIdx;
        }
    }

    private int GetLength(TokenizedText tokenizedText, int start, int end)
    {
        if (start >= end) return 0;
        var startToken = tokenizedText.Tokens[start];
        var endToken = tokenizedText.Tokens[end - 1];
        return (endToken.CharInterval.EndPos ?? 0) - (startToken.CharInterval.StartPos ?? 0);
    }
}