using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pchp.Core;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Pchp.Library.Streams
{
    // TODO: move to Runtime

    #region TextElement

    /// <summary>
    /// Either <see cref="string"/> or <see cref="byte"/>[].
    /// </summary>
    [DebuggerDisplay("{DebugType,nq}: {DebugDisplay}")]
    public struct TextElement
    {
        readonly object _data;

        /// <summary>
        /// Gets debuggable display string.
        /// </summary>
        string DebugDisplay => (_data == null) ? string.Empty : (IsText ? GetText() : Encoding.UTF8.GetString(GetBytes()));
        string DebugType => (_data == null) ? "NULL" : (IsText ? "Unicode" : "Bytes");

        public bool IsNull => _data == null;

        public bool IsBinary => !IsText;

        public bool IsText => _data.GetType() == typeof(string);

        internal string GetText() => (string)_data;

        internal byte[] GetBytes() => (byte[])_data;

        public string AsText(Encoding enc) => IsNull ? string.Empty : IsText ? GetText() : enc.GetString(GetBytes());

        public byte[] AsBytes(Encoding enc) => IsNull ? Core.Utilities.ArrayUtils.EmptyBytes : IsBinary ? GetBytes() : enc.GetBytes(GetText());

        public override string ToString() => IsText ? GetText() : Encoding.UTF8.GetString(GetBytes());

        public PhpString ToPhpString() => IsText ? new PhpString(GetText()) : new PhpString(GetBytes());

        /// <summary>
        /// Gets length of the string or byytes array.
        /// </summary>
        public int Length => (_data != null) ? (IsText ? GetText().Length : GetBytes().Length) : 0;

        /// <summary>
        /// An empty byte array.
        /// </summary>
        public static TextElement Empty => new TextElement(Core.Utilities.ArrayUtils.EmptyBytes);

        /// <summary>
        /// Null element (Invalid).
        /// </summary>
        public static TextElement Null => default(TextElement);

        public TextElement(byte[] bytes)
        {
            Debug.Assert(bytes != null);
            _data = bytes;
            Debug.Assert(IsBinary);
        }

        public TextElement(string text)
        {
            Debug.Assert(text != null);
            _data = text;
            Debug.Assert(IsText);
        }

        public TextElement(PhpString str, Encoding encoding)
        {
            Debug.Assert(str != null);
            _data = str.ContainsBinaryData
                ? (object)str.ToBytes(encoding)
                : str.ToString(encoding);

            Debug.Assert(IsText ^ IsBinary);
        }

        public static TextElement FromValue(Context ctx, PhpValue value)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.Object:
                    if (value.Object is byte[])
                    {
                        return new TextElement((byte[])value.Object);
                    }
                    goto default;

                case PhpTypeCode.WritableString:
                    return new TextElement(value.WritableString, ctx.StringEncoding);

                default:
                    return new TextElement(value.ToStringOrThrow(ctx));
            }
        }
    }

    #endregion

    #region Basic Stream Filters

    /// <summary>
    /// Interface encapsulating the stream filtering functionality.
    /// </summary>
    public interface IFilter
    {
        /// <summary>
        /// Processes the <paramref name="input"/> (either of type <see cref="string"/> or <see cref="byte"/>[]) 
        /// data and returns the filtered data in one of the formats above or <c>null</c>.
        /// </summary>
        TextElement Filter(Context ctx, TextElement input, bool closing);

        /// <summary>
        /// Called when the filter is attached to a stream.
        /// </summary>
        void OnCreate();

        /// <summary>
        /// Called when the containig stream is being closed.
        /// </summary>
        void OnClose();
    }

    /// <summary>
    /// Stream Filter used to convert \r\n to \n when reading a text file.
    /// </summary>
    public class TextReadFilter : IFilter
    {
        /// <summary>
        /// Processes the <paramref name="input"/> (either of type <see cref="string"/> or <see cref="byte"/>[]) 
        /// data and returns the filtered data in one of the formats above or <c>null</c>.
        /// </summary>
        public TextElement Filter(Context ctx, TextElement input, bool closing)
        {
            string str = input.AsText(ctx.StringEncoding);

            if (pending)
            {
                // Both \r\n together make a pair which would consume a pending \r.
                if (str.Length == 0) str = "\r";
                else if (str[0] != '\n') str.Insert(0, "\r");
            }

            // Replace the pair.
            str = str.Replace("\r\n", "\n");
            if (str.Length != 0)
            {
                // Check for pending \r at the end.
                pending = str[str.Length - 1] == '\r';

                // Postpone the resolution of \r\n vs. \r to the next filtering if this is not the last one.
                if (!closing && pending) str.Remove(str.Length - 1, 1);
            }

            //
            return new TextElement(str);
        }

        bool pending = false;

        /// <summary>
        /// Called when the filter is attached to a stream.
        /// </summary>
        public void OnCreate() { }

        /// <summary>
        /// Called when the containig stream is being closed.
        /// </summary>
        public void OnClose() { }
    }

    /// <summary>
    /// Stream Filter used to convert \n to \r\n when writing to a text file.
    /// </summary>
    public class TextWriteFilter : IFilter
    {
        /// <summary>
        /// Processes the <paramref name="input"/> (either of type <see cref="string"/> or <see cref="byte"/>[]) 
        /// data and returns the filtered data in one of the formats above or <c>null</c>.
        /// </summary>
        public TextElement Filter(Context ctx, TextElement input, bool closing)
        {
            return new TextElement(input.AsText(ctx.StringEncoding).Replace("\n", "\r\n"));
        }

        /// <summary>
        /// Called when the filter is attached to a stream.
        /// </summary>
        public void OnCreate() { }

        /// <summary>
        /// Called when the containig stream is being closed.
        /// </summary>
        public void OnClose() { }
    }

    #endregion

    #region Stream Filter Base Classes

    #region Filter options

    /// <summary>
    /// Indicates whether the filter is to be attached to the
    /// input/ouput filter-chain or both.
    /// </summary>
    [Flags]
    public enum FilterChainOptions
    {
        /// <summary>Insert the filter to the read filter chain of the stream (1).</summary>
        Read = 0x1,
        /// <summary>Insert the filter to the write filter chain of the stream (2).</summary>
        Write = 0x2,
        /// <summary>Insert the filter to both the filter chains of the stream (3).</summary>
        ReadWrite = Read | Write,
        /// <summary>Prepend the filter to the filter-chain (0x10).</summary>
        Head = 0x10,
        /// <summary>Append the filter to the filter-chain (0x20).</summary>
        Tail = 0x20
    }

    #endregion

    /// <summary>
    /// Implementor of this interface provides filter creation.
    /// </summary>
    public interface IFilterFactory
    {
        /// <summary>
        /// Returns the list of filters created by this <see cref="IFilterFactory"/>.
        /// </summary>
        /// <returns>The list of implemented filters.</returns>
        string[] GetImplementedFilterNames();

        /// <summary>
        /// Checks if a filter is being created by this factory and optionally returns a new instance of this filter.
        /// </summary>
        /// <param name="name">The name of the filter (may contain wildcards).</param>
        /// <param name="instantiate"><c>true</c> to fill <paramref name="instance"/> with a new instance of that filter.</param>
        /// <param name="instance">Filled with a new instance of an implemented filter if <paramref name="instantiate"/>.</param>
        /// <param name="parameters">Additional parameters provided to the filter constructor.</param>
        /// <returns><c>true</c> if a filter with the given name was found.</returns>
        bool GetImplementedFilter(string name, bool instantiate, out PhpFilter instance, PhpValue parameters);
    }

    /// <summary>
    /// Base class for PHP stream filters.
    /// </summary>
    public abstract class PhpFilter : IFilter
    {
        #region Filtering methods and properties.

        /// <summary>
        /// Creates a new instance of the <see cref="PhpFilter"/>.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        public PhpFilter(object parameters)
        {
            this.parameters = parameters;
        }

        /// <summary>
        /// The filter name, same as the name used for creating the filter (see GetFilter).
        /// </summary>
        public string FilterName
        {
            get;
            private set;
        }

        #region IFilter Overrides

        /// <summary>
        /// Processes the <paramref name="input"/> (either of type <see cref="string"/> or <see cref="byte"/>[]) 
        /// data and returns the filtered data in one of the formats above or <c>null</c>.
        /// </summary>
        public abstract TextElement Filter(Context ctx, TextElement input, bool closing);

        /// <summary>
        /// Called when the filter is attached to a stream.
        /// </summary>
        public void OnCreate() { }

        /// <summary>
        /// Called when the containig stream is being closed.
        /// </summary>
        public void OnClose() { }
        #endregion

        /// <summary>
        /// An additional <c>mixed</c> parameter passed at <c>stream_filter_append/prepend</c>.
        /// </summary>
        protected readonly object parameters;

        #endregion

        #region Stream Filter Chain Access

        /// <summary>
        /// Insert the filter into the filter chains.
        /// </summary>
        /// <param name="stream">Which stream's filter chains.</param>
        /// <param name="filter">What filter.</param>
        /// <param name="where">What position in the chains.</param>
        /// <param name="parameters">Additional parameters for the filter.</param>
        /// <returns>True if successful.</returns>
        public static bool AddToStream(PhpStream stream, string filter, FilterChainOptions where, PhpValue parameters)
        {
            PhpFilter readFilter, writeFilter;

            if ((stream.Options & StreamAccessOptions.Read) == 0) where &= ~FilterChainOptions.Read;
            if ((stream.Options & StreamAccessOptions.Write) == 0) where &= ~FilterChainOptions.Write;

            if ((where & FilterChainOptions.Read) > 0)
            {
                if (!GetFilter(filter, true, out readFilter, parameters))
                {
                    //PhpException.Throw(PhpError.Warning, CoreResources.GetString("invalid_filter_name", filter));
                    //return false;
                    throw new ArgumentException(nameof(filter));
                }

                stream.AddFilter(readFilter, where);
                readFilter.OnCreate();
                // Add to chain, (filters buffers too).
            }

            if ((where & FilterChainOptions.Write) > 0)
            {
                if (!GetFilter(filter, true, out writeFilter, parameters))
                {
                    //PhpException.Throw(PhpError.Warning, CoreResources.GetString("invalid_filter_name", filter));
                    //return false;
                    throw new ArgumentException(nameof(filter));
                }

                stream.AddFilter(writeFilter, where);
                writeFilter.OnCreate();
                // Add to chain.
            }

            return true;
        }

        #endregion

        #region Implemented Filters

        /// <summary>
        /// Searches for a filter implementation in the known <see cref="PhpFilter"/> descendants.
        /// </summary>
        /// <param name="filter">The name of the filter (may contain wildcards).</param>
        /// <param name="instantiate"><c>true</c> to fille <paramref name="instance"/> with a new instance of that filter.</param>
        /// <param name="instance">Filled with a new instance of an implemented filter if <paramref name="instantiate"/>.</param>
        /// <param name="parameters">Additional parameters for the filter.</param>
        /// <returns><c>true</c> if a filter with the given name was found.</returns>
        internal static bool GetFilter(string filter, bool instantiate, out PhpFilter instance, PhpValue parameters)
        {
            instance = null;

            foreach (IFilterFactory factory in systemFilters)
                if (factory.GetImplementedFilter(filter, instantiate, out instance, parameters))
                {
                    if (instance != null)
                        instance.FilterName = filter;

                    return true;
                }

            // TODO: the registered filter names may be wildcards - use fnmatch.
            string classname;
            if ((UserFilters != null) && (UserFilters.TryGetValue(filter, out classname)))
            {
                if (instantiate)
                {
                    // EX: [PhpFilter.GetFilter] create a new user filter; and support the WILDCARD naming too.
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Registers a user stream filter.
        /// </summary>
        /// <param name="filter">The name of the filter (may contain wildcards).</param>
        /// <param name="classname">The PHP user class (derived from <c>php_user_filter</c>) implementing the filter.</param>
        /// <returns><c>true</c> if the filter was succesfully added, <c>false</c> if the filter of such name already exists.</returns>
        public static bool AddUserFilter(string filter, string classname)
        {
            // Note: have to check for wildcard conflicts too (?)
            PhpFilter instance;
            if (GetFilter(filter, false, out instance, PhpValue.Null))
            {
                // EX: [PhpFilter.Register] stringtable - filter already exists, check the filter name string?
                return false;
            }

            // Check the given filter for validity?

            UserFilters.Add(filter, classname);
            return true;
        }

        /// <summary>
        /// Register a built-in stream filter factory.
        /// </summary>
        /// <param name="factory">The filter factory.</param>
        /// <returns><c>true</c> if successfully added.</returns>
        public static bool AddSystemFilter(IFilterFactory factory)
        {
            PhpFilter instance;
            bool ok = true;
            foreach (string filter in factory.GetImplementedFilterNames())
                if (GetFilter(filter, false, out instance, PhpValue.Null)) ok = false;
            Debug.Assert(ok);

            systemFilters.Add(factory);
            return ok;
        }

        /// <summary>
        /// Retrieves the list of registered filters.
        /// </summary>
        /// <returns>A <see cref="PhpArray"/> containing the names of available filters.</returns>
        public static IEnumerable<string> GetFilterNames()
        {
            var set = new HashSet<string>();
            foreach (IFilterFactory factory in systemFilters)
            {
                set.UnionWith(factory.GetImplementedFilterNames());
            }

            if (UserFilters != null)
            {
                set.UnionWith(UserFilters.Keys);
            }

            return set;
        }

        /// <summary>
        /// Gets or sets the collection of user filtername:classname associations.
        /// </summary>
        private static Dictionary<string, string> UserFilters
        {
            get
            {
                // EX: store userfilters in ScriptContext.
                return null;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>The list of built-in filters.</summary>
		private static List<IFilterFactory> systemFilters = new List<IFilterFactory>(); // TODO: thread safe

        #endregion
    }

    #endregion
}
