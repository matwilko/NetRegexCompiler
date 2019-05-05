using System.Text;

namespace NetRegexCompiler.Compiler.Extensions
{
    internal static class StringBuilderExtensions
    {
        public static void AppendReversed(this StringBuilder sb, string value)
        {
            sb.EnsureCapacity(sb.Length + value.Length);
            for (int i = value.Length - 1; i >= 0; i--)
            {
                sb.Append(value[i]);
            }
        }

        public static void AppendReversed(this StringBuilder sb, string value, int startIndex, int length)
        {
            sb.EnsureCapacity(sb.Length + length);
            for (int i = startIndex + length - 1; i >= startIndex; i--)
            {
                sb.Append(value[i]);
            }
        }

        public static void Reverse(this StringBuilder sb)
        {
            var lastElemToSwap = ((sb.Length - 1) / 2) + 1;
            var lastElem = sb.Length - 1;
            for (var i = lastElem; i >= lastElemToSwap; i--)
            {
                var topElem = sb[i];
                sb[i] = sb[lastElem - i];
                sb[lastElem - i] = topElem;
            }
        }
    }
}
