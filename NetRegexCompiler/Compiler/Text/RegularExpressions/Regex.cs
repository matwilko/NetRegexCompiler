// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// The Regex class represents a single compiled instance of a regular
// expression.

using System;
using System.Collections;
using System.Globalization;
using NetRegexCompiler.Compiler.Collections;

namespace NetRegexCompiler.Compiler.Text.RegularExpressions
{
    /// <summary>
    /// Represents an immutable, compiled regular expression. Also
    /// contains static methods that allow use of regular expressions without instantiating
    /// a Regex explicitly.
    /// </summary>
    public sealed partial class Regex
    {
        internal const int MaxOptionShift = 10;

        internal string pattern;                   // The string pattern provided
        internal RegexOptions roptions;            // the top-level options from the options string
        internal RegexRunnerFactory factory;
        internal Hashtable caps;                   // if captures are sparse, this is the hashtable capnum->index
        internal Hashtable capnames;               // if named captures are used, this maps names->index
        internal string[] capslist;                // if captures are sparse or named captures are used, this is the sorted list of names
        internal int capsize;                      // the size of the capture array
        
        internal ExclusiveReference _runnerref;              // cached runner
        internal WeakReference<RegexReplacement> _replref; // cached parsed replacement pattern
        internal RegexCode _code;                            // if interpreted, this is the code for RegexInterpreter
        internal bool _refsInitialized = false;
        
        /// <summary>
        /// Creates and compiles a regular expression object for the
        /// specified regular expression with options that modify the pattern.
        /// </summary>
        public Regex(string pattern, RegexOptions options)
            : this(pattern, options, s_defaultMatchTimeout)
        {
        }

        public Regex(string pattern, RegexOptions options, TimeSpan matchTimeout)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            if (options < RegexOptions.None || (((int)options) >> MaxOptionShift) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options));
            }

            if ((options & RegexOptions.ECMAScript) != 0
             && (options & ~(RegexOptions.ECMAScript |
                             RegexOptions.IgnoreCase |
                             RegexOptions.Multiline |
                             RegexOptions.CultureInvariant
#if DEBUG
                           | RegexOptions.Debug
#endif
                                               )) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options));
            }

            ValidateMatchTimeout(matchTimeout);

            // After parameter validation assign 
            this.pattern = pattern;
            roptions = options;
            internalMatchTimeout = matchTimeout;

            var culture = (options & RegexOptions.CultureInvariant) != 0
                ? CultureInfo.InvariantCulture
                : CultureInfo.CurrentCulture;

            // Parse the input
            var tree = RegexParser.Parse(pattern, roptions, culture);

            // Extract the relevant information
            capnames = tree.CapNames;
            capslist = tree.CapsList;
            _code = RegexWriter.Write(tree);
            caps = _code.Caps;
            capsize = _code.CapSize;

            InitializeReferences();

            factory = RegexCompiler.Compile(_code, roptions);
            _code = null;
        }

        /// <summary>
        /// Indicates whether the regular expression matches from right to left.
        /// </summary>
        public bool RightToLeft => UseOptionR();

        /// <summary>
        /// Returns the regular expression pattern passed into the constructor
        /// </summary>
        public override string ToString() => pattern;

        /*
         * Given a group number, maps it to a group name. Note that numbered
         * groups automatically get a group name that is the decimal string
         * equivalent of its number.
         *
         * Returns null if the number is not a recognized group number.
         */
        /// <summary>
        /// Retrieves a group name that corresponds to a group number.
        /// </summary>
        public string GroupNameFromNumber(int i)
        {
            if (capslist == null)
            {
                if (i >= 0 && i < capsize)
                    return i.ToString(CultureInfo.InvariantCulture);

                return string.Empty;
            }
            else
            {
                if (caps != null)
                {
                    if (!caps.TryGetValue(i, out i))
                        return string.Empty;
                }

                if (i >= 0 && i < capslist.Length)
                    return capslist[i];

                return string.Empty;
            }
        }

        /*
         * Given a group name, maps it to a group number. Note that numbered
         * groups automatically get a group name that is the decimal string
         * equivalent of its number.
         *
         * Returns -1 if the name is not a recognized group name.
         */
        /// <summary>
        /// Returns a group number that corresponds to a group name.
        /// </summary>
        public int GroupNumberFromName(string name)
        {
            int result = -1;

            if (name == null)
                throw new ArgumentNullException(nameof(name));

            // look up name if we have a hashtable of names
            if (capnames != null)
            {
                if (!capnames.TryGetValue(name, out result))
                    return -1;

                return result;
            }

            // convert to an int if it looks like a number
            result = 0;
            for (int i = 0; i < name.Length; i++)
            {
                char ch = name[i];

                if (ch > '9' || ch < '0')
                    return -1;

                result *= 10;
                result += (ch - '0');
            }

            // return int if it's in range
            if (result >= 0 && result < capsize)
                return result;

            return -1;
        }

        private void InitializeReferences()
        {
            if (_refsInitialized)
                throw new NotSupportedException("This operation is only allowed once per object.");

            _refsInitialized = true;
            _runnerref = new ExclusiveReference();
            _replref = new WeakReference<RegexReplacement>(null);
        }

        /// <summary>
        /// Internal worker called by all the public APIs
        /// </summary>
        /// <returns></returns>
        internal Match Run(bool quick, int prevlen, string input, int beginning, int length, int startat)
        {
            if (startat < 0 || startat > input.Length)
                throw new ArgumentOutOfRangeException(nameof(startat), "Start index cannot be less than 0 or greater than input length.");

            if (length < 0 || length > input.Length)
                throw new ArgumentOutOfRangeException(nameof(length), "Length cannot be less than 0 or exceed input length.");

            // There may be a cached runner; grab ownership of it if we can.
            RegexRunner runner = _runnerref.Get();

            // Create a RegexRunner instance if we need to
            if (runner == null)
            {
                // Use the compiled RegexRunner factory if the code was compiled to MSIL
                if (factory != null)
                    runner = factory.CreateInstance();
                else
                    runner = new RegexInterpreter(_code, UseOptionInvariant() ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture);
            }

            Match match;
            try
            {
                // Do the scan starting at the requested position
                match = runner.Scan(this, input, beginning, beginning + length, startat, prevlen, quick, internalMatchTimeout);
            }
            finally
            {
                // Release or fill the cache slot
                _runnerref.Release(runner);
            }

#if DEBUG
            if (Debug && match != null)
                match.Dump();
#endif
            return match;
        }
        
        /*
         * True if the L option was set
         */
        internal bool UseOptionR() => (roptions & RegexOptions.RightToLeft) != 0;

        internal bool UseOptionInvariant() => (roptions & RegexOptions.CultureInvariant) != 0;

#if DEBUG
        /// <summary>
        /// True if the regex has debugging enabled
        /// </summary>
        internal bool Debug => (roptions & RegexOptions.Debug) != 0;
#endif
    }
}
