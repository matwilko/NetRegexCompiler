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
                    GenerateAnchorChecks();
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

        private void GenerateAnchorChecks()
        {
            if (!IsRightToLeft)
            {
                if (Anchors.Beginning && Anchors.Start && Anchors.EndZ && Anchors.End)
                {
                    //        if (runtextpos > runtextbeg || runtextpos > runtextstart)
                    //        {
                    //            runtextpos = runtextend;
                    //            return false;
                    //        }

                    //        if (runtextpos < runtextend - 1)
                    //            runtextpos = runtextend - 1;
                    //        else if (runtextpos < runtextend)
                    //            runtextpos = runtextend;
                }
                else if (Anchors.Beginning && Anchors.Start && Anchors.EndZ)
                {
                    //        if (runtextpos > runtextbeg || runtextpos > runtextstart)
                    //        {
                    //            runtextpos = runtextend;
                    //            return false;
                    //        }

                    //        if (runtextpos < runtextend - 1)
                    //            runtextpos = runtextend - 1;
                }
                else if (Anchors.Beginning && Anchors.Start && Anchors.End)
                {
                    //        if (runtextpos > runtextbeg || runtextpos > runtextstart)
                    //        {
                    //            runtextpos = runtextend;
                    //            return false;
                    //        }

                    //        if (runtextpos < runtextend)
                    //            runtextpos = runtextend;
                }
                else if (Anchors.Beginning && Anchors.Start)
                {
                    //        if (runtextpos > runtextbeg || runtextpos > runtextstart)
                    //        {
                    //            runtextpos = runtextend;
                    //            return false;
                    //        }
                }
                else if (Anchors.Beginning && Anchors.EndZ && Anchors.End)
                {
                    //        if (runtextpos > runtextbeg)
                    //        {
                    //            runtextpos = runtextend;
                    //            return false;
                    //        }

                    //        if (runtextpos < runtextend - 1)
                    //            runtextpos = runtextend - 1;
                    //        else if (runtextpos < runtextend)
                    //            runtextpos = runtextend;
                }
                else if (Anchors.Beginning && Anchors.EndZ)
                {
                    //        if (runtextpos > runtextbeg)
                    //        {
                    //            runtextpos = runtextend;
                    //            return false;
                    //        }

                    //        if (runtextpos < runtextend - 1)
                    //            runtextpos = runtextend - 1;
                }
                else if (Anchors.Beginning && Anchors.End)
                {
                    //        if (runtextpos > runtextbeg))
                    //        {
                    //            runtextpos = runtextend;
                    //            return false;
                    //        }

                    //        if (runtextpos < runtextend)
                    //            runtextpos = runtextend;
                }
                else if (Anchors.Beginning)
                {
                    //        if (runtextpos > runtextbeg)
                    //        {
                    //            runtextpos = runtextend;
                    //            return false;
                    //        }
                }
                else if (Anchors.Start && Anchors.EndZ && Anchors.End)
                {
                    //        if (runtextpos > runtextstart)
                    //        {
                    //            runtextpos = runtextend;
                    //            return false;
                    //        }

                    //        if (runtextpos < runtextend - 1)
                    //            runtextpos = runtextend - 1;
                    //        else if (runtextpos < runtextend)
                    //            runtextpos = runtextend;
                }
                else if (Anchors.Start && Anchors.EndZ)
                {
                    //        if (runtextpos > runtextstart)
                    //        {
                    //            runtextpos = runtextend;
                    //            return false;
                    //        }

                    //        if (runtextpos < runtextend - 1)
                    //            runtextpos = runtextend - 1;
                }
                else if (Anchors.Start && Anchors.End)
                {
                    //        if (runtextpos > runtextstart)
                    //        {
                    //            runtextpos = runtextend;
                    //            return false;
                    //        }

                    //        if (runtextpos < runtextend)
                    //            runtextpos = runtextend;
                }
                else if (Anchors.Start)
                {
                    //        if (runtextpos > runtextstart)
                    //        {
                    //            runtextpos = runtextend;
                    //            return false;
                    //        }
                }
                else if (Anchors.EndZ && Anchors.End)
                {
                    //        if (runtextpos < runtextend - 1)
                    //            runtextpos = runtextend - 1;
                    //        else if (runtextpos < runtextend)
                    //            runtextpos = runtextend;
                }
                else if (Anchors.EndZ)
                {
                    //        if (runtextpos < runtextend - 1)
                    //            runtextpos = runtextend - 1;
                }
                else if (Anchors.End)
                {
                    //        if (runtextpos < runtextend)
                    //            runtextpos = runtextend;
                }

            }
            else
            {
                if (Anchors.Beginning && Anchors.Start && Anchors.EndZ && Anchors.End)
                {
                    //        if (runtextpos < runtextend || (runtextpos < runtextend - 1 || (runtextpos == runtextend - 1 && CharAt(runtextpos) != '\n')) || runtextpos < runtextstart)
                    //        {
                    //            runtextpos = runtextbeg;
                    //            return false;
                    //        }

                    //        if (runtextpos > runtextbeg)
                    //            runtextpos = runtextbeg;
                }
                else if (Anchors.Beginning && Anchors.Start && Anchors.EndZ)
                {
                    //        if ((runtextpos < runtextend - 1 || (runtextpos == runtextend - 1 && CharAt(runtextpos) != '\n')) || runtextpos < runtextstart)
                    //        {
                    //            runtextpos = runtextbeg;
                    //            return false;
                    //        }

                    //        if (runtextpos > runtextbeg)
                    //            runtextpos = runtextbeg;
                }
                else if (Anchors.Beginning && Anchors.Start && Anchors.End)
                {
                    //        if (runtextpos < runtextend || runtextpos < runtextstart)
                    //        {
                    //            runtextpos = runtextbeg;
                    //            return false;
                    //        }

                    //        if (runtextpos > runtextbeg)
                    //            runtextpos = runtextbeg;
                }
                else if (Anchors.Beginning && Anchors.Start)
                {
                    //        if (runtextpos < runtextstart)
                    //        {
                    //            runtextpos = runtextbeg;
                    //            return false;
                    //        }

                    //        if (runtextpos > runtextbeg)
                    //            runtextpos = runtextbeg;
                }
                else if (Anchors.Beginning && Anchors.EndZ && Anchors.End)
                {
                    //        if (runtextpos < runtextend || (runtextpos < runtextend - 1 || (runtextpos == runtextend - 1 && CharAt(runtextpos) != '\n')))
                    //        {
                    //            runtextpos = runtextbeg;
                    //            return false;
                    //        }

                    //        if (runtextpos > runtextbeg)
                    //            runtextpos = runtextbeg;
                }
                else if (Anchors.Beginning && Anchors.EndZ)
                {
                    //        if (runtextpos < runtextend - 1 || (runtextpos == runtextend - 1 && CharAt(runtextpos) != '\n'))
                    //        {
                    //            runtextpos = runtextbeg;
                    //            return false;
                    //        }

                    //        if (runtextpos > runtextbeg)
                    //            runtextpos = runtextbeg;
                }
                else if (Anchors.Beginning && Anchors.End)
                {
                    //        if (runtextpos < runtextend)
                    //        {
                    //            runtextpos = runtextbeg;
                    //            return false;
                    //        }

                    //        if (runtextpos > runtextbeg)
                    //            runtextpos = runtextbeg;
                }
                else if (Anchors.Beginning)
                {
                    //        if (runtextpos > runtextbeg)
                    //            runtextpos = runtextbeg;
                }
                else if (Anchors.Start && Anchors.EndZ && Anchors.End)
                {
                    //        if (runtextpos < runtextend || (runtextpos < runtextend - 1 || (runtextpos == runtextend - 1 && CharAt(runtextpos) != '\n')) || runtextpos < runtextstart)
                    //        {
                    //            runtextpos = runtextbeg;
                    //            return false;
                    //        }
                }
                else if (Anchors.Start && Anchors.EndZ)
                {
                    //        if ((runtextpos < runtextend - 1 || (runtextpos == runtextend - 1 && CharAt(runtextpos) != '\n')) || runtextpos < runtextstart)
                    //        {
                    //            runtextpos = runtextbeg;
                    //            return false;
                    //        }
                }
                else if (Anchors.Start && Anchors.End)
                {
                    //        if (runtextpos < runtextend || runtextpos < runtextstart)
                    //        {
                    //            runtextpos = runtextbeg;
                    //            return false;
                    //        }
                }
                else if (Anchors.Start)
                {
                    //        if (runtextpos < runtextstart)
                    //        {
                    //            runtextpos = runtextbeg;
                    //            return false;
                    //        }
                }
                else if (Anchors.EndZ && Anchors.End)
                {
                    //        if (runtextpos < runtextend || (runtextpos < runtextend - 1 || (runtextpos == runtextend - 1 && CharAt(runtextpos) != '\n')))
                    //        {
                    //            runtextpos = runtextbeg;
                    //            return false;
                    //        }
                }
                else if (Anchors.EndZ)
                {
                    //        if (runtextpos < runtextend - 1 || (runtextpos == runtextend - 1 && CharAt(runtextpos) != '\n'))
                    //        {
                    //            runtextpos = runtextbeg;
                    //            return false;
                    //        }
                }
                else if (Anchors.End)
                {
                    //        if (runtextpos < runtextend)
                    //        {
                    //            runtextpos = runtextbeg;
                    //            return false;
                    //        }
                }
            }

            if (BoyerMoorePrefix != null)
            {
            //        return _code.BMPrefix.IsMatch(runtext, runtextpos, runtextbeg, runtextend);
            }

            Writer.Write($"return true;"); // found a valid start or end anchor
        }
    }
}
