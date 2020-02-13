using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SimpleParser
{
    public class SimpleTokenizer
    {
        public LanguageTable Table { get; private set; }
        public Regex RegexPattern { get; private set; }
        public SimpleTokenizer(LanguageTable t)
        {
            Table = t;
            var operators = t.GetKeys();
            // we go the longway to manually loop over the collection to distinguish between single-character operators and multi-character operators.
            var single_char = "";
            var multi_char = "";
            foreach (string op in operators)
            {
                if (op.Length == 1)
                {
                    single_char += op;
                }
                else if (op.Length > 1)
                {
                    multi_char += Regex.Escape(op) + "|";
                }
                // we ignore length 0 errogenic stuff.
            }
            // we need to escape all the special characters because we want to match our operators literally.
            var single_ops = Regex.Escape(single_char);
            var pattern = $@"({multi_char}[{single_ops}\(\),])";
            //KLog.Log(KLogLevel.Debug, $"The pattern string is: {pattern}");
            RegexPattern = new Regex(pattern, RegexOptions.Compiled);
        }
        public IEnumerable<string> Tokenize(string input)
        {
            return RegexPattern.Split(input);
        }
    }
}
