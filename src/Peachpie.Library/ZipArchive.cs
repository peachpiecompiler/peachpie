using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Pchp.Core;
using Pchp.Library.Spl;
using Pchp.Library.Streams;

namespace Pchp.Library
{
    [PhpType(PhpTypeAttribute.InheritName)]
    [PhpExtension("zip")]
    public class ZipArchive //: Countable
    {
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

        public int status => 0;

        public int statusSys => 0;

        public int numFiles => _archive?.Entries?.Count ?? 0;

        public string filename { get; private set; } = string.Empty;

        public string comment => string.Empty;

        #endregion

        #region Opening and closing

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
                string fullPath = PhpPath.AbsolutePath(ctx, filename);

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

        #endregion

        #region Adding entries

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

        public bool addFile(Context ctx, string filename, string localname = null, int start = 0, int length = 0)
        {
            if (!CheckInitialized())
            {
                return false;
            }

            try
            {
                var entry = CreateEntryIfNotExists(localname ?? Path.GetFileName(filename));

                using (var entryStream = entry.Open())
                using (PhpStream handle = PhpStream.Open(ctx, filename, "r", StreamOpenOptions.Empty))
                {
                    if (start > 0)
                    {
                        handle.Seek(start, SeekOrigin.Begin);
                    }

                    if (length == 0)
                    {
                        handle.RawStream.CopyTo(entryStream);
                    }
                    else
                    {
                        var data = handle.ReadBytes(length);
                        entryStream.Write(data, 0, data.Length);
                    }
                }

                return true;
            }
            catch (System.Exception e)
            {
                PhpException.Throw(PhpError.Warning, e.Message);
                return false;
            }
        }

        public bool addFromString(Context ctx, string localname, string contents)
        {
            return addFromString(localname, ctx.StringEncoding.GetBytes(contents));
        }

        public bool addFromString(string localname, byte[] contents)
        {
            if (!CheckInitialized())
            {
                return false;
            }

            try
            {
                var entry = CreateEntryIfNotExists(localname);

                using (var entryStream = entry.Open())
                {
                    entryStream.Write(contents, 0, contents.Length);
                }

                return true;
            }
            catch (System.Exception e)
            {
                PhpException.Throw(PhpError.Warning, e.Message);
                return false;
            }
        }

        public bool addGlob(string pattern, int flags = 0, PhpArray options = null)
        {
            PhpException.FunctionNotSupported(nameof(addGlob));
            return false;
        }

        public bool addPattern(string pattern, string path = ".", PhpArray options = null)
        {
            PhpException.FunctionNotSupported(nameof(addGlob));
            return false;
        }

        #endregion

        #region Helper methods

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

        #endregion
    }
}
