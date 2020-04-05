using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pchp.Core;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Pchp.Core.Reflection;

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

        public override string ToString() => IsNull ? string.Empty : IsText ? GetText() : Encoding.UTF8.GetString(GetBytes());

        public PhpString ToPhpString() => IsNull ? default(PhpString) : IsText ? new PhpString(GetText()) : new PhpString(GetBytes());

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

                case PhpTypeCode.MutableString:
                    return new TextElement(value.MutableString, ctx.StringEncoding);

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
        TextElement Filter(IEncodingProvider enc, TextElement input, bool closing);

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
        public TextElement Filter(IEncodingProvider enc, TextElement input, bool closing)
        {
            string str = input.AsText(enc.StringEncoding);

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
        public TextElement Filter(IEncodingProvider enc, TextElement input, bool closing)
        {
            return new TextElement(input.AsText(enc.StringEncoding).Replace("\n", "\r\n"));
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
        string[] GetImplementedFilterNames(Context ctx);

        /// <summary>
        /// Checks if a filter is being created by this factory and optionally returns a new instance of this filter.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="name">The name of the filter (may contain wildcards).</param>
        /// <param name="instantiate"><c>true</c> to fill <paramref name="instance"/> with a new instance of that filter.</param>
        /// <param name="instance">Filled with a new instance of an implemented filter if <paramref name="instantiate"/>.</param>
        /// <param name="parameters">Additional parameters provided to the filter constructor.</param>
        /// <returns><c>true</c> if a filter with the given name was found.</returns>
        bool GetImplementedFilter(Context ctx, string name, bool instantiate, out PhpFilter instance, PhpValue parameters);
    }

    internal class UserFilterFactory : IFilterFactory
    {
        class UserFilters
        {
            /// <summary>
            /// List of {name, filter class}.
            /// The filter class is lazily resolved from <see cref="string"/> to <see cref="Pchp.Core.Reflection.PhpTypeInfo"/>.
            /// </summary>
            readonly List<KeyValuePair<string, object>> _filters = new List<KeyValuePair<string, object>>();

            public bool TryRegisterFilter(string filter, string classname)
            {
                for (int i = 0; i < _filters.Count; i++)
                {
                    if (_filters[i].Key.EqualsOrdinalIgnoreCase(filter))
                    {
                        return false;
                    }
                }

                _filters.Add(new KeyValuePair<string, object>(filter, classname));
                return true;
            }

            public bool GetImplementedFilter(Context ctx, string name, bool instantiate, out PhpFilter instance, PhpValue parameters)
            {
                instance = null;

                for (int i = 0; i < _filters.Count; i++)
                {
                    var pair = _filters[i];

                    // TODO: wildcard
                    if (pair.Key.EqualsOrdinalIgnoreCase(name))
                    {
                        if (instantiate)
                        {
                            var tinfo = pair.Value as PhpTypeInfo;
                            if (tinfo == null)
                            {
                                Debug.Assert(pair.Value is string);
                                tinfo = ctx.GetDeclaredTypeOrThrow((string)pair.Value, autoload: true);

                                if (tinfo != null) // always true
                                {
                                    _filters[i] = new KeyValuePair<string, object>(pair.Key, tinfo);
                                }
                                else
                                {
                                    throw null; // unreachable
                                }
                            }

                            instance = (php_user_filter)tinfo.Creator(ctx);
                        }

                        return true;
                    }
                }

                return false;
            }

            public string[] GetImplementedFilterNames()
            {
                if (_filters.Count == 0)
                {
                    return Array.Empty<string>();
                }

                //

                var arr = new string[_filters.Count];
                for (int i = 0; i < arr.Length; i++)
                {
                    arr[i] = _filters[i].Key;
                }

                return arr;
            }
        }

        public static bool TryRegisterFilter(Context ctx, string filter, string classname)
        {
            return ctx.GetStatic<UserFilters>().TryRegisterFilter(filter, classname);
        }

        public bool GetImplementedFilter(Context ctx, string name, bool instantiate, out PhpFilter instance, PhpValue parameters)
        {
            if (ctx.TryGetStatic<UserFilters>(out var filters))
            {
                return filters.GetImplementedFilter(ctx, name, instantiate, out instance, parameters);
            }

            instance = null;
            return false;
        }

        public string[] GetImplementedFilterNames(Context ctx)
        {
            if (ctx.TryGetStatic<UserFilters>(out var filters))
            {
                return filters.GetImplementedFilterNames();
            }
            else
            {
                return Array.Empty<string>();
            }
        }
    }

    /// <summary>
    /// Base class for PHP stream filters.
    /// </summary>
    public abstract class PhpFilter : IFilter
    {
        /// <summary>
        /// The filter name, same as the name used for creating the filter (see GetFilter).
        /// </summary>
        public string filtername { get; internal set; } = string.Empty;

        /// <summary>
        /// An additional <c>mixed</c> parameter passed at <c>stream_filter_append/prepend</c>.
        /// </summary>
        public PhpValue @params { get; internal set; } = string.Empty;

        #region IFilter Overrides

        /// <summary>
        /// Processes the <paramref name="input"/> (either of type <see cref="string"/> or <see cref="byte"/>[]) 
        /// data and returns the filtered data in one of the formats above or <c>null</c>.
        /// </summary>
        public abstract TextElement Filter(IEncodingProvider enc, TextElement input, bool closing);

        /// <summary>
        /// Called when the filter is attached to a stream.
        /// </summary>
        public void OnCreate() { }

        /// <summary>
        /// Called when the containig stream is being closed.
        /// </summary>
        public void OnClose() { }

        #endregion

        #region Stream Filter Chain Access

        /// <summary>
        /// Insert the filter into the filter chains.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="stream">Which stream's filter chains.</param>
        /// <param name="filter">What filter.</param>
        /// <param name="where">What position in the chains.</param>
        /// <param name="parameters">Additional parameters for the filter.</param>
        /// <returns>Filters that have been added.</returns>
        public static (PhpFilter readFilter, PhpFilter writeFilter) AddToStream(Context ctx, PhpStream stream, string filter, FilterChainOptions where, PhpValue parameters)
        {
            if ((stream.Options & StreamAccessOptions.Read) == 0) where &= ~FilterChainOptions.Read;
            if ((stream.Options & StreamAccessOptions.Write) == 0) where &= ~FilterChainOptions.Write;

            PhpFilter readFilter = null, writeFilter = null;

            if ((where & FilterChainOptions.Read) != 0)
            {
                if (GetFilter(ctx, filter, true, out readFilter, parameters))
                {
                    stream.AddFilter(readFilter, where);
                    readFilter.OnCreate();
                    // Add to chain, (filters buffers too).
                }
                else
                {
                    PhpException.Throw(PhpError.Warning, Core.Resources.ErrResources.invalid_filter_name, filter);
                    //return false;
                    throw new ArgumentException(nameof(filter));
                }
            }

            if ((where & FilterChainOptions.Write) != 0)
            {
                if (GetFilter(ctx, filter, true, out writeFilter, parameters))
                {
                    stream.AddFilter(writeFilter, where);
                    writeFilter.OnCreate();
                    // Add to chain.
                }
                else
                {
                    PhpException.Throw(PhpError.Warning, Core.Resources.ErrResources.invalid_filter_name, filter);
                    //return false;
                    throw new ArgumentException(nameof(filter));
                }
            }

            return (readFilter, writeFilter);
        }

        #endregion

        #region Implemented Filters

        /// <summary>
        /// Searches for a filter implementation in the known <see cref="PhpFilter"/> descendants.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="filter">The name of the filter (may contain wildcards).</param>
        /// <param name="instantiate"><c>true</c> to fille <paramref name="instance"/> with a new instance of that filter.</param>
        /// <param name="instance">Filled with a new instance of an implemented filter if <paramref name="instantiate"/>.</param>
        /// <param name="parameters">Additional parameters for the filter.</param>
        /// <returns><c>true</c> if a filter with the given name was found.</returns>
        internal static bool GetFilter(Context ctx, string filter, bool instantiate, out PhpFilter instance, PhpValue parameters)
        {
            foreach (var factory in _filterFactories)
            {
                if (factory.GetImplementedFilter(ctx, filter, instantiate, out instance, parameters))
                {
                    if (instance != null)
                    {
                        instance.filtername = filter;
                        instance.@params = parameters.DeepCopy();
                    }

                    return true;
                }
            }

            instance = null;
            return false;
        }

        /// <summary>
        /// Register a built-in stream filter factory.
        /// </summary>
        /// <param name="factory">The filter factory. Must not be <c>null</c>.</param>
        public static void AddFilterFactory(IFilterFactory factory)
        {
            _filterFactories.Add(factory ?? throw new ArgumentNullException(nameof(factory)));
        }

        /// <summary>
        /// Retrieves the list of registered filters.
        /// </summary>
        /// <returns>A <see cref="PhpArray"/> containing the names of available filters.</returns>
        public static IEnumerable<string> GetFilterNames(Context ctx)
        {
            var set = new HashSet<string>();

            foreach (var factory in _filterFactories)
            {
                set.UnionWith(factory.GetImplementedFilterNames(ctx));
            }

            return set;
        }

        /// <summary>The list of filters factories.</summary>
		static readonly List<IFilterFactory> _filterFactories = new List<IFilterFactory>()  // NOTE: thread-safety not needed, the list is only being read
        {
            new UserFilterFactory(),
        };

        #endregion
    }

    /// <summary>
    /// User filter base class, the derived classes to be used by <see cref="PhpFilters.stream_filter_register"/>.
    /// </summary>
    [PhpType(PhpTypeAttribute.PhpTypeName.NameOnly), PhpExtension("standard")]
    public class php_user_filter : PhpFilter
    {
        /// <summary>
        /// Called when applying the filter.
        /// </summary>
        public virtual long filter(PhpResource @in, PhpResource @out, PhpAlias consumed, bool closing) => 0;

        /// <summary>
        /// Called when creating the filter.
        /// </summary>
        public virtual bool onCreate() => true;

        /// <summary>
        /// Called when closing the filter.
        /// </summary>
        public virtual void onClose() { }

        #region PhpFilter

        [PhpHidden]
        public sealed override TextElement /*PhpFilter.*/Filter(IEncodingProvider enc, TextElement input, bool closing)
        {
            var @in = new UserFilterBucketBrigade() { bucket = input.ToPhpString() };
            var @out = new UserFilterBucketBrigade();
            var consumed = new PhpAlias(0L);

            switch ((PhpFilters.FilterStatus)filter(@in, @out, consumed, closing))
            {
                case PhpFilters.FilterStatus.OK:
                    return new TextElement(@out.bucket, enc.StringEncoding);

                case PhpFilters.FilterStatus.MoreData:
                    return TextElement.Empty;

                case PhpFilters.FilterStatus.FatalError:
                default:
                    // silently stop feeding this filter
                    return TextElement.Null;
            }
        }

        #endregion
    }

    public sealed class UserFilterBucketBrigade : PhpResource
    {
        public UserFilterBucketBrigade()
            : base("userfilter.bucket brigade")
        {
        }

        internal PhpString bucket;

        internal long consumed = 0;
    }

    /// <summary>
    /// Object created by <c>stream_bucket_make_writeable</c> and <c>stream_​bucket_​new</c>.
    /// </summary>
    public sealed class UserFilterBucket : stdClass
    {
        // PhpResource bucket{ get; set; }
        public PhpString data;
        public long datalen;
    }

    #endregion
}
