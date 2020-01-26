using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FunctionalPatches.SimpleParser
{
    public class SimpleTokenizer
    {
        public IEnumerable<string> Tokenize(string input)
        {
            // operations are right associative.
            var regex = new Regex(@"([!&|\(\)])|([^!&|\(\)]+)", RegexOptions.Compiled);
            var matches = regex.Matches(input);
            foreach (Match match in matches)
            {
                yield return match.Value;
            }
        }
    }
}
