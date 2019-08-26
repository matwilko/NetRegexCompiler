using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using RegexOptions = NetRegexCompiler.Compiler.Text.RegularExpressions.RegexOptions;

namespace NetRegexCompiler.GenerateBenchmarks
{
	class Program
	{
		static void Main(string[] args)
        {
            var outputDir = args[0];
            Directory.CreateDirectory(outputDir);

            var seed = int.Parse(args[1]);
            var random = new Random(seed);

            var output = args[2];

            var regexes = GetRegexes(@"regexlib.json")
				.SelectMany(r => r.Examples.Select(e => (r.Regex, e)))
	            .ToList();

            var chosenRegexes = Enumerable.Range(0, int.MaxValue)
	            .Select(_ => regexes[random.Next(0, regexes.Count)])
	            .Select(r => (r.Regex, r.e, OptionsPowerSet[random.Next(0, OptionsPowerSet.Length)]))
	            .Where(r => { try { new System.Text.RegularExpressions.Regex(r.Regex, (System.Text.RegularExpressions.RegexOptions) (int) r.Item3); return true; } catch { return false; } })
	            .Take(10)
	            .ToList();

            using (var benchmarksFile = File.Open(Path.Combine(outputDir, "Benchmarks.cs"), FileMode.Create, FileAccess.Write, FileShare.None))
            using (var benchmarkTheory = new StreamWriter(benchmarksFile))
            {
                benchmarkTheory.WriteLine(@"using System.Collections.Generic;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Attributes;

namespace NetRegexCompiler.Benchmarks
{
    partial class Benchmarks
    {
");

                foreach ((var regex, var example, var options) in chosenRegexes)
                {
                    Console.WriteLine($"Processing `{regex}`");
                    var fileId = IdFor(regex, options);
                    using (var file = File.Open(Path.Combine(outputDir, fileId + ".cs"), FileMode.Create, FileAccess.Write, FileShare.None))
                    using (var writer = new StreamWriter(file))
                    {
                        var id = IdFor(regex, options);
						NetRegexCompiler.Compiler.Text.RegularExpressions.RegexCSharpCompiler.GenerateCSharpCode(writer, regex, options, "NetRegexCompiler.CompiledRegexes", "CR" + id);

                        if (output == "interpreted")
                        {
	                        benchmarkTheory.WriteLine($@"            private static readonly Regex INR{id}Instance = new Regex(NetRegexCompiler.CompiledRegexes.CR{id}.Instance.ToString(), NetRegexCompiler.CompiledRegexes.CR{id}.Instance.Options);");
	                        benchmarkTheory.WriteLine($@"            [Benchmark] public Match INR{id}() => INR{id}Instance.Match({JsonConvert.SerializeObject(example)});");
                        }

                        if (output == "netcompiled")
                        {
	                        benchmarkTheory.WriteLine($@"            private static readonly Regex CNR{id}Instance = new Regex(NetRegexCompiler.CompiledRegexes.CR{id}.Instance.ToString(), NetRegexCompiler.CompiledRegexes.CR{id}.Instance.Options | RegexOptions.Compiled);");
							benchmarkTheory.WriteLine($@"            [Benchmark] public Match CNR{id}() => CNR{id}Instance.Match({JsonConvert.SerializeObject(example)});");
                        }

                        if (output == "compiled")
                        {
	                        benchmarkTheory.WriteLine($@"            [Benchmark] public Match CR{id}() => NetRegexCompiler.CompiledRegexes.CR{id}.Instance.Match({JsonConvert.SerializeObject(example)});");
                        }
                    }
                }

                benchmarkTheory.Write(@"
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

        private static string IdFor(string regex, RegexOptions options)
        {
            var bytes = Encoding.UTF8.GetBytes(regex + options);
            var hash = MD5.ComputeHash(bytes);
            return string.Join("", hash.Select(i => i.ToString("X2")));
        }

        private static IEnumerable<RegexExample> GetRegexes(string filename)
        {
	        using (var stream = typeof(Program).Assembly.GetManifestResourceStream($"NetRegexCompiler.GenerateBenchmarks.{filename}"))
	        using (var reader = new StreamReader(stream))
	        using (var jsonReader = new JsonTextReader(reader))
		        return new JsonSerializer().Deserialize<IEnumerable<RegexExample>>(jsonReader);
        }

        private static RegexOptions[] OptionsPowerSet { get; } =
        {
	        RegexOptions.None,
	        RegexOptions.CultureInvariant,
	        RegexOptions.ECMAScript,
	        RegexOptions.ECMAScript | RegexOptions.CultureInvariant,
	        RegexOptions.ExplicitCapture,
	        RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase,
	        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.ECMAScript,
	        RegexOptions.IgnoreCase | RegexOptions.ECMAScript | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture,
	        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant,
	        RegexOptions.IgnorePatternWhitespace,
	        RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant,
	        RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace,
	        RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace,
	        RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace,
	        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant,
	        RegexOptions.Multiline,
	        RegexOptions.Multiline | RegexOptions.CultureInvariant,
	        RegexOptions.Multiline | RegexOptions.ECMAScript,
	        RegexOptions.Multiline | RegexOptions.ECMAScript | RegexOptions.CultureInvariant,
	        RegexOptions.Multiline | RegexOptions.ExplicitCapture,
	        RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ECMAScript,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ECMAScript | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant,
	        RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace,
	        RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant,
	        RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace,
	        RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant,
	        RegexOptions.RightToLeft,
	        RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.ExplicitCapture | RegexOptions.RightToLeft,
	        RegexOptions.ExplicitCapture | RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.RightToLeft,
	        RegexOptions.IgnoreCase | RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.RightToLeft,
	        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft,
	        RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft,
	        RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft,
	        RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft,
	        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.Multiline | RegexOptions.RightToLeft,
	        RegexOptions.Multiline | RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.RightToLeft,
	        RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.RightToLeft,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.RightToLeft,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft,
	        RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft,
	        RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.Singleline,
	        RegexOptions.Singleline | RegexOptions.CultureInvariant,
	        RegexOptions.ExplicitCapture | RegexOptions.Singleline,
	        RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.Singleline,
	        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Singleline,
	        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.CultureInvariant,
	        RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace,
	        RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant,
	        RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace,
	        RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace,
	        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace,
	        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant,
	        RegexOptions.Multiline | RegexOptions.Singleline,
	        RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.CultureInvariant,
	        RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.Singleline,
	        RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.Singleline,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.CultureInvariant,
	        RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace,
	        RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant,
	        RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace,
	        RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant,
	        RegexOptions.Singleline | RegexOptions.RightToLeft,
	        RegexOptions.Singleline | RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.RightToLeft,
	        RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.RightToLeft,
	        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.RightToLeft,
	        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft,
	        RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft,
	        RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft,
	        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft,
	        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.RightToLeft,
	        RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.RightToLeft,
	        RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.RightToLeft,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.RightToLeft,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft,
	        RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft,
	        RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft | RegexOptions.CultureInvariant,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft,
	        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft | RegexOptions.CultureInvariant
        };
	}
}
