using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace NetRegexCompiler.Compiler.Text.RegularExpressions
{
    internal sealed partial class RegexCSharpCompiler
    {
        private void GenerateGo()
        {
            using (Writer.Method("protected override void Go()"))
            {
                var culture = DeclareCulture();
                foreach (var operation in Operations)
                    using (Writer.OpenScope($"{operation.Label}: // {operation.CodeName}({string.Join(", ", operation.Operands.Select(o => CSharpWriter.ConvertFormatArgument(o)))})", requireBraces: true, clearLine: true))
                    {
                        CurrentOperation = operation;
                        GenerateOpCode(culture);
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
                                    GenerateBacktrackOpCode(operation, culture);
                                    Writer.Write($"break");
                                }
                        }
                    }
                }
            }
        }

        private void GenerateOpCode(Local culture)
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

                case RegexCode.Setcount:
                    StackPush(Textpos(), Operand(0));
                    TrackPush();
                    break;

                case RegexCode.Nullcount:
                    StackPush(-1, Operand(0));
                    TrackPush();
                    break;

                case RegexCode.Branchcount:
                    // StackPush:
                    //  0: Mark
                    //  1: Count
                {
                    StackPop(2);
                    var mark = Writer.DeclareLocal($"int mark = {StackPeek()};");
                    var count = Writer.DeclareLocal($"int count = {StackPeek(1)};");
                    var matched = Writer.DeclareLocal($"int matched = {Textpos()} - {mark};");

                    using (Writer.If($"{count} >= {Operand(1)} || ({matched} == 0 && {count} >= 0)"))
                    {                                   // Max loops or empty match -> straight now
                        TrackPush2(mark, count);            // Save old mark, count
                        //advance = 2;                      // Straight
                    }
                    using (Writer.Else())
                    {                                  // Nonempty match -> count+loop now
                        TrackPush(mark);                       // remember mark
                        StackPush(Textpos(), $"{count} + 1");  // Make new mark, incr count
                        Goto(Operand(0));                      // Loop
                    }

                    break;
                }

                case RegexCode.Lazybranchcount:
                    // StackPush:
                    //  0: Mark
                    //  1: Count
                {
                    StackPop(2);
                    var mark = Writer.DeclareLocal($"int mark = {StackPeek()};");
                    var count = Writer.DeclareLocal($"int count = {StackPeek(1)}");

                    using (Writer.If($"{count} < 0"))
                    {                        // Negative count -> loop now
                        TrackPush2(mark);                        // Save old mark
                        StackPush(Textpos(), $"{count} + 1");    // Make new mark, incr count
                        Goto(Operand(0));                        // Loop
                    }
                    using (Writer.Else())
                    {                                  // Nonneg count -> straight now
                        TrackPush(mark, count, Textpos());  // Save mark, count, position
                    }

                    break;
                }

                case RegexCode.Setjump:
                    StackPush(Trackpos(), Crawlpos());
                    TrackPush();
                    break;

                case RegexCode.Forejump:
                    // StackPush:
                    //  0: Saved trackpos
                    //  1: Crawlpos
                    StackPop(2);
                    Trackto(StackPeek());
                    TrackPush(StackPeek(1));
                    break;

                case RegexCode.Bol:
                    using (Writer.If($"{Leftchars()} > 0 && {CharAt($"{Textpos()} - 1")} != '{'\n'}"))
                        Backtrack();
                    break;

                case RegexCode.Eol:
                    using (Writer.If($"{Rightchars()} > 0 && {CharAt(Textpos())} != '{'\n'}'"))
                        Backtrack();
                    break;

                case RegexCode.Boundary:
                    using (Writer.If($"!{IsBoundary(Textpos(), runtextbeg, runtextend)}"))
                        Backtrack();
                    break;

                case RegexCode.Nonboundary:
                    using (Writer.If($"{IsBoundary(Textpos(), runtextbeg, runtextend)}"))
                        Backtrack();
                    break;

                case RegexCode.ECMABoundary:
                    using (Writer.If($"!{IsECMABoundary(Textpos(), runtextbeg, runtextend)}"))
                        Backtrack();
                    break;

                case RegexCode.NonECMABoundary:
                    using (Writer.If($"{IsECMABoundary(Textpos(), runtextbeg, runtextend)}"))
                        Backtrack();
                    break;

                case RegexCode.Beginning:
                    using (Writer.If($"{Leftchars()} > 0"))
                        Backtrack();
                    break;

                case RegexCode.Start:
                    using (Writer.If($"{Textpos()} != {Textstart()}"))
                        Backtrack();
                    break;

                case RegexCode.EndZ:
                    using (Writer.If($"{Rightchars()} > 1 || {Rightchars()} == 1 && {CharAt(Textpos())} != '{'\n'}')"))
                        Backtrack();
                    break;

                case RegexCode.End:
                    using (Writer.If($"{Rightchars()} > 0"))
                        Backtrack();
                    break;

                case RegexCode.One:
                    using (Writer.If($"{Forwardchars()} < 1 || {Forwardcharnext(culture)} != '{(char)Operand(0)}'"))
                        Backtrack();
                    break;

                case RegexCode.Notone:
                    using (Writer.If($"{Forwardchars()} < 1 || {Forwardcharnext(culture)} == '{(char)Operand(0)}'"))
                        Backtrack();
                    break;

                case RegexCode.Set:
                    using (Writer.If($"{Forwardchars()} < 1 || !{CharInClass(Forwardcharnext(culture), Strings[Operand(0)])}"))
                        Backtrack();
                    break;

                case RegexCode.Multi:
                {
                    //if (!Stringmatch(_code.Strings[Operand(0)]))
                    //    break;
                    // Stringmatch inlined here as only place used
                    // return false => Backtrack()
                    
                    //    int c; <- inlined because we know str.Length
                    var pos = Writer.DeclareLocal($"int pos;");

                    var str = Strings[Operand(0)];
                    if (!IsRightToLeft)
                    {
                        using (Writer.If($"{runtextend} - {runtextpos} < {str.Length}"))
                            Backtrack();

                        Writer.Write($"{pos} = {runtextpos} + {str.Length}");
                    }
                    else
                    {
                        using (Writer.If($"{runtextpos} - {runtextbeg} < {str.Length}"))
                            Backtrack();

                        Writer.Write($"{pos} = {runtextpos}");
                    }

                    if (!IsCaseInsensitive)
                    {
                        // TODO: Measure at what point unrolling the string check loop is bad juju
                        var conditions = str.AsEnumerable()
                            .Reverse()
                            .Select(chr => (FormattableString) $"('{chr}' != {runtext}[--pos])")
                            .Cast<object>()
                            .ToArray();
                        var combinationString = string.Join(" || ", conditions.Select((_, i) => $"{{{i}}}"));
                        using (Writer.If(FormattableStringFactory.Create(combinationString, conditions)))
                            Backtrack();
                    }
                    else
                    {
                        // TODO: Measure at what point unrolling the string check loop is bad juju
                        var conditions = str.AsEnumerable()
                            .Reverse()
                            .Select(chr => (FormattableString)$"({culture}.TextInfo.ToLower('{chr}') != {runtext}[--pos])")
                            .Cast<object>()
                            .ToArray();
                        var combinationString = string.Join(" || ", conditions.Select((_, i) => $"{{{i}}}"));
                        using (Writer.If(FormattableStringFactory.Create(combinationString, conditions)))
                            Backtrack();
                    }

                    if (!IsRightToLeft)
                        Writer.Write($"{runtextpos} = {pos} + {str.Length}");
                    else
                        Writer.Write($"{runtextpos} = {pos}");

                    break;
                }

                case RegexCode.Ref:
                {
                    int capnum = Operand(0);


                    using (Writer.If(IsMatched(capnum)))
                    {
                        //if (!Refmatch(MatchIndex(capnum), MatchLength(capnum)))
                        //break;
                        // Refmatch inlined as it's only used here
                        // return false => Backtrack();

                        var index = Writer.DeclareLocal($"var index = {MatchIndex(capnum)};");
                        var len = Writer.DeclareLocal($"var len = {MatchLength(capnum)};");
                        var pos = Writer.DeclareLocal($"int pos;");

                        if (!IsRightToLeft)
                        {
                            using (Writer.If($"{runtextend} - {runtextpos} < {len}"))
                                Backtrack();

                            Writer.Write($"{pos} = {runtextpos} + {len}");
                        }
                        else
                        {
                            using (Writer.If($"{runtextpos} - {runtextbeg} < {len}"))
                                Backtrack();

                            Writer.Write($"{pos} = {runtextpos}");
                        }

                        var cmpos = Writer.DeclareLocal($"int cmpos = {index} + {len};");
                        var c = Writer.DeclareLocal($"int c = len;");

                        if (!IsCaseInsensitive)
                        {
                            using (Writer.While($"{c}-- != 0"))
                                using (Writer.If($"{runtext}[--{cmpos}] != {runtext}[--{pos}]"))
                                    Backtrack();
                        }
                        else
                        {
                            using (Writer.While($"{c}-- != 0"))
                                using (Writer.If($"{culture}.TextInfo.ToLower({runtext}[--{cmpos}]) != {culture}.TextInfo.ToLower({runtext}[--{pos}])"))
                                    Backtrack();
                        }

                        if (!IsRightToLeft)
                            Writer.Write($"{runtextpos} = {pos} + {len}");
                        else
                            Writer.Write($"{runtextpos} = {pos}");

                    }

                    if (!IsECMA)
                    using (Writer.Else())
                        Backtrack();

                    break;
                }

                case RegexCode.Onerep:
                {
                    using (Writer.If($"{Forwardchars()} < '{Operand(1)})'"))
                        Backtrack();

                    var c = Writer.DeclareLocal($"int c = '{Operand(1)}';");
                    using (Writer.While($"{c}-- > 0"))
                        using (Writer.If($"{Forwardcharnext(culture)} != '{(char) Operand(0)}'"))
                            Backtrack();

                    break;
                }

                case RegexCode.Notonerep:
                {
                    using (Writer.If($"{Forwardchars()} < {Operand(1)}"))
                        Backtrack();

                    var c = Writer.DeclareLocal($"int c = '{Operand(1)}';");
                    using (Writer.While($"{c}-- > 0"))
                    using (Writer.If($"{Forwardcharnext(culture)} == '{(char)Operand(0)}'"))
                        Backtrack();

                    break;
                }

                case RegexCode.Setrep:
                {
                    using (Writer.If($"{Forwardchars()} < {Operand(1)}"))
                        Backtrack();

                    var c = Writer.DeclareLocal($"int c = '{Operand(1)}';");
                    using (Writer.While($"{c}-- > 0"))
                        using (Writer.If($"!{CharInClass(Forwardcharnext(culture), Strings[Operand(0)])}"))
                            Backtrack();

                    break;
                }

                case RegexCode.Oneloop:
                {
                    var c = Writer.DeclareLocal($"int c = {Operand(1)};");

                    using (Writer.If($"({Operand(1)} > {Forwardchars()}"))
                        Writer.Write($"{c} = {Forwardchars()}");
                    
                    var i = Writer.DeclareLocal($"int i;");
                    using (Writer.For($"{i} = c; {i} > 0; {i}--"))
                    {
                        using (Writer.If($"{Forwardcharnext(culture)} != '{(char)Operand(0)}'"))
                        {
                            Backwardnext();
                            Writer.Write($"break");
                        }
                    }

                    using (Writer.If($"{c} > {i}"))
                        TrackPush($"{c} - {i} - 1", $"{Textpos()} - {Bump()}");

                    break;
                }

                case RegexCode.Notoneloop:
                {
                    var c = Writer.DeclareLocal($"int c = {Operand(1)};");

                    using (Writer.If($"({Operand(1)} > {Forwardchars()}"))
                        Writer.Write($"{c} = {Forwardchars()}");
                    
                    var i = Writer.DeclareLocal($"int i;");
                    using (Writer.For($"{i} = c; {i} > 0; {i}--"))
                    {
                        using (Writer.If($"{Forwardcharnext(culture)} == '{(char)Operand(0)}'"))
                        {
                            Backwardnext();
                            Writer.Write($"break");
                        }
                    }

                    using (Writer.If($"{c} > {i}"))
                        TrackPush($"{c} - {i} - 1", $"{Textpos()} - {Bump()}");

                    break;
                }

                case RegexCode.Setloop:
                {
                    var c = Writer.DeclareLocal($"int c = {Operand(1)};");

                    using (Writer.If($"({Operand(1)} > {Forwardchars()}"))
                        Writer.Write($"{c} = {Forwardchars()}");
                    
                    var i = Writer.DeclareLocal($"int i;");
                    using (Writer.For($"{i} = c; {i} > 0; {i}--"))
                    {
                        using (Writer.If(CharInClass(Forwardcharnext(culture), Strings[Operand(0)])))
                        {
                            Backwardnext();
                            Writer.Write($"break");
                        }
                    }

                    using (Writer.If($"{c} > {i}"))
                        TrackPush($"{c} - {i} - 1", $"{Textpos()} - {Bump()}");

                    break;
                }

                case RegexCode.Onelazy:
                case RegexCode.Notonelazy:
                case RegexCode.Setlazy:
                {
                    var c = Writer.DeclareLocal($"int c = {Operand(1)};");

                    using (Writer.If($"({Operand(1)} > {Forwardchars()}"))
                        Writer.Write($"{c} = {Forwardchars()}");

                    using (Writer.If($"{c} > 0"))
                        TrackPush($"{c} - 1", Textpos());

                    break;
                }
            }
        }

        private void GenerateBacktrackOpCode(BacktrackOperation operation, Local culture)
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

                case RegexCode.Setcount | RegexCode.Back:
                    StackPop(2);
                    Backtrack();
                    break;

                case RegexCode.Nullcount | RegexCode.Back:
                    StackPop(2);
                    Backtrack();
                    break;

                case RegexCode.Branchcount | RegexCode.Back:
                    // TrackPush:
                    //  0: Previous mark
                    // StackPush:
                    //  0: Mark (= current pos, discarded)
                    //  1: Count
                    TrackPop();
                    StackPop(2);
                    using (Writer.If($"{StackPeek(1)} > 0"))
                    {                         // Positive -> can go straight
                        Textto(StackPeek());                             // Zap to mark
                        TrackPush2(TrackPeek(), $"{StackPeek(1)} - 1");  // Save old mark, old count
                        GotoNextOperation();
                    }

                    StackPush(TrackPeek(), $"{StackPeek(1)} - 1");       // recall old mark, old count
                    
                    Backtrack();
                    break;

                case RegexCode.Branchcount | RegexCode.Back2:
                    // TrackPush:
                    //  0: Previous mark
                    //  1: Previous count
                    TrackPop(2);
                    StackPush(TrackPeek(), TrackPeek(1));           // Recall old mark, old count
                    Backtrack();
                    break;

                case RegexCode.Lazybranchcount | RegexCode.Back:
                    // TrackPush:
                    //  0: Mark
                    //  1: Count
                    //  2: Textpos
                {
                    TrackPop(3);
                    var mark = Writer.DeclareLocal($"int mark = {TrackPeek()};");
                    var textpos = Writer.DeclareLocal($"int textpos = {TrackPeek(2)};");

                    using (Writer.If($"{TrackPeek(1)} < {Operand(1)} && {textpos} != {mark}"))
                    { // Under limit and not empty match -> loop
                        Textto(textpos);                            // Recall position
                        StackPush(textpos, $"{TrackPeek(1)} + 1");  // Make new mark, incr count
                        TrackPush2(mark);                           // Save old mark
                        Goto(Operand(0));                           // Loop
                    }
                    using (Writer.Else())
                    {                                          // Max loops or empty match -> backtrack
                        StackPush(TrackPeek(), TrackPeek(1));       // Recall old mark, count
                        Backtrack();
                    }

                    break;
                }

                case RegexCode.Lazybranchcount | RegexCode.Back2:
                    // TrackPush:
                    //  0: Previous mark
                    // StackPush:
                    //  0: Mark (== current pos, discarded)
                    //  1: Count
                    TrackPop();
                    StackPop(2);
                    StackPush(TrackPeek(), $"{StackPeek(1)} - 1");   // Recall old mark, count
                    Backtrack();
                    break;                                           // Backtrack

                case RegexCode.Setjump | RegexCode.Back:
                    StackPop(2);
                    Backtrack();
                    break;

                case RegexCode.Forejump | RegexCode.Back:
                    // TrackPush:
                    //  0: Crawlpos
                    TrackPop();

                    using (Writer.While($"{Crawlpos()} != {TrackPeek()}"))
                        Uncapture();

                    Backtrack();
                    break;

                case RegexCode.Oneloop | RegexCode.Back:
                case RegexCode.Notoneloop | RegexCode.Back:
                case RegexCode.Setloop | RegexCode.Back:
                {
                    TrackPop(2);
                    var i = Writer.DeclareLocal($"int i = {TrackPeek()};");
                    var pos = Writer.DeclareLocal($"int pos = {TrackPeek(1)};");

                    Textto(pos);

                    using (Writer.If($"{i} > 0"))
                        TrackPush($"{i} - 1", $"{pos} - {Bump()}");

                    GotoNextOperation();
                    break;
                }

                case RegexCode.Onelazy | RegexCode.Back:
                {
                    TrackPop(2);
                    var pos = Writer.DeclareLocal($"int pos = {TrackPeek(1)};");
                    Textto(pos);

                    using (Writer.If($"{Forwardcharnext(culture)} != '{(char)Operand(0)}'"))
                        Backtrack();

                    var i = Writer.DeclareLocal($"int i = {TrackPeek()}");

                    using (Writer.If($"{i} > 0"))
                        TrackPush($"{i} - 1", $"{pos} + {Bump()}");
                    
                    GotoNextOperation();
                    break;
                }

                case RegexCode.Notonelazy | RegexCode.Back:
                {
                    TrackPop(2);
                    var pos = Writer.DeclareLocal($"int pos = {TrackPeek(1)};");
                    Textto(pos);

                    using (Writer.If($"{Forwardcharnext(culture)} == '{(char)Operand(0)}'"))
                        Backtrack();

                    var i = Writer.DeclareLocal($"int i = {TrackPeek()}");

                    using (Writer.If($"{i} > 0"))
                        TrackPush($"{i} - 1", $"{pos} + {Bump()}");

                    GotoNextOperation();
                    break;
                    }

                case RegexCode.Setlazy | RegexCode.Back:
                {
                    TrackPop(2);
                    var pos = Writer.DeclareLocal($"int pos = {TrackPeek(1)};");
                    Textto(pos);

                    using (Writer.If(CharInClass(Forwardcharnext(culture), Strings[Operand(0)])))
                        Backtrack();

                    var i = Writer.DeclareLocal($"int i = {TrackPeek()}");

                    using (Writer.If($"{i} > 0"))
                        TrackPush($"{i} - 1", $"{pos} + {Bump()}");

                    GotoNextOperation();
                    break;
                }
            }
        }
    }
}
