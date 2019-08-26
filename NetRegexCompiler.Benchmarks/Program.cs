using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace NetRegexCompiler.Benchmarks
{
	class Program
	{
		static void Main(string[] args)
		{
			BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
		}
	}

	[JsonExporterAttribute.Full]
	[MemoryDiagnoser]
	public partial class Benchmarks
	{

	}
}
