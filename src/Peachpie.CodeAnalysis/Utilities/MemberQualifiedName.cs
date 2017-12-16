using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devsense.PHP.Syntax;

namespace Pchp.CodeAnalysis.Utilities
{
    /// <summary>
    /// Name of a member (method, property, field) within a type.
    /// </summary>
    [DebuggerDisplay("{_qn,nq}::{_name,nq}")]
    internal struct MemberQualifiedName : IEquatable<MemberQualifiedName>
    {
        readonly QualifiedName _qn;
        readonly Name _name;

        /// <summary>
        /// Containing type name.
        /// </summary>
        public QualifiedName TypeName => _qn;

        /// <summary>
        /// The member name.
        /// </summary>
        public Name MemberName => _name;

        public MemberQualifiedName(QualifiedName typename, Name membername)
        {
            _qn = typename;
            _name = membername;
        }

        public bool Equals(MemberQualifiedName other) => TypeName.Equals(other.TypeName) && MemberName.Equals(other.MemberName);

        public override int GetHashCode() => _qn.GetHashCode() ^ _name.GetHashCode();

        public override bool Equals(object obj) => obj is MemberQualifiedName m && Equals(m);
    }
}
