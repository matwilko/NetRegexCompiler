using System;
using System.Linq;

namespace NetRegexCompiler.Compiler.Text.RegularExpressions
{
    internal sealed partial class RegexCSharpCompiler
    {
        private int Operand(int i) => CurrentOperation.Operands[i];

        private void Backtrack() => Writer.Write($"goto backtrack;");

        private void Goto(int operationPos)
        {
            var operation = Operations.Single(op => op.Index == operationPos);
            Writer.Write($"goto {operation.Label};");
        }
        
        private FormattableString IsMatched(int cap) => $"{runmatch}.IsMatched({cap})";
    }
}
