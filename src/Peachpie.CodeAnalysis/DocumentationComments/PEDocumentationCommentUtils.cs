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
            ref (CultureInfo Culture, string XmlText) lazyDocComment)
        {
            // Have we cached anything?
            if (lazyDocComment.XmlText == null)
            {
                lazyDocComment = (
                    Culture: preferredCulture,
                    XmlText: containingPEModule.DocumentationProvider.GetDocumentationForSymbol(
                            symbol.GetDocumentationCommentId(), preferredCulture, cancellationToken));
            }

            // Does the cached version match the culture we asked for?
            if (Equals(lazyDocComment.Culture, preferredCulture))
            {
                return lazyDocComment.Item2;
            }

            // We've already cached a different culture - create a fresh version.
            return containingPEModule.DocumentationProvider.GetDocumentationForSymbol(
                symbol.GetDocumentationCommentId(), preferredCulture, cancellationToken);
        }
    }
}
