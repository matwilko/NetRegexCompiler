using System.IO;

namespace NetRegexCompiler.Compiler.Text.RegularExpressions
{
    internal sealed class RegexCSharpCompiler
    {
        private TextWriter Writer { get; }
        private RegexCode Code { get; }
        private int[] Codes { get; }
        private string[] Strings { get; }
        private RegexPrefix? FirstCharacterPrefix { get; }
        private RegexBoyerMoore BoyerMoorePrefix { get; }
        private Anchors Anchors { get; }
        private int TrackCount { get; }
        private RegexOptions Options { get; }

        private RegexCSharpCompiler(TextWriter writer, RegexCode code, RegexOptions options)
        {
            Writer = writer;
            Code = code;
            Codes = code.Codes;
            Strings = code.Strings;
            FirstCharacterPrefix = code.FCPrefix;
            BoyerMoorePrefix = code.BMPrefix;
            Anchors = new Anchors(code.Anchors);
            TrackCount = code.TrackCount;
            Options = options;
        }

        public static void GenerateCSharpCode(TextWriter writer, RegexCode code, RegexOptions options)
        {
            var codeGenerator = new RegexCSharpCompiler(writer, code, options);

        }
    }
}
