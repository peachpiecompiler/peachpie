using Devsense.PHP.Text;
using Microsoft.CodeAnalysis.Text;

namespace Peachpie.CodeAnalysis.Syntax
{
    /// <summary>
    /// Implements <see cref="ILineBreaks"/> of <see cref="SourceText"/>.
    /// </summary>
    sealed class SourceLineBreaks : LineBreaks
    {
        private readonly SourceText/*!*/_source;
        
        public SourceLineBreaks(SourceText source) : base(source.Length)
        {
            _source = source;
        }

        public override int EndOfLineBreak(int index) => _source.Lines[index].EndIncludingLineBreak;

        public override int Count => _source.Lines.Count - 1;
    }
}