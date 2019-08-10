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

        private void GotoNextOperation()
        {
            Writer.Write($"goto {Operations[CurrentOperation.Id + 1].Label}");
        }
        
        private FormattableString IsMatched(int cap) => $"{runmatch}.IsMatched({cap})";

        private void StackPop()
        {
            Writer.Write($"runstackpos++");
        }

        private FormattableString StackPeek() => $"{runstack}[{runstackpos} - 1]";

        private void StackPush(object I1)
        {
            Writer.Write($"{runstack}[--{runstackpos}] = {I1}");
        }
        
        private void TrackPop(int framesize = 1)
        {
            if (framesize == 1)
                Writer.Write($"{runtrackpos}++");
            else
                Writer.Write($"{runtrackpos} += {framesize}");
        }

        private FormattableString TrackPeek(int i = 0) => $"{runtrack}[{runtrackpos} - {i + 1}]";

        private void TrackPush(params object[] IX)
        {
            var backtrackOp = BacktrackOperations.Add(CurrentOperation, isBack2: false);
            foreach (var I in IX)
                Writer.Write($"{runtrack}[--{runtrackpos}] = {I}");
            Writer.Write($"{runtrack}[--{runtrackpos}] = {backtrackOp.Id}");
        }

        private void TrackPush2(params object[] IX)
        {
            var backtrackOp = BacktrackOperations.Add(CurrentOperation, isBack2: true);
            foreach (var I in IX)
                Writer.Write($"{runtrack}[--{runtrackpos}] = {I}");
            Writer.Write($"{runtrack}[--{runtrackpos}] = {backtrackOp.Id}");
        }

        private FormattableString Popcrawl() => $"{runcrawl}[{runcrawlpos}++]";

        private FormattableString Textpos() => $"{runtextpos}";

        private void Textto(object pos)
        {
            Writer.Write($"{runtextpos} = {pos}");
        }

        private void TransferCapture(int capnum, int uncapnum, FormattableString start, FormattableString end)
        {
            Writer.Write($"TransferCapture({capnum}, {uncapnum}, {start}, {end})");
        }

        private void Capture(int capnum, FormattableString start, FormattableString end)
        {
            Writer.Write($"Capture({capnum}, {start}, {end})");
        }

        private void TransferCapture(FormattableString capnum, FormattableString uncapnum, FormattableString start, FormattableString end)
        {
            Writer.Write($"TransferCapture({capnum}, {uncapnum}, {start}, {end})");
        }

        private void Capture(FormattableString capnum, FormattableString start, FormattableString end)
        {
            Writer.Write($"Capture({capnum}, {start}, {end})");
        }

        private void Uncapture()
        {
            Writer.Write($"{runmatch}.RemoveMatch({Popcrawl()})");
        }
    }
}
