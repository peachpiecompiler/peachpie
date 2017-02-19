using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Devsense.PHP.Text;
using Devsense.PHP.Syntax.Ast;
using Devsense.PHP.Syntax;
using System.Diagnostics;

namespace Pchp.CodeAnalysis
{
    internal static class AstUtils
    {
        /// <summary>
        /// Fixes <see cref="ItemUse"/> so it propagates correctly through our visitor.
        /// </summary>
        /// <remarks><c>IsMemberOf</c> will be set on Array, not ItemUse itself.</remarks>
        public static void PatchItemUse(ItemUse item)
        {
            if (item.IsMemberOf != null)
            {
                var varlike = item.Array as VarLikeConstructUse;

                Debug.Assert(varlike != null);
                Debug.Assert(varlike.IsMemberOf == null);

                // fix this ast weirdness:
                varlike.IsMemberOf = item.IsMemberOf;
                item.IsMemberOf = null;
            }
        }

        /// <summary>
        /// Determines whether method has <c>$this</c> variable.
        /// </summary>
        public static bool HasThisVariable(MethodDecl method)
        {
            return method != null && (method.Modifiers & PhpMemberAttributes.Static) == 0;
        }

        public static Span BodySpanOrInvalid(this AstNode routine)
        {
            if (routine is FunctionDecl)
            {
                return ((FunctionDecl)routine).Body.Span;
            }
            if (routine is MethodDecl)
            {
                var node = (MethodDecl)routine;
                return (node.Body != null) ? node.Body.Span : Span.Invalid;
            }
            if (routine is LambdaFunctionExpr)
            {
                return ((LambdaFunctionExpr)routine).Body.Span;
            }
            else
            {
                return Span.Invalid;
            }
        }

        /// <summary>
        /// Gets <see cref="Microsoft.CodeAnalysis.Text.LinePosition"/> from source position.
        /// </summary>
        public static LinePosition LinePosition(this ILineBreaks lines, int pos)
        {
            int line, col;
            lines.GetLineColumnFromPosition(pos, out line, out col);

            return new LinePosition(line, col);
        }

        /// <summary>
        /// Attribute name determining the field below is app-static instead of context-static.
        /// </summary>
        public const string AppStaticTagName = "@appstatic";

        /// <summary>
        /// Lookups notation determining given field as app-static instead of context-static.
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public static bool IsAppStatic(this FieldDeclList field)
        {
            if (field != null && field.Modifiers.IsStatic())
            {
                var phpdoc = field.PHPDoc;
                if (phpdoc != null)
                {
                    return phpdoc.Elements
                        .OfType<PHPDocBlock.UnknownTextTag>()
                        .Any(t => t.TagName.Equals(AppStaticTagName, StringComparison.OrdinalIgnoreCase));
                }
            }

            return false;
        }

        /// <summary>
        /// Wraps given <see cref="Devsense.PHP.Text.Span"/> into <see cref="Microsoft.CodeAnalysis.Text.TextSpan"/> representing the same value.
        /// </summary>
        public static Microsoft.CodeAnalysis.Text.TextSpan ToTextSpan(this Devsense.PHP.Text.Span span)
        {
            return span.IsValid
                ? new Microsoft.CodeAnalysis.Text.TextSpan(span.Start, span.Length)
                : new Microsoft.CodeAnalysis.Text.TextSpan();
        }

        /// <summary>
        /// CLR compliant anonymous class name.
        /// </summary>
        public static string GetAnonymousTypeName(this AnonymousTypeDecl tdecl)
        {
            var fname = System.IO.Path.GetFileName(tdecl.ContainingSourceUnit.FilePath).Replace('.', '_');  // TODO: relative to app root
            // PHP: class@anonymous\0{FULLPATH}{BUFFER_POINTER,X8}
            return $"class@anonymous {fname}{tdecl.Span.Start.ToString("X4")}";
        }

        /// <summary>
        /// Builds qualified name for an anonymous PHP class.
        /// Instead of name provided by parser, we do create our own which is more readable and shorter.
        /// </summary>
        /// <remarks>Wherever <see cref="AnonymousTypeDecl.QualifiedName"/> would be used, use this method instead.</remarks>
        public static QualifiedName GetAnonymousTypeQualifiedName(this AnonymousTypeDecl tdecl)
        {
            return new QualifiedName(new Name(GetAnonymousTypeName(tdecl)));
        }
    }
}
