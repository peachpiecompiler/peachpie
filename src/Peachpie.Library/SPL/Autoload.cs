using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pchp.Core.Reflection;
using System.Diagnostics;

namespace Pchp.Library.Spl
{
    [PhpExtension(SplExtension.Name)]
    public static class Autoload
    {
        #region SplAutoloadService

        sealed class SplAutoloadService : IPhpAutoloadService
        {
            readonly Context _ctx;
            readonly LinkedList<IPhpCallable> _autoloaders = new LinkedList<IPhpCallable>();

            public SplAutoloadService(Context ctx)
            {
                Debug.Assert(ctx != null);
                _ctx = ctx;
            }

            /// <summary>
            /// List of autoload callbacks.
            /// </summary>
            public LinkedList<IPhpCallable> Autoloaders => _autoloaders;

            /// <summary>
            /// Array of autoload extensions.
            /// </summary>
            public string[] AutoloadExtensions = DefaultAutoloadExtensions;

            internal static string[] DefaultAutoloadExtensions = new[] { ".php" };

            /// <summary>
            /// Finds node within <see cref="_autoloaders"/> matches given callback.
            /// </summary>
            public LinkedListNode<IPhpCallable> FindAutoload(IPhpCallable autoloadFunction)
            {
                if (autoloadFunction != null)
                {
                    for (var node = _autoloaders.First; node != null; node = node.Next)
                    {
                        if (object.ReferenceEquals(node.Value, autoloadFunction)
                            || node.Value.Equals(autoloadFunction))
                        {
                            return node;
                        }
                    }
                }

                return null;
            }

            /// <summary>
            /// Performs autoload.
            /// </summary>
            public PhpTypeInfo AutoloadTypeByName(string fullName)
            {
                // remove leading \ from the type name
                if (fullName.Length != 0 && fullName[0] == '\\')
                {
                    fullName = fullName.Substring(1);
                }

                //
                var resolved = _ctx.AutoloadByTypeNameFromClassMap(fullName, onlyAllowed: true);
                if (resolved == null)
                {
                    using (var token = new Context.RecursionCheckToken(_ctx, fullName))
                    {
                        if (!token.IsInRecursion)
                        {
                            var args = new[] { (PhpValue)fullName };

                            for (var node = _autoloaders.First; node != null && resolved == null; node = node.Next)
                            {
                                node.Value.Invoke(_ctx, args);
                                resolved = _ctx.GetDeclaredType(fullName);
                            }
                        }
                    }
                }

                //
                return resolved;
            }
        }

        #endregion

        #region SplAutoloadCallback

        /// <summary>
        /// A callable implementation invoking <see cref="spl_autoload(Context, string)"/>.
        /// </summary>
        sealed class SplAutoloadCallback : PhpCallback
        {
            public readonly static SplAutoloadCallback Instance = new SplAutoloadCallback();

            private SplAutoloadCallback() { }

            public override PhpValue ToPhpValue() => PhpValue.Create(SplAutoloadFunction);

            protected override PhpCallable BindCore(Context ctx) => (_ctx, args) =>
            {
                spl_autoload(_ctx, args[0].ToStringOrThrow(_ctx));
                return PhpValue.Null;
            };
        }

        #endregion

        #region Constants

        /// <summary>
        /// The name of <c>spl_autoload</c> default function.
        /// </summary>
        const string SplAutoloadFunction = "spl_autoload";

        #endregion

        #region spl_autoload_call, spl_autoload_extensions, spl_autoload_functions, spl_autoload_register, spl_autoload_unregister, spl_autoload

        /// <summary>
        /// This function can be used to manually search for a class or interface using the registered __autoload functions.
        /// </summary>
        public static void spl_autoload_call(Context ctx, string className)
        {
            // If class isn't defined autoload functions are called automatically until class is declared
            var autoload = ctx.GetSplAutoload();
            if (autoload != null && ctx.GetDeclaredType(className) == null)
            {
                autoload.AutoloadTypeByName(className);
            }
        }

        /// <summary>
        /// Gets comma separated list of extensions for the default spl autoload.
        /// </summary>
        public static string spl_autoload_extensions(Context ctx)
        {
            var splautoload = ctx.GetSplAutoload();
            return string.Join(",", (splautoload != null) ? splautoload.AutoloadExtensions : SplAutoloadService.DefaultAutoloadExtensions);
        }

