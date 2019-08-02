using System;
using System.Collections.Generic;
using System.Text;

namespace NetRegexCompiler.Compiler.Text.RegularExpressions
{
    internal sealed partial class RegexCSharpCompiler
    {
        private void GenerateGo()
        {
            using (Writer.Method("protected override void Go()"))
            {
            }
        }
    }
}
