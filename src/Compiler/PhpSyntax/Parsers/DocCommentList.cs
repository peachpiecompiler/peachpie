using Pchp.Syntax.AST;
using Pchp.Syntax.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Pchp.Syntax.Parsers
{
    /// <summary>
    /// Helper class containing list of DOC comments during tokenization.
    /// Provides searching for DOC comment above given position.
    /// </summary>
    internal class DocCommentList
    {
        private struct DocInfo
        {
            /// <summary>
            /// DOC comment instance.
            /// </summary>
            public PHPDocBlock PhpDoc;

            /// <summary>
            /// DOC comment span including following whitespace.
            /// </summary>
            public Span Extent;
        }

        #region Fields & Properties

        /// <summary>
        /// Ordered list of DOC comments. Can be <c>null</c>.
        /// </summary>
        private List<DocInfo> _doclist;

        /// <summary>
        /// Extent of included DOC comments span.
        /// </summary>
        public Span Extent { get { return _span; } }
        private Span _span = Span.Invalid;

        #endregion

        /// <summary>
        /// Inserts DOC block into the list.
        /// </summary>
        public void AppendBlock(PHPDocBlock/*!*/phpDoc, int endPosition)
        {
            Debug.Assert(phpDoc != null);
            Debug.Assert(endPosition >= phpDoc.Span.End);
            Debug.Assert(_doclist == null || _doclist.Count == 0 || _doclist.Last().Extent.Start < phpDoc.Span.Start, "Blocks have to be appended in order.");

            var docinfo = new DocInfo()
            {
                PhpDoc = phpDoc,
                Extent = Span.FromBounds(phpDoc.Span.Start, endPosition)
            };

            var list = _doclist;
            if (list == null)
            {
                _doclist = list = new List<DocInfo>(4);
            }
            
            list.Add(docinfo);

            _span = Span.FromBounds(list[0].Extent.Start, list.Last().Extent.End);
        }

        /// <summary>
        /// Finds DOC comment above given position, removes it from the internal list and returns its reference.
        /// </summary>
        public bool TryReleaseBlock(int position, out PHPDocBlock phpdoc)
        {
            var index = this.FindIndex(position - 1);
            if (index >= 0)
            {
                var list = _doclist;
                phpdoc = list[index].PhpDoc;
                list.RemoveAt(index);
                this.UpdateSpan();

                //
                return true;
            }

            //
            phpdoc = null;
            return false;
        }

        /// <summary>
        /// Finds DOC comment at given position and annotates statement with it.
        /// </summary>
        public void Annotate(IDeclarationElement element)
        {
            Annotate((LangElement)element, element.EntireDeclarationSpan.Start);
        }

        /// <summary>
        /// Merges DOC comments into the list of statements.
        /// </summary>
        /// <param name="extent">Span of code block containing <paramref name="stmts"/>.</param>
        /// <param name="stmts">List of statements to be merged with overlapping DOC comments.</param>
        public void Merge(Text.Span extent, IList<Statement>/*!*/stmts)
        {
            if (!_span.IsValid || !extent.IsValid)
                return;

            Debug.Assert(extent.IsValid);
            Debug.Assert(stmts != null);

            if (_span.OverlapsWith(extent))
            {
                var list = _doclist;
                int insertAt = 0;
                int count = 0;

                DocInfo doc;
                int indexFrom = FindFirstIn(list, extent);
                
                for (var index = indexFrom; index < list.Count && (doc = list[index]).Extent.OverlapsWith(extent); index++)
                {
                    // skip stmts fully above {doc}
                    while (insertAt < stmts.Count && stmts[insertAt].Span.End < doc.Extent.End)
                    {
                        insertAt++;
                    }

                    // insert {doc} into {stmts}
                    if (insertAt == stmts.Count || stmts[insertAt].Span.Start >= doc.Extent.End)
                    {
                        stmts.Insert(insertAt, new PHPDocStmt(doc.PhpDoc));
                        insertAt++;
                    }

                    //
                    count++;
                }

                //
                if (count != 0)
                {
                    list.RemoveRange(indexFrom, count);
                    this.UpdateSpan();
                }
            }
        }

        private void UpdateSpan()
        {
            var list = _doclist;
            _span = (list == null || list.Count == 0)
                    ? Span.Invalid
                    : Span.FromBounds(list[0].Extent.Start, list.Last().Extent.End);
        }

        /// <summary>
        /// Finds index of DOC comment at given position.
        /// </summary>
        private int FindIndex(int position)
        {
            if (_span.Contains(position))
            {
                Debug.Assert(_doclist != null);
                int index = FindIndex(_doclist, position);
                if (index >= 0 && _doclist[index].Extent.Contains(position))
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>
        /// Binary search.
        /// </summary>
        private static int FindIndex(List<DocInfo>/*!*/list, int position)
        {
            int a = 0, b = list.Count - 1;
            while (a <= b)
            {
                int x = (a + b) >> 1;
                var doc = list[x];
                if (doc.Extent.Contains(position))
                    return x;

                if (position > doc.Extent.Start)
                    a = x + 1;
                else
                    b = x - 1;
            }

            return -1;
        }

        /// <summary>
        /// Gets lowest index of DOC comment that intersects given span. Returns count of items if nothing was found.
        /// </summary>
        private static int FindFirstIn(List<DocInfo>/*!*/list, Span span)
        {
            Debug.Assert(span.IsValid);
            Debug.Assert(list != null);

            int result = list.Count;
            int a = 0, b = result - 1;
            while (a <= b)
            {
                // modified binary search to find lowest index of element within {span}

                int x = (a + b) >> 1;
                var doc = list[x];
                if (doc.Extent.Start >= span.End)
                {
                    b = x - 1;
                }
                else if (doc.Extent.End <= span.Start)
                {
                    a = x + 1;
                }
                else
                {
                    result = x;
                    b = x - 1;  // and try lower
                }
            }

            //
            return result;
        }

        /// <summary>
        /// Finds DOC comment at given position and annotates statement with it.
        /// </summary>
        private void Annotate(LangElement/*!*/stmt, int start)
        {
            PHPDocBlock phpdoc;
            if (TryReleaseBlock(start, out phpdoc))
            {
                stmt.SetPHPDoc(phpdoc);
            }
        }
    }
}
