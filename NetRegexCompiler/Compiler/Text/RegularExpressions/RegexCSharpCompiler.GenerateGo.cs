using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NetRegexCompiler.Compiler.Text.RegularExpressions
{
    internal sealed partial class RegexCSharpCompiler
    {
        private void GenerateGo()
        {
            using (Writer.Method("protected override void Go()"))
            {
                foreach (var operation in Operations)
                using (Writer.OpenScope($"{operation.Label}: // {operation.CodeName}({string.Join(", ", operation.Operands.Select(o => CSharpWriter.ConvertFormatArgument(o)))})", requireBraces: true, clearLine: true))
                {
                    CurrentOperation = operation;
                    GenerateOpCode();
                    Writer.Write($"break");
                }
            }

            if (BacktrackOperations.Any())
            {
                using (Writer.OpenScope("backtrack:"))
                {
                    using (Writer.Switch($"{track}.Pop())"))
                    {
                        foreach (var operation in BacktrackOperations)
                            using (Writer.OpenScope($"case {operation.Id}: // {operation.Operation.Label}, {operation.CodeName} ({(!operation.IsBack2 ? "Back" : "Back2")})"))
                            {
                                CurrentOperation = operation.Operation;
                                GenerateBacktrackOpCode(operation);
                                Writer.Write($"break");
                            }
                    }
                }
            }
        }

        private void GenerateOpCode()
        {
            switch (CurrentOperation.Code)
            {
                case RegexCode.Stop:
                    Writer.Write($"return;");
                    break;

                case RegexCode.Nothing:
                    Backtrack();
                    break;

                case RegexCode.Goto:
                    Goto(Operand(0));
                    break;

                case RegexCode.Testref:
                    using (Writer.If($"!{IsMatched(Operand(0))}"))
                        Backtrack(); // break;
                    // advance = 1;
                    // continue;
                    break;

                case RegexCode.Lazybranch: 
                    TrackPush(Textpos());
                    // advance = 1;
                    // continue;
                    break;

                case RegexCode.Setmark:
                    StackPush(Textpos());
                    TrackPush();
                    // advance = 0;
                    //continue;
                    break;

                case RegexCode.Nullmark:
                    StackPush(-1);
                    TrackPush();
                    // advance = 0;
                    // continue;
                    break;
            }
        }

        private void GenerateBacktrackOpCode(BacktrackOperation operation)
        {
            switch (operation.CombinedCode)
            {
                case RegexCode.Lazybranch | RegexCode.Back:
                    //TrackPop();
                    Textto(TrackPop()); // Textto(TrackPeek());
                    Goto(Operand(0));
                    break;

            }
        }
    }
}
