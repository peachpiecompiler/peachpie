using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using System.Reflection.Emit;
using Pchp.Syntax.Parsers;

namespace Pchp.Syntax
{
	// 
	//  Identifier            Representation
	// --------------------------------------------------------------------
	//  variable, field       VariableName     (case-sensitive)
	//  class constant        VariableName     (case-sensitive)
	//  namespace constant    QualifiedName    (case-sensitive)
	//  method                Name             (case-insensitive)
	//  class, function       QualifiedName    (case-insensitive)
    //  primitive type        PrimitiveTypeName(case-insensitive)
	//  namespace component   Name             (case-sensitive?)
	//  label                 VariableName     (case-sensitive?)
	//

	#region Name

	/// <summary>
	/// Case-insensitive culture-sensitive (TODO ???) simple name in Unicode C normal form.
	/// Used for names of methods and namespace components.
	/// </summary>
	[DebuggerNonUserCode]
	public struct Name : IEquatable<Name>, IEquatable<string>
	{
		public string/*!*/ Value
		{
			get { return value; }
		}
		private readonly string/*!*/ value;
        private readonly int hashCode;
        
		#region Special Names

		public static readonly Name[] EmptyNames = new Name[0];
		public static readonly Name EmptyBaseName = new Name("");
		public static readonly Name SelfClassName = new Name("self");
        public static readonly Name StaticClassName = new Name("static");
		public static readonly Name ParentClassName = new Name("parent");
		public static readonly Name AutoloadName = new Name("__autoload");
		public static readonly Name ClrCtorName = new Name(".ctor");
		public static readonly Name ClrInvokeName = new Name("Invoke"); // delegate Invoke method
		public static readonly Name AppStaticName = new Name("AppStatic");
		public static readonly Name AppStaticAttributeName = new Name("AppStaticAttribute");
		public static readonly Name ExportName = new Name("Export");
		public static readonly Name ExportAttributeName = new Name("ExportAttribute");
        public static readonly Name DllImportAttributeName = new Name("DllImportAttribute");
        public static readonly Name DllImportName = new Name("DllImport");
		public static readonly Name OutAttributeName = new Name("OutAttribute");
		public static readonly Name OutName = new Name("Out");
		public static readonly Name DeclareHelperName = new Name("<Declare>");
		public static readonly Name LambdaFunctionName = new Name("<Lambda>");
        public static readonly Name ClosureFunctionName = new Name("{closure}");

        #region SpecialMethodNames

        /// <summary>
        /// Contains special (or &quot;magic&quot;) method names.
        /// </summary>
        public static class SpecialMethodNames
        {
            /// <summary>Constructor.</summary>
            public static readonly Name Construct = new Name("__construct");

            /// <summary>Destructor.</summary>
            public static readonly Name Destruct = new Name("__destruct");

            /// <summary>Invoked when cloning instances.</summary>
            public static readonly Name Clone = new Name("__clone");

            /// <summary>Invoked when casting to string.</summary>
            public static readonly Name Tostring = new Name("__tostring");

            /// <summary>Invoked when serializing instances.</summary>
            public static readonly Name Sleep = new Name("__sleep");

            /// <summary>Invoked when deserializing instanced.</summary>
            public static readonly Name Wakeup = new Name("__wakeup");

            /// <summary>Invoked when an unknown field is read.</summary>
            public static readonly Name Get = new Name("__get");

            /// <summary>Invoked when an unknown field is written.</summary>
            public static readonly Name Set = new Name("__set");

            /// <summary>Invoked when an unknown method is called.</summary>
            public static readonly Name Call = new Name("__call");

            /// <summary>Invoked when an object is called like a function.</summary>
            public static readonly Name Invoke = new Name("__invoke");

            /// <summary>Invoked when an unknown method is called statically.</summary>
            public static readonly Name CallStatic = new Name("__callStatic");

            /// <summary>Invoked when an unknown field is unset.</summary>
            public static readonly Name Unset = new Name("__unset");

            /// <summary>Invoked when an unknown field is tested for being set.</summary>
            public static readonly Name Isset = new Name("__isset");
        };

        #endregion

        /// <summary>
        /// Name suffix of attribute class name.
        /// </summary>
        internal const string AttributeNameSuffix = "Attribute";

