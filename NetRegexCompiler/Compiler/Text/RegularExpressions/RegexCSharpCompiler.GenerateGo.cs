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
                    Writer.Write($"{EnsureStorage}()");
                    using (Writer.Switch($"{runtrack}[{runtrackpos}++])"))
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

                case RegexCode.Getmark:
                    StackPop();
                    TrackPush(StackPeek());
                    Textto(StackPeek());
                    // advance = 0;
                    // continue;
                    break;

                case RegexCode.Capturemark:
                    if (Operand(1) != -1)
                        using (Writer.If($"!{IsMatched(Operand(1))}"))
                            Backtrack();
                    StackPop();
                    // TODO: Can we inline TransferCapture/Capture efficiently?
                    if (Operand(1) != -1)
                        TransferCapture(Operand(0), Operand(1), StackPeek(), Textpos());
                    else
                        Capture(Operand(0), StackPeek(), Textpos());
                    TrackPush(StackPeek());

                    // advance = 2;

                    //continue;
                    break;

                case RegexCode.Branchmark:
                {
                    StackPop();

                    var matched = Writer.DeclareLocal($"var matched = {Textpos()} - {StackPeek()});");

                    using (Writer.If($"{matched} != 0"))
                    {                     // Nonempty match -> loop now
                        TrackPush(StackPeek(), Textpos());  // Save old mark, textpos
                        StackPush(Textpos());               // Make new mark
                        Goto(Operand(0));                   // Loop
                    }
                    using (Writer.Else())
                    {                                  // Empty match -> straight now
                        TrackPush2(StackPeek());            // Save old mark
                        //advance = 1;                      // Straight
                    }

                    break;
                }
            }
        }

        private void GenerateBacktrackOpCode(BacktrackOperation operation)
        {
            switch (operation.CombinedCode)
            {
                case RegexCode.Lazybranch | RegexCode.Back:
                    TrackPop();
                    Textto(TrackPeek());
                    Goto(Operand(0));
                    break;

                case RegexCode.Setmark | RegexCode.Back:
                case RegexCode.Nullmark | RegexCode.Back:
                    StackPop();
                    Backtrack();
                    break;

                case RegexCode.Getmark | RegexCode.Back:
                    TrackPop();
                    StackPush(TrackPeek());
                    Backtrack();
                    break;

                case RegexCode.Capturemark | RegexCode.Back:
                    TrackPop();
                    StackPush(TrackPeek());
                    Uncapture();
                    if (Operand(0) != -1 && Operand(1) != -1)
                        Uncapture();
                    
                    Backtrack();
                    break;

                case RegexCode.Branchmark | RegexCode.Back:
                    TrackPop(2);
                    StackPop();
                    Textto(TrackPeek(1));                       // Recall position
                    TrackPush2(TrackPeek());                    // Save old mark
                    GotoNextOperation(); // advance = 1;        // Straight
                    break;

                case RegexCode.Branchmark | RegexCode.Back2:
                    TrackPop();
                    StackPush(TrackPeek());                     // Recall old mark
                    Backtrack();                                // Backtrack
                    break;
            }
        }
    }
}
