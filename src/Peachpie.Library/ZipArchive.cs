using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Pchp.Core;
using Pchp.Core.Utilities;
using Pchp.Library.Spl;
using Pchp.Library.Streams;

namespace Pchp.Library
{
    /// <summary>
    /// A file archive, compressed with Zip.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    [PhpExtension("zip")]
    public class ZipArchive : Countable
    {
        private struct EntryLengths
        {
            public long Length;
            public long CompressedLength;
        }

        #region Constants

        public const int CREATE = 1;
        public const int OVERWRITE = 2;
        public const int EXCL = 4;
        public const int CHECKCONS = 8;

        public const int FL_NOCASE = 16;
        public const int FL_NODIR = 32;
        public const int FL_COMPRESSED = 64;
        public const int FL_UNCHANGED = 128;

        public const int CM_DEFAULT = 0;
        public const int CM_STORE = 1;
        public const int CM_SHRINK = 2;
        public const int CM_REDUCE_1 = 3;
        public const int CM_REDUCE_2 = 4;
        public const int CM_REDUCE_3 = 5;
        public const int CM_REDUCE_4 = 6;
        public const int CM_IMPLODE = 7;
        public const int CM_DEFLATE = 8;
        public const int CM_DEFLATE64 = 9;
        public const int CM_PKWARE_IMPLODE = 10;
        public const int CM_BZIP2 = 11;

        /// <summary>
        /// No error.
        /// </summary>
        public const int ER_OK = 0;

        /// <summary>
        /// Multi-disk zip archives not supported.
        /// </summary>
        public const int ER_MULTIDISK = 1;

        /// <summary>
        /// Renaming temporary file failed.
        /// </summary>
        public const int ER_RENAME = 2;

        /// <summary>
        /// Closing zip archive failed.
        /// </summary>
        public const int ER_CLOSE = 3;

        /// <summary>
        /// Seek error.
        /// </summary>
        public const int ER_SEEK = 4;

        /// <summary>
        /// Read error.
        /// </summary>
        public const int ER_READ = 5;

        /// <summary>
        /// Write error.
        /// </summary>
        public const int ER_WRITE = 6;

        /// <summary>
        /// CRC error.
        /// </summary>
        public const int ER_CRC = 7;

        /// <summary>
        /// Containing zip archive was closed.
        /// </summary>
        public const int ER_ZIPCLOSED = 8;

        /// <summary>
        /// No such file.
        /// </summary>
        public const int ER_NOENT = 9;

        /// <summary>
        /// File already exists.
        /// </summary>
        public const int ER_EXISTS = 10;

        /// <summary>
        /// Can't open file.
        /// </summary>
        public const int ER_OPEN = 11;

        /// <summary>
        /// Failure to create temporary file.
        /// </summary>
        public const int ER_TMPOPEN = 12;

        /// <summary>
        /// Zlib error.
        /// </summary>
        public const int ER_ZLIB = 13;

        /// <summary>
        /// Malloc failure.
        /// </summary>
        public const int ER_MEMORY = 14;

        /// <summary>
        /// Entry has been changed.
        /// </summary>
        public const int ER_CHANGED = 15;

        /// <summary>
        /// Compression method not supported.
        /// </summary>
        public const int ER_COMPNOTSUPP = 16;

        /// <summary>
        /// Premature EOF.
        /// </summary>
        public const int ER_EOF = 17;

        /// <summary>
        /// Invalid argument.
        /// </summary>
        public const int ER_INVAL = 18;

        /// <summary>
        /// Not a zip archive.
        /// </summary>
        public const int ER_NOZIP = 19;

        /// <summary>
        /// Internal error.
        /// </summary>
        public const int ER_INTERNAL = 20;

        /// <summary>
        /// Zip archive inconsistent.
        /// </summary>
        public const int ER_INCONS = 21;

        /// <summary>
        /// Can't remove file.
        /// </summary>
        public const int ER_REMOVE = 22;

