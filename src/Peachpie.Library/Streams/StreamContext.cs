using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Library.Streams
{
    /// <summary>
	/// Resource type used for associating additional options with stream wrappers.
	/// </summary>
	/// <remarks>
	/// Stream Contexts are stored in a Resource to save useless deep-copying
	/// of the contained constant array.
	/// </remarks>
	public class StreamContext : PhpResource
    {
        /// <summary>
        /// Default StreamContext. Cannot be null.
        /// </summary>
		public static readonly StreamContext/*!*/Default = new StreamContext(null, false);

        #region Properties

        /// <summary>
        /// The contained context array (2D associative array: first wrapper, then options).
        /// </summary>
        public PhpArray Data
        {
            get { return _data; }
            set { _data = value; }
        }
        protected PhpArray _data;

        /// <summary>
        /// The additional parameters (currently only a notification callback).
        /// </summary>
        public PhpArray Parameters
        {
            get { return _parameters; }
            set { _parameters = value; }
        }
        protected PhpArray _parameters;

        /// <summary>
        /// The type name displayed when printing a variable of type StreamContext.
        /// </summary>
        public const string StreamContextTypeName = "stream-context";

        #endregion

        #region Constructors

        /// <summary>
        /// Create an empty StreamContext (allows lazy PhpArray instantiation).
        /// </summary>
        public StreamContext()
            : this(null, true) { }

        /// <summary>
		/// Create a new context resource from an array of wrapper options.
		/// </summary>
		/// <param name="data">A 2-dimensional array of wrapper options</param>
        public StreamContext(PhpArray data)
            : this(data, true) { }

        /// <summary>
        /// Create a new context resource from an array of wrapper options.
        /// </summary>
        /// <param name="data">A 2-dimensional array of wrapper options</param>
        /// <param name="registerInCtx">Whether to register this instance in current <see cref="Context"/>. Should be <c>false</c> for static resources.</param>
        private StreamContext(PhpArray data, bool registerInCtx)
            : base(StreamContextTypeName/*, registerInCtx*/)
        {
            _data = data;
        }

        #endregion

        /// <summary>
        /// Checks the context for validity, throws a warning it is not.
        /// </summary>
        /// <param name="resource">Resource which should contain a StreamContext.</param>
        /// <param name="allowNull"><c>True</c> to allow <c>NULL</c> context, that will be without any warning converted to Default <see cref="StreamContext"/>.</param>
        /// <returns>The given resource cast to <see cref="StreamContext"/> or <c>null</c> if invalid and <c>allowNull</c> is <c>false</c>.</returns>
        /// <exception cref="PhpException">In case the context is invalid.</exception>
        public static StreamContext GetValid(PhpResource resource, bool allowNull = false)
        {
            // implicit default from NULL
            if (allowNull && resource == null)
                return StreamContext.Default;

            // try to cast to StreamContext
            var result = resource as StreamContext;
            if (result != null /* TODO: Why is default context disposed? && result.IsValid*/)
                return result;

            PhpException.Throw(PhpError.Warning, Resources.LibResources.invalid_context_resource);
            return null;
        }

        /// <summary>
        /// Gets wrapper specific options.
        /// </summary>
        public PhpArray GetOptions(string scheme)
        {
            return (_data != null && _data.TryGetValue(scheme, out var options)) ? options.ArrayOrNull() : null;
        }

        /// <summary>
        /// Gets a wrapper-specific option identified by the scheme and the option name.
        /// </summary>
        /// <param name="scheme">The target wrapper scheme.</param>
        /// <param name="option">The option name.</param>
        /// <returns>The specific option or <b>null</b> if no such option exists.</returns>
        public PhpValue GetOption(string scheme, string option)
        {
            var options = GetOptions(scheme);
            var result = options != null ? options[option] : PhpValue.Null;

            //
            return result;
        }
    }
}