		public bool IsCloneName
		{
			get { return this.Equals(SpecialMethodNames.Clone); }
		}

		public bool IsConstructName
		{
			get { return this.Equals(SpecialMethodNames.Construct); }
		}

		public bool IsDestructName
		{
			get { return this.Equals(SpecialMethodNames.Destruct); }
		}

        public bool IsCallName
        {
            get { return this.Equals(SpecialMethodNames.Call); }
        }

        public bool IsCallStaticName
        {
            get { return this.Equals(SpecialMethodNames.CallStatic); }
        }

        public bool IsToStringName
        {
            get { return this.Equals(SpecialMethodNames.Tostring); }
        }

        public bool IsParentClassName
        {
            get { return this.Equals(Name.ParentClassName); }
        }

        public bool IsSelfClassName
        {
            get { return this.Equals(Name.SelfClassName); }
        }

        public bool IsStaticClassName
        {
            get { return this.Equals(Name.StaticClassName); }
        }

        public bool IsReservedClassName
        {
            get { return IsParentClassName || IsSelfClassName || IsStaticClassName; }
        }

		#endregion

		/// <summary>
		/// Creates a name. 
		/// </summary>
		/// <param name="value">The name shouldn't be <B>null</B>.</param>
		public Name(string/*!*/ value)
		{
			Debug.Assert(value != null);
			this.value = value;
            this.hashCode = StringComparer.OrdinalIgnoreCase.GetHashCode(value);
		}

        #region Utils

        /// <summary>
        /// Separator of class name and its static field in a form of <c>CLASS::MEMBER</c>.
        /// </summary>
        public const string ClassMemberSeparator = "::";

        /// <summary>
        /// Splits the <paramref name="value"/> into class name and member name if it is double-colon separated.
        /// </summary>
        /// <param name="value">Full name.</param>
        /// <param name="className">Will contain the class name fragment if the <paramref name="value"/> is in a form of <c>CLASS::MEMBER</c>. Otherwise <c>null</c>.</param>
        /// <param name="memberName">Will contain the member name fragment if the <paramref name="value"/> is in a form of <c>CLASS::MEMBER</c>. Otherwise it contains original <paramref name="value"/>.</param>
        /// <returns>True iff the <paramref name="value"/> is in a form of <c>CLASS::MEMBER</c>.</returns>
        public static bool IsClassMemberSyntax(string/*!*/value, out string className, out string memberName)
        {
            Debug.Assert(value != null);
            //Debug.Assert(QualifiedName.Separator.ToString() == ":::" && !value.Contains(QualifiedName.Separator.ToString())); // be aware of deprecated namespace syntax

            int separator;
            if ((separator = value.IndexOf(':')) >= 0 &&    // value.Contains( ':' )
                (separator = System.Globalization.CultureInfo.InvariantCulture.CompareInfo.IndexOf(value, ClassMemberSeparator, separator, value.Length - separator, System.Globalization.CompareOptions.Ordinal)) > 0) // value.Contains( "::" )
            {
                className = value.Remove(separator);
                memberName = value.Substring(separator + ClassMemberSeparator.Length);
                return true;
            }
            else
            {
                className = null;
                memberName = value;
                return false;
            }
        }

        /// <summary>
        /// Determines if given <paramref name="value"/> is in a form of <c>CLASS::MEMBER</c>.
        /// </summary>
        /// <param name="value">Full name.</param>
        /// <returns>True iff the <paramref name="value"/> is in a form of <c>CLASS::MEMBER</c>.</returns>
        public static bool IsClassMemberSyntax(string value)
        {
            return value != null && value.Contains(ClassMemberSeparator);
        }

        #endregion

        #region Basic Overrides

        public override bool Equals(object obj)
		{
            return obj != null && obj.GetType() == typeof(Name) && Equals((Name)obj);
		}

		public override int GetHashCode()
		{
            return this.hashCode;
		}

		public override string ToString()
		{
			return this.value;
		}

		#endregion

		#region IEquatable<Name> Members

		public bool Equals(Name other)
		{
            return this.GetHashCode() == other.GetHashCode() && Equals(other.Value);
		}

		public static bool operator ==(Name name, Name other)
		{
			return name.Equals(other);
		}

		public static bool operator !=(Name name, Name other)
		{
			return !name.Equals(other);
		}

