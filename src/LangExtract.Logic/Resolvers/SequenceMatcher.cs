namespace LangExtract.Logic.Resolvers;

/// <summary>
/// Sequence matcher for finding matching blocks between two sequences.
/// Similar to Python's difflib.SequenceMatcher.
/// </summary>
public class SequenceMatcher
{
    private IReadOnlyList<string> _sequence1;
    private IReadOnlyList<string> _sequence2;

    public void SetSequences(IReadOnlyList<string> seq1, IReadOnlyList<string> seq2)
    {
        _sequence1 = seq1;
        _sequence2 = seq2;
    }

    public void SetSequence1(IReadOnlyList<string> seq)
    {
        _sequence1 = seq;
    }

    public void SetSequence2(IReadOnlyList<string> seq)
    {
        _sequence2 = seq;
    }

    /// <summary>
    /// Get matching blocks between the two sequences.
    /// </summary>
    public IReadOnlyList<MatchingBlock> GetMatchingBlocks()
    {
        var matches = new List<MatchingBlock>();
        var queue = new Queue<(int i1, int i2, int j1, int j2)>();
        queue.Enqueue((0, _sequence1.Count, 0, _sequence2.Count));

        while (queue.Count > 0)
        {
            var (i1, i2, j1, j2) = queue.Dequeue();
            var (i, j, k) = FindLongestMatch(i1, i2, j1, j2);

            if (k > 0)
            {
                matches.Add(new MatchingBlock(i, j, k));

                if (i1 < i && j1 < j)
                {
                    queue.Enqueue((i1, i, j1, j));
                }

                if (i + k < i2 && j + k < j2)
                {
                    queue.Enqueue((i + k, i2, j + k, j2));
                }
            }
        }

        matches.Sort((a, b) => a.IndexA.CompareTo(b.IndexA));

        // Add sentinel
        matches.Add(new MatchingBlock(_sequence1.Count, _sequence2.Count, 0));

        return matches;
    }

    /// <summary>
    /// Find the longest matching block in the sequences.
    /// </summary>
    private (int i, int j, int k) FindLongestMatch(int i1, int i2, int j1, int j2)
    {
        int bestI = i1;
        int bestJ = j1;
        int bestSize = 0;

        var j2len = new Dictionary<int, int>();

        for (int i = i1; i < i2; i++)
        {
            var newJ2len = new Dictionary<int, int>();

            for (int j = j1; j < j2; j++)
            {
                if (_sequence1[i] == _sequence2[j])
                {
                    int k = j2len.ContainsKey(j - 1) ? j2len[j - 1] + 1 : 1;
                    newJ2len[j] = k;

                    if (k > bestSize)
                    {
                        bestI = i - k + 1;
                        bestJ = j - k + 1;
                        bestSize = k;
                    }
                }
            }

            j2len = newJ2len;
        }

        return (bestI, bestJ, bestSize);
    }
}