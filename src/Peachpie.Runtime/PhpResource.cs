#nullable enable

using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
	/// Base class for PHP Resources - both built-in and extension-resources.
	/// Resources rely on GC Finalization - override FreeManaged for cleanup.
	/// When printing a resource variable in PHP, "Resource id #x" prints out.
	/// </summary>
    [DebuggerDisplay("resource id='{Id}' type='{TypeName,nq}'")]
    public class PhpResource : IDisposable, IPhpConvertible, IPhpPrintable
    {
        /// <summary>The name of this variable type.</summary>
		public const string PhpTypeName = "resource";

        /// <summary>The resources' TypeName to be displayed after call to Dispose</summary>
        static string DisposedTypeName => "Unknown";

        /// <summary>
        /// An invalid resource instance.
        /// </summary>
        public static PhpResource Invalid => new PhpResource();

        /// <summary>
		/// Allocate a unique identifier for a resource.
		/// </summary>
		/// <remarks>
		/// Internal resources are given even numbers while resources
		/// allocated by extensions get odd numbers to minimize the communication
		/// between internal and external resource managers.
		/// </remarks>
		/// <returns>The unique identifier of an internal resource (even number starting from 2).</returns>
		static int RegisterInternalInstance()
        {
            // Even numbers are reserved for internal use (odd for externals)
            return Interlocked.Increment(ref s_ResourceIdCounter) * 2;
        }

        /// <summary>
        /// Create a new instance with the given Id.
        /// </summary>
        /// <param name="resourceId">Unique resource identifier (odd for external resources).</param>
        /// <param name="resourceTypeName">The type to be reported to use when dumping a resource.</param>
        protected PhpResource(int resourceId, string resourceTypeName)
        {
            _resourceId = resourceId;
            _typeName = resourceTypeName;
        }

        /// <summary>
        /// Create a new instance of a given Type and Name.
        /// The instance Id is auto-incrementing starting from 1.
        /// </summary>
        /// <param name="resourceTypeName">The type to be reported to use when dumping a resource.</param>
        public PhpResource(string resourceTypeName)
            : this(RegisterInternalInstance(), resourceTypeName)
        { }

        /// <summary>
        /// Creates a new invalid resource.
        /// </summary>
        private PhpResource()
        {
            _disposed = true;
            _typeName = DisposedTypeName;
        }

        /// <summary>
        /// Returns a string that represents the current PhpResource.
        /// </summary>
        /// <returns>'Resource id #{ID}'</returns>
        public override string ToString() => PhpTypeName + " id #" + _resourceId;

        #region IDisposable

        /// <summary>
        /// Disposes the resource.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        /// <summary>
        /// Cleans-up the resource.
        /// </summary>
        /// <remarks>
        /// When disposing non-deterministically, only unmanaged resources should be freed. 
        /// <seealso cref="FreeUnmanaged"/>
        /// </remarks>
        /// <param name="disposing">Whether the resource is disposed deterministically.</param>
        void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;

                // dispose managed resources:
                if (disposing)
                {
                    this.FreeManaged();
                }

                // dispose unmanaged resources ("unfinalized"):
                this.FreeUnmanaged();

                // shows the user this Resource is no longer valid:
                _typeName = PhpResource.DisposedTypeName;
            }
        }

        /// <summary>
        /// Override this virtual method in your descendants to perform 
        /// cleanup of Managed resources - those having a Finalizer of their own.
        /// </summary>
        /// <remarks>
        /// Note that when Disposing explicitly, both FreeManaged and FreeUnmanaged are called.
        /// </remarks>
        protected virtual void FreeManaged()
        {
        }

        /// <summary>
        /// Override this virtual method to cleanup the contained unmanaged objects.
        /// </summary>
        /// <remarks>
        /// Note that when Dispose(false) is called from the Finalizer,
        /// the order of finalization is random. In other words, contained
        /// managed objects may have been already finalized - don't reference them.
        /// </remarks>
        protected virtual void FreeUnmanaged()
        {
        }

        #endregion

        /// <summary>Identifier of a PhpResource instance. Unique index starting at 1</summary>
        public int Id => _resourceId;

        // <summary>Type of PhpResource - used by extensions and get_resource_type()</summary>
        //REMoved public int Type { get { return mType; }}

        /// <summary>Type resource name - string to be reported to user when dumping a resource.</summary>
        public string TypeName => _typeName;

        /// <summary>false if the resource has been already disposed</summary>
        public bool IsValid => !_disposed;

        /// <summary>
        /// Explicitly provide empty set of properties to be printed by var_dump or print_r.
        /// </summary>
        IEnumerable<KeyValuePair<string, PhpValue>> IPhpPrintable.Properties => Array.Empty<KeyValuePair<string, PhpValue>>();

        /// <summary>Unique resource identifier (even for internal resources, odd for external ones).</summary>
        /// <remarks>
        /// Internal resources are given even numbers while resources
        /// allocated by extensions get odd numbers to minimize the communication
        /// between internal and external resource managers.
        /// </remarks>
        protected readonly int _resourceId;

        /// <summary>
        /// Type resource name - string to be reported to user when dumping a resource.
        /// </summary>
        protected string _typeName;

        /// <summary>
        /// Set in Dispose to avoid multiple cleanup attempts.
        /// </summary>
        private bool _disposed;

        /// <summary>Static counter for unique PhpResource instance Id's.</summary>
        private static int s_ResourceIdCounter;

        #region IPhpConvertible

        double IPhpConvertible.ToDouble() => ((IPhpConvertible)this).ToLong();

        long IPhpConvertible.ToLong() => IsValid ? Id : 0;

        bool IPhpConvertible.ToBoolean() => IsValid;

        //PhpBytes IPhpConvertible.ToBinaryString();

        Convert.NumberInfo IPhpConvertible.ToNumber(out PhpNumber number)
        {
            number = PhpNumber.Create(Id);
            return Convert.NumberInfo.LongInteger;
        }

        string IPhpConvertible.ToString(Context ctx) => ToString();

        object IPhpConvertible.ToClass() => new stdClass(PhpValue.FromClass(this));

        PhpArray IPhpConvertible.ToArray() => PhpArray.New(PhpValue.FromClass(this));

        #endregion
    }
}