		#endregion

		#region IEquatable<string> Members

		public bool Equals(string other)
		{
            return string.Equals(value, other, StringComparison.OrdinalIgnoreCase);
		}

		#endregion
	}

	#endregion

	#region VariableName

	/// <summary>
	/// Case-sensitive simple name in Unicode C normal form.
	/// Used for names of variables and constants.
	/// </summary>
	[DebuggerNonUserCode]
	public struct VariableName : IEquatable<VariableName>, IEquatable<string>
	{
		public string/*!*/ Value { get { return value; } set { this.value = value; } }
		private string/*!*/ value;

        #region Special Names

        public static readonly VariableName ThisVariableName = new VariableName("this");

		#region Autoglobals

        public const string EnvName = "_ENV";
        public const string ServerName = "_SERVER";
        public const string GlobalsName = "GLOBALS";
        public const string RequestName = "_REQUEST";
        public const string GetName = "_GET";
        public const string PostName = "_POST";
        public const string CookieName = "_COOKIE";
        public const string HttpRawPostDataName = "HTTP_RAW_POST_DATA";
        public const string FilesName = "_FILES";
        public const string SessionName = "_SESSION";

        #endregion
        
        public bool IsThisVariableName
		{
			get
			{
				return this == ThisVariableName;
			}
		}

        #region IsAutoGlobal

        /// <summary>
        /// Gets value indicting whether the name represents an auto-global variable.
        /// </summary>
        public bool IsAutoGlobal
        {
            get
            {
                return IsAutoGlobalVariableName(this.Value);
            }
        }

        /// <summary>
        /// Checks whether a specified name is the name of an auto-global variable.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Whether <paramref name="name"/> is auto-global.</returns>
        public static bool IsAutoGlobalVariableName(string name)
        {
            switch (name)
            {
                case GlobalsName:
                case ServerName:
                case EnvName:
                case CookieName:
                case HttpRawPostDataName:
                case FilesName:
                case RequestName:
                case GetName:
                case PostName:
                case SessionName:
                    return true;

                default:
                    return false;
            }
        }

        #endregion

        #endregion

        /// <summary>
		/// Creates a name. 
		/// </summary>
		/// <param name="value">The name, cannot be <B>null</B> nor empty.</param>
		public VariableName(string/*!*/ value)
		{
			Debug.Assert(value != null);
			// TODO (missing from Mono): this.value = value.Normalize();

			this.value = value;
		}

		#region Basic Overrides

		public override bool Equals(object obj)
		{
			if (!(obj is VariableName)) return false;
			return Equals((VariableName)obj);
		}

		public override int GetHashCode()
		{
			return value.GetHashCode();
		}

		public override string ToString()
		{
			return this.value;
		}

		#endregion

		#region IEquatable<VariableName> Members

		public bool Equals(VariableName other)
		{
			return this.value.Equals(other.value);
		}

		public static bool operator ==(VariableName name, VariableName other)
		{
			return name.Equals(other);
		}

		public static bool operator !=(VariableName name, VariableName other)
		{
			return !name.Equals(other);
		}

		#endregion

		#region IEquatable<string> Members

		public bool Equals(string other)
		{
			return value.Equals(other);
		}

		public static bool operator ==(VariableName name, string str)
		{
			return name.Equals(str);
		}

		public static bool operator !=(VariableName name, string str)
		{
			return !name.Equals(str);
		}

		#endregion
	}

	#endregion

    #region QualifiedName

    /// <summary>
	/// Case-insensitive culture-sensitive (TODO ???) qualified name in Unicode C normal form.
	/// </summary>
    [DebuggerNonUserCode]
    public struct QualifiedName : IEquatable<QualifiedName>
    {
        #region Special names

