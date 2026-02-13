namespace LangExtract.Core;

public class TokenInterval
{
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }

    public TokenInterval(int startIndex = 0, int endIndex = 0)
    {
        StartIndex = startIndex;
        EndIndex = endIndex;
    }
}