namespace LangExtract.Logic.Resolvers;

/// <summary>
/// Represents a matching block between two sequences.
/// </summary>
public class MatchingBlock
{
    public int IndexA { get; }
    public int IndexB { get; }
    public int Size { get; }

    public MatchingBlock(int indexA, int indexB, int size)
    {
        IndexA = indexA;
        IndexB = indexB;
        Size = size;
    }

    public override string ToString()
    {
        return $"Match(a={IndexA}, b={IndexB}, size={Size})";
    }
}