namespace LangExtract.Core;

/// <summary>
/// Class for representing a character interval.
/// </summary>
public class CharInterval
{
    /// <summary>
    /// The starting position of the interval (inclusive).
    /// </summary>
    public int? StartPos { get; set; }

    /// <summary>
    /// The ending position of the interval (exclusive).
    /// </summary>
    public int? EndPos { get; set; }

    public CharInterval(int? startPos = null, int? endPos = null)
    {
        StartPos = startPos;
        EndPos = endPos;
    }
}