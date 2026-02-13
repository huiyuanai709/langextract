using System.Text.RegularExpressions;
using LangExtract.Core;

namespace LangExtract.Logic.Tokenizers;

public partial class RegexTokenizer : ITokenizer
{
    private static readonly Regex TokenPattern = TokenRegex();
    private static readonly Regex DigitsPattern = DigitsRegex();
    private static readonly Regex WordPattern = WordRegex();

    public TokenizedText Tokenize(string text)
    {
        var tokens = new List<Token>();
        int previousEnd = 0;
        int i = 0;

        foreach (Match match in TokenPattern.Matches(text))
        {
            var token = new Token(
                index: i,
                tokenType: TokenType.Word,
                charInterval: new CharInterval(match.Index, match.Index + match.Length),
                firstTokenAfterNewline: false
            );

            if (i > 0)
            {
                // Check for newline in gap
                string gap = text.Substring(previousEnd, match.Index - previousEnd);
                if (gap.Contains("\n") || gap.Contains("\r"))
                {
                    token.FirstTokenAfterNewline = true;
                }
            }

            if (DigitsPattern.IsMatch(match.Value))
            {
                token.TokenType = TokenType.Number;
            }
            else if (WordPattern.IsMatch(match.Value))
            {
                token.TokenType = TokenType.Word;
            }
            else
            {
                token.TokenType = TokenType.Punctuation;
            }

            tokens.Add(token);
            previousEnd = match.Index + match.Length;
            i++;
        }

        return new TokenizedText(text, tokens);
    }

    [GeneratedRegex(@"[^\W\d_]+|\d+|([^\w\s]|_)\1*", RegexOptions.Compiled)]
    private static partial Regex TokenRegex();
    [GeneratedRegex(@"^\d+$", RegexOptions.Compiled)]
    private static partial Regex DigitsRegex();
    [GeneratedRegex(@"^(?:[^\W\d_]+|\d+)\Z", RegexOptions.Compiled)]
    private static partial Regex WordRegex();
}