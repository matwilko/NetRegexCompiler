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

        private void TrackPush(FormattableString I1)
        {
            var backtrackOp = BacktrackOperations.Add(CurrentOperation, isBack2: false);
            Writer.Write($"{track}.Push({I1});");
            Writer.Write($"{track}.Push({backtrackOp.Id});");
        }

        private FormattableString Textpos() => $"{runtextpos}";
    }
}
