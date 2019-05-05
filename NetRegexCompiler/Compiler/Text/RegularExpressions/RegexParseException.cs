// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace NetRegexCompiler.Compiler.Text.RegularExpressions
{
    internal sealed class RegexParseException : ArgumentException
    {
        private readonly RegexParseError _error;

        /// <summary>
        /// The error that happened during parsing.
        /// </summary>
        public RegexParseError Error => _error;

        /// <summary>
        /// The offset in the supplied pattern.
        /// </summary>
        public int Offset { get; }

        public RegexParseException(RegexParseError error, int offset, string message) : base(message)
        {
            _error = error;
            Offset = offset;
        }

        public RegexParseException() : base()
        {
        }

        public RegexParseException(string message) : base(message)
        {
        }

        public RegexParseException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
