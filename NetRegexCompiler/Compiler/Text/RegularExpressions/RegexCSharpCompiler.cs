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