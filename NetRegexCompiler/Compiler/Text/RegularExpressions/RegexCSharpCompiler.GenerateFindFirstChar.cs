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
                //if (0 != (_code.Anchors & (RegexFCD.Beginning | RegexFCD.Start | RegexFCD.EndZ | RegexFCD.End)))
                //{
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
                //}
                //else if (_code.BMPrefix != null)
                //{
                //    runtextpos = _code.BMPrefix.Scan(runtext, runtextpos, runtextbeg, runtextend);

                //    if (runtextpos == -1)
                //    {
                //        runtextpos = (_code.RightToLeft ? runtextbeg : runtextend);
                //        return false;
                //    }

                //    return true;
                //}
                //else if (_code.FCPrefix == null)
                //{
                //    return true;
                //}

                //_rightToLeft = _code.RightToLeft;
                //_caseInsensitive = _code.FCPrefix.GetValueOrDefault().CaseInsensitive;
                //string set = _code.FCPrefix.GetValueOrDefault().Prefix;

                //if (RegexCharClass.IsSingleton(set))
                //{
                //    char ch = RegexCharClass.SingletonChar(set);

                //    for (int i = Forwardchars(); i > 0; i--)
                //    {
                //        if (ch == Forwardcharnext())
                //        {
                //            Backwardnext();
                //            return true;
                //        }
                //    }
                //}
                //else
                //{
                //    for (int i = Forwardchars(); i > 0; i--)
                //    {
                //        if (RegexCharClass.CharInClass(Forwardcharnext(), set))
                //        {
                //            Backwardnext();
                //            return true;
                //        }
                //    }
                //}

                //return false;
            }
        }
    }
}