        /// <summary>
        /// Sets comma separated list of extensions for the default spl autoload.
        /// </summary>
        public static string spl_autoload_extensions(Context ctx, string fileExtensions)
        {
            ctx.EnsureSplAutoload().AutoloadExtensions = fileExtensions.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            return fileExtensions;
        }

        /// <summary>
        /// Default SPL autoload that tries to load scripts with name of class name concatenated with autoload extensions.
        /// </summary>
        public static void spl_autoload(Context ctx, string className)
        {
            var extensions = ctx.GetSplAutoload()?.AutoloadExtensions ?? SplAutoloadService.DefaultAutoloadExtensions;
            PhpTypeInfo resolved = null;

            for (int i = 0; (resolved = ctx.GetDeclaredType(className)) == null && i < extensions.Length; i++)
            {
                var ext = extensions[i];

                // try to dynamically include the file specified by the class name, if it exists
                string fileName = className + ext;
                ctx.Include(null, fileName, true, false);
            }

            if (resolved == null)
            {
                //PhpException.Throw(PhpError.Error, string.Format(CoreResources.class_could_not_be_loaded, className));
                throw new InvalidOperationException("class_could_not_be_loaded");
            }
        }

        /// <summary>
        /// Gets array of registered autoload functions.
        /// Gets <c>false</c> if SPL autoload was not enabled.
        /// </summary>
        [return: CastToFalse]
        public static PhpArray spl_autoload_functions(Context ctx)
        {
            var autoload = ctx.GetSplAutoload();
            if (autoload != null)
            {
                PhpArray result = new PhpArray();
                foreach (var func in autoload.Autoloaders)
                {
                    result.Add(func.ToPhpValue());
                }

                return result;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Registers the default autoload function.
        /// </summary>
        public static bool spl_autoload_register(Context ctx) => spl_autoload_register(ctx, SplAutoloadCallback.Instance, true, false);

        /// <summary>
        /// Registers autoload function.
        /// </summary>
        public static bool spl_autoload_register(Context ctx, IPhpCallable autoloadFunction, bool throwError = true, bool prepend = false)
        {
            if (autoloadFunction == null)
            {
                //PhpException.ArgumentNull("autoloadFunction");
                //return false;
                throw new ArgumentNullException(nameof(autoloadFunction));
            }

            if (autoloadFunction is PhpCallback && !((PhpCallback)autoloadFunction).IsValid)
            {
                return false;
            }

            var autoload = ctx.EnsureSplAutoload();
            if (autoload.FindAutoload(autoloadFunction) != null)
            {
                return false;
            }

            if (prepend)
                autoload.Autoloaders.AddFirst(autoloadFunction);
            else
                autoload.Autoloaders.AddLast(autoloadFunction);

            return true;
        }

        /// <summary>
        /// Unregisteres the autoload function.
        /// </summary>
        public static bool spl_autoload_unregister(Context ctx, IPhpCallable autoloadFunction)
        {
            var functionNode = FindAutoloadFunction(ctx, autoloadFunction);
            if (functionNode != null)
            {
                functionNode.List.Remove(functionNode);
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region helpers

        static SplAutoloadService GetSplAutoload(this Context ctx) => ctx.AutoloadService as SplAutoloadService;

        /// <summary>
        /// Ensures SPL autoload mechanism is installed in Context.
        /// </summary>
        static SplAutoloadService EnsureSplAutoload(this Context ctx)
        {
            var autoload = ctx.AutoloadService as SplAutoloadService;
            if (autoload == null)
            {
                ctx.AutoloadService = autoload = new SplAutoloadService(ctx);
            }

            return autoload;
        }

        /// <summary>
        /// Finds the specified autoload function list element.
        /// </summary>
        /// <param name="ctx">Current script context.</param>
        /// <param name="autoloadFunction">The PHP representation of callback function to find in list of SPL autoload functions.</param>
        /// <returns>List node or null if such a functions does not exist in the list.</returns>
        private static LinkedListNode<IPhpCallable> FindAutoloadFunction(Context ctx, IPhpCallable autoloadFunction)
        {
            return ctx.GetSplAutoload()?.FindAutoload(autoloadFunction);
        }

        #endregion
    }
}