        public static readonly QualifiedName Error = new QualifiedName(new Name("<error>"), Name.EmptyNames);
		public static readonly QualifiedName Global = new QualifiedName(new Name("<Global>"), Name.EmptyNames);
		public static readonly QualifiedName Lambda = new QualifiedName(new Name("Lambda"), Name.EmptyNames);
		public static readonly QualifiedName Null = new QualifiedName(new Name("null"), Name.EmptyNames);
		public static readonly QualifiedName True = new QualifiedName(new Name("true"), Name.EmptyNames);
		public static readonly QualifiedName False = new QualifiedName(new Name("false"), Name.EmptyNames);
		public static readonly QualifiedName Array = new QualifiedName(new Name("array"), Name.EmptyNames);
		public static readonly QualifiedName Object = new QualifiedName(new Name("object"), Name.EmptyNames);
		public static readonly QualifiedName Integer = new QualifiedName(new Name("int"), Name.EmptyNames);
		public static readonly QualifiedName LongInteger = new QualifiedName(new Name("int64"), Name.EmptyNames);
		public static readonly QualifiedName String = new QualifiedName(new Name("string"), Name.EmptyNames);
		public static readonly QualifiedName Boolean = new QualifiedName(new Name("bool"), Name.EmptyNames);
		public static readonly QualifiedName Double = new QualifiedName(new Name("double"), Name.EmptyNames);
		public static readonly QualifiedName Resource = new QualifiedName(new Name("resource"), Name.EmptyNames);
		public static readonly QualifiedName SystemObject = new QualifiedName(new Name("Object"), new Name[] { new Name("System") });
        public static readonly QualifiedName Callable = new QualifiedName(new Name("callable"), Name.EmptyNames);

        public bool IsSimpleName
        {
            get
            {
                return Namespaces.Length == 0;
            }
        }

        /// <summary>
        /// Gets value indicating whether this name represents a primitive type.
        /// </summary>
        public bool IsPrimitiveTypeName
        {
            get
            {
                return IsSimpleName &&
                    (   Equals(Array) ||
                        Equals(Object) ||
                        Equals(Integer) ||
                        Equals(LongInteger) ||
                        Equals(String) ||
                        Equals(Boolean) ||
                        Equals(Double) ||
                        Equals(Resource) ||
                        Equals(Callable));
            }
        }

        public bool IsParentClassName
        {
            get { return IsSimpleName && name == Name.ParentClassName; }
        }

        public bool IsSelfClassName
        {
            get { return IsSimpleName && name == Name.SelfClassName; }
        }

        public bool IsStaticClassName
        {
            get { return IsSimpleName && name == Name.StaticClassName; }
        }

        public bool IsReservedClassName
        {
            get { return this.IsSimpleName && this.name.IsReservedClassName; }
        }

        public bool IsAutoloadName
        {
            get { return IsSimpleName && name == Name.AutoloadName; }
        }

        public bool IsAppStaticAttributeName
        {
            get { return IsSimpleName && (name == Name.AppStaticName || name == Name.AppStaticAttributeName); }
        }

        public bool IsExportAttributeName
        {
            get { return IsSimpleName && (name == Name.ExportName || name == Name.ExportAttributeName); }
        }

        public bool IsDllImportAttributeName
        {
            get { return IsSimpleName && (name == Name.DllImportName || name == Name.DllImportAttributeName); }
        }

        public bool IsOutAttributeName
        {
            get { return IsSimpleName && (name == Name.OutName || name == Name.OutAttributeName); }
        }

        #endregion

        public const char Separator = '\\';

        #region Properties

        /// <summary>
		/// The outer most namespace is the first in the array.
		/// </summary>
		public Name[]/*!*/ Namespaces { get { return namespaces; } set { namespaces = value; } }
		private Name[]/*!*/ namespaces;

		/// <summary>
		/// Base name. Contains the empty string for namespaces.
		/// </summary>
		public Name Name { get { return name; } set { name = value; } }
		private Name name;

        /// <summary>
        /// <c>True</c> if this represents fully qualified name (absolute namespace).
        /// </summary>
        public bool IsFullyQualifiedName { get { return isFullyQualifiedName; } internal set { isFullyQualifiedName = value; } }
        private bool isFullyQualifiedName;

        #endregion

        #region Construction

        ///// <summary>
        ///// Creates a qualified name with or w/o a base name. 
        ///// </summary>
        //internal QualifiedName(string/*!*/ qualifiedName, bool hasBaseName)
        //{
        //    Debug.Assert(qualifiedName != null);
        //    QualifiedName qn = Parse(qualifiedName, 0, qualifiedName.Length, hasBaseName);
        //    this.name = qn.name;
        //    this.namespaces = qn.namespaces;
        //    this.isFullyQualifiedName = qn.IsFullyQualifiedName;
        //}

