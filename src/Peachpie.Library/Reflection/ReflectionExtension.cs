using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pchp.Core;

namespace Pchp.Library.Reflection
{
    /// <summary>
    /// Information about an extension.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(ReflectionUtils.ExtensionName)]
    public class ReflectionExtension : Reflector
    {
        /// <summary>
        /// Name of the extension.
        /// </summary>
        public string name { get; private set; }

        [PhpFieldsOnlyCtor]
        protected ReflectionExtension()
        {
            // to be used by deriving class
        }

        public ReflectionExtension(string name)
        {
            __construct(name);
        }

        /// <summary>
        /// Clones.
        /// </summary>
        private ReflectionExtension __clone() => this;

        /// <summary>
        /// Constructs a ReflectionExtension.
        /// </summary>
        public void __construct(string name)
        {
            if (!Context.IsExtensionLoaded(name))
            {
                // ???
            }

            this.name = name ?? throw new ReflectionException();
        }

        /// <summary>
        /// Export.
        /// </summary>
        public static string export(Context ctx, string name, bool @return = false)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets classes.
        /// </summary>
        [return: NotNull]
        public PhpArray getClasses()
        {
            var result = new PhpArray();

            foreach (var tinfo in Context.GetTypesByExtension(name))
            {
                result.Add(tinfo.Name, PhpValue.FromClass(new ReflectionClass(tinfo)));
            }

            return result;
        }

        /// <summary>
        /// Gets class names.
        /// </summary>
        [return: NotNull]
        public PhpArray getClassNames()
        {
            return new PhpArray(Context.GetTypesByExtension(name).Select(tinfo => tinfo.Name));
        }

        /// <summary>
        /// Gets constants.
        /// </summary>
        [return: NotNull]
        public PhpArray getConstants(Context ctx)
        {
            var result = new PhpArray();

            foreach (var c in ctx.GetConstants())
            {
                if (string.Equals(c.ExtensionName, this.name, StringComparison.OrdinalIgnoreCase))
                {
                    result[c.Name] = c.Value;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets dependencies.
        /// </summary>
        [return: NotNull]
        public PhpArray getDependencies() { throw new NotImplementedException(); }

        /// <summary>
        /// Gets function names.
        /// </summary>
        [return: NotNull]
        public PhpArray getFunctions()
        {
            var result = new PhpArray();

            foreach (var routine in Context.GetRoutinesByExtension(name))
            {
                result.Add(routine.Name, PhpValue.FromClass(new ReflectionFunction(routine)));
            }

            return result;
        }
        public PhpArray getINIEntries() { throw new NotImplementedException(); }

        /// <summary>
        /// Gets extension name.
        /// </summary>
        [return: NotNull]
        public string getName() => name;

        /// <summary>
        /// Gets extension version.
        /// </summary>
        [return: NotNull]
        public string getVersion() { throw new NotImplementedException(); }

        /// <summary>
        /// Print extension info.
        /// </summary>
        public void info(Context ctx)
        {
            // TODO: print extension info

            ctx.Echo(name);
        }

        /// <summary>
        /// Returns whether this extension is persistent.
        /// </summary>
        public bool isPersistent() => true;

        /// <summary>
        /// Returns whether this extension is temporary.
        /// </summary>
        public bool isTemporary() => false;

        /// <summary>
        /// Exports the extension and returns it.
        /// </summary>
        [return: NotNull]
        public string __toString() => name;
    }
}
