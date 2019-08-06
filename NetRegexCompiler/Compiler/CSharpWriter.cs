﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NetRegexCompiler.Compiler
{
    internal sealed class CSharpWriter : IDisposable
    {
        private TextWriter Writer { get; }

        public CSharpWriter(TextWriter writer)
        {
            Writer = writer;
            Scopes = new Stack<List<string>>();
            Scopes.Push(new List<string>());
        }

        private static string Indent = "    ";
        
        private Stack<List<string>> Scopes { get; }
        private List<string> CurrentScope => Scopes.Peek();

        public CloseScope OpenScope(string declaration, bool requireBraces = false, bool clearLine = false)
        {
            CurrentScope.Add(declaration);
            Scopes.Push(new List<string>());
            return new CloseScope(this, requireBraces, clearLine);
        }

        public CSharpWriter Using(string ns)
        {
            CurrentScope.Add($"using {ns};");
            return this;
        }

        public CloseScope Namespace(string ns) => OpenScope($"namespace {ns}", requireBraces: true, clearLine: true);
        public CloseScope Type(string typeDecl) => OpenScope(typeDecl, requireBraces: true, clearLine: true);
        public CloseScope Method(string methodDecl) => OpenScope(methodDecl, requireBraces: true, clearLine: true);

        public CloseScope If(FormattableString expr) => OpenScope($"if ({FormatExpression(expr)})", clearLine: true);
        public CloseScope ElseIf(FormattableString expr) => OpenScope($"else if ({FormatExpression(expr)})", clearLine: true);
        public CloseScope Else() => OpenScope($"else", clearLine: true);

        public CloseScope While(FormattableString expr) => OpenScope($"while ({FormatExpression(expr)})", clearLine: true);
        public CloseScope For(FormattableString expr) => OpenScope($"for ({FormatExpression(expr)})", clearLine: true);

        public Field DeclareField(FormattableString declaration)
        {
            var field = Field.Parse(declaration);
            Write(declaration);
            return field;
        }

        public Method ReferenceMethod(string methodName) => new Method(methodName);

        public Local DeclareLocal(FormattableString declaration)
        {
            var local = Local.Parse(declaration);
            Write(declaration);
            return local;
        }

        public Local ReferenceLocal(string localName) => Local.Parse(localName);
        
        public CSharpWriter Write(FormattableString code)
        {
            if (code.Format.Last() == ';')
                CurrentScope.Add(FormatExpression(code));
            else
                CurrentScope.Add(FormatExpression(code) + ";");

            return this;
        }

        private static string FormatExpression(FormattableString expr)
        {
            var arguments = expr.GetArguments();
            for (var i = 0; i < arguments.Length; i++)
                arguments[i] = ConvertFormatArgument(arguments[i]);

            return string.Format(CultureInfo.InvariantCulture, expr.Format, arguments);
        }

        private static HashSet<char> VerbatimChars { get; } = new HashSet<char>(new[] { ' ', '-', '[', ']', '*', '(', ')', '=', ',', ':' });

        private static Dictionary<char, string> EscapedChars { get; } = new Dictionary<char, string>
        {
            { '\n', "\\n" },
            { '\t', "\\t" }
        };

        public static string ConvertFormatArgument(object o)
        {
            switch (o)
            {
                case string s:
                {
                    if (s.All(char.IsLetterOrDigit))
                        return s;

                    var sb = new StringBuilder(s.Length * 6); // Space for every char to become \uXXXX
                    foreach (var c in s)
                        sb.Append(FormatChar(c));

                    return sb.ToString();
                }

                case bool b: return b ? "true" : "false";
                case char c: return FormatChar(c);
                case byte i: return i.ToString("D", CultureInfo.InvariantCulture);
                case short i: return i.ToString("D", CultureInfo.InvariantCulture);
                case int i: return i.ToString("D", CultureInfo.InvariantCulture);
                case long i: return i.ToString("D", CultureInfo.InvariantCulture);
                case Field f: return f.Name;
                case Method m: return m.Name;
                case Local l: return l.Name;

                case FormattableString fs: return FormatExpression(fs);

                default: throw new FormatException("Unknown type for formatting");
            }

            string FormatChar(char c)
            {
                if (char.IsLetterOrDigit(c) ||  VerbatimChars.Contains(c))
                    return c.ToString();
                else if (EscapedChars.TryGetValue(c, out var str))
                    return str;
                else
                    return $"\\x{((int) c):X4}";
            }
        }
        
        public void Dispose()
        {
            Debug.Assert(Scopes.Count == 1);
            var rootScope = Scopes.Pop();
            foreach (var line in rootScope)
                Writer.WriteLine(line);
        }

        public readonly struct CloseScope : IDisposable
        {
            private CSharpWriter Writer { get; }
            private bool RequireBraces { get; }
            private bool ClearLine { get; }

            public CloseScope(CSharpWriter writer, bool requireBraces, bool clearLine)
            {
                Writer = writer;
                RequireBraces = requireBraces;
                ClearLine = clearLine;
            }

            public void Dispose()
            {
                var closingScope = Writer.Scopes.Pop();
                var parentScope = Writer.Scopes.Peek();
                if (RequireBraces || closingScope.Count > 1)
                {
                    parentScope.Add("{");
                    foreach (var line in closingScope)
                        parentScope.Add(Indent + line);

                    if (string.IsNullOrWhiteSpace(parentScope.Last()))
                        parentScope.RemoveAt(parentScope.Count - 1);

                    parentScope.Add("}");
                }
                else
                {
                    foreach (var line in closingScope)
                        parentScope.Add(Indent + line);
                }

                if (ClearLine)
                    parentScope.Add(string.Empty);
            }
        }
    }

    internal sealed class Field
    {
        private static Regex DefinitionCheck { get; } = new Regex("^(private|protected|internal)( static)?( readonly)? ([a-zA-Z_][a-zA-Z0-9_]*(\\[\\])*) ([a-zA-Z_][a-zA-Z0-9_]*)( = .*)?;$", RegexOptions.Compiled);
        private static Regex NameCheck { get; } = new Regex("^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);
        public string Name { get; }

        private Field(string name)
        {
            Name = name;
        }

        public static Field Parse(FormattableString declaration)
        {
            var match = DefinitionCheck.Match(declaration.Format);
            if (!match.Success)
                throw new InvalidOperationException("Bad field declaration");

            return new Field(match.Groups[6].Value);
        }

        public static Field Parse(string name)
        {
            if (!NameCheck.IsMatch(name))
                throw new FormatException("Bad field name");

            return new Field(name);
        }
    }

    internal sealed class Method
    {
        private static Regex Validation { get; } = new Regex("^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);
        public string Name { get; }

        public Method(string name)
        {
            if (!Validation.IsMatch(name))
                throw new FormatException("Invalid method name");

            Name = name;
        }

        public override string ToString() => Name;
    }

    internal sealed class Local
    {
        private static Regex DefinitionCheck { get; } = new Regex("^((var|[a-zA-Z_][a-zA-Z0-9_]*(\\[\\])*) ([a-zA-Z_][a-zA-Z0-9_]*) = .*?;|([a-zA-Z_][a-zA-Z0-9_]*(\\[\\])*) ([a-zA-Z_][a-zA-Z0-9_]*);)$", RegexOptions.Compiled);
        private static Regex Validation { get; } = new Regex("^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);

        public string Name { get; }

        private Local(string name)
        {
            Name = name;
        }

        public static Local Parse(FormattableString str)
        {
            var match = DefinitionCheck.Match(str.Format);
            if (!match.Success)
                throw new FormatException("Bad local declaration");

            return match.Groups[4].Success
                ? new Local(match.Groups[4].Value)
                : new Local(match.Groups[7].Value);
        }

        public static Local Parse(string localName)
        {
            if (!Validation.IsMatch(localName))
                throw new FormatException("Bad local name");
            
            return new Local(localName);
        }
    }
}
