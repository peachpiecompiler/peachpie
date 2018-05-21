using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// Provides encoding to be used for conversion between byte array and unicode string.
    /// </summary>
    public interface IEncodingProvider
    {
        /// <summary>
        /// Encoding used to convert between unicode strings and binary strings.
        /// </summary>
        Encoding StringEncoding { get; }
    }

    /// <summary>
    /// <see cref="IEncodingProvider"/> providing <see cref="Encoding.UTF8"/>.
    /// </summary>
    public sealed class Utf8EncodingProvider : IEncodingProvider
    {
        private Utf8EncodingProvider() { }

        /// <summary>
        /// Singletong instance.
        /// </summary>
        public static readonly IEncodingProvider Instance = new Utf8EncodingProvider();

        /// <summary>
        /// Gets UTF8.
        /// </summary>
        public Encoding StringEncoding => Encoding.UTF8;
    }
}
