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
                    //        if (Anchors.Beginning && runtextpos > runtextbeg) || (Anchors.Start && runtextpos > runtextstart))
                    //        {
                    //            runtextpos = runtextend;
                    //            return false;
                    //        }

                    //        if (Anchors.EndZ && runtextpos < runtextend - 1)
                    //            runtextpos = runtextend - 1;
                    //        else if (Anchors.End && runtextpos < runtextend)
                    //            runtextpos = runtextend;
                }
                else if (Anchors.Beginning && Anchors.Start && Anchors.EndZ)
                {
                    //        if (Anchors.Beginning && runtextpos > runtextbeg) || (Anchors.Start && runtextpos > runtextstart))
                    //        {
                    //            runtextpos = runtextend;
                    //            return false;
                    //        }

                    //        if (Anchors.EndZ && runtextpos < runtextend - 1)
                    //            runtextpos = runtextend - 1;
                    //        else if (Anchors.End && runtextpos < runtextend)
                    //            runtextpos = runtextend;
                }
                else if (Anchors.Beginning && Anchors.Start && Anchors.End)
                {
                    //        if (Anchors.Beginning && runtextpos > runtextbeg) || (Anchors.Start && runtextpos > runtextstart))
                    //        {
                    //            runtextpos = runtextend;
                    //            return false;
                    //        }

                    //        if (Anchors.EndZ && runtextpos < runtextend - 1)
                    //            runtextpos = runtextend - 1;
                    //        else if (Anchors.End && runtextpos < runtextend)
                    //            runtextpos = runtextend;
                }
                else if (Anchors.Beginning && Anchors.Start)
                {
                    //        if (Anchors.Beginning && runtextpos > runtextbeg) || (Anchors.Start && runtextpos > runtextstart))
                    //        {
                    //            runtextpos = runtextend;
                    //            return false;
                    //        }

                    //        if (Anchors.EndZ && runtextpos < runtextend - 1)
                    //            runtextpos = runtextend - 1;
                    //        else if (Anchors.End && runtextpos < runtextend)
                    //            runtextpos = runtextend;
                }
                else if (Anchors.Beginning && Anchors.EndZ && Anchors.End)
                {
                    //        if (Anchors.Beginning && runtextpos > runtextbeg) || (Anchors.Start && runtextpos > runtextstart))
                    //        {
                    //            runtextpos = runtextend;
                    //            return false;
                    //        }

                    //        if (Anchors.EndZ && runtextpos < runtextend - 1)
                    //            runtextpos = runtextend - 1;
                    //        else if (Anchors.End && runtextpos < runtextend)
                    //            runtextpos = runtextend;
                }
                else if (Anchors.Beginning && Anchors.EndZ)
                {
                    //        if (Anchors.Beginning && runtextpos > runtextbeg) || (Anchors.Start && runtextpos > runtextstart))
                    //        {
                    //            runtextpos = runtextend;
                    //            return false;
                    //        }

                    //        if (Anchors.EndZ && runtextpos < runtextend - 1)
                    //            runtextpos = runtextend - 1;
                    //        else if (Anchors.End && runtextpos < runtextend)
                    //            runtextpos = runtextend;
                }
                else if (Anchors.Beginning && Anchors.End)
                {
                    //        if (Anchors.Beginning && runtextpos > runtextbeg) || (Anchors.Start && runtextpos > runtextstart))
                    //        {
                    //            runtextpos = runtextend;
                    //            return false;
                    //        }

                    //        if (Anchors.EndZ && runtextpos < runtextend - 1)
                    //            runtextpos = runtextend - 1;
                    //        else if (Anchors.End && runtextpos < runtextend)
                    //            runtextpos = runtextend;
                }
                else if (Anchors.Beginning)
                {
                    //        if (Anchors.Beginning && runtextpos > runtextbeg) || (Anchors.Start && runtextpos > runtextstart))
                    //        {
                    //            runtextpos = runtextend;
                    //            return false;
                    //        }

                    //        if (Anchors.EndZ && runtextpos < runtextend - 1)
                    //            runtextpos = runtextend - 1;
                    //        else if (Anchors.End && runtextpos < runtextend)
                    //            runtextpos = runtextend;
                }
                else if (Anchors.Start && Anchors.EndZ && Anchors.End)
                {
                    //        if (Anchors.Beginning && runtextpos > runtextbeg) || (Anchors.Start && runtextpos > runtextstart))
                    //        {
                    //            runtextpos = runtextend;
                    //            return false;
                    //        }

                    //        if (Anchors.EndZ && runtextpos < runtextend - 1)
                    //            runtextpos = runtextend - 1;
                    //        else if (Anchors.End && runtextpos < runtextend)
                    //            runtextpos = runtextend;
                }
                else if (Anchors.Start && Anchors.EndZ)
                {
                    //        if (Anchors.Beginning && runtextpos > runtextbeg) || (Anchors.Start && runtextpos > runtextstart))
                    //        {
                    //            runtextpos = runtextend;
                    //            return false;
                    //        }

                    //        if (Anchors.EndZ && runtextpos < runtextend - 1)
                    //            runtextpos = runtextend - 1;
                    //        else if (Anchors.End && runtextpos < runtextend)
                    //            runtextpos = runtextend;
                }
                else if (Anchors.Start && Anchors.End)
                {
                    //        if (Anchors.Beginning && runtextpos > runtextbeg) || (Anchors.Start && runtextpos > runtextstart))
                    //        {
                    //            runtextpos = runtextend;
                    //            return false;
                    //        }

                    //        if (Anchors.EndZ && runtextpos < runtextend - 1)
                    //            runtextpos = runtextend - 1;
                    //        else if (Anchors.End && runtextpos < runtextend)
                    //            runtextpos = runtextend;
                }
                else if (Anchors.Start)
                {
                    //        if (Anchors.Beginning && runtextpos > runtextbeg) || (Anchors.Start && runtextpos > runtextstart))
                    //        {
                    //            runtextpos = runtextend;
                    //            return false;
                    //        }

                    //        if (Anchors.EndZ && runtextpos < runtextend - 1)
                    //            runtextpos = runtextend - 1;
                    //        else if (Anchors.End && runtextpos < runtextend)
                    //            runtextpos = runtextend;
                }
                else if (Anchors.EndZ && Anchors.End)
                {
                    //        if (Anchors.Beginning && runtextpos > runtextbeg) || (Anchors.Start && runtextpos > runtextstart))
                    //        {
                    //            runtextpos = runtextend;
                    //            return false;
                    //        }

                    //        if (Anchors.EndZ && runtextpos < runtextend - 1)
                    //            runtextpos = runtextend - 1;
                    //        else if (Anchors.End && runtextpos < runtextend)
                    //            runtextpos = runtextend;
                }
                else if (Anchors.EndZ)
                {
                    //        if (Anchors.Beginning && runtextpos > runtextbeg) || (Anchors.Start && runtextpos > runtextstart))
                    //        {
                    //            runtextpos = runtextend;
                    //            return false;
                    //        }

                    //        if (Anchors.EndZ && runtextpos < runtextend - 1)
                    //            runtextpos = runtextend - 1;
                    //        else if (Anchors.End && runtextpos < runtextend)
                    //            runtextpos = runtextend;
                }
                else if (Anchors.End)
                {
                    //        if (Anchors.Beginning && runtextpos > runtextbeg) || (Anchors.Start && runtextpos > runtextstart))
                    //        {
                    //            runtextpos = runtextend;
                    //            return false;
                    //        }

                    //        if (Anchors.EndZ && runtextpos < runtextend - 1)
                    //            runtextpos = runtextend - 1;
                    //        else if (Anchors.End && runtextpos < runtextend)
                    //            runtextpos = runtextend;
                }

            }
            else
            {
                if (Anchors.Beginning && Anchors.Start && Anchors.EndZ && Anchors.End)
                {
                    //        if (Anchors.End && runtextpos < runtextend) || (Anchors.EndZ && (runtextpos < runtextend - 1 || (runtextpos == runtextend - 1 && CharAt(runtextpos) != '\n'))) || (.Anchors.Start && runtextpos < runtextstart))
                    //        {
                    //            runtextpos = runtextbeg;
                    //            return false;
                    //        }

                    //        if (Anchors.Beginning && runtextpos > runtextbeg)
                    //            runtextpos = runtextbeg;
                }
                else if (Anchors.Beginning && Anchors.Start && Anchors.EndZ)
                {
                    //        if (Anchors.End && runtextpos < runtextend) || (Anchors.EndZ && (runtextpos < runtextend - 1 || (runtextpos == runtextend - 1 && CharAt(runtextpos) != '\n'))) || (.Anchors.Start && runtextpos < runtextstart))
                    //        {
                    //            runtextpos = runtextbeg;
                    //            return false;
                    //        }

                    //        if (Anchors.Beginning && runtextpos > runtextbeg)
                    //            runtextpos = runtextbeg;
                }
                else if (Anchors.Beginning && Anchors.Start && Anchors.End)
                {
                    //        if (Anchors.End && runtextpos < runtextend) || (Anchors.EndZ && (runtextpos < runtextend - 1 || (runtextpos == runtextend - 1 && CharAt(runtextpos) != '\n'))) || (.Anchors.Start && runtextpos < runtextstart))
                    //        {
                    //            runtextpos = runtextbeg;
                    //            return false;
                    //        }

                    //        if (Anchors.Beginning && runtextpos > runtextbeg)
                    //            runtextpos = runtextbeg;
                }
                else if (Anchors.Beginning && Anchors.Start)
                {
                    //        if (Anchors.End && runtextpos < runtextend) || (Anchors.EndZ && (runtextpos < runtextend - 1 || (runtextpos == runtextend - 1 && CharAt(runtextpos) != '\n'))) || (.Anchors.Start && runtextpos < runtextstart))
                    //        {
                    //            runtextpos = runtextbeg;
                    //            return false;
                    //        }

                    //        if (Anchors.Beginning && runtextpos > runtextbeg)
                    //            runtextpos = runtextbeg;
                }
                else if (Anchors.Beginning && Anchors.EndZ && Anchors.End)
                {
                    //        if (Anchors.End && runtextpos < runtextend) || (Anchors.EndZ && (runtextpos < runtextend - 1 || (runtextpos == runtextend - 1 && CharAt(runtextpos) != '\n'))) || (.Anchors.Start && runtextpos < runtextstart))
                    //        {
                    //            runtextpos = runtextbeg;
                    //            return false;
                    //        }

                    //        if (Anchors.Beginning && runtextpos > runtextbeg)
                    //            runtextpos = runtextbeg;
                }
                else if (Anchors.Beginning && Anchors.EndZ)
                {
                    //        if (Anchors.End && runtextpos < runtextend) || (Anchors.EndZ && (runtextpos < runtextend - 1 || (runtextpos == runtextend - 1 && CharAt(runtextpos) != '\n'))) || (.Anchors.Start && runtextpos < runtextstart))
                    //        {
                    //            runtextpos = runtextbeg;
                    //            return false;
                    //        }

                    //        if (Anchors.Beginning && runtextpos > runtextbeg)
                    //            runtextpos = runtextbeg;
                }
                else if (Anchors.Beginning && Anchors.End)
                {
                    //        if (Anchors.End && runtextpos < runtextend) || (Anchors.EndZ && (runtextpos < runtextend - 1 || (runtextpos == runtextend - 1 && CharAt(runtextpos) != '\n'))) || (.Anchors.Start && runtextpos < runtextstart))
                    //        {
                    //            runtextpos = runtextbeg;
                    //            return false;
                    //        }

                    //        if (Anchors.Beginning && runtextpos > runtextbeg)
                    //            runtextpos = runtextbeg;
                }
                else if (Anchors.Beginning)
                {
                    //        if (Anchors.End && runtextpos < runtextend) || (Anchors.EndZ && (runtextpos < runtextend - 1 || (runtextpos == runtextend - 1 && CharAt(runtextpos) != '\n'))) || (.Anchors.Start && runtextpos < runtextstart))
                    //        {
                    //            runtextpos = runtextbeg;
                    //            return false;
                    //        }

                    //        if (Anchors.Beginning && runtextpos > runtextbeg)
                    //            runtextpos = runtextbeg;
                }
                else if (Anchors.Start && Anchors.EndZ && Anchors.End)
                {
                    //        if (Anchors.End && runtextpos < runtextend) || (Anchors.EndZ && (runtextpos < runtextend - 1 || (runtextpos == runtextend - 1 && CharAt(runtextpos) != '\n'))) || (.Anchors.Start && runtextpos < runtextstart))
                    //        {
                    //            runtextpos = runtextbeg;
                    //            return false;
                    //        }

                    //        if (Anchors.Beginning && runtextpos > runtextbeg)
                    //            runtextpos = runtextbeg;
                }
                else if (Anchors.Start && Anchors.EndZ)
                {
                    //        if (Anchors.End && runtextpos < runtextend) || (Anchors.EndZ && (runtextpos < runtextend - 1 || (runtextpos == runtextend - 1 && CharAt(runtextpos) != '\n'))) || (.Anchors.Start && runtextpos < runtextstart))
                    //        {
                    //            runtextpos = runtextbeg;
                    //            return false;
                    //        }

                    //        if (Anchors.Beginning && runtextpos > runtextbeg)
                    //            runtextpos = runtextbeg;
                }
                else if (Anchors.Start && Anchors.End)
                {
                    //        if (Anchors.End && runtextpos < runtextend) || (Anchors.EndZ && (runtextpos < runtextend - 1 || (runtextpos == runtextend - 1 && CharAt(runtextpos) != '\n'))) || (.Anchors.Start && runtextpos < runtextstart))
                    //        {
                    //            runtextpos = runtextbeg;
                    //            return false;
                    //        }

                    //        if (Anchors.Beginning && runtextpos > runtextbeg)
                    //            runtextpos = runtextbeg;
                }
                else if (Anchors.Start)
                {
                    //        if (Anchors.End && runtextpos < runtextend) || (Anchors.EndZ && (runtextpos < runtextend - 1 || (runtextpos == runtextend - 1 && CharAt(runtextpos) != '\n'))) || (.Anchors.Start && runtextpos < runtextstart))
                    //        {
                    //            runtextpos = runtextbeg;
                    //            return false;
                    //        }

                    //        if (Anchors.Beginning && runtextpos > runtextbeg)
                    //            runtextpos = runtextbeg;
                }
                else if (Anchors.EndZ && Anchors.End)
                {
                    //        if (Anchors.End && runtextpos < runtextend) || (Anchors.EndZ && (runtextpos < runtextend - 1 || (runtextpos == runtextend - 1 && CharAt(runtextpos) != '\n'))) || (.Anchors.Start && runtextpos < runtextstart))
                    //        {
                    //            runtextpos = runtextbeg;
                    //            return false;
                    //        }

                    //        if (Anchors.Beginning && runtextpos > runtextbeg)
                    //            runtextpos = runtextbeg;
                }
                else if (Anchors.EndZ)
                {
                    //        if (Anchors.End && runtextpos < runtextend) || (Anchors.EndZ && (runtextpos < runtextend - 1 || (runtextpos == runtextend - 1 && CharAt(runtextpos) != '\n'))) || (.Anchors.Start && runtextpos < runtextstart))
                    //        {
                    //            runtextpos = runtextbeg;
                    //            return false;
                    //        }

                    //        if (Anchors.Beginning && runtextpos > runtextbeg)
                    //            runtextpos = runtextbeg;
                }
                else if (Anchors.End)
                {
                    //        if (Anchors.End && runtextpos < runtextend) || (Anchors.EndZ && (runtextpos < runtextend - 1 || (runtextpos == runtextend - 1 && CharAt(runtextpos) != '\n'))) || (.Anchors.Start && runtextpos < runtextstart))
                    //        {
                    //            runtextpos = runtextbeg;
                    //            return false;
                    //        }

                    //        if (Anchors.Beginning && runtextpos > runtextbeg)
                    //            runtextpos = runtextbeg;
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
