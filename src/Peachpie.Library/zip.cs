using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Pchp.Core;
using Pchp.Core.Utilities;

namespace Pchp.Library
{
    /// <summary>
    /// Represents a zip archive.
    /// </summary>
    public sealed class ZipArchiveResource : PhpResource
    {
        internal ZipArchiveResource(System.IO.Compression.ZipArchive archive)
            : base("Zip Directory")
        {
            Archive = archive;
            Enumerator = archive.Entries.GetEnumerator();
        }

        internal System.IO.Compression.ZipArchive Archive { get; }

        internal IEnumerator<ZipArchiveEntry> Enumerator { get; }

        protected override void FreeManaged()
        {
            Archive.Dispose();

            base.FreeManaged();
        }
    }

    /// <summary>
    /// Represents an entry in a zip archive.
    /// </summary>
    public sealed class ZipEntryResource : PhpResource
    {
        internal ZipEntryResource(ZipArchiveEntry entry)
            : base("Zip Entry")
        {
            Entry = entry;
        }

        internal ZipArchiveEntry Entry { get; }

        internal Stream DataStream { get; set; }

        protected override void FreeManaged()
        {
            if (DataStream != null)
            {
                DataStream.Dispose();
            }

            base.FreeManaged();
        }
    }

    /// <summary>
    /// Zip Functions.
    /// </summary>
    [PhpExtension("zip")]
    public static class PhpZip
    {
        private const int ER_NOENT = 9;
        private const int ER_OPEN = 11;
        private const int ER_INTERNAL = 20;

        /// <summary>
        /// Opens a new zip archive for reading.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="filename">The file name of the ZIP archive to open.</param>
        /// <returns>Returns a resource handle for later use with <see cref="zip_read(ZipArchiveResource)"/>
        /// and <see cref="zip_close(ZipArchiveResource)"/> or returns the number of error if
        /// <paramref name="filename"/> does not exist or in case of other error.</returns>
        public static PhpValue zip_open(Context ctx, string filename)
        {
            try
            {
                string fullPath = FileSystemUtils.AbsolutePath(ctx, filename);
                var fileStream = File.Open(fullPath, FileMode.Open);
                var archive = new System.IO.Compression.ZipArchive(fileStream, ZipArchiveMode.Read);
                return PhpValue.FromClass(new ZipArchiveResource(archive));
            }
            catch (FileNotFoundException e)
            {
                PhpException.Throw(PhpError.Warning, e.Message);
                return ER_NOENT;
            }
            catch (IOException e)
            {
                PhpException.Throw(PhpError.Warning, e.Message);
                return ER_OPEN;
            }
        }

        /// <summary>
        /// Closes the given ZIP file archive.
        /// </summary>
        /// <param name="zip">A ZIP file previously opened with <see cref="zip_open(Context, string)"/>.</param>
        public static void zip_close(ZipArchiveResource zip) => zip?.Dispose();

