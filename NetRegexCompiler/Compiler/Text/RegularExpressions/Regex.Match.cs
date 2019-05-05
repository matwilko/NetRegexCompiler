// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace NetRegexCompiler.Compiler.Text.RegularExpressions
{
    public partial class Regex
    {
        /*
         * Returns true if the regex finds a match within the specified string
         */
        /// <summary>
        /// Searches the input string for one or more matches using the previous pattern,
        /// options, and starting position.
        /// </summary>
        public bool IsMatch(string input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return IsMatch(input, UseOptionR() ? input.Length : 0);
        }

        /*
         * Returns true if the regex finds a match after the specified position
         * (proceeding leftward if the regex is leftward and rightward otherwise)
         */
        /// <summary>
        /// Searches the input string for one or more matches using the previous pattern and options,
        /// with a new starting position.
        /// </summary>
        public bool IsMatch(string input, int startat)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return (null == Run(true, -1, input, 0, input.Length, startat));
        }

        /*
         * Finds the first match for the regular expression starting at the beginning
         * of the string (or at the end of the string if the regex is leftward)
         */
        /// <summary>
        /// Matches a regular expression with a string and returns
        /// the precise result as a RegexMatch object.
        /// </summary>
        public Match Match(string input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return Match(input, UseOptionR() ? input.Length : 0);
        }

        /*
         * Finds the first match, starting at the specified position
         */
        /// <summary>
        /// Matches a regular expression with a string and returns
        /// the precise result as a RegexMatch object.
        /// </summary>
        public Match Match(string input, int startat)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return Run(false, -1, input, 0, input.Length, startat);
        }

        /*
         * Finds the first match, restricting the search to the specified interval of
         * the char array.
         */
        /// <summary>
        /// Matches a regular expression with a string and returns the precise result as a
        /// RegexMatch object.
        /// </summary>
        public Match Match(string input, int beginning, int length)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return Run(false, -1, input, beginning, length, UseOptionR() ? beginning + length : beginning);
        }

        /*
         * Finds the first match for the regular expression starting at the beginning
         * of the string Enumerator(or at the end of the string if the regex is leftward)
         */
        /// <summary>
        /// Returns all the successful matches as if Match was called iteratively numerous times.
        /// </summary>
        public MatchCollection Matches(string input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return Matches(input, UseOptionR() ? input.Length : 0);
        }

        /*
         * Finds the first match, starting at the specified position
         */
        /// <summary>
        /// Returns all the successful matches as if Match was called iteratively numerous times.
        /// </summary>
        public MatchCollection Matches(string input, int startat)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return new MatchCollection(this, input, 0, input.Length, startat);
        }
    }
}