		internal QualifiedName(IList<string>/*!*/ names, bool hasBaseName, bool fullyQualified)
		{
			Debug.Assert(names != null && names.Count > 0);

            //
            if (hasBaseName)
			{
				name = new Name(names[names.Count - 1]);
				namespaces = new Name[names.Count - 1];
			}
			else
			{
				name = Name.EmptyBaseName;
				namespaces = new Name[names.Count];
			}

			for (int i = 0; i < namespaces.Length; i++)
				namespaces[i] = new Name(names[i]);

            //
            isFullyQualifiedName = fullyQualified;
		}

		public QualifiedName(Name name)
            :this(name, Name.EmptyNames, false)
		{
		}

		public QualifiedName(Name name, Name[]/*!*/ namespaces)
            :this(name, namespaces, false)
		{
		}

        public QualifiedName(Name name, Name[]/*!*/ namespaces, bool fullyQualified)
        {
            if (namespaces == null)
                throw new ArgumentNullException("namespaces");

            this.name = name;
            this.namespaces = namespaces;
            this.isFullyQualifiedName = fullyQualified;
        }

		internal QualifiedName(Name name, QualifiedName namespaceName)
		{
			Debug.Assert(namespaceName.name.Value == "");

			this.name = name;
			this.namespaces = namespaceName.Namespaces;
            this.isFullyQualifiedName = namespaceName.IsFullyQualifiedName;
		}

		internal QualifiedName(QualifiedName name, QualifiedName namespaceName)
		{
			Debug.Assert(namespaceName.name.Value == "");

            this.name = name.name;
				
			if (name.IsSimpleName)
			{
				this.namespaces = namespaceName.Namespaces;
			}
			else // used for nested types
			{
				this.namespaces = ArrayUtils.Concat(namespaceName.namespaces, name.namespaces);
			}

            this.isFullyQualifiedName = namespaceName.IsFullyQualifiedName;
		}

        //internal static QualifiedName Parse(string/*!*/ buffer, int startIndex, int length, bool hasBaseName)
        //{
        //    Debug.Assert(buffer != null && startIndex >= 0 && startIndex <= buffer.Length - length);

        //    QualifiedName result = new QualifiedName();

        //    // handle fully qualified namespace name:
        //    if (length > 0 && buffer[startIndex] == Separator)
        //    {
        //        result.isFullyQualifiedName = true;
        //        startIndex++;
        //        length--;
        //    }

        //    // names separated by Separator:
        //    int slash_count = 0;
        //    for (int i = startIndex; i < startIndex + length; i++)
        //        if (buffer[i] == Separator) slash_count++;

        //    int separator_count = slash_count;// / Separator.ToString().Length;

        //    //Debug.Assert(slash_count % Separator.Length == 0);

        //    if (separator_count == 0)
        //    {
        //        Name entire_name = new Name(buffer.Substring(startIndex, length));

        //        if (hasBaseName)
        //        {
        //            result.namespaces = Name.EmptyNames;
        //            result.name = entire_name;
        //        }
        //        else
        //        {
        //            result.namespaces = new Name[] { entire_name };
        //            result.name = Name.EmptyBaseName;
        //        }
        //    }
        //    else
        //    {
        //        result.namespaces = new Name[separator_count + (hasBaseName ? 0 : 1)];

        //        int current_name = startIndex;
        //        int next_separator = startIndex;
        //        int i = 0;
        //        do
        //        {
        //            while (buffer[next_separator] != Separator)
        //                next_separator++;

        //            result.namespaces[i++] = new Name(buffer.Substring(current_name, next_separator - current_name));
        //            next_separator += Separator.ToString().Length;
        //            current_name = next_separator;
        //        }
        //        while (i < separator_count);

        //        Name base_name = new Name(buffer.Substring(current_name, length - current_name));

        //        if (hasBaseName)
        //        {
        //            result.name = base_name;
        //        }
        //        else
        //        {
        //            result.namespaces[separator_count] = base_name;
        //            result.name = Name.EmptyBaseName;
        //        }
        //    }

        //    return result;
        //}

