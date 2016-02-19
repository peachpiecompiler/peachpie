using Pchp.Syntax;
using Pchp.Syntax.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis
{
    internal static class NameUtils
    {
        /// <summary>
        /// Combines name and its namespace.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="ns">Can be <c>null</c>.</param>
        /// <returns></returns>
        public static QualifiedName MakeQualifiedName(Name name, NamespaceDecl ns)
        {
            return (ns == null || ns.QualifiedName.Namespaces.Length == 0) ?
                new QualifiedName(name) :
                new QualifiedName(name, ns.QualifiedName.Namespaces);
        }

        /// <summary>
        /// Gets full qualified name of the type declaration.
        /// </summary>
        /// <param name="type">Type, cannot be <c>null</c>.</param>
        /// <returns>Qualified name of the type.</returns>
        public static QualifiedName MakeQualifiedName(this TypeDecl type)
        {
            return MakeQualifiedName(type.Name, type.Namespace);
        }

        /// <summary>
        /// Gets CLR name using dot as a name separator.
        /// </summary>
        public static string ClrName(this QualifiedName qname)
        {
            if (qname.IsSimpleName) return qname.Name.Value;

            var ns = string.Join(".", qname.Namespaces);

            if (!string.IsNullOrEmpty(qname.Name.Value))
                ns += "." + qname.Name.Value;

            return ns;
        }

        /// <summary>
        /// Compares two arrays.
        /// </summary>
        public static bool NamesEquals(this Name[] names1, Name[] names2)
        {
            if (names1.Length != names2.Length)
                return false;

            for (int i = 0; i < names1.Length; i++)
                if (names1[i] != names2[i])
                    return false;

            return true;
        }

        /// <summary>
        /// Gets value indicating whether given qualified name was not set.
        /// </summary>
        public static bool IsEmpty(this QualifiedName qname)
        {
            return qname.IsSimpleName && string.IsNullOrEmpty(qname.Name.Value);
        }

        /// <summary>
        /// Special PHP type and function names.
        /// </summary>
        public struct SpecialNames
        {
            public static QualifiedName ArrayAccess { get { return new QualifiedName(new Name("ArrayAccess")); } }
            public static QualifiedName Iterator { get { return new QualifiedName(new Name("Iterator")); } }
            public static QualifiedName Traversable { get { return new QualifiedName(new Name("Traversable")); } }
            public static QualifiedName Closure { get { return new QualifiedName(new Name("Closure")); } }
            public static QualifiedName Exception { get { return new QualifiedName(new Name("Exception")); } }

            public static Name offsetGet { get { return new Name("offsetGet"); } }
            public static Name offsetSet { get { return new Name("offsetSet"); } }
            public static Name current { get { return new Name("current"); } }
            public static Name __invoke { get { return new Name("__invoke"); } }
            public static Name __toString { get { return new Name("__toString"); } }
        }
    }
}
