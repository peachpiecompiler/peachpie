using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Pchp.Core;
using Pchp.Library.Spl;

namespace Pchp.Library.Phar
{
    /// <summary>
    /// A high-level interface to accessing and creating phar archives.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    [PhpExtension(PharExtension.ExtensionName)]
    public sealed class Phar : /*RecursiveDirectoryIterator,*/ Countable, ArrayAccess
    {
        #region .ctor

        public Phar(string fname, int flags = 0, string alias = default)
        {
            __construct(fname, flags, alias);
        }

        public void __construct(string fname, int flags = 0, string alias = default)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Countable, ArrayAccess

        public long count()
        {
            throw new NotImplementedException();
        }

        public bool offsetExists(PhpValue offset)
        {
            throw new NotImplementedException();
        }

        public PhpValue offsetGet(PhpValue offset)
        {
            throw new NotImplementedException();
        }

        public void offsetSet(PhpValue offset, PhpValue value)
        {
            throw new NotImplementedException();
        }

        public void offsetUnset(PhpValue offset)
        {
            throw new NotImplementedException();
        }

        #endregion

        /// <summary>
        /// Reads the currently executed file (a phar) and registers its manifest.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="self">Current script.</param>
        /// <param name="alias">The alias that can be used in phar:// URLs to refer to this archive, rather than its full path.</param>
        /// <param name="dataoffset">Unused.</param>
        /// <returns>Always <c>true</c>.</returns>
        public static bool mapPhar(Context ctx, [ImportValue(ImportValueAttribute.ValueSpec.CallerScript)] RuntimeTypeHandle self, string alias = default, int dataoffset = 0)
        {
            if (PharExtensions.MapPhar(ctx, Type.GetTypeFromHandle(self), alias))
            {
                return true;
            }
            else
            {
                throw new PharException();
            }
        }

        public static string running(bool retphar = true)
        {
            // 
            throw new NotImplementedException();
        }

        /// <summary>
        /// Return the API version of the phar file format that will be used when creating phars. The Phar extension supports reading API version 1.0.0 or newer.
        /// API version 1.1.0 is required for SHA-256 and SHA-512 hash,
        /// and API version 1.1.1 is required to store empty directories.
        /// </summary>
        /// <returns></returns>
        public static string apiVersion() => "1.0.0";

        /// <summary>
        /// Returns whether phar extension supports writing and creating phars.
        /// </summary>
        public static bool canWrite() => false;

        public static bool loadPhar(string filename, string alias = default) => throw new NotSupportedException();

        public static void mount(string pharpath, string externalpath) => throw new NotSupportedException();

        public static void mungServer(PhpArray munglist) => throw new NotSupportedException();

        public static bool unlinkArchive(string archive) => throw new NotSupportedException();

        public static bool webPhar(string alias = default, string index = "index.php", string f404 = default, PhpArray mimetypes = default, IPhpCallable rewrites = default) => throw new NotSupportedException();
    }
}
