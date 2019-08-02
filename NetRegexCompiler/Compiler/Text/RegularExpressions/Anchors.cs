namespace NetRegexCompiler.Compiler.Text.RegularExpressions
{
    internal readonly struct Anchors
    {
        private readonly int anchors;

        public Anchors(int anchors)
        {
            this.anchors = anchors;
        }

        private bool HasFlag(int flag) => (anchors & flag) == flag;
        public bool Beginning => HasFlag(RegexFCD.Beginning);
        public bool Bol => HasFlag(RegexFCD.Bol);
        public bool Start => HasFlag(RegexFCD.Start);
        public bool Eol => HasFlag(RegexFCD.Eol);
        public bool EndZ => HasFlag(RegexFCD.EndZ);
        public bool End => HasFlag(RegexFCD.End);
        public bool Boundary => HasFlag(RegexFCD.Boundary);
        public bool ECMABoundary => HasFlag(RegexFCD.ECMABoundary);
    }
}