        /// <summary>
        /// Builds <see cref="QualifiedName"/> with first element aliased if posible.
        /// </summary>
        /// <param name="qname">Qualified name to translate.</param>
        /// <param name="aliases">Enumeration of aliases.</param>
        /// <param name="currentNamespace">Current namespace to be prepended if no alias if found.</param>
        /// <returns>Qualified name that has been tralated according to given naming context.</returns>
        public static QualifiedName TranslateAlias(QualifiedName qname, Dictionary<string, QualifiedName>/*!*/aliases, QualifiedName? currentNamespace)
        {
            if (!qname.IsFullyQualifiedName)
            {
                // get first part of the qualified name:
                string first = qname.IsSimpleName ? qname.Name.Value : qname.Namespaces[0].Value;

                // return the alias if found:
                QualifiedName alias;
                if (aliases != null && aliases.TryGetValue(first, out alias))
                {
                    if (qname.IsSimpleName)
                    {
                        qname = alias;
                    }
                    else
                    {
                        // [ alias.namespaces, alias.name, qname.namespaces+1 ]
                        Name[] names = new Name[qname.namespaces.Length + alias.namespaces.Length];
                        for (int i = 0; i < alias.namespaces.Length; ++i) names[i] = alias.namespaces[i];
                        names[alias.namespaces.Length] = alias.name;
                        for (int j = 1; j < qname.namespaces.Length; ++j) names[alias.namespaces.Length + j] = qname.namespaces[j];

                        qname = new QualifiedName(qname.name, names);
                    }
                }
                else
                {
                    if (currentNamespace.HasValue)
                    {
                        Debug.Assert(string.IsNullOrEmpty(currentNamespace.Value.Name.Value));
                        qname = new QualifiedName(qname, currentNamespace.Value);
                    }
                    else
                    {
                        qname = new QualifiedName(qname.Name, qname.Namespaces);
                    }
                }

                // the name is translated (fully qualified)
                qname.IsFullyQualifiedName = true;
            }

            return qname;
        }

        /// <summary>
        /// Convert namespaces + name into list of strings.
        /// </summary>
        /// <returns>String List of namespaces (additionaly with <see cref="Name"/> component if it is not empty).</returns>
        internal List<string>/*!*/ToStringList()
        {
            List<string> list = new List<string>( this.Namespaces.Select( x => x.Value ) );

            if (!string.IsNullOrEmpty(this.Name.Value))
                list.Add(this.Name.Value);

            return list;
        }

		#endregion

		#region Basic Overrides

		public override bool Equals(object obj)
		{
			return obj != null && obj.GetType() == typeof(QualifiedName) && this.Equals((QualifiedName)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int result = name.GetHashCode();
				for (int i = 0; i < namespaces.Length; i++)
					result ^= namespaces[i].GetHashCode() << (i & 0x0f);

				return result;
			}
		}

        /// <summary>
        /// Return the namespace PHP name in form "A\B\C", not ending with <see cref="Separator"/>.
        /// </summary>
        public string NamespacePhpName
        {
            get
            {
                var ns = this.namespaces;
                if (ns.Length != 0)
                {
                    StringBuilder result = new StringBuilder(ns[0].Value, ns.Length * 8);
                    for (int i = 1; i < ns.Length; i++)
                    {
                        result.Append(Separator);
                        result.Append(ns[i].Value);
                    }
                    return result.ToString();
                }
                else
                {
                    return string.Empty;
                }
            }
        }

		public string ToString(Name? memberName, bool instance)
		{
			StringBuilder result = new StringBuilder();
			for (int i = 0; i < namespaces.Length; i++)
			{
				result.Append(namespaces[i]);
				result.Append(Separator);
			}
			result.Append(Name);
			if (memberName.HasValue)
			{
				result.Append(instance ? "->" : Name.ClassMemberSeparator);
				result.Append(memberName.Value.ToString());
			}

			return result.ToString();
		}

		public override string ToString()
		{
            var ns = this.namespaces;
            if (ns.Length == 0)
            {
                return this.Name.Value;
            }
            else
            {
                StringBuilder result = new StringBuilder(ns.Length * 8);
                for (int i = 0; i < ns.Length; i++)
                {
                    result.Append(ns[i]);
                    result.Append(Separator);
                }
                result.Append(this.Name.Value);
                return result.ToString();
            }
		}

		#endregion

		#region IEquatable<QualifiedName> Members

