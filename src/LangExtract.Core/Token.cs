namespace LangExtract.Core;

public class Token
{
    public int Index { get; set; }
    public TokenType TokenType { get; set; }
    public CharInterval CharInterval { get; set; }
    public bool FirstTokenAfterNewline { get; set; }

    public Token(int index, TokenType tokenType, CharInterval charInterval, bool firstTokenAfterNewline = false)
    {
        Index = index;
        TokenType = tokenType;
        CharInterval = charInterval;
        FirstTokenAfterNewline = firstTokenAfterNewline;
    }
}