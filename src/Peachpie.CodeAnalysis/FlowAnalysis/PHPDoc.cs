using Devsense.PHP.Ast.DocBlock;
using Devsense.PHP.Syntax;
using Pchp.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Helpers class for resolving PHPDoc types.
    /// </summary>
    internal static class PHPDoc
    {
        public static IDocEntry GetDocEntry(this IDocBlock phpdoc, string tag)
        {
            if (phpdoc != null)
            {
                for (var entry = phpdoc.Entries; entry != null; entry = entry.Next)
                {
                    if (IsDocTag(entry, tag))
                    {
                        return entry;
                    }
                }
            }

            return null;
        }

        public static bool IsDocTag(this IDocEntry entry, string tag)
        {
            if (entry != null)
            {
                var str = entry.ToString();
                if (str.StartsWith(tag, StringComparison.Ordinal))
                {
                    var idx = tag.Length;
                    if (str.Length == idx || char.IsWhiteSpace(str[idx]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets parameter type from given PHPDoc block.
        /// </summary>
        public static IDocEntry GetParamTag(PHPDocBlock phpdoc, int paramIndex, string paramName)
        {
            if (phpdoc != null)
            {
                int pi = 0;
                for (var entry = phpdoc.Entries; entry != null; entry = entry.Next)
                {
                    if (IsDocTag(entry, "@param"))
                    {
                        if (entry.ToString().IndexOf(paramName) >= 0)
                        {
                            return entry;
                        }

                        //
                        pi++;
                    }
                }
            }

            return null;
        }

        static ReadOnlySpan<char> SliceWord(ReadOnlySpan<char> text)
        {
            var i = 0;
            while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
            while (i < text.Length && !char.IsWhiteSpace(text[i])) i++;

            return text.Slice(0, i);
        }

        public static bool GetEntryText(this IDocEntry entry, out string text)
        {
            if (entry != null)
            {
                var str = entry.ToString().AsSpan().Trim();

                // @tagname [text]
                
                // @tagname
                if (str.Length > 0 && str[0] == '@')
                {
                    var tagname = SliceWord(str);
                    str = str.Slice(tagname.Length).TrimStart();
                }

                // 
                text = str.ToString();
                return true;
            }

            text = null;
            return false;
        }

        public static bool GetEntryText(this IDocEntry entry,
            out string tagname,
            out string typename,
            out string varname,
            out string description)
        {
            tagname = typename = varname = description = null;

            if (entry != null)
            {
                var str = entry.ToString().AsSpan().Trim();

                // @tagname [typename] [$varname] [description]
                
                // @tagname
                if (str.Length > 0 && str[0] == '@')
                {
                    tagname = SliceWord(str).ToString();
                    str = str.Slice(tagname.Length).TrimStart();
                }
                
                // typename
                if (str.Length > 0 && str[0] != '$')
                {
                    typename = SliceWord(str).ToString();
                    str = str.Slice(typename.Length).TrimStart();
                }

                // $varname
                if (str.Length > 0 && str[0] == '$')
                {
                    varname = SliceWord(str).ToString();
                    str = str.Slice(varname.Length).TrimStart();
                }

                // 
                description = str.ToString();
                return true;
            }

            return false;
        }
    }
}
