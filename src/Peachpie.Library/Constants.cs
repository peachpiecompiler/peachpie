using Pchp.Core;
using Pchp.Core.Reflection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Library
{
    /// <summary>
	/// Implements PHP function over constants.
	/// </summary>
	/// <threadsafety static="true"/>
    [PhpExtension("standard", "Core")]
    public static class Constants
    {
        /// <summary>
        /// Defines a constant.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="name">The name of the constant. Can be arbitrary string.</param>
        /// <param name="value">The value of the constant. Can be <B>null</B> or a scalar or array.</param>
        /// <param name="caseInsensitive">Whether the name is case insensitive.</param>
        /// <returns>Whether the new constant has been defined.</returns>
        public static bool define(Context ctx, string name, PhpValue value, bool caseInsensitive = false)
            => ctx.DefineConstant(name, value, caseInsensitive);

        /// <summary>
        /// Resolves the constant and gets its value if possible.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="callerCtx">Current class scope.</param>
        /// <param name="this">Current <c>$this</c> value. Used to resolve <c>static::</c>.</param>
        /// <param name="name">The name of the constant. Might be a class constant.</param>
        /// <param name="value">The constant value or <see cref="PhpValue.Null"/> if constant was not resolved.</param>
        /// <returns>Whether the constant was resolved.</returns>
        /// <exception cref="Exception">(Error) if <c>static::</c> used out of the class scope.</exception>
        static bool TryGetConstant(Context ctx, [ImportValue(ImportValueAttribute.ValueSpec.CallerClass)]RuntimeTypeHandle callerCtx, object @this, string name, out PhpValue value)
        {
            if (!string.IsNullOrEmpty(name))
            {
                var sepidx = name.IndexOf(':');
                if (sepidx < 0)
                {
                    // a global constant
                    return ctx.TryGetConstant(name, out value);
                }
                else if (sepidx + 1 < name.Length && name[sepidx + 1] == ':')
                {
                    // a class constant:

                    // cut the type name only, 
                    // eventually trim the leading backslash:
                    var tname = name[0] == '\\' ? name.Substring(1, sepidx - 1) : name.Remove(sepidx);

                    PhpTypeInfo tinfo;

                    if (tname.EqualsOrdinalIgnoreCase("static"))
                    {
                        if (@this != null)
                        {
                            tinfo = @this.GetPhpTypeInfo();
                        }
                        else
                        {
                            // TODO: when called in a static class, we falsely report the error
                            throw PhpException.ErrorException(Core.Resources.ErrResources.static_used_out_of_class);
                        }
                    }
                    else
                    {
                        tinfo = ctx.ResolveType(tname, callerCtx, true);
                    }

                    if (tinfo != null)
                    {
                        var p = tinfo.GetDeclaredConstant(name.Substring(sepidx + 2));
                        if (p != null)
                        {
                            value = p.GetValue(ctx, null);
                            return true;
                        }
                    }
                }
            }

            //
            value = PhpValue.Null;
            return false;
        }

        /// <summary>
        /// Determines whether a constant is defined.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="callerCtx">type of caller class. Used to resolve reserved type names if used in <paramref name="name"/>.</param>
        /// <param name="this">Optional. Reference to <c>$this</c> object. Used to resolve <c>static::</c> type reference.</param>
        /// <param name="name">The name of the constant. Might be a class constant.</param>
        /// <returns>Whether the constant is defined.</returns>
        public static bool defined(Context ctx, [ImportValue(ImportValueAttribute.ValueSpec.CallerClass)]RuntimeTypeHandle callerCtx, [ImportValue(ImportValueAttribute.ValueSpec.This)] object @this, string name)
        {
            return TryGetConstant(ctx, callerCtx, @this, name, out _);
        }

        /// <summary>
        /// Retrieves a value of a constant.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="callerCtx">type of caller class. Used to resolve reserved type names if used in <paramref name="name"/>.</param>
        /// <param name="name">The name of the constant.</param>
        /// <param name="this">Optional. Reference to <c>$this</c> object. Used to resolve <c>static::</c> type reference.</param>
        /// <returns>The value.</returns>
        public static PhpValue constant(Context ctx, [ImportValue(ImportValueAttribute.ValueSpec.CallerClass)]RuntimeTypeHandle callerCtx, [ImportValue(ImportValueAttribute.ValueSpec.This)] object @this, string name)
        {
            if (!TryGetConstant(ctx, callerCtx, @this, name, out var value))
            {
                PhpException.Throw(PhpError.Warning, Core.Resources.ErrResources.constant_not_found, name);
            }

            //
            return value;
        }

        /// <summary>
        /// Retrieves defined constants.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="categorize">Returns a multi-dimensional array with categories in the keys of the first dimension and constants and their values in the second dimension. </param>
        /// <returns>Retrives the names and values of all the constants currently defined.</returns>
        public static PhpArray get_defined_constants(Context ctx, bool categorize = false)
        {
            var result = new PhpArray();

            foreach (var c in ctx.GetConstants())
            {
                if (categorize)
                {
                    var extensionName = c.IsUser ? "user" : c.ExtensionName;
                    if (extensionName != null)
                    {
                        result
                            .EnsureItemArray((IntStringKey)extensionName)
                            .SetItemValue((IntStringKey)c.Name, c.Value);
                    }
                    else
                    {
                        // constant is uncategorized
                        Debug.WriteLine($"constant {c.Name} is uncategoried");
                    }
                }
                else
                {
                    result.Add(c.Name, c.Value);
                }
            }

            //
            return result;
        }
    }
}
