namespace LangExtract.Core
{
    public interface ITokenizer
    {
        TokenizedText Tokenize(string text);
    }
}