        /// <summary>
        /// Reads the next entry in a zip file archive.
        /// </summary>
        /// <param name="zip">A ZIP file previously opened with <see cref="zip_open(Context, string)"/>.</param>
        /// <returns>Returns a directory entry resource for later use with the zip_entry_... functions, or FALSE
        /// if there are no more entries to read, or an error code if an error occurred.</returns>
        public static PhpValue zip_read(ZipArchiveResource zip)
        {
            if (zip == null || !zip.IsValid)
            {
                PhpException.InvalidArgument(nameof(zip));
                return false;
            }

            try
            {
                if (zip.Enumerator.MoveNext())
                {
                    return PhpValue.FromClass(new ZipEntryResource(zip.Enumerator.Current));
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                PhpException.Throw(PhpError.Warning, e.Message);
                return ER_INTERNAL;
            }
        }

        /// <summary>
        /// Returns the name of the specified directory entry.
        /// </summary>
        /// <param name="zip_entry">A directory entry returned by <see cref="zip_read"/>.</param>
        /// <returns>The name of the directory entry.</returns>
        public static string zip_entry_name(ZipEntryResource zip_entry) => zip_entry.Entry.FullName;

        /// <summary>
        /// Returns the actual size of the specified directory entry.
        /// </summary>
        /// <param name="zip_entry">A directory entry returned by <see cref="zip_read"/>.</param>
        /// <returns>The size of the directory entry.</returns>
        public static long zip_entry_filesize(ZipEntryResource zip_entry) => zip_entry.Entry.Length;

        /// <summary>
        /// Returns the compressed size of the specified directory entry.
        /// </summary>
        /// <param name="zip_entry">A directory entry returned by <see cref="zip_read(ZipArchiveResource)"/>.</param>
        /// <returns>The compressed size.</returns>
        public static long zip_entry_compressedsize(ZipEntryResource zip_entry) => zip_entry.Entry.CompressedLength;

        /// <summary>
        /// Returns the compression method of the specified directory entry.
        /// </summary>
        /// <remarks>
        /// Currently not supported.
        /// </remarks>
        /// <param name="zip_entry">A directory entry returned by <see cref="zip_read(ZipArchiveResource)"/>.</param>
        /// <returns>The compression method.</returns>
        public static string zip_entry_compressionmethod(ZipEntryResource zip_entry)
        {
            // TODO: Implement using reflection or by moving the code of ZipArchive here)
            PhpException.FunctionNotSupported(nameof(zip_entry_compressionmethod));
            return string.Empty;
        }

        /// <summary>
        /// Opens a directory entry in a zip file for reading.
        /// </summary>
        /// <param name="zip">A valid resource handle returned by <see cref="zip_open(Context, string)"/>.</param>
        /// <param name="zip_entry">A directory entry returned by <see cref="zip_read(ZipArchiveResource)"/>.</param>
        /// <param name="mode">Ignored, only binary read is supported.</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool zip_entry_open(ZipArchiveResource zip, ZipEntryResource zip_entry, string mode = "rb")
        {
            if (zip_entry == null || !zip_entry.IsValid)
            {
                PhpException.InvalidArgument(nameof(zip_entry));
                return false;
            }

            if (zip_entry.DataStream != null)
            {
                return true;
            }

            try
            {
                zip_entry.DataStream = zip_entry.Entry.Open();
                return true;
            }
            catch (Exception e)
            {
                PhpException.Throw(PhpError.Warning, e.Message);
                return false;
            }
        }

        /// <summary>
        /// Closes the specified directory entry.
        /// </summary>
        /// <param name="zip_entry">A directory entry previously opened with
        /// <see cref="zip_entry_open(ZipArchiveResource, ZipEntryResource, string)"/>.</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool zip_entry_close(ZipEntryResource zip_entry)
        {
            if (zip_entry != null)
            {
                zip_entry.Dispose();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Reads from an open directory entry.
        /// </summary>
        /// <param name="zip_entry">A directory entry returned by <see cref="zip_read(ZipArchiveResource)"/>.</param>
        /// <param name="length">The number of bytes to return.</param>
        /// <returns>Returns the data read, empty string or NULL on end of a file and on error.</returns>
        public static PhpValue zip_entry_read(ZipEntryResource zip_entry, int length = 1024)
        {
            // Although according to the PHP documentation error codes should be retrieved,
            // it returns empty strings or NULL instead
            if (zip_entry == null)
            {
                PhpException.ArgumentNull(nameof(zip_entry));
                return PhpValue.Null;
            }

            if (zip_entry == null || !zip_entry.IsValid || zip_entry.DataStream == null)
            {
                PhpException.InvalidArgument(nameof(zip_entry));
                return PhpValue.Create(new PhpString(string.Empty));
            }

            if (length <= 0)
            {
                PhpException.InvalidArgument(nameof(length));
                return PhpValue.Create(new PhpString(string.Empty));
            }

            try
            {
                var buffer = new byte[length];
                int read = zip_entry.DataStream.Read(buffer, 0, length);

                if (read != length)
                {
                    Array.Resize(ref buffer, read);
                }

                return PhpValue.Create(new PhpString(buffer));
            }
            catch (Exception e)
            {
                PhpException.Throw(PhpError.Warning, e.Message);
                return PhpValue.Create(new PhpString(string.Empty));
            }
        }
    }
}
