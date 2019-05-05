// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// The MatchCollection lists the successful matches that
// result when searching a string for a regular expression.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace NetRegexCompiler.Compiler.Text.RegularExpressions
{
    /*
     * This collection returns a sequence of successful match results, either
     * from GetMatchCollection() or GetExecuteCollection(). It stops when the
     * first failure is encountered (it does not return the failed match).
     */
    /// <summary>
    /// Represents the set of names appearing as capturing group
    /// names in a regular expression.
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(CollectionDebuggerProxy<Match>))]
    public class MatchCollection : IList<Match>, IReadOnlyList<Match>, IList
    {
        private readonly Regex _regex;
        private readonly List<Match> _matches;
        private bool _done;
        private readonly string _input;
        private readonly int _beginning;
        private readonly int _length;
        private int _startat;
        private int _prevlen;

        internal MatchCollection(Regex regex, string input, int beginning, int length, int startat)
        {
            if (startat < 0 || startat > input.Length)
                throw new ArgumentOutOfRangeException(nameof(startat), "Start index cannot be less than 0 or greater than input length.");

            _regex = regex;
            _input = input;
            _beginning = beginning;
            _length = length;
            _startat = startat;
            _prevlen = -1;
            _matches = new List<Match>();
            _done = false;
        }

        public bool IsReadOnly => true;

        /// <summary>
        /// Returns the number of captures.
        /// </summary>
        public int Count
        {
            get
            {
                EnsureInitialized();
                return _matches.Count;
            }
        }

        /// <summary>
        /// Returns the ith Match in the collection.
        /// </summary>
        public virtual Match this[int i]
        {
            get
            {
                if (i < 0)
                    throw new ArgumentOutOfRangeException(nameof(i));

                Match match = GetMatch(i);

                if (match == null)
                    throw new ArgumentOutOfRangeException(nameof(i));

                return match;
            }
        }

        /// <summary>
        /// Provides an enumerator in the same order as Item[i].
        /// </summary>
        public IEnumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<Match> IEnumerable<Match>.GetEnumerator() => new Enumerator(this);

        private Match GetMatch(int i)
        {
            Debug.Assert(i >= 0, "i cannot be negative.");

            if (_matches.Count > i)
                return _matches[i];

            if (_done)
                return null;

            Match match;

            do
            {
                match = _regex.Run(false, _prevlen, _input, _beginning, _length, _startat);

                if (!match.Success)
                {
                    _done = true;
                    return null;
                }

                _matches.Add(match);

                _prevlen = match.Length;
                _startat = match._textpos;
            } while (_matches.Count <= i);

            return match;
        }

        private void EnsureInitialized()
        {
            if (!_done)
            {
                GetMatch(int.MaxValue);
            }
        }

        public bool IsSynchronized => false;

        public object SyncRoot => this;

        public void CopyTo(Array array, int arrayIndex)
        {
            EnsureInitialized();
            ((ICollection)_matches).CopyTo(array, arrayIndex);
        }

        public void CopyTo(Match[] array, int arrayIndex)
        {
            EnsureInitialized();
            _matches.CopyTo(array, arrayIndex);
        }

        int IList<Match>.IndexOf(Match item)
        {
            EnsureInitialized();
            return _matches.IndexOf(item);
        }

        void IList<Match>.Insert(int index, Match item)
        {
            throw new NotSupportedException("Collection is read-only.");
        }

        void IList<Match>.RemoveAt(int index)
        {
            throw new NotSupportedException("Collection is read-only.");
        }

        Match IList<Match>.this[int index]
        {
            get { return this[index]; }
            set { throw new NotSupportedException("Collection is read-only."); }
        }

        void ICollection<Match>.Add(Match item)
        {
            throw new NotSupportedException("Collection is read-only.");
        }

        void ICollection<Match>.Clear()
        {
            throw new NotSupportedException("Collection is read-only.");
        }

        bool ICollection<Match>.Contains(Match item)
        {
            EnsureInitialized();
            return _matches.Contains(item);
        }

        bool ICollection<Match>.Remove(Match item)
        {
            throw new NotSupportedException("Collection is read-only.");
        }

        int IList.Add(object value)
        {
            throw new NotSupportedException("Collection is read-only.");
        }

        void IList.Clear()
        {
            throw new NotSupportedException("Collection is read-only.");
        }

        bool IList.Contains(object value) =>
            value is Match && ((ICollection<Match>)this).Contains((Match)value);

        int IList.IndexOf(object value) =>
            value is Match ? ((IList<Match>)this).IndexOf((Match)value) : -1;

        void IList.Insert(int index, object value)
        {
            throw new NotSupportedException("Collection is read-only.");
        }

        bool IList.IsFixedSize => true;

        void IList.Remove(object value)
        {
            throw new NotSupportedException("Collection is read-only.");
        }

        void IList.RemoveAt(int index)
        {
            throw new NotSupportedException("Collection is read-only.");
        }

        object IList.this[int index]
        {
            get { return this[index]; }
            set { throw new NotSupportedException("Collection is read-only."); }
        }

        private sealed class Enumerator : IEnumerator<Match>
        {
            private readonly MatchCollection _collection;
            private int _index;

            internal Enumerator(MatchCollection collection)
            {
                Debug.Assert(collection != null, "collection cannot be null.");

                _collection = collection;
                _index = -1;
            }

            public bool MoveNext()
            {
                if (_index == -2)
                    return false;

                _index++;
                Match match = _collection.GetMatch(_index);

                if (match == null)
                {
                    _index = -2;
                    return false;
                }

                return true;
            }

            public Match Current
            {
                get
                {
                    if (_index < 0)
                        throw new InvalidOperationException("Enumeration has either not started or has already finished.");

                    return _collection.GetMatch(_index);
                }
            }

            object IEnumerator.Current => Current;

            void IEnumerator.Reset()
            {
                _index = -1;
            }

            void IDisposable.Dispose() { }
        }
    }
}
