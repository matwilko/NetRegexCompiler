using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using RegexOptions = NetRegexCompiler.Compiler.Text.RegularExpressions.RegexOptions;

namespace NetRegexCompiler.GenerateTests
{
    class Program
    {
        static void Main(string[] args)
        {
            var outputDir = args[0];
            Directory.CreateDirectory(outputDir);

            var options = args.Skip(1)
                .Select(s => (RegexOptions) Enum.Parse(typeof(RegexOptions), s))
                .Aggregate(RegexOptions.None, (f1, f2) => f1 | f2);

            var regexes = JsonConvert.DeserializeObject<IEnumerable<RegexExample>>(File.ReadAllText(@"dotnettests.json"))
                .Concat(JsonConvert.DeserializeObject<IEnumerable<RegexExample>>(File.ReadAllText(@"regexlib.json")));
            using (var theoryFile = File.Open(Path.Combine(outputDir, "Theories.cs"), FileMode.Create, FileAccess.Write, FileShare.None))
            using (var theoryWriter = new StreamWriter(theoryFile))
            {
                theoryWriter.WriteLine(@"using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NetRegexCompiler.Tests.CompiledTestRegexes
{
    class Theories
    {
        public static IEnumerable<object[]> RegexTests()
        {");

                foreach (var regex in regexes)
                {
                    Console.WriteLine($"Processing `{regex.Regex}`");
                    var fileId = IdFor(regex, options);
                    using (var file = File.Open(Path.Combine(outputDir, fileId + ".cs"), FileMode.Create, FileAccess.Write, FileShare.None))
                    using (var writer = new StreamWriter(file))
                    {
                        var id = IdFor(regex, options);
                        try
                        {
                            NetRegexCompiler.Compiler.Text.RegularExpressions.RegexCSharpCompiler.GenerateCSharpCode(writer, regex.Regex, options, "NetRegexCompiler.Tests.CompiledTestRegexes", "CR" + id);
                            foreach (var example in regex.Examples.Where(ex => InterpreterRunsTestInReasonableTime(regex.Regex, (int)options, ex)))
                                theoryWriter.WriteLine($@"            yield return new object[] {{ CR{id}.Instance, (RegexOptions){(int) options}, {JsonConvert.SerializeObject(example)}, ""{id}"" }};");
                        }
                        catch (Exception ex) when (ex.Message.StartsWith("Invalid pattern"))
                        {
                        }
                    }
                }

                theoryWriter.Write(@"
            }
    }
}");
            }
        }

        public sealed class RegexExample
        {
            public string Regex { get; }
            public string[] Examples { get; }

            public RegexExample(string regex, string[] examples)
            {
                Regex = regex;
                Examples = examples;
            }
        }

        private static MD5 MD5 { get; } = MD5.Create();

        private static string IdFor(RegexExample ex, RegexOptions options)
        {
            var bytes = Encoding.UTF8.GetBytes(ex.Regex + options.ToString());
            var hash = MD5.ComputeHash(bytes);
            return string.Join("", hash.Select(i => i.ToString("X2")));
        }

        private static bool InterpreterRunsTestInReasonableTime(string pattern, int options, string testString)
        {
            try
            {
                new System.Text.RegularExpressions.Regex(pattern, (System.Text.RegularExpressions.RegexOptions) options, TimeSpan.FromMilliseconds(1000)).IsMatch(testString);
                return true;
            }
            catch (System.Text.RegularExpressions.RegexMatchTimeoutException)
            {
                return false;
            }
        }
    }
}