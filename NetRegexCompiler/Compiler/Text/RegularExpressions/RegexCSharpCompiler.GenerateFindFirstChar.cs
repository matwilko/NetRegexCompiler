using System;
using System.Collections.Generic;
using System.Linq;
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
                // var text = runtext;
                // var index = runtextpos;
                // var beglimit = runtextbeg;
                // var endlimit = runtextend;
                //if (!RightToLeft)
                //{
                //    if (index < beglimit || endlimit - index < Pattern.Length)
                //        return false;

                //    if (CaseInsensitive)
                //    {
                //        if (text.Length - index < Pattern.Length)
                //        {
                //        return false;
                //        }
                //        
                //        return (0 == string.Compare(Pattern, 0, text, index, Pattern.Length, CaseInsensitive, _culture));
                //    }
                //    else
                //    {
                //        return (0 == string.CompareOrdinal(Pattern, 0, text, index, Pattern.Length));
                //    }
                //}
                //else
                //{
                //    if (index > endlimit || index - beglimit < Pattern.Length)
                //        return false;

                //    var index = index - Pattern.Length;
                //    if (CaseInsensitive)
                //    {
                //        if (text.Length - index < Pattern.Length)
                //        {
                //            return false;
                //        }
                      
                //        return (0 == string.Compare(Pattern, 0, text, index, Pattern.Length, CaseInsensitive, _culture));
                //    }
                //    else
                //    {
                //        return (0 == string.CompareOrdinal(Pattern, 0, text, index, Pattern.Length));
                //    }
                //}
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
            var culture = Writer.DeclareField($@"private static readonly CultureInfo BoyerMooreCulture = CultureInfo.GetCultureInfo(""{BoyerMoorePrefix._culture.ToString()}"");");
            
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

                using (Writer.For($";;"))
                {
                     using (Writer.If($"{test} >= {endlimit} || {test} < {beglimit}"))
                         Writer.Write($"return -1");

                     if (!IsCaseInsensitive)
                        Writer.Write($"{chTest} = {text}[{test}]");
                     else
                        Writer.Write($"{chTest} = {culture}.TextInfo.ToLower({text}[{test}]))");

                     using (Writer.If($"{chTest} != {chMatch}"))
                     {
                         using (Writer.If($"{chTest} < 128"))
                             Writer.Write($"{advance} = {negativeAscii}[{chTest}]");
                         if (negativeUnicode != null)
                         using (Writer.ElseIf($"null != ({unicodeLookup} = {negativeUnicode}[{chTest} >> 8]))"))
                            Writer.Write($"{advance} = {unicodeLookup}[{chTest} & 0xFF]");
                         using (Writer.Else())
                            Writer.Write($"{advance} = {defadv}");

                         Writer.Write($"{test} += {advance}");
                     }
                     using (Writer.Else())
                     {
                         Writer.Write($"{test2} = {test}");
                         Writer.Write($"{match} = {startMatch}");

                         using (Writer.For($";;"))
                         {
                             using (Writer.If($"{match} == {endMatch}"))
                             {
                                 if (IsRightToLeft)
                                     Writer.Write($"return test2 + 1");
                                 else
                                     Writer.Write($"return test2");
                             }

                             Writer.Write($"{match} -= {bump}");
                             Writer.Write($"{test2} -= {bump}");

                             if (!IsCaseInsensitive)
                                 Writer.Write($"{chTest} = {text}[{test2}]");
                             else
                                 Writer.Write($"{chTest} = {culture}.TextInfo.ToLower({text}[{test2}])");

                             using (Writer.If($"{chTest} != {pattern}[{match}]"))
                             {
                                 Writer.Write($"{advance} = {positive}[{match}]");
                                 using (Writer.If($"({chTest} & 0xFF80) == 0"))
                                     Writer.Write($"{test2} = ({match} - {startMatch}) + {negativeAscii}[{chTest}]");
                                 if (negativeUnicode != null)
                                     using (Writer.ElseIf($"null != ({unicodeLookup} = {negativeUnicode}[{chTest} >> 8])"))
                                         Writer.Write($"{test2} = ({match} - {startMatch}) + {unicodeLookup}[{chTest} & 0xFF]");
                                 using (Writer.Else())
                                 {
                                     Writer.Write($"{test} += {advance}");
                                     Writer.Write($"break");
                                 }

                                 if (!IsRightToLeft)
                                 {
                                     using (Writer.If($"{test2} > {advance}"))
                                         Writer.Write($"{advance} = {test2}");
                                 }
                                 else
                                 {
                                     using (Writer.If($"{test2} < {advance}"))
                                         Writer.Write($"{advance} = {test2}");
                                 }

                                 Writer.Write($"{test} += {advance}");
                                 Writer.Write($"break");
                             }
                         }
                     }
                }
            }
        }
    }
}