		public bool Equals(QualifiedName other)
		{
			if (!this.name.Equals(other.name) || this.namespaces.Length != other.namespaces.Length) return false;

            for (int i = 0; i < namespaces.Length; i++)
			{
				if (!this.namespaces[i].Equals(other.namespaces[i]))
					return false;
			}

			return true;
		}

		public static bool operator ==(QualifiedName name, QualifiedName other)
		{
			return name.Equals(other);
		}

		public static bool operator !=(QualifiedName name, QualifiedName other)
		{
			return !name.Equals(other);
		}

		#endregion
	}

    internal class ConstantQualifiedNameComparer : IEqualityComparer<QualifiedName>
    {
        public static readonly ConstantQualifiedNameComparer Singleton = new ConstantQualifiedNameComparer();

        public bool Equals(QualifiedName x, QualifiedName y)
        {
            return x.Equals(y) && string.Equals(x.Name.Value, y.Name.Value, StringComparison.Ordinal);   // case sensitive comparison of names
        }

        public int GetHashCode(QualifiedName obj)
        {
            return obj.GetHashCode();
        }
    }

	#endregion

	#region GenericQualifiedName

	/// <summary>
	/// Case-insensitive culture-sensitive (TODO ???) qualified name in Unicode C normal form
	/// with associated list of generic qualified names.
	/// </summary>
	public struct GenericQualifiedName
	{
        /// <summary>
        /// Empty GenericQualifiedName array.
        /// </summary>
        public static readonly GenericQualifiedName[] EmptyGenericQualifiedNames = new GenericQualifiedName[0];

        /// <summary>
        /// Qualified name without generics.
        /// </summary>
		public QualifiedName QualifiedName { get { return qualifiedName; } }
		private QualifiedName qualifiedName;

		/// <summary>
        /// Array of <see cref="GenericQualifiedName"/> or <see cref="PrimitiveTypeName"/>.
		/// </summary>
        public object[]/*!!*/ GenericParams { get { return genericParams; } }
        private object[]/*!!*/ genericParams;

        /// <summary>
        /// Gets value indicating whether the name has generic type parameters.
        /// </summary>
        public bool IsGeneric { get { return genericParams != null && genericParams.Length != 0; } }

        public GenericQualifiedName(QualifiedName qualifiedName, object[]/*!!*/ genericParams)
		{
			Debug.Assert(genericParams != null);
            Debug.Assert(genericParams.All(obj => obj == null || obj is PrimitiveTypeName || obj is GenericQualifiedName));

			this.qualifiedName = qualifiedName;
			this.genericParams = genericParams;
		}

		public GenericQualifiedName(QualifiedName qualifiedName)
		{
			this.qualifiedName = qualifiedName;
			this.genericParams = ArrayUtils.EmptyObjects;
		}
	}

	#endregion

    #region PrimitiveTypeName

    /// <summary>
    /// Represents primitive type name.
    /// </summary>
    public struct PrimitiveTypeName : IEquatable<PrimitiveTypeName>, IEquatable<QualifiedName>, IEquatable<string>
    {
        public QualifiedName QualifiedName { get { return qualifiedName; } }
        private readonly QualifiedName qualifiedName;

        public Name Name { get { return qualifiedName.Name; } }

        public PrimitiveTypeName(QualifiedName qualifiedName)
        {
            if (!qualifiedName.IsPrimitiveTypeName)
                throw new ArgumentException();

            this.qualifiedName = qualifiedName;
        }

        #region IEquatable<PrimitiveName> Members

        public bool Equals(PrimitiveTypeName other)
        {
            return Equals(other.qualifiedName);
        }

        #endregion

        #region IEquatable<QualifiedName> Members

        public bool Equals(QualifiedName other)
        {
            return Equals(other.Name.Value);
        }

        #endregion

        #region IEquatable<string> Members

