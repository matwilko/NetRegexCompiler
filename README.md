# NetRegexCompiler

A tool to effectively run the `RegexOptions.Compiled` compilation at build time, rather than at runtime, and directly embed the generated code in your assembly.

The code for Regex compilation was taken directly from corefx at [dotnet/corefx/src/System.Text.RegularExpressions@17382de](https://github.com/dotnet/corefx/tree/17382def0f680653870c31af1acf086ac41dcc0b/src/System.Text.RegularExpressions) and then modified to suit the purpose of this project.

In essence, the lightweight-codegen done to generate IL directly at runtime is modified to generate broadly equivalent C# code, which is generated from the regex during the build, and included in the project directly as generated C# code.