        /// <summary>
        /// Entry has been deleted.
        /// </summary>
        public const int ER_DELETED = 23;

        #endregion

        #region Fields and properties

        private System.IO.Compression.ZipArchive _archive;

        // Once an entry's stream has been opened (even though no data were written to it),
        // its length cannot be obtained directly
        private Dictionary<ZipArchiveEntry, EntryLengths> _openedEntryLengths = new Dictionary<ZipArchiveEntry, EntryLengths>();

        /// <summary>
        /// Status of the Zip Archive.
        /// </summary>
        public int status => 0;

        /// <summary>
        /// System status of the Zip Archive.
        /// </summary>
        public int statusSys => 0;

        /// <summary>
        /// Number of files in archive.
        /// </summary>
        public int numFiles => _archive?.Entries?.Count ?? 0;

        /// <summary>
        /// File name in the file system.
        /// </summary>
        public string filename { get; private set; } = string.Empty;

        /// <summary>
        /// Comment for the archive.
        /// </summary>
        public string comment => string.Empty;

        #endregion

        #region Archive information

        /// <summary>
        /// Counts the number of files in the achive.
        /// </summary>
        public long count() => numFiles;

        /// <summary>
        /// Returns the status error message, system and/or zip messages.
        /// </summary>
        /// <returns>A string with the status message on success or FALSE on failure.</returns>
        public string getStatusString()
        {
            if (!CheckInitialized())
            {
                return null;
            }

            // According to the tests, this is shown all the time (if the archive is initialized)
            return "No error";
        }

        #endregion

        #region Archive manipulation

        /// <summary>
        /// Open a ZIP file archive.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="filename">The file name of the ZIP archive to open.</param>
        /// <param name="flags">The mode to use to open the archive.</param>
        /// <returns>TRUE on success or the error code.</returns>
        public PhpValue open(Context ctx, string filename, int flags = 0)
        {
            if ((flags & CHECKCONS) != 0)
            {
                PhpException.ArgumentValueNotSupported(nameof(flags), nameof(CHECKCONS));
            }

            if (_archive != null)
            {
                // Save the changes to the current archive before opening another one
                _archive.Dispose();
                _archive = null;
            }

            try
            {
                string fullPath = FileSystemUtils.AbsolutePath(ctx, filename);

                FileMode mode;
                if (File.Exists(fullPath))
                {
                    if ((flags & EXCL) != 0)
                    {
                        PhpException.Throw(PhpError.Warning, Resources.Resources.file_exists, fullPath);
                        return ER_EXISTS;
                    }
                    else if ((flags & OVERWRITE) != 0)
                    {
                        mode = FileMode.Truncate;
                    }
                    else
                    {
                        mode = FileMode.Open;
                    }
                }
                else
                {
                    if ((flags & CREATE) != 0)
                    {
                        mode = FileMode.Create;
                    }
                    else
                    {
                        PhpException.Throw(PhpError.Warning, Resources.Resources.file_not_exists, fullPath);
                        return ER_NOENT;
                    }
                }

                var fileStream = File.Open(fullPath, mode);
                _archive = new System.IO.Compression.ZipArchive(fileStream, ZipArchiveMode.Update);
                this.filename = fullPath;

                return true;
            }
            catch (IOException e)
            {
                PhpException.Throw(PhpError.Warning, e.Message);
                return ER_OPEN;
            }
            catch (InvalidDataException e)
            {
                PhpException.Throw(PhpError.Warning, e.Message);
                return ER_INCONS;
            }
            catch (System.Exception e)
            {
                PhpException.Throw(PhpError.Warning, e.Message);
                return ER_INTERNAL;
            }
        }

        /// <summary>
        /// Close opened or created archive and save changes.
        /// </summary>
        /// <remarks>
        /// This method is automatically called at the end of the script.
        /// </remarks>
        /// <returns>TRUE on success or FALSE on failure.</returns>
        public bool close()
        {
            if (!CheckInitialized())
            {
                return false;
            }

            _archive.Dispose();
            _archive = null;
            this.filename = string.Empty;

            return true;
        }

