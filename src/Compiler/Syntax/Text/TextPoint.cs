using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PHP.Core.Text
{
    /// <summary>
    /// Represents position within text.
    /// </summary>
    public struct TextPoint : IComparable<TextPoint>, IEquatable<TextPoint>
    {
        #region Fields

        private readonly ILineBreaks _lineBreaks;
        private readonly int _position;

        #endregion

        #region Construction

        public TextPoint(ILineBreaks lineBreaks, int position)
        {
            //if (lineBreaks == null)
            //    throw new ArgumentNullException("lineBreaks");
            //if (position > lineBreaks.TextLength)
            //    throw new ArgumentException("position");

            _lineBreaks = lineBreaks;
            _position = position;
        }

        #endregion

        #region Properties

        public ILineBreaks LineBreaks
        {
            get
            {
                return _lineBreaks;
            }
        }

        public int Position
        {
            get
            {
                return _position;
            }
        }

        public int Line
        {
            get
            {
                return _lineBreaks.GetLineFromPosition(_position);
            }
        }

        public int Column
        {
            get
            {
                int line, column;
                _lineBreaks.GetLineColumnFromPosition(_position, out line, out column);
                return column;
            }
        }

        #endregion

        #region Methods

        public static implicit operator int(TextPoint point)
        {
            return point.Position;
        }

        public override int GetHashCode()
        {
            return _position.GetHashCode() ^ _lineBreaks.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is TextPoint)
                return this.Equals((TextPoint)obj);

            return false;
        }

        public TextPoint Add(int offset)
        {
            return new TextPoint(this.LineBreaks, _position + offset);
        }

        public TextPoint Subtract(int offset)
        {
            return this.Add(-offset);
        }

        public static TextPoint operator -(TextPoint point, int offset)
        {
            return point.Add(-offset);
        }

        public static int operator -(TextPoint start, TextPoint other)
        {
            if (start.LineBreaks != other.LineBreaks)
                throw new ArgumentException();
            
            return start.Position - other.Position;
        }

        public static bool operator ==(TextPoint left, TextPoint right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TextPoint left, TextPoint right)
        {
            return !(left == right);
        }

        public static TextPoint operator +(TextPoint point, int offset)
        {
            return point.Add(offset);
        }

        public static bool operator >(TextPoint left, TextPoint right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator <(TextPoint left, TextPoint right)
        {
            return left.CompareTo(right) < 0;
        }

        #endregion

        #region IComparable<TextPoint>

        public int CompareTo(TextPoint other)
        {
            if (this.LineBreaks != other.LineBreaks)
                throw new ArgumentException();

            return _position.CompareTo(other._position);
        }

        #endregion

        #region IEquatable<TextPoint> Members

        public bool Equals(TextPoint other)
        {
            return other.LineBreaks == this.LineBreaks && other.Position == this.Position;
        }

        #endregion
    }
}
