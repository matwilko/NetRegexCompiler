using System;
using System.Collections.Generic;
using System.Text;

namespace NetRegexCompiler.Compiler.Text.RegularExpressions
{
    internal sealed partial class RegexCSharpCompiler
    {
        private void GenerateFindFirstChar()
        {
            using (Writer.Method("protected override bool FindFirstChar()"))
            {
                var culture = DeclareCulture();

                if (Anchors.Beginning || Anchors.Start || Anchors.EndZ || Anchors.End)
                {
                    //    if (!_code.RightToLeft)
                    //    {
                    //        if ((0 != (_code.Anchors & RegexFCD.Beginning) && runtextpos > runtextbeg) ||
                    //            (0 != (_code.Anchors & RegexFCD.Start) && runtextpos > runtextstart))
                    //        {
                    //            runtextpos = runtextend;
                    //            return false;
                    //        }

                    //        if (0 != (_code.Anchors & RegexFCD.EndZ) && runtextpos < runtextend - 1)
                    //        {
                    //            runtextpos = runtextend - 1;
                    //        }
                    //        else if (0 != (_code.Anchors & RegexFCD.End) && runtextpos < runtextend)
                    //        {
                    //            runtextpos = runtextend;
                    //        }
                    //    }
                    //    else
                    //    {
                    //        if ((0 != (_code.Anchors & RegexFCD.End) && runtextpos < runtextend) ||
                    //            (0 != (_code.Anchors & RegexFCD.EndZ) && (runtextpos < runtextend - 1 ||
                    //                                                      (runtextpos == runtextend - 1 && CharAt(runtextpos) != '\n'))) ||
                    //            (0 != (_code.Anchors & RegexFCD.Start) && runtextpos < runtextstart))
                    //        {
                    //            runtextpos = runtextbeg;
                    //            return false;
                    //        }

                    //        if (0 != (_code.Anchors & RegexFCD.Beginning) && runtextpos > runtextbeg)
                    //        {
                    //            runtextpos = runtextbeg;
                    //        }
                    //    }

                    //    if (_code.BMPrefix != null)
                    //    {
                    //        return _code.BMPrefix.IsMatch(runtext, runtextpos, runtextbeg, runtextend);
                    //    }

                    //    return true; // found a valid start or end anchor
                }
                else if (BoyerMoorePrefix != null)
                { 
                    //    runtextpos = _code.BMPrefix.Scan(runtext, runtextpos, runtextbeg, runtextend);

                    //    if (runtextpos == -1)
                    //    {
                    //        runtextpos = (_code.RightToLeft ? runtextbeg : runtextend);
                    //        return false;
                    //    }

                    //    return true;
                }
                else if (FirstCharacterPrefix == null)
                {
                    Writer.Write($"return true;");
                }

                var set = FirstCharacterPrefix.GetValueOrDefault().Prefix;

                if (RegexCharClass.IsSingleton(set))
                {
                    var ch = RegexCharClass.SingletonChar(set);
                    var i = Writer.ReferenceLocal("i");
                    using (Writer.For($"int {i} = {Forwardchars()}; {i} > 0; {i}--"))
                    {
                        using (Writer.If($"'{ch}' == {Forwardcharnext()}"))
                        {
                            Backwardnext();
                            Writer.Write($"return true;");
                        }
                    }
                }
                else
                {
                    var i = Writer.ReferenceLocal("i");
                    using (Writer.For($"int {i} = {Forwardchars()}; i > 0; i--"))
                    {
                        using (Writer.If($@"RegexCharClass.CharInClass({Forwardcharnext()}, ""{set}"")"))
                        {
                            Backwardnext();
                            Writer.Write($"return true;");
                        }
                    }
                }

                Writer.Write($"return false;");
            }
        }
    }
}
