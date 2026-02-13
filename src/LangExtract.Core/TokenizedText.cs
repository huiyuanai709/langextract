namespace LangExtract.Core;

public class TokenizedText
{
    public string Text { get; set; }
    public List<Token> Tokens { get; set; }

    public TokenizedText(string text, List<Token>? tokens = null)
    {
        Text = text;
        Tokens = tokens ?? new List<Token>();
    }
}