        /// <summary>
        /// Extract the complete archive to the specified destination.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="destination">Location where to extract the files.</param>
        /// <param name="entries">The entries to extract, currently not supported.</param>
        /// <returns>TRUE on success or FALSE on failure.</returns>
        public bool extractTo(Context ctx, string destination, PhpValue entries = default(PhpValue))
        {
            if (!CheckInitialized())
            {
                return false;
            }

            if (!Operators.IsEmpty(entries))
            {
                PhpException.ArgumentValueNotSupported(nameof(entries), entries);
                return false;
            }

            try
            {
                _archive.ExtractToDirectory(FileSystemUtils.AbsolutePath(ctx, destination));
                return true;
            }
            catch (System.Exception e)
            {
                PhpException.Throw(PhpError.Warning, e.Message);
                return false;
            }
        }

        #endregion

        #region Entry adding and deleting

        /// <summary>
        /// Adds an empty directory in the archive.
        /// </summary>
        /// <param name="dirname">The directory to add.</param>
        /// <returns>TRUE on success or FALSE on failure.</returns>
        public bool addEmptyDir(string dirname)
        {
            if (!CheckInitialized())
            {
                return false;
            }

            if (string.IsNullOrEmpty(dirname))
            {
                PhpException.InvalidArgument(nameof(dirname));
                return false;
            }

            try
            {
                CreateEntryIfNotExists(dirname);
                return true;
            }
            catch (System.Exception e)
            {
                PhpException.Throw(PhpError.Warning, e.Message);
                return false;
            }
        }

        /// <summary>
        /// Adds a file to a ZIP archive from a given path.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="filename">The path to the file to add.</param>
        /// <param name="localname">If supplied, this is the local name inside the ZIP archive that will override the filename.</param>
        /// <param name="start">For partial copy, start position.</param>
        /// <param name="length">For partial copy, length to be copied, if 0 or -1 the whole file (starting from <paramref name="start"/>) is used.</param>
        /// <returns>TRUE on success or FALSE on failure.</returns>
        public bool addFile(Context ctx, string filename, string localname = null, int start = 0, int length = 0)
        {
            if (!CheckInitialized())
            {
                return false;
            }

            ZipArchiveEntry entry = null;
            try
            {
                entry = CreateEntryIfNotExists(localname ?? Path.GetFileName(filename));
                entry.LastWriteTime = File.GetLastWriteTime(FileSystemUtils.AbsolutePath(ctx, filename));

                using (var entryStream = entry.Open())
                using (PhpStream handle = PhpStream.Open(ctx, filename, "r", StreamOpenOptions.Empty))
                {
                    if (start != 0)
                    {
                        handle.Seek(start, SeekOrigin.Begin);
                    }

                    if (length == 0 || length == -1)
                    {
                        handle.RawStream.CopyTo(entryStream);
                    }
                    else
                    {
                        // We need to copy the contents manually if the length was specified
                        var buffer = new byte[Math.Min(length, PhpStream.DefaultBufferSize)];
                        int copied = 0;
                        while (copied < length)
                        {
                            int lastCopied = handle.RawStream.Read(buffer, 0, Math.Min(buffer.Length, length - copied));
                            entryStream.Write(buffer, 0, lastCopied);
                            copied += lastCopied;
                        }
                    }
                }

                return true;
            }
            catch (System.Exception e)
            {
                PhpException.Throw(PhpError.Warning, e.Message);
                entry?.Delete();
                return false;
            }
        }

        /// <summary>
        /// Add a file to a ZIP archive using its contents.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="localname">The name of the entry to create.</param>
        /// <param name="contents">The contents to use to create the entry. It is used in a binary safe mode.</param>
        /// <returns>TRUE on success or FALSE on failure.</returns>
        public bool addFromString(Context ctx, string localname, string contents)
        {
            return addFromString(localname, ctx.StringEncoding.GetBytes(contents));
        }

