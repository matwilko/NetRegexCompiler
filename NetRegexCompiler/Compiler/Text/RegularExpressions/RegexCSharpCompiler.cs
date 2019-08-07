﻿using System;
using System.IO;
using System.Linq;

namespace NetRegexCompiler.Compiler.Text.RegularExpressions
{
    internal sealed partial class RegexCSharpCompiler : IDisposable
    {
        private CSharpWriter Writer { get; }
        private RegexCode Code { get; }
        private int[] Codes { get; }
        private Operation[] Operations { get; }
        private string[] Strings { get; }
        private RegexPrefix? FirstCharacterPrefix { get; }
        private RegexBoyerMoore BoyerMoorePrefix { get; }
        private Anchors Anchors { get; }
        private int TrackCount { get; }
        private RegexOptions Options { get; }

        private bool IsRightToLeft => (Options & RegexOptions.RightToLeft) != 0;
        private bool IsCultureInvariant => (Options & RegexOptions.CultureInvariant) != 0;
        private bool IsCaseInsensitive => FirstCharacterPrefix.GetValueOrDefault().CaseInsensitive;

        public void Dispose()
        {
            Writer.Dispose();
        }

        private RegexCSharpCompiler(TextWriter writer, RegexCode code, RegexOptions options)
        {
            Writer = new CSharpWriter(writer);
            Code = code;
            Codes = code.Codes;
            Operations = Operation.GenerateFromCodes(Codes);
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

        private Local DeclareCulture()
        {
            if (IsCultureInvariant)
                return Writer.DeclareLocal($"var culture = CultureInfo.InvariantCulture;");
            else
                return Writer.DeclareLocal($"var culture = CultureInfo.CurrentCulture;");
        }

        private FormattableString Forwardchars()
        {
            if (IsRightToLeft)
                return $"{runtextpos} - {runtextbeg}";
            else
                return $"{runtextend} - {runtextpos}";
        }

        private FormattableString Forwardcharnext(Local culture)
        {
            if (IsCaseInsensitive)
            {
                if (IsRightToLeft)
                    return $"{culture}.TextInfo.ToLower({runtext}[--{runtextpos}])";
                else
                    return $"{culture}.TextInfo.ToLower({runtext}[{runtextpos}++])";
            }
            else
            {
                if (IsRightToLeft)
                    return $"{runtext}[--{runtextpos}]";
                else
                    return $"{runtext}[{runtextpos}++]";
            }
        }

        private void Backwardnext()
        {
            if (IsRightToLeft)
                Writer.Write($"{runtextpos} += 1;");
            else
                Writer.Write($"{runtextpos} += -1;");
        }

        private FormattableString CharAt(object expr) => $"{runtext}[{expr}]";

        private readonly struct Operation
        {
            public int Id { get; }
            public int Index { get; }
            public int Code { get; }
            public int[] Operands { get; }

            public string Label => $"op_{Index}";
            public string CodeName => CodeNames[Code];

            private Operation(int id, int index, int code, int[] operands)
            {
                Id = id;
                Index = index;
                Code = code;
                Operands = operands;
            }

            public static Operation[] GenerateFromCodes(int[] codes)
            {
                return Get().ToArray();

                IEnumerable<Operation> Get()
                {
                    var id = 0;
                    for (var i = 0; i < codes.Length; i += RegexCode.OpcodeSize(codes[i]))
                    {
                        var code = codes[i] & ~(RegexCode.Rtl | RegexCode.Ci);
                        var operandCount = RegexCode.OpcodeSize(codes[i]) - 1;
                        if (operandCount == 0)
                        {
                            yield return new Operation(id++, i, code, new int[0]);
                            continue;
                        }

                        var operands = new int[operandCount];
                        Array.Copy(codes, i + 1, operands, 0, operands.Length);
                        yield return new Operation(id++, i, code, operands);
                    }
                }
            }

            private static readonly string[] CodeNames =
            {
                "Onerep", "Notonerep", "Setrep",
                "Oneloop", "Notoneloop", "Setloop",
                "Onelazy", "Notonelazy", "Setlazy",
                "One", "Notone", "Set",
                "Multi", "Ref",
                "Bol", "Eol", "Boundary", "Nonboundary", "Beginning", "Start", "EndZ", "End",
                "Nothing",
                "Lazybranch", "Branchmark", "Lazybranchmark",
                "Nullcount", "Setcount", "Branchcount", "Lazybranchcount",
                "Nullmark", "Setmark", "Capturemark", "Getmark",
                "Setjump", "Backjump", "Forejump", "Testref", "Goto",
                "Prune", "Stop",
                "ECMABoundary", "NonECMABoundary"
            };
        }
    }
}