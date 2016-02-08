using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PHP.Core.Text
{
    /// <summary>
    /// Represents span within text.
    /// </summary>
    public struct TextSpan : IEquatable<TextSpan>
    {
        #region Fields

        private readonly TextPoint _start;
        private readonly int _length;

        #endregion

        #region Construction

        public TextSpan(ILineBreaks lineBreaks, int start, int length)
        {
            _start = new TextPoint(lineBreaks, start);
            _length = length;
        }

        public TextSpan(TextPoint start, TextPoint end)
        {
            if (!object.ReferenceEquals(start.LineBreaks, end.LineBreaks))
                throw new ArgumentException();

            if (start.LineBreaks == null)
                throw new ArgumentException();

            _start = start;
            _length = end.Position - start.Position;

            if (_length < 0)
                throw new ArgumentException();
        }

        public TextSpan(ILineBreaks lineBreaks, Span span)
		{
			if (lineBreaks == null)
                throw new ArgumentNullException("lineBreaks");
			
            if (span.End > lineBreaks.TextLength)
				throw new ArgumentOutOfRangeException("span");
			
            _start = new TextPoint(lineBreaks, span.Start);
			_length = span.Length;
		}

        public TextSpan(TextPoint start, int length)
		{
			if (length < 0 || start.Position + length > start.LineBreaks.TextLength)
				throw new ArgumentOutOfRangeException("length");

            _start = start;
			_length = length;
		}

        #endregion

        #region Properties

        public TextPoint Start { get { return _start; } }
        public TextPoint End { get { return new TextPoint(_start.LineBreaks, _start.Position + _length); } }
        public int Length { get { return _length; } }
        public bool IsEmpty { get { return _length == 0; } }
        public ILineBreaks LineBreaks { get { return _start.LineBreaks; } }
        public Span Span { get { return new Span(_start.Position, _length); } }
        public int FirstLine { get { return Start.Line; } }
        public int LastLine { get { return End.Line; } }
        public int FirstColumn { get { return Start.Column; } }
        public int LastColumn { get { return End.Column; } }

        #endregion

        #region Methods

        public static implicit operator Span(TextSpan span)
		{
			return span.Span;
		}

        public static TextSpan Combine(TextSpan left, TextSpan right)
        {
            return new TextSpan(left.Start, right.End);
        }

        public bool Contains(int position)
		{
			return this.Span.Contains(position);
		}
		
        public bool Contains(TextPoint point)
		{
			EnsureCompatible(point.LineBreaks);
			return this.Span.Contains(point.Position);
		}
		
        public bool Contains(Span span)
		{
			return this.Span.Contains(span);
		}
		
        public bool Contains(TextSpan span)
		{
            EnsureCompatible(span.LineBreaks);
			return this.Span.Contains(span.Span);
		}
		
        public bool OverlapsWith(Span span)
		{
            return this.Span.OverlapsWith(span);
		}
        
        public bool OverlapsWith(TextSpan span)
		{
            EnsureCompatible(span.LineBreaks);
            return this.Span.OverlapsWith(span.Span);
		}
		
        public TextSpan? Overlap(Span span)
		{
            Span? overlap = this.Span.Overlap(span);
            if (overlap.HasValue)
                return new TextSpan?(new TextSpan(this.LineBreaks, overlap.Value));

            return null;
		}
        
        public TextSpan? Overlap(TextSpan span)
		{
            EnsureCompatible(span.LineBreaks);
            return this.Overlap(span.Span);
		}
        
        public bool IntersectsWith(Span span)
		{
            return this.Span.IntersectsWith(span);
		}
		
        public bool IntersectsWith(TextSpan span)
		{
            EnsureCompatible(span.LineBreaks);
            return this.Span.IntersectsWith(span.Span);
		}
		
        public TextSpan? Intersection(Span span)
		{
            Span? intersection = this.Span.Intersection(span);
            if (intersection.HasValue)
                return new TextSpan?(new TextSpan(this.LineBreaks, intersection.Value));

            return null;
		}
        
        public TextSpan? Intersection(TextSpan span)
		{
            EnsureCompatible(span.LineBreaks);
            return this.Intersection(span.Span);
		}
		
        public override int GetHashCode()
		{
			return this.Span.GetHashCode() ^ this.LineBreaks.GetHashCode();
		}
		
        public override string ToString()
		{
            return this.Span.ToString();
		}
		
        public override bool Equals(object obj)
		{
			if (obj is TextSpan)
			{
				TextSpan left = (TextSpan)obj;
				return left == this;
			}
			return false;
		}
		
        public static bool operator ==(TextSpan left, TextSpan right)
		{
            return left.Equals(right);
		}
		
        public static bool operator !=(TextSpan left, TextSpan right)
		{
            return !left.Equals(right);
		}

		private void EnsureCompatible(ILineBreaks lineBreaks)
		{
			if (this.LineBreaks != lineBreaks)
				throw new ArgumentException();
		}

        /// <summary>
        /// Gets portion of document defined by this <see cref="Span"/>.
        /// </summary>
        public string GetText(string document)
        {
            return this.Span.GetText(document);
        }

        #endregion

        #region IEquatable<TextSpan> Members

        public bool Equals(TextSpan other)
        {
            return this.LineBreaks == other.LineBreaks && this.Span == other.Span;
        }

        #endregion
    }
}