        public bool Equals(string other)
        {
            return qualifiedName.Name.Value.Equals(other, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }

    #endregion

	#region NamingContext

    [DebuggerNonUserCode]
    public sealed class NamingContext
	{
        #region Fields & Properties

        /// <summary>
        /// Current namespace.
        /// </summary>
        public readonly QualifiedName? CurrentNamespace;

        /// <summary>
        /// PHP aliases. Can be null.
        /// </summary>
        public Dictionary<string, QualifiedName> Aliases { get { return _aliases; } }

        /// <summary>
        /// Function aliases. Can be null.
        /// </summary>
        public Dictionary<string, QualifiedName> FunctionAliases { get { return _functionAliases; } }

        /// <summary>
        /// Constant aliases. Can be null.
        /// </summary>
        public Dictionary<string, QualifiedName> ConstantAliases { get { return _constantAliases; } }

        private Dictionary<string, QualifiedName> _aliases;
        private Dictionary<string, QualifiedName> _functionAliases;
        private Dictionary<string, QualifiedName> _constantAliases;

        #endregion

        #region Construction

        /// <summary>
        /// Initialize new instance of <see cref="NamingContext"/>.
        /// </summary>
        /// <param name="currentNamespace">Current namespace. Can be null. Not starting with <see cref="QualifiedName.Separator"/>.</param>
        /// <param name="aliases">Amount of aliases. <c>0</c> means the <see cref="Aliases"/> property will be null.</param>
        public NamingContext(string currentNamespace, int aliases)
        {
            // current namespace:
            if (string.IsNullOrEmpty(currentNamespace))
            {
                this.CurrentNamespace = null;
            }
            else
            {
                Debug.Assert(currentNamespace[0] != QualifiedName.Separator);   // not starting with separator
                this.CurrentNamespace = new QualifiedName(currentNamespace.Split(QualifiedName.Separator), false, true);
            }

            // aliases (just initialize dictionary, items added later):
            _aliases = (aliases > 0) ? new Dictionary<string, QualifiedName>(aliases, StringComparer.OrdinalIgnoreCase) : null;
        }

        /// <summary>
        /// Initializes new instance of <see cref="NamingContext"/>
        /// </summary>
        public NamingContext(QualifiedName? currentNamespace, Dictionary<string, QualifiedName> aliases)
        {
            Debug.Assert(!currentNamespace.HasValue || string.IsNullOrEmpty(currentNamespace.Value.Name.Value));

            this.CurrentNamespace = currentNamespace;
            _aliases = aliases;
        }

        /// <summary>
        /// Add an alias into the <see cref="Aliases"/>.
        /// </summary>
        /// <param name="alias">Alias name.</param>
        /// <param name="qualifiedName">Aliased namespace. Not starting with <see cref="QualifiedName.Separator"/>.</param>
        /// <remarks>Used when constructing naming context at runtime.</remarks>
        public void AddAlias(string alias, string qualifiedName)
        {
            Debug.Assert(!string.IsNullOrEmpty(alias));
            Debug.Assert(!string.IsNullOrEmpty(qualifiedName));
            Debug.Assert(qualifiedName[0] != QualifiedName.Separator);   // not starting with separator

            AddAlias(alias, new QualifiedName(qualifiedName.Split(QualifiedName.Separator), true, true));
        }

        private static bool AddAlias(Dictionary<string, QualifiedName>/*!*/dict, string alias, QualifiedName qname)
        {
            var count = dict.Count;
            dict[alias] = qname;
            return count != dict.Count;  // item was added
        }

        /// <summary>
        /// Adds an alias into the context.
        /// </summary>
        public bool AddAlias(string alias, QualifiedName qname)
        {
            var aliases = _aliases;
            if (aliases == null)
                _aliases = aliases = new Dictionary<string, QualifiedName>(StringComparer.OrdinalIgnoreCase);

            return AddAlias(aliases, alias, qname);
        }

        /// <summary>
        /// Adds a function alias into the context.
        /// </summary>
        public bool AddFunctionAlias(string alias, QualifiedName qname)
        {
            var aliases = _functionAliases;
            if (aliases == null)
                _functionAliases = aliases = new Dictionary<string, QualifiedName>(StringComparer.OrdinalIgnoreCase);

            return AddAlias(aliases, alias, qname);
        }

        /// <summary>
        /// Adds a constant into the context.
        /// </summary>
        public bool AddConstantAlias(string alias, QualifiedName qname)
        {
            var aliases = _constantAliases;
            if (aliases == null)
                _constantAliases = aliases = new Dictionary<string, QualifiedName>(StringComparer.OrdinalIgnoreCase);

            return AddAlias(aliases, alias, qname);
        }

        #endregion
    }

	#endregion
}
