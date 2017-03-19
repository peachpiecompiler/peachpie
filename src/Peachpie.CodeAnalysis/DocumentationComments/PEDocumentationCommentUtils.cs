using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.DocumentationComments
{
    internal static class PEDocumentationCommentUtils
    {
        internal static string GetDocumentationComment(
            Symbol symbol,
            PEModuleSymbol containingPEModule,
            CultureInfo preferredCulture,
            CancellationToken cancellationToken,
            ref Tuple<CultureInfo, string> lazyDocComment)
        {
            // Have we cached anything?
            if (lazyDocComment == null)
            {
                Interlocked.CompareExchange(
                    ref lazyDocComment,
                    Tuple.Create(
                        preferredCulture,
                        containingPEModule.DocumentationProvider.GetDocumentationForSymbol(
                            symbol.GetDocumentationCommentId(), preferredCulture, cancellationToken)),
                    null);
            }

            // Does the cached version match the culture we asked for?
            if (object.Equals(lazyDocComment.Item1, preferredCulture))
            {
                return lazyDocComment.Item2;
            }

            // We've already cached a different culture - create a fresh version.
            return containingPEModule.DocumentationProvider.GetDocumentationForSymbol(
                symbol.GetDocumentationCommentId(), preferredCulture, cancellationToken);
        }
    }
}
