using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace NetRegexCompiler.Compiler.Text.RegularExpressions
{
    internal sealed partial class RegexCSharpCompiler
    {
        private Method BoyerMoorePrefixScan { get; } = new Method("BoyerMoorePrefixScan");

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
                    GenerateBoyerMoorePrefixScanCheck();
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

            if (BoyerMoorePrefix != null) 
                GenerateBoyerMoorePrefixScan();
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

        private void GenerateBoyerMoorePrefixScanCheck()
        {
            using (Writer.If($"{BoyerMoorePrefixScan}() == -1"))
            {
                if (IsRightToLeft)
                    Writer.Write($"{runtextpos} = {runtextbeg}");
                else
                    Writer.Write($"{runtextpos} = {runtextend}");
                Writer.Write($"return false");
            }

            Writer.Write($"return true");
        }

        private void GenerateBoyerMoorePrefixScan()
        {
            var positive = Writer.DeclareField($"private static readonly int[] positive = new int[] {{ {string.Join(", ", BoyerMoorePrefix.Positive.Select(i => CSharpWriter.ConvertFormatArgument(i)))} }};");
            var negativeAscii = Writer.DeclareField($"private static readonly int[] negativeAscii = new int[] {{ {string.Join(", ", BoyerMoorePrefix.NegativeASCII.Select(i => CSharpWriter.ConvertFormatArgument(i)))} }};");
            var negativeUnicode = BoyerMoorePrefix.NegativeUnicode != null
                ? Writer.DeclareField($"private static readonly int[][] negativeUnicode = new int[][] {{ {string.Join(", ", BoyerMoorePrefix.NegativeUnicode.Select(ia => $"new int[] {{ {string.Join(", ", ia.Select(i => CSharpWriter.ConvertFormatArgument(i)))} }}"))} }};")
                : default(Field);
            
            using (Writer.Method($"private int {BoyerMoorePrefixScan}()"))
            {
                var text = Writer.DeclareLocal($"var text = {runtext};");
                var index = Writer.DeclareLocal($"var index = {runtextpos};");
                var beglimit = Writer.DeclareLocal($"var beglimit = {runtextbeg};");
                var endlimit = Writer.DeclareLocal($"var endlimit = {runtextend};");
                
                var pattern = Writer.DeclareLocal($@"var pattern = ""{BoyerMoorePrefix.Pattern}"";");
                
                var defadv = !IsRightToLeft
                    ? Writer.DeclareLocal($"var defadv = {pattern}.Length;")
                    : Writer.DeclareLocal($"var defadv = -{pattern}.Length;");
                var startMatch = !IsRightToLeft
                    ? Writer.DeclareLocal($"var startMatch = {pattern}.Length - 1;")
                    : Writer.DeclareLocal($"var startMatch = 0;");
                var endMatch = !IsRightToLeft
                    ? Writer.DeclareLocal($"var endMatch = 0;")
                    : Writer.DeclareLocal($"var endMatch = -{defadv} - 1;");
                var test = !IsRightToLeft
                    ? Writer.DeclareLocal($"var test = {index} + {defadv} - 1;")
                    : Writer.DeclareLocal($"var test = {index} + {defadv};");
                var bump = !IsRightToLeft
                    ? Writer.DeclareLocal($"var bump = 1;")
                    : Writer.DeclareLocal($"var bump = -1;");

                var chMatch = Writer.DeclareLocal($"char chMatch = Pattern[{startMatch}];");
                var chTest = Writer.DeclareLocal($"char chTest;");
                var test2 = Writer.DeclareLocal($"int test2;");
                var match = Writer.DeclareLocal($"int match;");
                var advance = Writer.DeclareLocal($"int advance;");
                var unicodeLookup = Writer.DeclareLocal($"int[] unicodeLookup;");

                // for (; ; )
                // {
                //     if (test >= endlimit || test < beglimit)
                //         return -1;
                   
                //     chTest = text[test];
                   
                //     if (CaseInsensitive)
                //         chTest = _culture.TextInfo.ToLower(chTest);
                   
                //     if (chTest != chMatch)
                //     {
                //         if (chTest < 128)
                //             advance = NegativeASCII[chTest];
                //         else if (null != NegativeUnicode && (null != (unicodeLookup = NegativeUnicode[chTest >> 8])))
                //             advance = unicodeLookup[chTest & 0xFF];
                //         else
                //             advance = defadv;
                   
                //         test += advance;
                //     }
                //     else
                //     { // if (chTest == chMatch)
                //         test2 = test;
                //         match = startmatch;
                   
                //         for (; ; )
                //         {
                //             if (match == endmatch)
                //                 return (RightToLeft ? test2 + 1 : test2);
                   
                //             match -= bump;
                //             test2 -= bump;
                   
                //             chTest = text[test2];
                   
                //             if (CaseInsensitive)
                //                 chTest = _culture.TextInfo.ToLower(chTest);
                   
                //             if (chTest != Pattern[match])
                //             {
                //                 advance = Positive[match];
                //                 if ((chTest & 0xFF80) == 0)
                //                     test2 = (match - startmatch) + NegativeASCII[chTest];
                //                 else if (null != NegativeUnicode && (null != (unicodeLookup = NegativeUnicode[chTest >> 8])))
                //                     test2 = (match - startmatch) + unicodeLookup[chTest & 0xFF];
                //                 else
                //                 {
                //                     test += advance;
                //                     break;
                //                 }
                   
                //                 if (RightToLeft ? test2 < advance : test2 > advance)
                //                     advance = test2;
                   
                //                 test += advance;
                //                 break;
                //             }
                //         }
                //     }
                // }
            }
        }
    }
}