        /// <summary>
        /// Add a file to a ZIP archive using its contents.
        /// </summary>
        /// <param name="localname">The name of the entry to create.</param>
        /// <param name="contents">The contents to use to create the entry. It is used in a binary safe mode.</param>
        /// <returns>TRUE on success or FALSE on failure.</returns>
        public bool addFromString(string localname, byte[] contents)
        {
            if (!CheckInitialized())
            {
                return false;
            }

            ZipArchiveEntry entry = null;
            try
            {
                entry = CreateEntryIfNotExists(localname);

                using (var entryStream = entry.Open())
                {
                    entryStream.Write(contents, 0, contents.Length);
                }

                return true;
            }
            catch (System.Exception e)
            {
                PhpException.Throw(PhpError.Warning, e.Message);
                entry?.Delete();
                return false;
            }
        }

        /// <summary>
        /// Currently not supported.
        /// </summary>
        public bool addGlob(string pattern, int flags = 0, PhpArray options = null)
        {
            PhpException.FunctionNotSupported(nameof(addGlob));
            return false;
        }

        /// <summary>
        /// Currently not supported.
        /// </summary>
        public bool addPattern(string pattern, string path = ".", PhpArray options = null)
        {
            PhpException.FunctionNotSupported(nameof(addGlob));
            return false;
        }

        /// <summary>
        /// Delete an entry in the archive using its name.
        /// </summary>
        /// <param name="name">Name of the entry to delete.</param>
        /// <returns>TRUE on success or FALSE on failure.</returns>
        public bool deleteName(string name)
        {
            if (!CheckInitialized() || string.IsNullOrEmpty(name))
            {
                return false;
            }

            return TryDeleteEntry(GetEntryByName(name));
        }

        /// <summary>
        /// Delete an entry in the archive using its index.
        /// </summary>
        /// <param name="index">Index of the entry to delete.</param>
        /// <returns>TRUE on success or FALSE on failure.</returns>
        public bool deleteIndex(int index)
        {
            if (!CheckInitialized() || index < 0)
            {
                return false;
            }

            return TryDeleteEntry(GetEntryByIndex(index));
        }

        #endregion

        #region Entry reading

        /// <summary>
        /// Returns the index of the entry in the archive.
        /// </summary>
        /// <param name="name">The name of the entry to look up.</param>
        /// <param name="flags">The flags are specified by ORing the following values,
        /// or 0 for none of them: <see cref="FL_NOCASE"/>, <see cref="FL_NODIR"/>.</param>
        /// <returns>The index of the entry on success or FALSE on failure.</returns>
        [return: CastToFalse]
        public int locateName(string name, int flags = 0)
        {
            if (!CheckInitialized())
            {
                return -1;
            }

            var entry = GetEntryByName(name, flags);
            if (entry == null)
            {
                return -1;
            }

            return _archive.Entries.IndexOf(entry);
        }

        /// <summary>
        /// Returns the name of an entry using its index.
        /// </summary>
        /// <param name="index">Index of the entry.</param>
        /// <param name="flags">Currently not supported.</param>
        /// <returns>The name on success or FALSE on failure.</returns>
        [return: CastToFalse]
        public string getNameIndex(int index, int flags = 0)
        {
            CheckFlags(flags);
            if (!CheckInitialized())
            {
                return null;
            }

            return GetEntryByIndex(index)?.FullName;
        }

        /// <summary>
        /// The function obtains information about the entry defined by its index.
        /// </summary>
        /// <param name="index">Index of the entry.</param>
        /// <param name="flags">Currently not supported.</param>
        /// <returns>An array containing the entry details or FALSE on failure.</returns>
        [return: CastToFalse]
        public PhpArray statIndex(int index, int flags = 0)
        {
            CheckFlags(flags);
            if (!CheckInitialized())
            {
                return null;
            }

            return TryGetEntryDetails(GetEntryByIndex(index));
        }

