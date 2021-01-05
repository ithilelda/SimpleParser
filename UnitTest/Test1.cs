using Xunit;
using Xunit.Abstractions;
using SimpleParser;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UnitTest
{
    public class Test1
    {
        private readonly ITestOutputHelper output;

        public Test1(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Theory]
        [InlineData("2 + 5 * 3")]
        [InlineData("\"fdd3we43dsx\" + a5")]
        [InlineData("$0:private+ 6")]
        [InlineData("6 *(55+ !6)*3")]
        [InlineData("6 *!(55+ -6) * 3")]
        public void Tokenizer(string value)
        {
            var parser = new SimpleParser<LanguageTable>();
            var tokens = parser.Tokenize(value);
            output.WriteLine(string.Join(", ", tokens));
            Assert.True(true);
        }

        [Theory]
        [InlineData("2 + 5 * 3")]
        [InlineData("\"fdd3we43dsx\" + a5")]
        [InlineData("$0:private+ 6")]
        [InlineData("6 *(55+ !6)*3")]
        [InlineData("6 *!(55+ -6) * 3")]
        public void ShuntingYard(string value)
        {
            var parser = new SimpleParser<LanguageTable>();
            var tokens = parser.Tokenize(value);
            var rpn = parser.ShuntingYard(tokens);
            output.WriteLine(string.Join(", ", rpn));
            Assert.True(true);
        }

        [Fact]
        public void RegexMatchPrecedence()
        {
            var regex = $@"&&|&|-";
            var source = "a&&&b c& && 5d";
            var matches = Regex.Matches(source, regex);
            output.WriteLine(string.Join(", ", matches.Cast<Match>()));
            Assert.Equal(4, matches.Count);
        }

    }
}
