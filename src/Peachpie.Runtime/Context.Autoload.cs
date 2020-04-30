#nullable enable

using Pchp.Core.Reflection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// An interface providing methods for the autoload mechanism in PHP.
    /// </summary>
    public interface IPhpAutoloadService
    {
        /// <summary>
        /// Performs autoload of given type name and gets its <see cref="PhpTypeInfo"/> (type descriptor).
        /// </summary>
        /// <param name="fullName">Full class, interface or trait name.</param>
        /// <returns>Type descriptor or a <c>null</c> reference if the process couldn't resolve the type.</returns>
        PhpTypeInfo? AutoloadTypeByName(string fullName);
    }

    partial class Context : IPhpAutoloadService
    {
        /// <summary>
        /// Default implementation of PHP autoload.
        /// </summary>
        PhpTypeInfo? IPhpAutoloadService.AutoloadTypeByName(string fullName) => DefaultAutoloadTypeByName(fullName) ?? ImplicitAutoloadTypeByName(fullName);

        /// <summary>
        /// Default implementation of PHP autoload looks for <c>__autoload</c> global function and calls it.
        /// </summary>
        PhpTypeInfo? DefaultAutoloadTypeByName(string fullName)
        {
            var autoload = _lazyAutoloadRoutine;
            if (autoload == null)
            {
                _lazyAutoloadRoutine = autoload = GetDeclaredFunction(AutoloadFunctionName);

                if (autoload == null)
                {
                    return null;
                }
            }

            // remove leading \ from the type name
            if (fullName.Length != 0 && fullName[0] == '\\')
            {
                fullName = fullName.Substring(1);
            }

            //
            using (var token = new RecursionCheckToken(this, fullName.ToLowerInvariant()))
            {
                if (!token.IsInRecursion)
                {
                    // CALL __autoload(fullName)
                    autoload.PhpCallable(this, new PhpValue[] { (PhpValue)fullName });

                    //
                    return GetDeclaredType(fullName);
                }
            }

            //
            return null;
        }

        /// <summary>
        /// Performs autoload from class map which is constructed in build time from specified class map and psr-4 schemas.
        /// </summary>
        public PhpTypeInfo? AutoloadByTypeNameFromClassMap(string fullName, bool onlyAllowed)
        {
            if (fullName == null) throw new ArgumentNullException(nameof(fullName));

            Debug.Assert(fullName.Length != 0 && fullName[0] != '\\');

            if (s_typeMap.TryGetType(fullName, out var tinfo, out var autoload) && tinfo != null)
            {
                if (onlyAllowed && autoload == 0)
                {
                    // type was not marked as to be autoloaded
                    return null;
                }

                if (autoload == PhpTypeAttribute.AutoloadAllowNoSideEffect)
                {
                    _types.DeclareType(tinfo);
                    return tinfo;
                }
                else
                {
                    var script = ScriptsMap.GetDeclaredScript(tinfo.RelativePath);

                    // pretend we are PHP and include the script:

                    if (script.IsValid && !_scripts.IsIncluded(script.Index))   // include_once:
                    {
                        script.Evaluate(this, this.Globals, null);

                        return GetDeclaredType(fullName, autoload: false);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Implicit autoload mechanism working through CLR reflection.
        /// This ensures when even the caller does not define PHP class autoloading, we allows seamless use of PHP classes and PHP program.
        /// This mechanism gets enabled by default, disabled only when SPL autoload gets initiated (or anything that sets own <see cref="Context.AutoloadService"/>).
        /// </summary>
        PhpTypeInfo? ImplicitAutoloadTypeByName(string fullName)
        {
            return AutoloadByTypeNameFromClassMap(fullName, onlyAllowed: !EnableImplicitAutoload);
        }

        /// <summary>
        /// Map of all PHP types in loaded assemblies.
        /// </summary>
        static readonly AutoloadClassMap s_typeMap = new AutoloadClassMap();

        /// <summary>
        /// Default autoload function in PHP to be used when there is no autoload service.
        /// </summary>
        const string AutoloadFunctionName = "__autoload";

        /// <summary>
        /// Lazily resolved <c>__autoload</c> routine in current context.
        /// </summary>
        RoutineInfo _lazyAutoloadRoutine;

        /// <summary>
        /// Gets instance of <see cref="IPhpAutoloadService"/> to be used for resolving autoloads.
        /// Cannot get a <c>null</c> reference.
        /// </summary>
        public virtual IPhpAutoloadService AutoloadService
        {
            get { return _autoloadService ?? this; }
            set { _autoloadService = value; }
        }
        IPhpAutoloadService _autoloadService;
    }
}