        /// <summary>
        /// The function obtains information about the entry defined by its index.
        /// </summary>
        /// <param name="name">Name of the entry.</param>
        /// <param name="flags">The flags argument specifies how the name lookup should be done.</param>
        /// <returns>An array containing the entry details or FALSE on failure.</returns>
        [return: CastToFalse]
        public PhpArray statName(string name, int flags = 0)
        {
            CheckFlags(flags, FL_NOCASE | FL_NODIR);
            if (!CheckInitialized())
            {
                return null;
            }

            return TryGetEntryDetails(GetEntryByName(name, flags));
        }

        /// <summary>
        /// Returns the entry contents using its index.
        /// </summary>
        /// <param name="index">Index of the entry.</param>
        /// <param name="length">The length to be read from the entry. If 0, then the entire entry is read.</param>
        /// <param name="flags">Currently not supported.</param>
        /// <returns>The contents of the entry on success or FALSE on failure.</returns>
        [return: CastToFalse]
        public PhpString getFromIndex(int index, int length = 0, int flags = 0)
        {
            CheckFlags(flags);
            if (!CheckInitialized())
            {
                return default(PhpString);
            }

            return TryGetEntryContents(GetEntryByIndex(index), length);
        }

        /// <summary>
        /// Returns the entry contents using its index.
        /// </summary>
        /// <param name="name">Name of the entry.</param>
        /// <param name="length">The length to be read from the entry. If 0, then the entire entry is read.</param>
        /// <param name="flags">The flags argument specifies how the name lookup should be done.</param>
        /// <returns>The contents of the entry on success or FALSE on failure.</returns>
        [return: CastToFalse]
        public PhpString getFromName(string name, int length = 0, int flags = 0)
        {
            CheckFlags(flags, FL_NOCASE);
            if (!CheckInitialized())
            {
                return default(PhpString);
            }

            return TryGetEntryContents(GetEntryByName(name, flags), length);
        }

        /// <summary>
        /// Get a file handler to the entry defined by its name. For now it only supports read operations.
        /// </summary>
        /// <param name="ctx">Current runtime context.</param>
        /// <param name="name">The name of the entry to use.</param>
        /// <returns>A file pointer (resource) on success or FALSE on failure.</returns>
        [return: CastToFalse]
        public PhpStream getStream(Context ctx, string name)
        {
            if (!CheckInitialized())
            {
                return null;
            }

            var entry = GetEntryByName(name);
            if (entry == null)
            {
                return null;
            }

            try
            {
                // TODO: Create a proper stream wrapper instead
                return new NativeStream(ctx, OpenEntryStream(entry), null, StreamAccessOptions.Read, $"zip://{this.filename}#{name}", new StreamContext());
            }
            catch (System.Exception e)
            {
                PhpException.Throw(PhpError.Warning, e.Message);
                return null;
            }
        }

        #endregion

        #region Unsupported property getters and setters

        /// <summary>
        /// Currently not supported.
        /// </summary>
        public bool getArchiveComment(int flags = 0)
        {
            PhpException.FunctionNotSupported(nameof(getArchiveComment));
            return false;
        }

        /// <summary>
        /// Currently not supported.
        /// </summary>
        public bool getCommentIndex(int index, int flags = 0)
        {
            PhpException.FunctionNotSupported(nameof(getCommentIndex));
            return false;
        }

        /// <summary>
        /// Currently not supported.
        /// </summary>
        public bool getCommentName(string name, int flags = 0)
        {
            PhpException.FunctionNotSupported(nameof(getCommentName));
            return false;
        }

        /// <summary>
        /// Currently not supported.
        /// </summary>
        public bool getExternalAttributesIndex(int index, ref int opsys, ref int attr, int flags = 0)
        {
            PhpException.FunctionNotSupported(nameof(getExternalAttributesIndex));
            return false;
        }

        /// <summary>
        /// Currently not supported.
        /// </summary>
        public bool getExternalAttributesName(string name, ref int opsys, ref int attr, int flags = 0)
        {
            PhpException.FunctionNotSupported(nameof(getExternalAttributesName));
            return false;
        }

