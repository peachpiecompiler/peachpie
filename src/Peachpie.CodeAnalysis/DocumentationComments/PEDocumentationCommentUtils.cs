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
            ref KeyValuePair<CultureInfo, string> lazyDocComment)
        {
            // Have we cached anything?
            if (lazyDocComment.Value == null)
            {
                lazyDocComment = new KeyValuePair<CultureInfo, string>(
                    preferredCulture,
                    containingPEModule.DocumentationProvider.GetDocumentationForSymbol(
                            symbol.GetDocumentationCommentId(), preferredCulture, cancellationToken));
            }

            // Does the cached version match the culture we asked for?
            if (Equals(lazyDocComment.Key, preferredCulture))
            {
                return lazyDocComment.Value;
            }

            // We've already cached a different culture - create a fresh version.
            return containingPEModule.DocumentationProvider.GetDocumentationForSymbol(
                symbol.GetDocumentationCommentId(), preferredCulture, cancellationToken);
        }
    }
}
