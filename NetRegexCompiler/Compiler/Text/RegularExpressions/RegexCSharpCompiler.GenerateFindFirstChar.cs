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
                else
                {
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

        private void GenerateAnchorChecks()
        {
            if (!IsRightToLeft)
            {
                if (Anchors.Beginning && Anchors.Start && Anchors.EndZ && Anchors.End)
                {
                    using (Writer.If($"{runtextpos} > {runtextbeg} || {runtextpos} > {runtextstart}"))
                    {
                        Writer.Write($"{runtextpos} = {runtextend}");
                        Writer.Write($"return false");
                    }

                    using (Writer.If($"{runtextpos} < {runtextend} - 1"))
                        Writer.Write($"{runtextpos} = {runtextend} - 1");
                    using (Writer.ElseIf($"{runtextpos} < {runtextend}"))
                        Writer.Write($"{runtextpos} = {runtextend}");
                }
                else if (Anchors.Beginning && Anchors.Start && Anchors.EndZ)
                {
                    using (Writer.If($"{runtextpos} > {runtextbeg} ||{runtextpos} > {runtextstart}"))
                    {
                        Writer.Write($"{runtextpos} = {runtextend}");
                        Writer.Write($"return false");
                    }

                    using (Writer.If($"{runtextpos} < {runtextend} - 1"))
                        Writer.Write($"{runtextpos} = {runtextend} - 1");
                }
                else if (Anchors.Beginning && Anchors.Start && Anchors.End)
                {
                    using (Writer.If($"{runtextpos} > {runtextbeg} || {runtextpos} > {runtextstart}"))
                    {
                        Writer.Write($"{runtextpos} = {runtextend}");
                        Writer.Write($"return false");
                    }

                    using (Writer.If($"{runtextpos} < {runtextend}"))
                        Writer.Write($"{runtextpos} = {runtextend}");
                }
                else if (Anchors.Beginning && Anchors.Start)
                {
                    using (Writer.If($"{runtextpos} > {runtextbeg} || {runtextpos} > {runtextstart}"))
                    {
                        Writer.Write($"{runtextpos} = {runtextend}");
                        Writer.Write($"return false");
                    }
                }
                else if (Anchors.Beginning && Anchors.EndZ && Anchors.End)
                {
                    using (Writer.If($"{runtextpos} > {runtextbeg}"))
                    {
                        Writer.Write($"{runtextpos} = {runtextend}");
                        Writer.Write($"return false");
                    }

                    using (Writer.If($"{runtextpos} < {runtextend} - 1"))
                        Writer.Write($"{runtextpos} = {runtextend} - 1");
                    using (Writer.ElseIf($"{runtextpos} < {runtextend}"))
                        Writer.Write($"{runtextpos} = {runtextend}");
                }
                else if (Anchors.Beginning && Anchors.EndZ)
                {
                    using (Writer.If($"{runtextpos} > {runtextbeg}"))
                    {
                        Writer.Write($"{runtextpos} = {runtextend}");
                        Writer.Write($"return false");
                    }

                    using (Writer.If($"{runtextpos} < {runtextend} - 1"))
                        Writer.Write($"{runtextpos} = {runtextend} - 1");
                }
                else if (Anchors.Beginning && Anchors.End)
                {
                    using (Writer.If($"{runtextpos} > {runtextbeg}"))
                    {
                        Writer.Write($"{runtextpos} = {runtextend}");
                        Writer.Write($"return false");
                    }

                    using (Writer.If($"{runtextpos} < {runtextend}"))
                        Writer.Write($"{runtextpos} = {runtextend}");
                }
                else if (Anchors.Beginning)
                {
                    using (Writer.If($"{runtextpos} > {runtextbeg}"))
                    {
                        Writer.Write($"{runtextpos} = {runtextend}");
                        Writer.Write($"return false");
                    }
                }
                else if (Anchors.Start && Anchors.EndZ && Anchors.End)
                {
                    using (Writer.If($"{runtextpos} > {runtextstart}"))
                    {
                        Writer.Write($"{runtextpos} = {runtextend}");
                        Writer.Write($"return false");
                    }

                    using (Writer.If($"{runtextpos} < {runtextend} - 1"))
                        Writer.Write($"{runtextpos} = {runtextend} - 1");
                    using (Writer.ElseIf($"{runtextpos} < {runtextend}"))
                        Writer.Write($"{runtextpos} = {runtextend}");
                }
                else if (Anchors.Start && Anchors.EndZ)
                {
                    using (Writer.If($"{runtextpos} > {runtextstart}"))
                    {
                        Writer.Write($"{runtextpos} = {runtextend}");
                        Writer.Write($"return false");
                    }

                    using (Writer.If($"{runtextpos} < {runtextend} - 1"))
                        Writer.Write($"{runtextpos} = {runtextend} - 1");
                }
                else if (Anchors.Start && Anchors.End)
                {
                    using (Writer.If($"{runtextpos} > {runtextstart}"))
                    {
                        Writer.Write($"{runtextpos} = {runtextend}");
                        Writer.Write($"return false");
                    }

                    using (Writer.If($"{runtextpos} < {runtextend}"))
                        Writer.Write($"{runtextpos} = {runtextend}");
                }
                else if (Anchors.Start)
                {
                    using (Writer.If($"{runtextpos} > {runtextstart}"))
                    {
                        Writer.Write($"{runtextpos} = {runtextend}");
                        Writer.Write($"return false");
                    }
                }
                else if (Anchors.EndZ && Anchors.End)
                {
                    using (Writer.If($"{runtextpos} < {runtextend} - 1"))
                        Writer.Write($"{runtextpos} = {runtextend} - 1");
                    using (Writer.ElseIf($"{runtextpos} < {runtextend}"))
                        Writer.Write($"{runtextpos} = {runtextend}");
                }
                else if (Anchors.EndZ)
                {
                    using (Writer.If($"{runtextpos} < {runtextend} - 1"))
                        Writer.Write($"{runtextpos} = {runtextend} - 1");
                }
                else if (Anchors.End)
                {
                    using (Writer.If($"{runtextpos} < {runtextend}"))
                        Writer.Write($"{runtextpos} = {runtextend}");
                }

            }
            else
            {
                if (Anchors.Beginning && Anchors.Start && Anchors.EndZ && Anchors.End)
                {
                    using (Writer.If($"{runtextpos} < {runtextend} || ({runtextpos} < {runtextend} - 1 || ({runtextpos} == {runtextend} - 1 && CharAt({runtextpos}) != '\n')) || {runtextpos} < {runtextstart}"))
                    {
                        Writer.Write($"{runtextpos} = {runtextbeg}");
                        Writer.Write($"return false");
                    }

                    using (Writer.If($"{runtextpos} > {runtextbeg}"))
                        Writer.Write($"{runtextpos} = {runtextbeg}");
                }
                else if (Anchors.Beginning && Anchors.Start && Anchors.EndZ)
                {
                    using (Writer.If($"({runtextpos} < {runtextend} - 1 || ({runtextpos} == {runtextend} - 1 && CharAt({runtextpos}) != '\n')) || {runtextpos} < {runtextstart}"))
                    {
                        Writer.Write($"{runtextpos} = {runtextbeg}");
                        Writer.Write($"return false");
                    }

                    using (Writer.If($"{runtextpos} > {runtextbeg}"))
                        Writer.Write($"{runtextpos} = {runtextbeg}");
                }
                else if (Anchors.Beginning && Anchors.Start && Anchors.End)
                {
                    using (Writer.If($"{runtextpos} < {runtextend} || {runtextpos} < {runtextstart}"))
                    {
                        Writer.Write($"{runtextpos} = {runtextbeg}");
                        Writer.Write($"return false");
                    }

                    using (Writer.If($"{runtextpos} > {runtextbeg}"))
                        Writer.Write($"{runtextpos} = {runtextbeg}");
                }
                else if (Anchors.Beginning && Anchors.Start)
                {
                    using (Writer.If($"{runtextpos} < {runtextstart}"))
                    {
                        Writer.Write($"{runtextpos} = {runtextbeg}");
                        Writer.Write($"return false");
                    }

                    using (Writer.If($"{runtextpos} > {runtextbeg}"))
                        Writer.Write($"{runtextpos} = {runtextbeg}");
                }
                else if (Anchors.Beginning && Anchors.EndZ && Anchors.End)
                {
                    using (Writer.If($"{runtextpos} < {runtextend} || ({runtextpos} < {runtextend} - 1 || ({runtextpos} == {runtextend} - 1 && CharAt({runtextpos}) != '\n'))"))
                    {
                        Writer.Write($"{runtextpos} = {runtextbeg}");
                        Writer.Write($"return false");
                    }

                    using (Writer.If($"{runtextpos} > {runtextbeg}"))
                        Writer.Write($"{runtextpos} = {runtextbeg}");
                }
                else if (Anchors.Beginning && Anchors.EndZ)
                {
                    using (Writer.If($"{runtextpos} < {runtextend} - 1 || ({runtextpos} == {runtextend} - 1 && CharAt({runtextpos}) != '\n')"))
                    {
                        Writer.Write($"{runtextpos} = {runtextbeg}");
                        Writer.Write($"return false");
                    }

                    using (Writer.If($"{runtextpos} > {runtextbeg}"))
                        Writer.Write($"{runtextpos} = {runtextbeg}");
                }
                else if (Anchors.Beginning && Anchors.End)
                {
                    using (Writer.If($"{runtextpos} < {runtextend}"))
                    {
                        Writer.Write($"{runtextpos} = {runtextbeg}");
                        Writer.Write($"return false");
                    }

                    using (Writer.If($"{runtextpos} > {runtextbeg}"))
                        Writer.Write($"{runtextpos} = {runtextbeg}");
                }
                else if (Anchors.Beginning)
                {
                    using (Writer.If($"{runtextpos} > {runtextbeg}"))
                        Writer.Write($"{runtextpos} = {runtextbeg}");
                }
                else if (Anchors.Start && Anchors.EndZ && Anchors.End)
                {
                    using (Writer.If($"{runtextpos} < {runtextend} || ({runtextpos} < {runtextend} - 1 || ({runtextpos} == {runtextend} - 1 && CharAt({runtextpos}) != '\n')) || {runtextpos} < {runtextstart}"))
                    {
                        Writer.Write($"{runtextpos} = {runtextbeg}");
                        Writer.Write($"return false");
                    }
                }
                else if (Anchors.Start && Anchors.EndZ)
                {
                    using (Writer.If($"({runtextpos} < {runtextend} - 1 || ({runtextpos} == {runtextend} - 1 && CharAt({runtextpos}) != '\n')) || {runtextpos} < {runtextstart}"))
                    {
                        Writer.Write($"{runtextpos} = {runtextbeg}");
                        Writer.Write($"return false");
                    }
                }
                else if (Anchors.Start && Anchors.End)
                {
                    using (Writer.If($"{runtextpos} < {runtextend} || {runtextpos} < {runtextstart}"))
                    {
                        Writer.Write($"{runtextpos} = {runtextbeg}");
                        Writer.Write($"return false");
                    }
                }
                else if (Anchors.Start)
                {
                    using (Writer.If($"{runtextpos} < {runtextstart}"))
                    {
                        Writer.Write($"{runtextpos} = {runtextbeg}");
                        Writer.Write($"return false");
                    }
                }
                else if (Anchors.EndZ && Anchors.End)
                {
                    using (Writer.If($"{runtextpos} < {runtextend} || ({runtextpos} < {runtextend} - 1 || ({runtextpos} == {runtextend} - 1 && CharAt({runtextpos}) != '\n'))"))
                    {
                        Writer.Write($"{runtextpos} = {runtextbeg}");
                        Writer.Write($"return false");
                    }
                }
                else if (Anchors.EndZ)
                {
                    using (Writer.If($"{runtextpos} < {runtextend} - 1 || ({runtextpos} == {runtextend} - 1 && CharAt({runtextpos}) != '\n')"))
                    {
                        Writer.Write($"{runtextpos} = {runtextbeg}");
                        Writer.Write($"return false");
                    }
                }
                else if (Anchors.End)
                {
                    using (Writer.If($"{runtextpos} < {runtextend}"))
                    {
                        Writer.Write($"{runtextpos} = {runtextbeg}");
                        Writer.Write($"return false");
                    }
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
