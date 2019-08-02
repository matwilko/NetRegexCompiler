using System;
using System.IO;

namespace NetRegexCompiler.Compiler.Text.RegularExpressions
{
    internal sealed class RegexCSharpCompiler : IDisposable
    {
        private CSharpWriter Writer { get; }
        private RegexCode Code { get; }
        private int[] Codes { get; }
        private string[] Strings { get; }
        private RegexPrefix? FirstCharacterPrefix { get; }
        private RegexBoyerMoore BoyerMoorePrefix { get; }
        private Anchors Anchors { get; }
        private int TrackCount { get; }
        private RegexOptions Options { get; }

        private bool IsRightToLeft => (Options & RegexOptions.RightToLeft) != 0;
        private bool IsCultureInvariant => (Options & RegexOptions.CultureInvariant) != 0;
        private bool IsCaseInsensitive => (Options & RegexOptions.IgnoreCase) != 0;

        public void Dispose()
        {
            Writer.Dispose();
        }

        private RegexCSharpCompiler(TextWriter writer, RegexCode code, RegexOptions options)
        {
            Writer = new CSharpWriter(writer);
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
            using(var codeGenerator = new RegexCSharpCompiler(writer, code, options))
                codeGenerator.Generate();
        }

        private void Generate()
        {
            Writer.Using("System.Diagnostics");
            Writer.Using("System.Globalization");
            using (Writer.Namespace("NetRegexCompiler.Compiler.Text.RegularExpressions"))
            using (Writer.Type("public sealed class CompiledRegexRunnerFactory : RegexRunnerFactory"))
            {
                Writer.Write($"protected internal override RegexRunner CreateInstance() => new CompiledRegexRunner();");
                using (Writer.Type("private sealed class CompiledRegexRunner : RegexRunner"))
                {
                    GenerateInitTrackCount();
                    GenerateFindFirstChar();
                    GenerateGo();
                }
            }
        }

        private readonly Field runtextbeg    = Field.Parse("runtextbeg");
        private readonly Field runtextend    = Field.Parse("runtextend");
        private readonly Field runtextstart  = Field.Parse("runtextstart");
        private readonly Field runtext       = Field.Parse("runtext");
        private readonly Field runtextpos    = Field.Parse("runtextpos");
        private readonly Field runtrack      = Field.Parse("runtrack");
        private readonly Field runtrackpos   = Field.Parse("runtrackpos");
        private readonly Field runstack      = Field.Parse("runstack");
        private readonly Field runstackpos   = Field.Parse("runstackpos");
        private readonly Field runcrawl      = Field.Parse("runcrawl");
        private readonly Field runcrawlpos   = Field.Parse("runcrawlpos");
        private readonly Field runtrackcount = Field.Parse("runtrackcount");
        private readonly Field runmatch      = Field.Parse("runmatch");
        private readonly Field runregex      = Field.Parse("runregex");

        private void GenerateInitTrackCount()
        {
            using (Writer.Method("protected override void InitTrackCount()"))
                Writer.Write($"{runtrackcount} = {Code.TrackCount};");
        }

        private void GenerateFindFirstChar()
        {
            using (Writer.Method("protected override bool FindFirstChar()"))
            {

            }
        }

        private void GenerateGo()
        {
            using (Writer.Method("protected override void Go()"))
            {
                
            }
        }
    }
}
