using System.Collections.Immutable;

namespace LangExtract.Logic.Resolvers;

public static class Constants
{
    public const float FuzzyAlignmentMinThreshold = 0.75f;
    public const string DefaultIndexSuffix = "_index";
        
    public static readonly ImmutableHashSet<string> AlignmentParamKeys = 
        ImmutableHashSet.Create(
            "enable_fuzzy_alignment",
            "fuzzy_alignment_threshold",
            "accept_match_lesser",
            "suppress_parse_errors"
        );
}