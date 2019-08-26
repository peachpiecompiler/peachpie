using Pchp.Core;
using System;
using System.Diagnostics;

namespace Pchp.Library.Reflection
{
    /// <summary>
    /// Helper class that resolves a unique ID for aliased values.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(ReflectionUtils.ExtensionName)]
    [DebuggerDisplay("ReflectionReference (reference identifier:'{getId()}')")]
    public sealed class ReflectionReference
    {
        readonly PhpAlias _alias;

        private ReflectionReference(PhpAlias alias)
        {
            _alias = alias ?? throw new ArgumentNullException(nameof(alias));
        }

        /// <summary>
        /// Returns <see cref="ReflectionReference"/> if array element is a reference, <c>null</c> otherwise.
        /// </summary>
        public static ReflectionReference fromArrayElement(PhpArray array, IntStringKey/*int|string*/key)
        {
            if (array != null && array.TryGetValue(key, out var value) && value.Object is PhpAlias alias && alias.ReferenceCount > 0)
            {
                return new ReflectionReference(alias);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns unique identifier for the reference.
        /// </summary>
        public int getId() => Spl.SplObjects.object_hash_internal(_alias);
    }
}
