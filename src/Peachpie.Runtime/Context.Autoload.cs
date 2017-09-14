using Pchp.Core.Reflection;
using System;
using System.Collections.Generic;
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
        PhpTypeInfo AutoloadTypeByName(string fullName);
    }

    partial class Context : IPhpAutoloadService
    {
        /// <summary>
        /// Default implementation of PHP autoload looks for <c>__autoload</c> global function and calls it.
        /// </summary>
        PhpTypeInfo IPhpAutoloadService.AutoloadTypeByName(string fullName)
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
