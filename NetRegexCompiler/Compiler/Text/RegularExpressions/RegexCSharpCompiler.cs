using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace NetRegexCompiler.Compiler.Text.RegularExpressions
{
    public sealed partial class RegexCSharpCompiler : IDisposable
    {
        private string Namespace { get; }
        private string ClassName { get; }
        private CSharpWriter Writer { get; }
        private string Pattern { get; }
        private RegexTree Tree { get; }
        private RegexCode Code { get; }
        private int[] Codes { get; }
        private Operation[] Operations { get; }
        private BacktrackOperationList BacktrackOperations { get; } = new BacktrackOperationList();
        private Operation CurrentOperation { get; set; }
        private string[] Strings { get; }
        private RegexPrefix? FirstCharacterPrefix { get; }
        private RegexBoyerMoore BoyerMoorePrefix { get; }
        private Anchors Anchors { get; }
        private int TrackCount { get; }
        private RegexOptions Options { get; }

        private bool IsRightToLeft => CurrentOperation.IsRightToLeft ?? (Options & RegexOptions.RightToLeft) != 0;
        private bool IsCultureInvariant => (Options & RegexOptions.CultureInvariant) != 0;
        private bool IsCaseInsensitive => CurrentOperation.IsCaseInsensitive ?? FirstCharacterPrefix.GetValueOrDefault().CaseInsensitive;
        private bool IsECMA => (Options & RegexOptions.ECMAScript) != 0;

        public void Dispose()
        {
            Writer.Dispose();
        }

        private RegexCSharpCompiler(TextWriter writer, string pattern, RegexOptions options, CultureInfo culture, string @namespace, string className)
        {
            Pattern = pattern;
            Tree = RegexParser.Parse(pattern, options, culture);
            Code = RegexWriter.Write(Tree);
            Writer = new CSharpWriter(writer);
            Codes = Code.Codes;
            Operations = Operation.GenerateFromCodes(Codes);
            Strings = Code.Strings;
            FirstCharacterPrefix = Code.FCPrefix;
            BoyerMoorePrefix = Code.BMPrefix;
            Anchors = new Anchors(Code.Anchors);
            TrackCount = Code.TrackCount;
            Options = options;
            Namespace = @namespace;
            ClassName = className;
        }

        public static void GenerateCSharpCode(TextWriter writer, string pattern, RegexOptions options, string @namespace, string className)
        {
            var culture = (options & RegexOptions.CultureInvariant) != 0
                ? CultureInfo.InvariantCulture
                : CultureInfo.CurrentCulture;

            using (var codeGenerator = new RegexCSharpCompiler(writer, pattern, options, culture, @namespace, className))
                codeGenerator.Generate();
        }

        private void Generate()
        {
            using (Writer.Namespace(Namespace))
            using (Writer.DisableWarning("164"))
            using (Writer.DisableWarning("162"))
            {
                Writer.Using("System");
                Writer.Using("System.Collections");
                Writer.Using("System.Collections.Generic");
                Writer.Using("System.Diagnostics");
                Writer.Using("System.Globalization");
                Writer.Using("System.Reflection");
                Writer.Using("System.Text");
                Writer.Using("System.Text.RegularExpressions");
                
                using (Writer.Type($"public sealed class {ClassName} : Regex"))
                {
                    Writer.Write($"public static Regex Instance {{ get; }} = new {ClassName}();");

                    GenerateCompiledFields();

                    using (Writer.Constructor($"public {ClassName}()"))
                    {
                        Writer.Write($@"this.pattern = ""{Pattern}""");

                        if (Tree.CapNames != null)
                            Writer.Write($"this.capnames = compiledCapNames;");
                        else
                            Writer.Write($"this.capnames = null;");

                        if (Tree.CapsList != null)
                            Writer.Write($"this.capslist = compiledCapsList;");
                        else
                            Writer.Write($"this.capslist = null;");

                        if (Code.Caps != null)
                            Writer.Write($"this.caps = compiledCaps;");
                        else
                            Writer.Write($"this.caps = null;");

                        Writer.Write($"this.capsize = {Code.CapSize};");
                        Writer.Write($"this.internalMatchTimeout = TimeSpan.FromMilliseconds(1000)");
                        Writer.Write($"this.factory = new CompiledRegexRunnerFactory();");
                        Writer.Write($"InitializeReferences();");
                    }

                    using (Writer.Type("private sealed class CompiledRegexRunnerFactory : RegexRunnerFactory"))
                    {
                        Writer.Write($"protected override RegexRunner CreateInstance() => new CompiledRegexRunner();");
                    }

                    using (Writer.Type("private sealed class CompiledRegexRunner : RegexRunner"))
                    {
                        Writer.Write($"public char CurrentChar => runtext[runtextpos];");
                        Writer.Write($"public string ForwardStr => runtext.Substring(runtextpos);");
                        GenerateInitTrackCount();
                        GenerateFindFirstChar();
                        GenerateGo();
                        //GenerateDebug();
                    }
                }
            }
        }

        private void GenerateCompiledFields()
        {
            if (Tree.CapNames != null)
            {
                var capNames = FormattableStringFactory.Create(string.Join(", ", Tree.CapNames.Select((kvp, i) => $@"{{{{ ""{{{i * 2}}}"", {{{i * 2 + 1}}} }}}}")), Tree.CapNames.SelectMany(kvp => new object[] { kvp.Key, kvp.Value }).ToArray());
                Writer.DeclareField($"private static readonly Hashtable compiledCapNames = new Hashtable() {{ {capNames} }};");
            }

            if (Tree.CapsList != null)
            {
                var capsList = FormattableStringFactory.Create(string.Join(", ", Tree.CapsList.Select((c, i) => $@"""{i}""")), Tree.CapsList);
                Writer.DeclareField($"private static readonly string[] compiledCapsList = new string[] {{ {capsList} }};");
            }

            if (Code.Caps != null)
            {
                var caps = FormattableStringFactory.Create(string.Join(", ", Code.Caps.Select((kvp, i) => $@"{{{{ ""{{{i * 2}}}"", {{{i * 2 + 1}}} }}}}")), Code.Caps.SelectMany(kvp => new object[] { kvp.Key, kvp.Value }).ToArray());
                Writer.DeclareField($"private static readonly Hashtable compiledCaps = new Hashtable() {{ {caps} }};");
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

        private readonly Method EnsureStorage = Method.Parse("EnsureStorage");
        
        private void GenerateInitTrackCount()
        {
            using (Writer.Method("protected override void InitTrackCount()"))
                Writer.Write($"{runtrackcount} = {Code.TrackCount};");
        }

        private void GenerateDebug()
        {
            using (Writer.Method($"private void DumpState()"))
            {
                Writer.Write($@"Debug.WriteLine(""Text:  "" + TextposDescription())");
                Writer.Write($@"Debug.WriteLine(""Track: "" + StackDescription({runtrack}, {runtrackpos}))");
                Writer.Write($@"Debug.WriteLine(""Stack: "" + StackDescription({runstack}, {runstackpos}))");
            }

            using (Writer.Method($"private static string StackDescription(int[] a, int index)"))
            {
                var sb = Writer.DeclareLocal($"var sb = new StringBuilder();");

                Writer.Write($"{sb}.Append(a.Length - index)");
                Writer.Write($"{sb}.Append('/')");
                Writer.Write($"{sb}.Append(a.Length)");

                using (Writer.If($"{sb}.Length < 8"))
                    Writer.Write($"    {sb}.Append(' ', 8 - {sb}.Length)");
                
                Writer.Write($"{sb}.Append('(')");

                var i = Writer.DeclareLocal($"int i;");
                using (Writer.For($"{i} = index; {i} < a.Length; {i}++"))
                {
                    using (Writer.If($"{i} > index"))
                        Writer.Write($"        {sb}.Append(' ')");
                    Writer.Write($"    {sb}.Append(a[i])");
                }

                Writer.Write($"{sb}.Append(')')");
                
                Writer.Write($"return {sb}.ToString()");
            }

            using (Writer.Method($"private string TextposDescription()"))
            {
                var sb = Writer.DeclareLocal($"var sb = new StringBuilder();");
                var remaining = Writer.DeclareLocal($"int remaining;");

                Writer.Write($"{sb}.Append({runtextpos})");

                using (Writer.If($"{sb}.Length < 8"))
                    Writer.Write($"{sb}.Append(' ', 8 - {sb}.Length)");

                using (Writer.If($"{runtextpos} > {runtextbeg}"))
                    Writer.Write($"{sb}.Append(CharDescription({runtext}[{runtextpos} - 1]))");
                using (Writer.Else())
                    Writer.Write($"{sb}.Append('^')");

                Writer.Write($"{sb}.Append('>')");

                Writer.Write($"{remaining} = {runtextend} - {runtextpos}");

                var i = Writer.DeclareLocal($"int i;");
                using (Writer.For($"{i} = {runtextpos}; {i} < {runtextend}; {i}++"))
                {
                    Writer.Write($"{sb}.Append(CharDescription({runtext}[{i}]))");
                }
                using (Writer.If($"{sb}.Length >= 64"))
                {
                    Writer.Write($"{sb}.Length = 61");
                    Writer.Write($@"{sb}.Append(""..."")");
                }
                using (Writer.Else())
                {
                    Writer.Write($"{sb}.Append('$')");
                }

                Writer.Write($"return {sb}.ToString()");
            }

            var s_definedCategories = Writer.DeclareField($@"private static readonly Dictionary<string,string> s_definedCategories = new Dictionary<string, string>() {{ {{ ""Cc"", ""\u000F"" }}, {{ ""Cf"", ""\u0010"" }}, {{ ""Cn"", ""\u001E"" }}, {{ ""Co"", ""\u0012"" }}, {{ ""Cs"", ""\u0011"" }}, {{ ""C"", ""\u0000\u000F\u0010\u001E\u0012\u0011\u0000"" }}, {{ ""Ll"", ""\u0002"" }}, {{ ""Lm"", ""\u0004"" }}, {{ ""Lo"", ""\u0005"" }}, {{ ""Lt"", ""\u0003"" }}, {{ ""Lu"", ""\u0001"" }}, {{ ""L"", ""\u0000\u0002\u0004\u0005\u0003\u0001\u0000"" }}, {{ ""__InternalRegexIgnoreCase__"", ""\u0000\u0002\u0003\u0001\u0000"" }}, {{ ""Mc"", ""\u0007"" }}, {{ ""Me"", ""\u0008"" }}, {{ ""Mn"", ""\u0006"" }}, {{ ""M"", ""\u0000\u0007\u0008\u0006\u0000"" }}, {{ ""Nd"", ""\u0009"" }}, {{ ""Nl"", ""\u000A"" }}, {{ ""No"", ""\u000B"" }}, {{ ""N"", ""\u0000\u0009\u000A\u000B\u0000"" }}, {{ ""Pc"", ""\u0013"" }}, {{ ""Pd"", ""\u0014"" }}, {{ ""Pe"", ""\u0016"" }}, {{ ""Po"", ""\u0019"" }}, {{ ""Ps"", ""\u0015"" }}, {{ ""Pf"", ""\u0018"" }}, {{ ""Pi"", ""\u0017"" }}, {{ ""P"", ""\u0000\u0013\u0014\u0016\u0019\u0015\u0018\u0017\u0000"" }}, {{ ""Sc"", ""\u001B"" }}, {{ ""Sk"", ""\u001C"" }}, {{ ""Sm"", ""\u001A"" }}, {{ ""So"", ""\u001D"" }}, {{ ""S"", ""\u0000\u001B\u001C\u001A\u001D\u0000"" }}, {{ ""Zl"", ""\u000D"" }}, {{ ""Zp"", ""\u000E"" }}, {{ ""Zs"", ""\u000C"" }}, {{ ""Z"", ""\u0000\u000D\u000E\u000C\u0000"" }} }};");

            using (Writer.Method($"private static string SetDescription(string set)"))
            {
                const int FLAGS = 0;
                const int SETLENGTH = 1;
                const int CATEGORYLENGTH = 2;
                const int SETSTART = 3;
                const char LastChar = '\uFFFF';
                const char GroupChar = (char)0;
                const string s_word = "\u0000\u0002\u0004\u0005\u0003\u0001\u0006\u0009\u0013\u0000";
                const string s_notWord = "\u0000\uFFFE\uFFFC\uFFFB\uFFFD\uFFFF\uFFFA\uFFF7\uFFED\u0000";

                var mySetLength = Writer.DeclareLocal($"int mySetLength = set[{SETLENGTH}];");
                var myCategoryLength = Writer.DeclareLocal($"int myCategoryLength = set[{CATEGORYLENGTH}];");
                var myEndPosition = Writer.DeclareLocal($"int myEndPosition = {SETSTART} + {mySetLength} + {myCategoryLength};");

                var desc = Writer.DeclareLocal($"StringBuilder desc = new StringBuilder();");

                Writer.Write($"desc.Append('[')");

                var index = Writer.DeclareLocal($"int index = {SETSTART};");
                var ch1 = Writer.DeclareLocal($"char ch1;");
                var ch2 = Writer.DeclareLocal($"char ch2;");

                using (Writer.If($"set[{FLAGS}] == 1"))
                    Writer.Write($"{desc}.Append('^');");

                using (Writer.While($"{index} < {SETSTART} + set[{SETLENGTH}]"))
                {
                    Writer.Write($"{ch1} = set[{index}]");
                    using (Writer.If($"index + 1 < set.Length"))
                        Writer.Write($"{ch2} = (char)(set[{index} + 1] - 1)");
                    using (Writer.Else())
                        Writer.Write($"{ch2} = '{LastChar}'");

                    Writer.Write($"{desc}.Append(CharDescription({ch1}))");

                    using (Writer.If($"{ch2} != {ch1}"))
                    {
                        using (Writer.If($"{ch1} + 1 != {ch2}"))
                            Writer.Write($"{desc}.Append('-')");
                        Writer.Write($"{desc}.Append(CharDescription({ch2}))");
                    }
                    
                    Writer.Write($"{index} += 2");
                }

                using (Writer.While($"{index} < {SETSTART} + set[{SETLENGTH}] + set[{CATEGORYLENGTH}]"))
                {
                    Writer.Write($"{ch1} = set[{index}]");
                    using (Writer.If($"{ch1} == 0"))
                    {
                        var found = Writer.DeclareLocal($"bool found = false;");

                        var lastindex = Writer.DeclareLocal($"int lastindex = set.IndexOf('{GroupChar}', {index} + 1);");
                        var group = Writer.DeclareLocal($"string group = set.Substring({index}, {lastindex} - {index} + 1);");

                        using (Writer.OpenScope($"foreach (KeyValuePair<string, string> kvp in s_definedCategories)"))
                        {
                            using (Writer.If($"{group}.Equals(kvp.Value)"))
                            {
                                using (Writer.If($"(short)set[{index} + 1] > 0"))
                                    Writer.Write($@"{desc}.Append(""{"\\p{"}"")");
                                using (Writer.Else())
                                    Writer.Write($@"{desc}.Append(""{"\\P{"}"")");

                                Writer.Write($"{desc}.Append(kvp.Key)");
                                Writer.Write($"{desc}.Append('}}')");

                                Writer.Write($"{found} = true");
                                Writer.Write($"break");
                            }
                        }

                        using (Writer.If($"!{found}"))
                        {
                            using (Writer.If($@"{group}.Equals(""{s_word}"")"))
                                Writer.Write($@"{desc}.Append(""{"\\w"}"")");
                            using (Writer.ElseIf($@"{group}.Equals(""{s_notWord}"")"))
                                Writer.Write($@"{desc}.Append(""{"\\W"}"")");
                            using (Writer.Else())
                                Writer.Write($@"Debug.Assert(false, $""Couldn't find a group to match '{group}'"")");
                        }

                        Writer.Write($"{index} = {lastindex}");
                    }
                    using (Writer.Else())
                    {
                        Writer.Write($"{desc}.Append(CategoryDescription({ch1}))");
                    }

                    Writer.Write($"{index}++");
                }

                using (Writer.If($"set.Length > {myEndPosition}"))
                {
                    Writer.Write($"{desc}.Append('-')");
                    Writer.Write($"{desc}.Append(SetDescription(set.Substring({myEndPosition})))");
                }

                Writer.Write($"{desc}.Append(']')");

                Writer.Write($"return {desc}.ToString()");
            }

            var Categories = Writer.DeclareField($@"private static readonly string[] Categories = new string[] {{""Lu"", ""Ll"", ""Lt"", ""Lm"", ""Lo"", ""__InternalRegexIgnoreCase__"", ""Mn"", ""Mc"", ""Me"", ""Nd"", ""Nl"", ""No"", ""Zs"", ""Zl"", ""Zp"", ""Cc"", ""Cf"", ""Cs"", ""Co"", ""Pc"", ""Pd"", ""Ps"", ""Pe"", ""Pi"", ""Pf"", ""Po"", ""Sm"", ""Sc"", ""Sk"", ""So"", ""Cn"" }};");

            using (Writer.Method($"private static string CategoryDescription(char ch)"))
            {
                const short SpaceConst = 100;
                const short NotSpaceConst = -100;
                using (Writer.If($"ch == {SpaceConst}"))
                    Writer.Write($@"return ""{"\\s"}""");
                using (Writer.ElseIf($"(short)ch == {NotSpaceConst}"))
                    Writer.Write($@"return ""{"\\S"}""");
                using (Writer.ElseIf($"(short)ch < 0"))
                {
                    Writer.Write($@"return ""{"\\P{"}"" + {Categories}[(-((short)ch) - 1)] + ""}}""");
                }
                using (Writer.Else())
                {
                    Writer.Write($@"return ""{"\\p{"}"" + {Categories}[(ch - 1)] + ""}}""");
                }
            }

            var Hex = Writer.DeclareField($"private static readonly char[] Hex = new char[] {{ '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' }};");

            using (Writer.Method($"private static string CharDescription(char ch)"))
            {
                using (Writer.If($"ch == '{'\\'}'"))
                    Writer.Write($@"return ""{"\\\\"}""");

                using (Writer.If($"ch >= ' ' && ch <= '~'"))
                {
                    Writer.Write($"return ch.ToString()");
                }

                var sb = Writer.DeclareLocal($"var sb = new StringBuilder();");
                var shift = Writer.DeclareLocal($"int shift;");

                using (Writer.If($"ch < 256"))
                {
                    Writer.Write($@"{sb}.Append(""{"\\x"}"")");
                    Writer.Write($"{shift} = 8");
                }
                using (Writer.Else())
                {
                    Writer.Write($@"{sb}.Append(""{"\\u"}"")");
                    Writer.Write($"{shift} = 16");
                }

                using (Writer.While($"{shift} > 0"))
                {
                    Writer.Write($"{shift} -= 4");
                    Writer.Write($"{sb}.Append({Hex}[(ch >> {shift}) & 0xF])");
                }

                Writer.Write($"return {sb}.ToString()");
            }
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

        private FormattableString CharAt(FormattableString expr) => CharAt((object) expr);
        private FormattableString CharAt(object expr) => $"{runtext}[{expr}]";

        private readonly struct Operation
        {
            public int Id { get; }
            public int Index { get; }
            public int Code { get; }

            public bool? IsCaseInsensitive { get; }
            public bool? IsRightToLeft { get; }

            public int[] Operands { get; }

            public string Label => $"op_{Index}";
            public string CodeName => CodeNames[Code];

            public Operation(int id, int index, int code, int[] operands, bool? isCaseInsensitive, bool? isRightToLeft)
            {
                Id = id;
                Index = index;
                Code = code;
                IsCaseInsensitive = isCaseInsensitive;
                IsRightToLeft = isRightToLeft;
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
                        var isCaseInsensitive = 0 != (codes[i] & RegexCode.Ci);
                        var isRightToLeft = 0 != (codes[i] & RegexCode.Rtl);
                        var operandCount = RegexCode.OpcodeSize(codes[i]) - 1;
                        if (operandCount == 0)
                        {
                            yield return new Operation(id++, i, code, new int[0], isCaseInsensitive, isRightToLeft);
                            continue;
                        }

                        var operands = new int[operandCount];
                        Array.Copy(codes, i + 1, operands, 0, operands.Length);
                        yield return new Operation(id++, i, code, operands, isCaseInsensitive, isRightToLeft);
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

        private sealed class BacktrackOperationList : IEnumerable<BacktrackOperation>
        {
            private Dictionary<(int operationId, bool isBack2), (int id, Operation operation)> Operations { get; } = new Dictionary<(int operationId, bool isBack2), (int, Operation)>();
            private int Id { get; set; }

            // Backtracking code that only depends on runtime state, no dependency on source or destination
            private static int[] UndifferentiatedBacktrackOperations { get; } =
            {
                RegexCode.Nullmark | RegexCode.Back,
                RegexCode.Setmark | RegexCode.Back,
                RegexCode.Getmark | RegexCode.Back,
                RegexCode.Branchmark | RegexCode.Back2,
                RegexCode.Lazybranchmark | RegexCode.Back2,
                RegexCode.Setcount | RegexCode.Back,
                RegexCode.Nullcount | RegexCode.Back,
                RegexCode.Branchcount | RegexCode.Back2,
                RegexCode.Lazybranchcount | RegexCode.Back2,
                RegexCode.Setjump | RegexCode.Back,
                RegexCode.Forejump | RegexCode.Back
            };

            public BacktrackOperation Add(Operation operation, bool isBack2)
            {
                 if (UndifferentiatedBacktrackOperations.Contains(BacktrackOperation.CombineCode(operation.Code, isBack2)))
                     return HandleUndifferentiatedOperation(operation.Code, isBack2);

                if (!Operations.TryGetValue((operation.Id, isBack2), out var op))
                    op = Operations[(operation.Id, isBack2)] = (Id++, operation);

                    return new BacktrackOperation(op.id, op.operation, isBack2);
            }

            private BacktrackOperation HandleUndifferentiatedOperation(int code, bool isBack2)
            {
                var opUniqueId = -(1 + Array.IndexOf(UndifferentiatedBacktrackOperations, BacktrackOperation.CombineCode(code, isBack2)));

                if (!Operations.TryGetValue((opUniqueId, isBack2), out var op))
                    op = Operations[(opUniqueId, isBack2)] = (Id++, new Operation(-1, -1, code, new int [0], null, null));

                return new BacktrackOperation(op.id, op.operation, isBack2);
            }

            public IEnumerator<BacktrackOperation> GetEnumerator() => Operations.Select(kvp => new BacktrackOperation(kvp.Value.id, kvp.Value.operation, kvp.Key.isBack2)).OrderBy(bo => bo.Id).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private readonly struct BacktrackOperation
        {
            public int Id { get; }
            public Operation Operation { get; }
            public bool IsBack2 { get; }

            public int CombinedCode => CombineCode(Operation.Code, IsBack2);

            public string CodeName => Operation.CodeName;

            public BacktrackOperation(int id, Operation operation, bool isBack2)
            {
                Id = id;
                Operation = operation;
                IsBack2 = isBack2;
            }

            public static int CombineCode(int code, bool isBack2) => !isBack2
                ? code | RegexCode.Back
                : code | RegexCode.Back2;
        }
    }
}