using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using LangExtract.Core;

namespace LangExtract.Logic.Tokenizers
{
    public class RegexTokenizer : ITokenizer
    {
        private static readonly Regex _tokenPattern = new Regex(@"[^\W\d_]+|\d+|([^\w\s]|_)\1*", RegexOptions.Compiled);
        private static readonly Regex _digitsPattern = new Regex(@"^\d+$", RegexOptions.Compiled);
        private static readonly Regex _wordPattern = new Regex(@"^(?:[^\W\d_]+|\d+)\Z", RegexOptions.Compiled);

        public TokenizedText Tokenize(string text)
        {
            var tokens = new List<Token>();
            int previousEnd = 0;
            int i = 0;

            foreach (Match match in _tokenPattern.Matches(text))
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

                if (_digitsPattern.IsMatch(match.Value))
                {
                    token.TokenType = TokenType.Number;
                }
                else if (_wordPattern.IsMatch(match.Value))
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
    }
}
