using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace NetRegexCompiler.Compiler.Text.RegularExpressions
{
    internal sealed partial class RegexCSharpCompiler
    {
        private void GenerateGo()
        {
            var backtrackOperations = new BacktrackOperations();
            using (Writer.Method("protected override void Go()"))
            {
                foreach (var operation in Operations)
                using (Writer.OpenScope($"{operation.Label}: // {operation.CodeName}({string.Join(", ", operation.Operands.Select(o => CSharpWriter.ConvertFormatArgument(o)))})", requireBraces: true, clearLine: true))
                {
                    GenerateOpCode(operation, backtrackOperations);
                    Writer.Write($"break");
                }
            }

            if (backtrackOperations.Any())
            {
                using (Writer.OpenScope("backtrack:"))
                {
                    using (Writer.Switch($"{track}.Pop())"))
                    {
                        foreach (var operation in backtrackOperations)
                            using (Writer.OpenScope($"case {operation.Id}: // {operation.Operation.Label}, {operation.CodeName} ({(!operation.IsBack2 ? "Back" : "Back2")})"))
                            {
                                GenerateBacktrackOpCode(operation);
                                Writer.Write($"break");
                            }
                    }
                }
            }
        }

        private void GenerateOpCode(Operation operation, BacktrackOperations backtrackOperations)
        {
            switch (operation.Code)
            {
                case RegexCode.Stop:
                    Writer.Write($"return;");
                    break;

                case RegexCode.Nothing:
                    Backtrack();
                    break;


            }
        }

        private void GenerateBacktrackOpCode(BacktrackOperation operation)
        {
            switch (operation.CombinedCode)
            {

            }
        }

        private void Backtrack() => Writer.Write($"goto backtrack;");

        private sealed class BacktrackOperations : IEnumerable<BacktrackOperation>
        {
            private Dictionary<(int operationId, bool isBack2), (int id, Operation operation)> Operations { get; } = new Dictionary<(int operationId, bool isBack2), (int, Operation)>();
            private int Id { get; set; }

            public BacktrackOperation Add(Operation operation, bool isBack2)
            {
                (int id, Operation operation) op;
                if (Operations.TryGetValue((operation.Id, isBack2), out op))
                    return new BacktrackOperation(op.id, op.operation, isBack2);

                op = (Id++, operation);
                Operations.Add((operation.Id, isBack2), op);
                return new BacktrackOperation(op.id, operation, isBack2);
            }

            public IEnumerator<BacktrackOperation> GetEnumerator() => Operations.Select(kvp => new BacktrackOperation(kvp.Value.id, kvp.Value.operation, kvp.Key.isBack2)).OrderBy(bo => bo.Id).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private readonly struct BacktrackOperation
        {
            public int Id { get; }
            public Operation Operation { get; }
            public bool IsBack2 { get; }

            public int CombinedCode => !IsBack2
                ? Operation.Code | RegexCode.Back
                : Operation.Code | RegexCode.Back2;

            public string CodeName => Operation.CodeName;

            public BacktrackOperation(int id, Operation operation, bool isBack2)
            {
                Id = id;
                Operation = operation;
                IsBack2 = isBack2;
            }
        }
    }
}
