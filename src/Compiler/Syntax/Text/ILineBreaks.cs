using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Pchp.Syntax.Text
{
    #region ILineBreaks

    /// <summary>
    /// Manages information about line breaks in the document.
    /// </summary>
    public interface  ILineBreaks
    {
        /// <summary>
        /// Gets amount of line breaks.
        /// </summary>
        /// <remarks>Lines count equals <see cref="Count"/> + <c>1</c>.</remarks>
        int Count { get; }

        /// <summary>
        /// Gets length of document.
        /// </summary>
        int TextLength { get; }

        /// <summary>
        /// Gets position of <paramref name="index"/>-th line end, including its break characters.
        /// </summary>
        /// <param name="index">Index of te line.</param>
        /// <returns>Position of the line end.</returns>
        int EndOfLineBreak(int index);

        /// <summary>
        /// Gets line number from <paramref name="position"/> within document.
        /// </summary>
        /// <param name="position">Position within document.</param>
        /// <returns>Line number.</returns>
        /// <exception cref="ArgumentOutOfRangeException">In case <paramref name="position"/> is out of line number range.</exception>
        int GetLineFromPosition(int position);

        /// <summary>
        /// Gets line and column from position number.
        /// </summary>
        /// <param name="position">Position with the document.</param>
        /// <param name="line">Line number.</param>
        /// <param name="column">Column nummber.</param>
        void GetLineColumnFromPosition(int position, out int line, out int column);
    }

    #endregion

    #region LineBreaks

    public abstract class LineBreaks : ILineBreaks
    {
        #region ILineBreaks Members

        public abstract int Count { get; }

        public abstract int EndOfLineBreak(int index);

        public int TextLength
        {
            get { return _textLength; }
        }

        /// <summary>
        /// Gets line number from <paramref name="position"/> within document.
        /// </summary>
        /// <param name="position">Position within document.</param>
        /// <returns>Line number.</returns>
        /// <exception cref="ArgumentOutOfRangeException">In case <paramref name="position"/> is out of text document range.</exception>
        public int GetLineFromPosition(int position)
        {
            if (position < 0 || position > this.TextLength)
                throw new ArgumentOutOfRangeException("position");
            
            //
            if (position == this.TextLength)
                return this.LinesCount - 1;
            
            // binary search
            int a = 0;
            int b = this.Count;
            while (a < b)
            {
                int x = (a + b) / 2;
                if (position < this.EndOfLineBreak(x))
                    b = x;
                else
                    a = x + 1;
            }
            return a;
        }

        public void GetLineColumnFromPosition(int position, out int line, out int column)
        {
            line = GetLineFromPosition(position);
            if (line == 0)
                column = position;
            else
                column = position - this.EndOfLineBreak(line - 1);
        }

        #endregion

        protected int _textLength;

        protected LineBreaks(int textLength)
        {
            _textLength = textLength;
        }

        public static LineBreaks/*!*/Create(string text)
        {
            return Create(text, CalculateLineEnds(text));
        }

        public static LineBreaks/*!*/Create(string text, List<int>/*!*/lineEnds)
        {
            if (text == null) throw new ArgumentNullException();
            return Create(text.Length, lineEnds);
        }

        internal static LineBreaks/*!*/Create(int textLength, List<int>/*!*/lineEnds)
        {
            if (textLength < 0) throw new ArgumentException();
            if (lineEnds == null) throw new ArgumentNullException();
            
            if (lineEnds.Count == 0 || lineEnds.Last() <= ushort.MaxValue)
            {
                return new ShortLineBreaks(textLength, lineEnds);
            }
            else
            {
                return new IntLineBreaks(textLength, lineEnds);
            }
        }

        /// <summary>
        /// Amount of lines in the document.
        /// </summary>
        public int LinesCount { get { return this.Count + 1; } }

        /// <summary>
        /// Gets list of line ends position.
        /// </summary>
        /// <param name="text">Document text.</param>
        /// <returns>List of line ends position.</returns>
        private static List<int>/*!*/CalculateLineEnds(string text)
        {
            List<int> list = new List<int>();
            if (text != null)
            {
                int i = 0;
                while (i < text.Length)
                {
                    int len = TextUtils.LengthOfLineBreak(text, i);
                    if (len == 0)
                    {
                        i++;
                    }
                    else
                    {
                        i += len;
                        list.Add(i);
                    }
                }
            }
            return list;
        }
    }

    #endregion

    #region ShortLineBreaks

    /// <summary>
    /// Optimized generalization of <see cref="LineBreaks"/> using <see cref="ushort"/> internally.
    /// </summary>
    internal sealed class ShortLineBreaks : LineBreaks
    {
        private readonly ushort[]/*!*/_lineEnds;

        public ShortLineBreaks(int textLength, List<int> lineEnds)
            :base(textLength)
        {
            var count = lineEnds.Count;
            if (count == 0)
            {
                _lineEnds = ArrayUtils.EmptyUShorts;
            }
            else
            {
                _lineEnds = new ushort[count];
                for (int i = 0; i < count; i++)
                    _lineEnds[i] = (ushort)lineEnds[i];
            }
        }

        public override int Count
        {
            get { return _lineEnds.Length; }
        }

        public override int EndOfLineBreak(int index)
        {
            return (int)_lineEnds[index];
        }
    }

    #endregion

    #region IntLineBreaks

    /// <summary>
    /// Generalization of <see cref="LineBreaks"/> using <see cref="int"/> internally.
    /// </summary>
    internal sealed class IntLineBreaks : LineBreaks
    {
        private readonly int[]/*!*/_lineEnds;

        public IntLineBreaks(int textLength, List<int> lineEnds)
            : base(textLength)
        {
            var count = lineEnds.Count;
            if (count == 0)
            {
                _lineEnds = ArrayUtils.EmptyIntegers;
            }
            else
            {
                _lineEnds = lineEnds.ToArray();
            }
        }

        public override int Count
        {
            get { return _lineEnds.Length; }
        }

        public override int EndOfLineBreak(int index)
        {
            return (int)_lineEnds[index];
        }
    }

    #endregion

    #region ExpandableLineBreaks

    /// <summary>
    /// Generalization of <see cref="LineBreaks"/> using <see cref="List{T}"/> internally.
    /// </summary>
    internal sealed class ExpandableLineBreaks : LineBreaks
    {
        private readonly List<int>/*!*/_lineEnds = new List<int>();

        public ExpandableLineBreaks()
            : base(0)
        {
        }

        public override int Count
        {
            get { return _lineEnds.Count; }
        }

        public override int EndOfLineBreak(int index)
        {
            return (int)_lineEnds[index];
        }

        public void Expand(char[] text, int from, int length)
        {
            int oldTextLength = _textLength;

            int i = from;
            int to = from + length;
            while (i < to)
            {
                int len = TextUtils.LengthOfLineBreak(text, i);
                if (len == 0)
                {
                    i++;
                }
                else
                {
                    i += len;
                    _lineEnds.Add(oldTextLength - from + i);
                }
            }

            //
            _textLength += length;
        }

        public LineBreaks/*!*/Finalize()
        {
            return LineBreaks.Create(_textLength, _lineEnds);
        }
    }

    #endregion

    //#region VirtualLineBreaks

    ///// <summary>
    ///// <see cref="ILineBreaks"/> implementation which is collecting line break information subsequently
    ///// and provides ability to shift resulting line and column.
    ///// </summary>
    //internal sealed class VirtualLineBreaks : ILineBreaks
    //{
    //    private readonly int lineShift, columnShift;
    //    private LineBreaks/*!*/lineBreaks;
    //    private ExpandableLineBreaks ExpandableLineBreaks { get { return (ExpandableLineBreaks)lineBreaks; } }

    //    public VirtualLineBreaks(LineBreaks lineBreaks, int lineShift, int columnShift)
    //    {
    //        this.lineShift = lineShift;
    //        this.columnShift = columnShift;
    //        this.lineBreaks = lineBreaks;
    //    }

    //    public VirtualLineBreaks(int lineShift, int columnShift)
    //        : this(new ExpandableLineBreaks(), lineShift, columnShift)
    //    {
    //    }

    //    /// <summary>
    //    /// Updates <see cref="TextLength"/> and line breaks with an additional piece of text.
    //    /// </summary>
    //    public void Expand(char[] text, int from, int length)
    //    {
    //        if (IsFinalized)
    //            throw new InvalidOperationException();

    //        this.ExpandableLineBreaks.Expand(text, from, length);
    //    }

    //    /// <summary>
    //    /// Compresses internal storage of line breaks and does not allow to expand any more.
    //    /// </summary>
    //    public ILineBreaks Finalize()
    //    {
    //        if (!IsFinalized)
    //            lineBreaks = this.ExpandableLineBreaks.Finalize();

    //        if (lineShift == 0 && columnShift == 0)
    //            return lineBreaks;
    //        else
    //            return this;
    //    }

    //    public bool IsFinalized { get { return !(lineBreaks is ExpandableLineBreaks); } }

    //    #region ILineBreaks Members

    //    public int Count
    //    {
    //        get { return lineBreaks.Count; }
    //    }

    //    public int TextLength
    //    {
    //        get { return lineBreaks.TextLength; }
    //    }

    //    public int EndOfLineBreak(int index)
    //    {
    //        return lineBreaks.EndOfLineBreak(index);
    //    }

    //    public int GetLineFromPosition(int position)
    //    {
    //        return lineBreaks.GetLineFromPosition(position) + lineShift;
    //    }

    //    public void GetLineColumnFromPosition(int position, out int line, out int column)
    //    {
    //        lineBreaks.GetLineColumnFromPosition(position, out line, out column);

    //        if (line == 0) column += columnShift;
    //        line += lineShift;
    //    }

    //    #endregion
    //}

    //#endregion
}
