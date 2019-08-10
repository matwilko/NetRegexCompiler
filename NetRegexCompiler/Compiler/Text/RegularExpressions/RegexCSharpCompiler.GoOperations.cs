using System.Linq;

namespace NetRegexCompiler.Compiler.Text.RegularExpressions
{
    internal sealed partial class RegexCSharpCompiler
    {
        private void Backtrack() => Writer.Write($"goto backtrack;");

        private void Goto(int operationPos)
        {
            var operation = Operations.Single(op => op.Index == operationPos);
            Writer.Write($"goto {operation.Label};");
        }
    }
}
