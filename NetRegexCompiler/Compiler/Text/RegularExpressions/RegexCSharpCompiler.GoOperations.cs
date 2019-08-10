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
            // when branching backward, ensure storage
            if (operation.Index < CurrentOperation.Index)
                Writer.Write($"{EnsureStorage}()");
            Writer.Write($"goto {operation.Label};");
        }
        
        private FormattableString IsMatched(int cap) => $"{runmatch}.IsMatched({cap})";

        private void StackPop()
        {
            Writer.Write($"runstackpos++");
        }

        private FormattableString StackPeek() => $"{runstack}[{runstackpos} - 1]";

        private void StackPush(FormattableString I1)
        {
            Writer.Write($"{runstack}[--{runstackpos}] = {I1}");
        }

        private void StackPush(int I1)
        {
            StackPush($"{I1}");
        }

        private void TrackPop()
        {
            Writer.Write($"{runtrackpos}++");
        }

        private FormattableString TrackPeek() => $"{runtrack}[{runtrackpos} - 1]";

        private void TrackPush()
        {
            var backtrackOp = BacktrackOperations.Add(CurrentOperation, isBack2: false);
            Writer.Write($"{runtrack}[--{runtrackpos}] = {backtrackOp.Id}");
        }

        private void TrackPush(FormattableString I1)
        {
            var backtrackOp = BacktrackOperations.Add(CurrentOperation, isBack2: false);
            Writer.Write($"{runtrack}[--{runtrackpos}] = {I1}");
            Writer.Write($"{runtrack}[--{runtrackpos}] = {backtrackOp.Id}");
        }

        private FormattableString Textpos() => $"{runtextpos}";

        private void Textto(FormattableString pos)
        {
            Writer.Write($"{runtextpos} = {pos}");
        }
    }
}
