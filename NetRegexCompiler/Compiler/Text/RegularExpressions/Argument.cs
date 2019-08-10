using System;

namespace NetRegexCompiler.Compiler.Text.RegularExpressions
{
    internal readonly struct Argument
    {
        public object Arg { get; }

        private Argument(object arg)
        {
            Arg = arg;
        }

        public static implicit operator Argument(int i) => new Argument(i);
        public static implicit operator Argument(FormattableString fs) => new Argument(fs);


    }
}