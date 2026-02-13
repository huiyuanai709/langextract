namespace LangExtract.Logic.Resolvers;

/// <summary>
/// Represents constraint settings for parsing.
/// </summary>
public class Constraint
{
    public bool Strict { get; set; }

    public Constraint(bool strict = false)
    {
        Strict = strict;
    }
}