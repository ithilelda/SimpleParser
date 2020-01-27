using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FunctionalPatches.SimpleParser
{
    public class SimpleTokenizer
    {
        public LanguageTable Table { get; private set; }
        public SimpleTokenizer(LanguageTable t) => Table = t;
        public IEnumerable<string> Tokenize(string input)
        {
            var operators = Table.Operators.Keys.ToArray();
            var ops = string.Concat(operators);
            var pattern = $@"([{ops}\(\)])|([^{ops}\(\)]+)";
            var regex = new Regex(pattern, RegexOptions.Compiled);
            var matches = regex.Matches(input);
            foreach (Match match in matches)
            {
                yield return match.Value;
            }
        }
    }
}