        /// <summary>
        /// Currently not supported.
        /// </summary>
        public bool setArchiveComment(string comment)
        {
            PhpException.FunctionNotSupported(nameof(setArchiveComment));
            return false;
        }

        /// <summary>
        /// Currently not supported.
        /// </summary>
        public bool setCommentIndex(int index, string comment)
        {
            PhpException.FunctionNotSupported(nameof(setCommentIndex));
            return false;
        }

        /// <summary>
        /// Currently not supported.
        /// </summary>
        public bool setCommentName(string name, string comment)
        {
            PhpException.FunctionNotSupported(nameof(setCommentName));
            return false;
        }

        /// <summary>
        /// Currently not supported.
        /// </summary>
        public bool setEncryptionIndex(int index, string method, string password = null)
        {
            PhpException.FunctionNotSupported(nameof(setEncryptionIndex));
            return false;
        }

        /// <summary>
        /// Currently not supported.
        /// </summary>
        public bool setEncryptionName(string name, string method, string password = null)
        {
            PhpException.FunctionNotSupported(nameof(setEncryptionName));
            return false;
        }

        /// <summary>
        /// Currently not supported.
        /// </summary>
        public bool setExternalAttributesIndex(int index, int opsys, int attr, int flags = 0)
        {
            PhpException.FunctionNotSupported(nameof(setExternalAttributesIndex));
            return false;
        }

        /// <summary>
        /// Currently not supported.
        /// </summary>
        public bool setExternalAttributesName(string name, int opsys, int attr, int flags = 0)
        {
            PhpException.FunctionNotSupported(nameof(setExternalAttributesName));
            return false;
        }

        /// <summary>
        /// Currently not supported.
        /// </summary>
        public bool setCompressionIndex(int index, int comp_method, int comp_flags = 0)
        {
            PhpException.FunctionNotSupported(nameof(setCompressionIndex));
            return false;
        }

        /// <summary>
        /// Currently not supported.
        /// </summary>
        public bool setCompressionName(string name, int comp_method, int comp_flags = 0)
        {
            PhpException.FunctionNotSupported(nameof(setCompressionName));
            return false;
        }

        /// <summary>
        /// Currently not supported.
        /// </summary>
        public bool setPassword(string password)
        {
            PhpException.FunctionNotSupported(nameof(setPassword));
            return false;
        }

        #endregion

        #region Unsupported operations

        /// <summary>
        /// Currently not supported.
        /// </summary>
        public bool unchangeAll()
        {
            PhpException.FunctionNotSupported(nameof(unchangeAll));
            return false;
        }

        /// <summary>
        /// Currently not supported.
        /// </summary>
        public bool unchangeArchive()
        {
            PhpException.FunctionNotSupported(nameof(unchangeArchive));
            return false;
        }

        /// <summary>
        /// Currently not supported.
        /// </summary>
        public bool unchangeIndex(int index)
        {
            PhpException.FunctionNotSupported(nameof(unchangeIndex));
            return false;
        }

        /// <summary>
        /// Currently not supported.
        /// </summary>
        public bool unchangeName(string name)
        {
            PhpException.FunctionNotSupported(nameof(unchangeName));
            return false;
        }

        /// <summary>
        /// Currently not supported.
        /// </summary>
        public bool renameName(string name, string newname)
        {
            PhpException.FunctionNotSupported(nameof(renameName));
            return false;
        }

        /// <summary>
        /// Currently not supported.
        /// </summary>
        public bool renameIndex(int index, string newname)
        {
            PhpException.FunctionNotSupported(nameof(renameIndex));
            return false;
        }

        #endregion

        #region Helper methods

        private static void CheckFlags(int flags, int supported = 0)
        {
            if ((flags & ~supported) != 0)
            {
                PhpException.ArgumentValueNotSupported(nameof(flags), flags);
            }
        }

