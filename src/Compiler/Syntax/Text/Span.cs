using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PHP.Core.Text
{
    /// <summary>
    /// Represents text span.
    /// </summary>
    public struct Span : IEquatable<Span>
    {
        #region Fields

        private int _start;
        private int _length;

        #endregion

        #region Properties

        public int Start
        {
            get
            {
                return _start;
            }
        }

        public int End
        {
            get
            {
                return _start + _length;
            }
        }

        public int Length
        {
            get
            {
                return _length;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return _length == 0;
            }
        }

        /// <summary>
        /// Gets value determining whether this span represents a valid span.
        /// </summary>
        public bool IsValid { get { return _length >= 0; } }

        /// <summary>
        /// Gets representation of an invalid span.
        /// </summary>
        public static Span Invalid { get { return new Span() { _start = 0, _length = -1 }; } }

        #endregion

        #region Construction

        public Span(int start, int length)
        {
            if (start < 0)
                throw new ArgumentOutOfRangeException("start");

            if (length < 0)
                throw new ArgumentOutOfRangeException("length");

            _start = start;
            _length = length;
        }

        public static Span FromBounds(int start, int end)
        {
            return new Span(start, end - start);
        }

        #endregion

        #region Methods

        public static Span Combine(Span left, Span right)
        {
            return Span.FromBounds(left.Start, right.End);
        }

        public bool Contains(int position)
        {
            return position >= this.Start && position < this.End;
        }

        public bool Contains(Span span)
        {
            return span._start >= this.Start && span.End <= this.End;
        }

        public bool OverlapsWith(Span span)
        {
            return Math.Max(this.Start, span.Start) < Math.Min(this.End, span.End);
        }

        public Span? Overlap(Span span)
        {
            int start = Math.Max(this.Start, span.Start);
            int end = Math.Min(this.End, span.End);
            if (start < end)
                return new Span?(Span.FromBounds(start, end));

            return null;
        }

        public bool IntersectsWith(Span span)
        {
            return span.Start <= this.End && span.End >= this.Start;
        }

        public Span? Intersection(Span span)
        {
            int start = Math.Max(this.Start, span.Start);
            int end = Math.Min(this.End, span.End);
            if (start <= end)
                return new Span?(Span.FromBounds(start, end));

            return null;
        }

        public static bool operator ==(Span left, Span right)
        {
            return left._start == right._start && left._length == right._length;
        }

        public static bool operator !=(Span left, Span right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Gets portion of document defined by this <see cref="Span"/>.
        /// </summary>
        public string GetText(string document)
        {
            return document.Substring(_start, _length);
        }

        #endregion

        #region Object Members

        public override int GetHashCode()
        {
            return _start.GetHashCode() ^ _length.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is Span)
                return Equals((Span)obj);

            return false;
        }

        public override string ToString()
        {
            return string.Format("[{0}..{1})", _start, _start + _length);
        }

        #endregion

        #region IEquatable<Span> Members

        public bool Equals(Span other)
        {
            return other._start == this._start && other._length == this._length;
        }

        #endregion
    }
}
