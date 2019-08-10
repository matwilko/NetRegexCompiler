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

                case RegexCode.Lazybranchmark:
                {
                    // We hit this the first time through a lazy loop and after each
                    // successful match of the inner expression.  It simply continues
                    // on and doesn't loop.
                    StackPop();

                    var oldMarkPos = Writer.DeclareLocal($"int oldMarkPos = {StackPeek()};");

                    using (Writer.If($"{Textpos()} != {oldMarkPos}"))
                    {              // Nonempty match -> try to loop again by going to 'back' state
                        using (Writer.If($"{oldMarkPos} != -1"))
                            TrackPush(oldMarkPos, Textpos());   // Save old mark, textpos
                        using (Writer.Else())
                            TrackPush(Textpos(), Textpos());
                    }
                    using (Writer.Else())
                    {
                        // The inner expression found an empty match, so we'll go directly to 'back2' if we
                        // backtrack.  In this case, we need to push something on the stack, since back2 pops.
                        // However, in the case of ()+? or similar, this empty match may be legitimate, so push the text
                        // position associated with that empty match.
                        StackPush(oldMarkPos);

                        TrackPush2(StackPeek());                // Save old mark
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

                case RegexCode.Lazybranchmark | RegexCode.Back:
                {
                    // After the first time, Lazybranchmark | RegexCode.Back occurs
                    // with each iteration of the loop, and therefore with every attempted
                    // match of the inner expression.  We'll try to match the inner expression,
                    // then go back to Lazybranchmark if successful.  If the inner expression
                    // fails, we go to Lazybranchmark | RegexCode.Back2
                    
                    TrackPop(2);
                    var pos = Writer.DeclareLocal($"int pos = {TrackPeek(1)};");
                    TrackPush2(TrackPeek());                // Save old mark
                    StackPush(pos);                         // Make new mark
                    Textto(pos);                            // Recall position
                    Goto(Operand(0));                       // Loop
                    break;
                }

                case RegexCode.Lazybranchmark | RegexCode.Back2:
                    // The lazy loop has failed.  We'll do a true backtrack and
                    // start over before the lazy loop.
                    StackPop();
                    TrackPop();
                    StackPush(TrackPeek());                      // Recall old mark
                    Backtrack();
                    break;
            }
        }
    }
}