        private bool CheckInitialized()
        {
            if (_archive != null)
            {
                return true;
            }
            else
            {
                PhpException.Throw(PhpError.Warning, Resources.Resources.zip_archive_uninitialized);
                return false;
            }
        }

        /// <summary>
        /// Creates an archive entry or throw an <see cref="InvalidOperationException"/> if already exists.
        /// </summary>
        private ZipArchiveEntry CreateEntryIfNotExists(string entryName)
        {
            if (_archive.GetEntry(entryName) != null)
            {
                throw new InvalidOperationException(string.Format(Resources.Resources.zip_entry_exists, entryName));
            }

            return _archive.CreateEntry(entryName);
        }

        private ZipArchiveEntry GetEntryByIndex(int index) => (index >= 0 && index < this.numFiles) ? _archive.Entries[index] : null;

        private ZipArchiveEntry GetEntryByName(string name, int flags = 0)
        {
            ZipArchiveEntry entry;
            if ((flags & FL_NOCASE) == 0)
            {
                entry = _archive.GetEntry(name);
            }
            else
            {
                entry = _archive.Entries
                    .FirstOrDefault(e => string.Equals(e.FullName, name, StringComparison.CurrentCultureIgnoreCase));
            }

            if (entry == null || ((flags & FL_NODIR) != 0) && GetEntryLengths(entry).Length == 0)
            {
                return null;
            }
            else
            {
                return entry;
            }
        }

        private PhpArray TryGetEntryDetails(ZipArchiveEntry entry)
        {
            if (entry == null)
            {
                return null;
            }

            var lengths = GetEntryLengths(entry);
            var result = new PhpArray(6);
            result.Add("name", entry.FullName);
            result.Add("index", _archive.Entries.IndexOf(entry));
            // TODO: ["crc"]
            result.Add("size", lengths.Length);
            result.Add("mtime", entry.LastWriteTime.ToUnixTimeSeconds());
            result.Add("comp_size", lengths.CompressedLength);
            result.Add("comp_method", GuessCompressionMethod(entry));

            return result;
        }

        private int GuessCompressionMethod(ZipArchiveEntry entry)
        {
            // Other methods than these two are rarely used
            var lengths = GetEntryLengths(entry);
            return (lengths.CompressedLength == lengths.Length) ? CM_STORE : CM_DEFLATE;
        }

        private EntryLengths GetEntryLengths(ZipArchiveEntry entry)
        {
            if (_openedEntryLengths.TryGetValue(entry, out var lengths))
            {
                return lengths;
            }
            else
            {
                return new EntryLengths() { Length = entry.Length, CompressedLength = entry.CompressedLength };
            }
        }

        private Stream OpenEntryStream(ZipArchiveEntry entry)
        {
            if (!_openedEntryLengths.ContainsKey(entry))
            {
                _openedEntryLengths[entry] = new EntryLengths() { Length = entry.Length, CompressedLength = entry.CompressedLength };
            }

            return entry.Open();
        }

        private PhpString TryGetEntryContents(ZipArchiveEntry entry, int length)
        {
            if (entry == null)
            {
                return default(PhpString);
            }

            if (length == 0)
            {
                length = (int)GetEntryLengths(entry).Length;
            }

            try
            {
                using (var stream = OpenEntryStream(entry))
                {
                    var buffer = new byte[length];
                    int read = stream.Read(buffer, 0, length);

                    if (read != length)
                    {
                        Array.Resize(ref buffer, read);
                    }

                    return new PhpString(buffer);
                }
            }
            catch (System.Exception e)
            {
                PhpException.Throw(PhpError.Warning, e.Message);
                return default(PhpString);
            }
        }

        private bool TryDeleteEntry(ZipArchiveEntry entry)
        {
            if (entry == null)
            {
                return false;
            }

            try
            {
                entry.Delete();
                return true;
            }
            catch (IOException e)
            {
                PhpException.Throw(PhpError.Warning, e.Message);
                return false;
            }
        }

        #endregion
    }
}
