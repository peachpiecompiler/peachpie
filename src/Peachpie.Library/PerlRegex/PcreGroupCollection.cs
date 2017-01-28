using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Library.PerlRegex
{
    /// <summary>
    /// Collection of successful groups ordered by occurance as it is in PCRE.
    /// </summary>
    internal struct PcreGroupCollection : IReadOnlyList<Group>
    {
        /// <summary>
        /// Original <see cref="Match"/>.
        /// </summary>
        readonly Match _match;

        /// <summary>
        /// Indexes to <see cref="Match.Groups"/>.
        /// </summary>
        readonly int[] _indexes;

        /// <summary>
        /// Gets value indicating whether the collection is an uninitialized structure.
        /// </summary>
        public bool IsDefault => _indexes == null;

        public PcreGroupCollection(Match match)
        {
            Debug.Assert(match != null);
            _match = match;
            _indexes = GetIndexes(match);
        }

        /// <summary>
        /// Initializes map of successful matches ordered by their position in text.
        /// </summary>
        static int[] GetIndexes(Match match)
        {
            // order matches by their Index and skip unsuccessful matches
            if (match.Success)
            {
                var groups = match.Groups;
                var indexes = new List<int>(groups.Count) { 0 };
                var lastUnsuccessful = 0;
                //
                for (int i = 1; i < groups.Count; i++)
                {
                    var g = groups[i];
                    if (g.Success)
                    {
                        if (lastUnsuccessful != 0)
                        {
                            while (lastUnsuccessful < i) indexes.Add(lastUnsuccessful++);
                            lastUnsuccessful = 0;
                        }

                        indexes.Add(i);
                    }
                    else if (lastUnsuccessful == 0)
                    {
                        lastUnsuccessful = i;
                    }
                }

                // sort by match position & outer first
                return indexes.ToArray();
            }
            else
            {
                return Array.Empty<int>();
            }
        }

        #region IReadOnlyList<Group>

        public Group this[int index] => _match.Groups[_indexes[index]];

        public int Count => _indexes.Length;

        public IEnumerator<Group> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
    }
}
