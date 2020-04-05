#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
	/// Represents enumerator which 
	/// </summary>
	public interface IPhpEnumerator : IEnumerator<KeyValuePair<PhpValue, PhpValue>>
    {
        /// <summary>
        /// Moves the enumerator to the last entry of the dictionary.
        /// </summary>
        /// <returns>Whether the enumerator has been sucessfully moved to the last entry.</returns>
        bool MoveLast();

        /// <summary>
        /// Moves the enumerator to the first entry of the dictionary.
        /// </summary>
        /// <returns>Whether the enumerator has been sucessfully moved to the first entry.</returns>
        bool MoveFirst();

        /// <summary>
        /// Moves the enumerator to the previous entry of the dictionary.
        /// </summary>
        /// <returns>Whether the enumerator has been sucessfully moved to the previous entry.</returns>
        bool MovePrevious();

        /// <summary>
        /// Gets whether the enumeration has ended and the enumerator points behind the last element.
        /// </summary>
        bool AtEnd { get; }

        /// <summary>
        /// Gets current unaliased value.
        /// </summary>
        PhpValue CurrentValue { get; }

        /// <summary>
        /// Aliases current entry value.
        /// </summary>
        PhpAlias CurrentValueAliased { get; }

        /// <summary>
        /// Gets current key.
        /// </summary>
        PhpValue CurrentKey { get; }
    }

    /// <summary>
    /// Provides methods which allows implementor to be used in PHP foreach statement as a source of enumeration.
    /// </summary>
    public interface IPhpEnumerable
    {
        ///// <summary>
        ///// Implementor's intrinsic enumerator which will be advanced during enumeration.
        ///// </summary>
        //IPhpEnumerator IntrinsicEnumerator { get; }

        /// <summary>
        /// Creates an enumerator used in foreach statement.
        /// </summary>
        /// <param name="aliasedValues">Whether the values returned by enumerator are assigned by reference.</param>
        /// <param name="caller">Type of the class in whose context the caller operates.</param>
        /// <returns>The dictionary enumerator.</returns>
        IPhpEnumerator GetForeachEnumerator(bool aliasedValues, RuntimeTypeHandle caller);
    }

    /// <summary>
    /// The PHP array interface provides operations for an array access.
    /// </summary>
    public interface IPhpArray // TODO: : IPhpEnumerable, IPhpConvertible
    {
        /// <summary>
        /// Gets number of items in the collection.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets value at given index.
        /// Gets <c>void</c> value in case the key is not found.
        /// </summary>
        PhpValue GetItemValue(IntStringKey key);

        /// <summary>
        /// Gets value at given index.
        /// Gets <c>void</c> value in case the key is not found.
        /// </summary>
        PhpValue GetItemValue(PhpValue index);

        /// <summary>
        /// Sets value at specific index. Value must not be an alias.
        /// </summary>
        void SetItemValue(IntStringKey key, PhpValue value);

        /// <summary>
        /// Sets value at specific index. Value must not be an alias.
        /// </summary>
        void SetItemValue(PhpValue index, PhpValue value);

        /// <summary>
        /// Writes aliased value at given index.
        /// </summary>
        void SetItemAlias(IntStringKey key, PhpAlias alias);

        /// <summary>
        /// Writes aliased value at given index.
        /// </summary>
        void SetItemAlias(PhpValue index, PhpAlias alias);

        /// <summary>
        /// Add a value to the end of array.
        /// Value can be an alias.
        /// </summary>
        void AddValue(PhpValue value);

        /// <summary>
        /// Removes a value matching given key.
        /// In case the value is not found, the method does nothing.
        /// </summary>
        void RemoveKey(IntStringKey key);

        /// <summary>
        /// Removes a value at given index.
        /// In case the value is not found, the method does nothing.
        /// </summary>
        void RemoveKey(PhpValue index);

        /// <summary>
        /// Ensures the item at given index is alias.
        /// </summary>
        PhpAlias EnsureItemAlias(IntStringKey key);

        /// <summary>
        /// Ensures the item at given index is class object.
        /// </summary>
        object EnsureItemObject(IntStringKey key);

        /// <summary>
        /// Ensures the item at given index is array.
        /// </summary>
        IPhpArray EnsureItemArray(IntStringKey key);
    }

    /// <summary>
    /// An interface allowing objects to implement custom <c>clone</c> operation.
    /// </summary>
    public interface IPhpCloneable
    {
        /// <summary>
        /// Returns the object clone.
        /// </summary>
        object Clone();
    }

    /// <summary>
    /// Provides explicit object behavior for <c>print_r</c> and <c>var_dump</c> implementations.
    /// </summary>
    public interface IPhpPrintable
    {
        /// <summary>
        /// Returns properties to be printed.
        /// </summary>
        IEnumerable<KeyValuePair<string, PhpValue>> Properties { get; }
    }

    /// <summary>
    /// Visitor implementation for a variable.
    /// </summary>
    /// <remarks>Used for serialization, printing, dumping.</remarks>
    public class PhpVariableVisitor
    {
        public virtual void Accept(PhpValue obj) => obj.Accept(this);
        public virtual void Accept(bool obj) { }
        public virtual void Accept(long obj) { }
        public virtual void Accept(double obj) { }
        public virtual void Accept(string obj) { }
        public virtual void Accept(PhpString obj) { }
        public virtual void Accept(PhpArray obj)
        {
            var iterator = obj.GetFastEnumerator();
            while (iterator.MoveNext())
            {
                AcceptArrayItem(iterator.Current);
            }
        }
        public virtual void AcceptArrayItem(KeyValuePair<IntStringKey, PhpValue> entry) => Accept(entry.Value);
        public virtual void Accept(PhpAlias obj) => Accept(obj.Value);
        public virtual void AcceptObject(object obj) { }
        public virtual void AcceptNull() { }
    }

    /// <summary>
    /// Provides method for serializing a variable into a stream.
    /// </summary>
    /// <remarks>Used for dumping, printing, export, serialization.</remarks>
    public interface IPhpVariableFormatter
    {
        /// <summary>
        /// Serializes a <paramref name="value"/> into a string (unicode or binary).
        /// </summary>
        /// <param name="value">Value to be serialized.</param>
        PhpString Serialize(PhpValue value);
    }

    /// <summary>
    /// Set of common array keys.
    /// </summary>
    /// <remarks>
    /// Used by runtime and compiler.
    /// Name of class field corresponds to the key string.
    /// </remarks>
    public static class CommonPhpArrayKeys
    {
        public readonly static IntStringKey GLOBALS = new IntStringKey("GLOBALS");
        public readonly static IntStringKey _ENV = new IntStringKey("_ENV");
        public readonly static IntStringKey _GET = new IntStringKey("_GET");
        public readonly static IntStringKey _POST = new IntStringKey("_POST");
        public readonly static IntStringKey _COOKIE = new IntStringKey("_COOKIE");
        public readonly static IntStringKey _REQUEST = new IntStringKey("_REQUEST");
        public readonly static IntStringKey _SERVER = new IntStringKey("_SERVER");
        public readonly static IntStringKey _FILES = new IntStringKey("_FILES");
        public readonly static IntStringKey _SESSION = new IntStringKey("_SESSION");
        public readonly static IntStringKey HTTP_RAW_POST_DATA = new IntStringKey("HTTP_RAW_POST_DATA");

        public readonly static IntStringKey DOCUMENT_ROOT = new IntStringKey("DOCUMENT_ROOT");
        public readonly static IntStringKey REMOTE_ADDR = new IntStringKey("REMOTE_ADDR");
        public readonly static IntStringKey REMOTE_ADDR_IPV6 = new IntStringKey("REMOTE_ADDR_IPV6");
        public readonly static IntStringKey REMOTE_PORT = new IntStringKey("REMOTE_PORT");
        public readonly static IntStringKey LOCAL_ADDR = new IntStringKey("LOCAL_ADDR");
        public readonly static IntStringKey LOCAL_PORT = new IntStringKey("LOCAL_PORT");
        public readonly static IntStringKey SERVER_ADDR = new IntStringKey("SERVER_ADDR");
        public readonly static IntStringKey SERVER_SOFTWARE = new IntStringKey("SERVER_SOFTWARE");
        public readonly static IntStringKey SERVER_PROTOCOL = new IntStringKey("SERVER_PROTOCOL");
        public readonly static IntStringKey SERVER_NAME = new IntStringKey("SERVER_NAME");
        public readonly static IntStringKey SERVER_PORT = new IntStringKey("SERVER_PORT");
        public readonly static IntStringKey REQUEST_METHOD = new IntStringKey("REQUEST_METHOD");
        public readonly static IntStringKey SCRIPT_NAME = new IntStringKey("SCRIPT_NAME");
        public readonly static IntStringKey SCRIPT_FILENAME = new IntStringKey("SCRIPT_FILENAME");
        public readonly static IntStringKey PHP_SELF = new IntStringKey("PHP_SELF");
        public readonly static IntStringKey QUERY_STRING = new IntStringKey("QUERY_STRING");
        public readonly static IntStringKey HTTP_HOST = new IntStringKey("HTTP_HOST");
        public readonly static IntStringKey HTTP_CONNECTION = new IntStringKey("HTTP_CONNECTION");
        public readonly static IntStringKey HTTP_USER_AGENT = new IntStringKey("HTTP_USER_AGENT");
        public readonly static IntStringKey HTTP_ACCEPT = new IntStringKey("HTTP_ACCEPT");
        public readonly static IntStringKey HTTP_ACCEPT_ENCODING = new IntStringKey("HTTP_ACCEPT_ENCODING");
        public readonly static IntStringKey HTTP_ACCEPT_LANGUAGE = new IntStringKey("HTTP_ACCEPT_LANGUAGE");
        public readonly static IntStringKey HTTP_COOKIE = new IntStringKey("HTTP_COOKIE");
        public readonly static IntStringKey HTTP_REFERER = new IntStringKey("HTTP_REFERER");
        public readonly static IntStringKey REQUEST_URI = new IntStringKey("REQUEST_URI");
        public readonly static IntStringKey REQUEST_TIME_FLOAT = new IntStringKey("REQUEST_TIME_FLOAT");
        public readonly static IntStringKey REQUEST_TIME = new IntStringKey("REQUEST_TIME");
        public readonly static IntStringKey HTTPS = new IntStringKey("HTTPS");
        public readonly static IntStringKey PATH_INFO = new IntStringKey("PATH_INFO");
        public readonly static IntStringKey PATH_TRANSLATED = new IntStringKey("PATH_TRANSLATED");
        public readonly static IntStringKey ORIG_PATH_INFO = new IntStringKey("ORIG_PATH_INFO");

        // commonly used array keys: // CONSIDER: generate the static cache in compile time
        public readonly static IntStringKey args = new IntStringKey("args");
        public readonly static IntStringKey wpdb = new IntStringKey("wpdb");
        public readonly static IntStringKey type = new IntStringKey("type");
        public readonly static IntStringKey title = new IntStringKey("title");
        public readonly static IntStringKey description = new IntStringKey("description");
        public readonly static IntStringKey status = new IntStringKey("status");
        public readonly static IntStringKey data = new IntStringKey("data");
        public readonly static IntStringKey key = new IntStringKey("key");
        public readonly static IntStringKey body = new IntStringKey("body");
        public readonly static IntStringKey where = new IntStringKey("where");
        public readonly static IntStringKey message = new IntStringKey("message");
        public readonly static IntStringKey orderby = new IntStringKey("orderby");
        public readonly static IntStringKey post = new IntStringKey("post");
        public readonly static IntStringKey post_type = new IntStringKey("post_type");
        public readonly static IntStringKey post_status = new IntStringKey("post_status");
        public readonly static IntStringKey flags = new IntStringKey("flags");
        public readonly static IntStringKey app = new IntStringKey("app");
        public readonly static IntStringKey path = new IntStringKey("path");
        public readonly static IntStringKey context = new IntStringKey("context");
        public readonly static IntStringKey result = new IntStringKey("result");
        public readonly static IntStringKey name = new IntStringKey("name");
        public readonly static IntStringKey user = new IntStringKey("user");
        public readonly static IntStringKey url = new IntStringKey("url");
        public readonly static IntStringKey action = new IntStringKey("action");
        public readonly static IntStringKey id = new IntStringKey("id");

        public readonly static IntStringKey a = new IntStringKey("a");
        public readonly static IntStringKey b = new IntStringKey("b");
        public readonly static IntStringKey c = new IntStringKey("c");
        public readonly static IntStringKey i = new IntStringKey("i");
        public readonly static IntStringKey q = new IntStringKey("q");
        public readonly static IntStringKey x = new IntStringKey("x");
    }

    public static class PhpVariable
    {
        #region Types

        /// <summary>
        /// PHP name for <see cref="int"/>.
        /// </summary>
        public const string TypeNameInt = "int";
        public const string TypeNameInteger = "integer";

        /// <summary>
        /// PHP name for <see cref="long"/>.
        /// </summary>
        public const string TypeNameLongInteger = "int64";

        /// <summary>
        /// PHP name for <see cref="double"/>.
        /// </summary>
        public const string TypeNameDouble = "double";
        public const string TypeNameFloat = "float";

        /// <summary>
        /// PHP name for <see cref="bool"/>.
        /// </summary>
        public const string TypeNameBool = "bool";
        public const string TypeNameBoolean = "boolean";

        /// <summary>
        /// PHP name for <see cref="string"/>.
        /// </summary>
        public const string TypeNameString = "string";

        /// <summary>
        /// PHP name for <see cref="System.Void"/>.
        /// </summary>
        public const string TypeNameVoid = "void";

        /// <summary>
        /// PHP name for <see cref="System.Object"/>.
        /// </summary>
        public const string TypeNameObject = "object";

        /// <summary>
        /// PHP name for <B>null</B>.
        /// </summary>
        public const string TypeNameNull = "NULL";

        /// <summary>
        /// PHP name for <B>true</B> constant.
        /// </summary>
        public const string True = "true";

        /// <summary>
        /// PHP name for <B>true</B> constant.
        /// </summary>
        public const string False = "false";

        /// <summary>
        /// Gets the PHP name of given value.
        /// </summary>
        /// <param name="value">The object which type name to get.</param>
        /// <returns>The PHP name of the type of <paramref name="value"/>.</returns>
        /// <remarks>Returns CLR type name for variables of unknown type.</remarks>
        public static string GetTypeName(PhpValue value)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.Null: return TypeNameNull;
                case PhpTypeCode.Boolean: return TypeNameBoolean;
                case PhpTypeCode.Long: return TypeNameInteger;
                case PhpTypeCode.Double: return TypeNameDouble;
                case PhpTypeCode.PhpArray: return PhpArray.PhpTypeName;
                case PhpTypeCode.String:
                case PhpTypeCode.MutableString: return TypeNameString;
                case PhpTypeCode.Object: return value.Object is PhpResource ? PhpResource.PhpTypeName : TypeNameObject;
                case PhpTypeCode.Alias: return GetTypeName(value.Alias.Value);
                default: throw new ArgumentException();
            }
        }

        #endregion

        /// <summary>
        /// Enumerates deep copy of iterator values.
        /// </summary>
        public static IEnumerator<KeyValuePair<IntStringKey, PhpValue>> EnumerateDeepCopies(IEnumerator<KeyValuePair<IntStringKey, PhpValue>> iterator)
        {
            while (iterator.MoveNext())
            {
                var entry = iterator.Current;
                yield return new KeyValuePair<IntStringKey, PhpValue>(entry.Key, entry.Value.DeepCopy());
            }
        }

        /// <summary>
		/// Checks whether a string is "valid" PHP variable identifier.
		/// </summary>
		/// <param name="name">The variable name.</param>
		/// <returns>
		/// Whether <paramref name="name"/> is "valid" name of variable, i.e. [_[:alpha:]][_0-9[:alpha:]]*.
		/// This doesn't say anything about whether a variable of such name can be used in PHP, e.g. <c>${0}</c> is ok.
		/// </returns>
		public static bool IsValidName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;

            // first char:
            if (!char.IsLetter(name[0]) && name[0] != '_') return false;

            // next chars:
            for (int i = 1; i < name.Length; i++)
            {
                if (!char.IsLetterOrDigit(name[i]) && name[i] != '_') return false;
            }

            return true;
        }

        /// <summary>
        /// Determines if given callable is syntactically valid.
        /// </summary>
        public static bool IsValidCallback(IPhpCallable? callable)
        {
            PhpCallback? tmp;

            return callable != null && ((tmp = callable as PhpCallback) == null || tmp.IsValid);
        }

        /// <summary>
        /// Determines if given callable is valid and referes toi an existing function.
        /// </summary>
        public static bool IsValidBoundCallback(Context ctx, IPhpCallable callable)
        {
            return callable is PhpCallback phpcallback
                ? phpcallback.IsValidBound(ctx)
                : callable != null;
        }

        /// <summary>
        /// Determines whether the value is <see cref="int"/> or <see cref="long"/>.
        /// </summary>
        public static bool IsInteger(this PhpValue value)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.Long:
                    return true;

                case PhpTypeCode.Alias:
                    return IsInteger(value.Alias.Value);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines whether the value is <see cref="bool"/> (either as a value or an alias).
        /// </summary>
        public static bool IsBoolean(this PhpValue value) => value.IsBoolean || (value.IsAlias && value.Alias.Value.IsBoolean);

        /// <summary>
        /// Determines whether the value is <see cref="double"/>.
        /// </summary>
        public static bool IsDouble(this PhpValue value)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.Double:
                    return true;

                case PhpTypeCode.Alias:
                    return IsDouble(value.Alias.Value);

                default:
                    return false;
            }
        }

        /// <summary>
        /// In case value is a resource, gets its reference.
        /// </summary>
        public static PhpResource? AsResource(this PhpValue value)
        {
            return value.AsObject() as PhpResource;
        }

        /// <summary>
        /// In case value contains <see cref="PhpArray"/>,
        /// its instance is returned. Otherwise <c>null</c>.
        /// </summary>
        /// <remarks>Value is dereferenced if necessary.</remarks>
        public static PhpArray? ArrayOrNull(this PhpValue value) => AsArray(value);

        /// <summary>
        /// Alias to <see cref="ToStringOrNull(PhpValue)"/>.
        /// </summary>
        public static string AsString(this PhpValue value) => ToStringOrNull(value);

        /// <summary>
        /// In case given value contains a string (<see cref="string"/> or <see cref="PhpString"/>),
        /// its string representation is returned.
        /// Otherwise <c>null</c>.
        /// </summary>
        public static string ToStringOrNull(this PhpValue value)
        {
            IsString(value, out var @string);
            return @string;
        }

        /// <summary>
        /// In case given value contains a string (<see cref="string"/> or <see cref="PhpString"/>),
        /// its string representation is returned.
        /// Otherwise <c>null</c>.
        /// </summary>
        public static byte[]? ToBytesOrNull(this PhpValue value)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.String: return Encoding.UTF8.GetBytes(value.String);
                case PhpTypeCode.MutableString: return value.MutableString.ToBytes(Encoding.UTF8);
                case PhpTypeCode.Alias: return ToBytesOrNull(value.Alias.Value);
                default: return null;
            }
        }

        /// <summary>
        /// In case given value contains a string (<see cref="string"/> or <see cref="PhpString"/>),
        /// its string representation is returned.
        /// Otherwise <c>null</c>.
        /// </summary>
        public static byte[]? ToBytesOrNull(this PhpValue value, Context ctx)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.String: return Encoding.UTF8.GetBytes(value.String);
                case PhpTypeCode.MutableString: return value.MutableString.ToBytes(ctx);
                case PhpTypeCode.Alias: return ToBytesOrNull(value.Alias.Value, ctx);
                default: return null;
            }
        }

        public static byte[] ToBytes(this PhpValue value, Context ctx)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.MutableString: return value.MutableString.ToBytes(ctx);
                case PhpTypeCode.Alias: return ToBytes(value.Alias.Value, ctx);
                default: return ctx.StringEncoding.GetBytes(value.ToString(ctx));
            }
        }

        /// <summary>
        /// In case the value contains a php string with binary data, gets array of bytes. Otherwise <c>null</c>.
        /// </summary>
        public static byte[]? AsBytesOrNull(this PhpValue value, Context ctx)
        {
            return (value.Object is PhpAlias alias ? alias.Value.Object : value.Object) is PhpString.Blob blob && blob.ContainsBinaryData
                ? blob.ToBytes(ctx)
                : null;
        }

        /// <summary>
        /// In case given value contains an array (<see cref="PhpArray"/>),
        /// it is returned. Otherwise <c>null</c>.
        /// </summary>
        public static PhpArray? AsArray(this PhpValue value)
        {
            return (value.Object is PhpAlias alias ? alias.Value.Object : value.Object) as PhpArray;
        }

        /// <summary>
        /// Checks the value is of type <c>string</c> or <c>&amp;string</c> and gets its value.
        /// Single-byte strings are decoded using <c>UTF-8</c>.
        /// </summary>
        public static bool IsPhpArray(this PhpValue value, /*[MaybeNullWhen(false)]*/out PhpArray? array) => (array = value.AsArray()) != null; // TODO: STANDARD21

        /// <summary>
        /// Checks the value is of type <c>string</c> or <c>&amp;string</c> and gets its value.
        /// Single-byte strings are decoded using <c>UTF-8</c>.
        /// </summary>
        public static bool IsString(this PhpValue value, out string @string) => value.IsStringImpl(out @string);

        /// <summary>
        /// Checks the value is constructed as mutable <see cref="PhpString"/>.
        /// UTF strings are not handled by this method.
        /// </summary>
        public static bool IsMutableString(this PhpValue value, out PhpString @string) => value.IsMutableStringImpl(out @string);

        /// <summary>
        /// Checks the value is of type <c>string</c> (both unicode and single-byte) or an alias to a string.
        /// </summary>
        public static bool IsString(this PhpValue value) => value.IsStringImpl();

        /// <summary>
        /// Gets value indicating the variable contains a single-byte string value.
        /// </summary>
        public static bool IsBinaryString(this PhpValue value, out PhpString @string)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.MutableString:
                    if (value.MutableStringBlob.ContainsBinaryData)
                    {
                        @string = value.MutableString;
                        return true;
                    }
                    else
                    {
                        goto default;
                    }

                case PhpTypeCode.Alias:
                    return value.Alias.Value.IsBinaryString(out @string);

                default:
                    @string = default;
                    return false;
            }
        }

        /// <summary>
        /// Gets value indicating the variable is Unicode string value.
        /// </summary>
        public static bool IsUnicodeString(this PhpValue value, /*[MaybeNullWhen(false)]*/out string? @string)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.String:
                    @string = value.String;
                    return true;

                case PhpTypeCode.MutableString:
                    if (value.MutableStringBlob.ContainsBinaryData)
                    {
                        goto default;
                    }
                    else
                    {
                        @string = value.MutableStringBlob.ToString();
                        return true;
                    }

                case PhpTypeCode.Alias:
                    return value.Alias.Value.IsUnicodeString(out @string);

                default:
                    @string = null;
                    return false;
            }
        }

        public static bool IsLong(this PhpValue value, out long l)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.Long:
                    l = value.Long;
                    return true;

                case PhpTypeCode.Alias:
                    return IsLong(value.Alias.Value, out l);

                default:
                    l = default;
                    return false;
            }
        }

        public static bool IsDouble(this PhpValue value, out double d)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.Double:
                    d = value.Double;
                    return true;
                case PhpTypeCode.Alias:
                    return IsDouble(value.Alias.Value, out d);
                default:
                    d = default;
                    return false;
            }
        }

        /// <summary>
        /// Checks the value is of type <c>bool</c> or <c>&amp;bool</c> and gets its value.
        /// </summary>
        public static bool IsBoolean(this PhpValue value, out bool b) => value.IsBooleanImpl(out b);
    }
}
