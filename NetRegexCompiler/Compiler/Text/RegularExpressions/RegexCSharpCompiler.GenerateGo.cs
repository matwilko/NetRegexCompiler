using System.Linq;

namespace NetRegexCompiler.Compiler.Text.RegularExpressions
{
    internal sealed partial class RegexCSharpCompiler
    {
        private void GenerateGo()
        {
            using (Writer.Method("protected override void Go()"))
            {
                foreach (var operation in Operations)
                    using (Writer.OpenScope($"{operation.Label}: // {operation.CodeName}({string.Join(", ", operation.Operands.Select(o => CSharpWriter.ConvertFormatArgument(o)))})", requireBraces: true, clearLine: true))
                        GenerateOpCode(operation);
            }
        }

        private void GenerateOpCode(Operation operation)
        {

        }
    }
}
