using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Pchp.Core;
using Pchp.Core.Utilities;
using Pchp.Library.Streams;

namespace Pchp.Library.Spl
{
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class SplFileInfo
    {
        private protected string _relativePath;     // Path string supplied in constructor (may be relative, non-cannonical etc.)
        private protected string _fullpath;
        private protected FileSystemInfo _entry;

        /// <summary>
        /// Resolves <see cref="FileSystemInfo"/> in case it is not initialized explicitly.
        /// Only applies to <see cref="SplFileInfo"/>.
        /// </summary>
        private protected FileSystemInfo ResolvedInfo
        {
            get
            {
                var entry = _entry;
                if (entry == null)
                {
                    if (System.IO.Directory.Exists(_fullpath))
                    {
                        entry = new DirectoryInfo(_fullpath);
                    }
                    else
                    {
                        entry = new FileInfo(_fullpath);
                    }

                    _entry = entry;
                }

                return entry;
            }
        }

        private protected string _info_class = "SplFileInfo";
        private protected string _file_class = "SplFileObject";

        private protected SplFileInfo CreateFileInfo(Context ctx, string class_name, string file_name)
        {
            if (string.IsNullOrEmpty(class_name) ||
                string.Equals(class_name, "SplFileInfo", StringComparison.OrdinalIgnoreCase))
            {
                return new SplFileInfo(ctx, file_name);
            }
            else
            {
                return (SplFileInfo)ctx.Create(class_name, (PhpValue)file_name);
            }
        }

        public SplFileInfo(Context ctx, string file_name)
        {
            __construct(ctx, file_name);
        }

        [PhpFieldsOnlyCtor]
        protected SplFileInfo()
        {
        }

        internal SplFileInfo(FileSystemInfo/*!*/entry, string file_name)
        {
            __construct(entry, file_name);
        }

        private protected void __construct(FileSystemInfo/*!*/entry, string file_name)
        {
            _entry = entry;
            _fullpath = entry.FullName;
            _relativePath = file_name;
        }

        public virtual void __construct(Context ctx, string file_name)
        {
            _relativePath = file_name;
            _fullpath = FileSystemUtils.AbsolutePath(ctx, file_name);
        }

        public virtual string getBasename(string suffix = null) => PhpPath.basename(_fullpath, suffix);

        public virtual long getATime()
        {
            if (ResolvedInfo.Exists)
            {
                return DateTimeUtils.UtcToUnixTimeStamp(ResolvedInfo.LastAccessTimeUtc);
            }

            throw new RuntimeException();
        }

        public virtual long getCTime()
        {
            if (ResolvedInfo.Exists)
            {
                return DateTimeUtils.UtcToUnixTimeStamp(ResolvedInfo.CreationTimeUtc);
            }

            throw new RuntimeException();
        }

        public virtual long getMTime()
        {
            if (ResolvedInfo.Exists)
            {
                return DateTimeUtils.UtcToUnixTimeStamp(ResolvedInfo.LastWriteTimeUtc);
            }

            throw new RuntimeException();
        }

        public virtual string getExtension()
        {
            var ext = ResolvedInfo.Extension;
            if (string.IsNullOrEmpty(ext))
            {
                return string.Empty;
            }
            Debug.Assert(ext[0] == '.');
            return ext.Substring(1);
        }

        public virtual SplFileInfo getFileInfo(Context ctx, string class_name = null) => CreateFileInfo(ctx, class_name ?? _info_class, _fullpath);
        public virtual string getFilename() => PhpPath.basename(_relativePath);
        public virtual long getGroup() { throw new NotImplementedException(); }
        public virtual long getInode() { throw new NotImplementedException(); }
        public virtual string getLinkTarget() { throw new NotImplementedException(); }
        public virtual long getOwner() { throw new NotImplementedException(); }
        public virtual string getPath() => PhpPath.dirname(_relativePath);
        public virtual SplFileInfo getPathInfo(Context ctx, string class_name = null) => CreateFileInfo(ctx, class_name ?? _info_class, PhpPath.dirname(_fullpath));
        public virtual string getPathname() => _relativePath;
        public virtual long getPerms() { throw new NotImplementedException(); }

        [return: CastToFalse]
        public virtual string getRealPath(Context ctx) => ResolvedInfo.FullName;

        public virtual long getSize() => (ResolvedInfo is FileInfo finfo) ? finfo.Length : 0;

        /// <summary>
        /// Returns the type of the file referenced.
        /// </summary>
        /// <returns>A string representing the type of the entry. May be one of <c>file</c>, <c>link</c>, or <c>dir</c>.</returns>
        /// <exception cref="RuntimeException">Throws a RuntimeException on error.</exception>
        [return: NotNull]
        public virtual string getType()
        {
            if (ResolvedInfo.Exists)
            {
                //var attrs = ResolvedInfo.Attributes;
                //if ((attrs & FileAttributes.Directory) != 0) return "dir";
                if (ResolvedInfo is FileInfo) return "file";
                if (ResolvedInfo is DirectoryInfo) return "dir";
                // TODO: symbolic link
            }

            throw new RuntimeException();
        }

        public virtual bool isDir() => ResolvedInfo.Exists && ResolvedInfo is DirectoryInfo;
        public virtual bool isExecutable() { throw new NotImplementedException(); }
        public virtual bool isFile() => ResolvedInfo.Exists && ResolvedInfo is FileInfo;
        public virtual bool isLink() => ResolvedInfo.Exists && (ResolvedInfo.Attributes & FileAttributes.ReparsePoint) != 0; // TODO: review determining symbolic links, ReparsePoint is usually right (at least on NTFS)
        public virtual bool isReadable() => ResolvedInfo.Exists;
        public virtual bool isWritable() => ResolvedInfo.Exists && !(ResolvedInfo is FileInfo && ResolvedInfo.Attributes.HasFlag(FileAttributes.ReadOnly));
        public virtual SplFileObject openFile(Context ctx, string open_mode = "r", bool use_include_path = false, PhpResource context = null)
        {
            if (string.IsNullOrEmpty(_file_class) || string.Equals(_file_class, nameof(SplFileObject), StringComparison.OrdinalIgnoreCase))
            {
                return new SplFileObject(ctx, _fullpath, open_mode, use_include_path, context);
            }
            else
            {
                return (SplFileObject)ctx.Create(_file_class, _fullpath, open_mode, use_include_path, PhpValue.FromClass(context));
            }
        }
        public virtual void setFileClass(string class_name = nameof(SplFileObject)) => _file_class = class_name;
        public virtual void setInfoClass(string class_name = nameof(SplFileInfo)) => _info_class = class_name;
        public virtual string __toString() => _relativePath;
        public override string ToString() => _relativePath;
    }

    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class SplFileObject : SplFileInfo, SeekableIterator, RecursiveIterator
    {
        public const long DROP_NEW_LINE = 1;
        public const long READ_AHEAD = 2;
        public const long SKIP_EMPTY = 4;
        public const long READ_CSV = 8;

        protected Context _ctx;

        private protected PhpStream _stream;

        public SplFileObject(Context ctx, string file_name, string open_mode = "r", bool use_include_path = false, PhpResource context = null)
        {
            __construct(ctx, file_name, open_mode, use_include_path, context);
        }

        public sealed override void __construct(Context ctx, string file_name)
        {
            __construct(ctx, file_name);
        }

        public virtual void __construct(Context ctx, string file_name, string open_mode = "r", bool use_include_path = false, PhpResource context = null)
        {
            _ctx = ctx;
            base.__construct(ctx, file_name);
            _entry = new FileInfo(_fullpath);

            var openFlags = use_include_path ? PhpPath.FileOpenOptions.UseIncludePath : PhpPath.FileOpenOptions.Empty;
            _stream = (PhpStream)PhpPath.fopen(ctx, file_name, open_mode, openFlags, context);
            if (_stream == null)
            {
                throw new RuntimeException(string.Format(Resources.Resources.file_cannot_open, file_name));
            }
        }

        [PhpFieldsOnlyCtor]
        protected SplFileObject()
        {
        }

        #region SeekableIterator, RecursiveIterator

        public virtual PhpValue/*string|array*/ current()
        {
            throw new NotImplementedException();
        }

        public virtual RecursiveIterator getChildren()
        {
            throw new NotImplementedException();
        }

        public virtual bool hasChildren()
        {
            throw new NotImplementedException();
        }

        public virtual PhpValue key()
        {
            throw new NotImplementedException();
        }

        public virtual void next()
        {
            throw new NotImplementedException();
        }

        public virtual void rewind()
        {
            throw new NotImplementedException();
        }

        public virtual void seek(long line_pos)
        {
            throw new NotImplementedException();
        }

        public virtual bool valid()
        {
            throw new NotImplementedException();
        }

        #endregion

        public virtual bool eof() => PhpPath.feof(_stream);

        public virtual bool fflush() => PhpPath.fflush(_stream);

        [return: CastToFalse]
        public virtual PhpString fgetc() => PhpPath.fgetc(_ctx, _stream);

        [return: CastToFalse]
        public virtual PhpArray fgetcsv(char delimiter = ',', char enclosure = '"', char escape = '\\') => PhpPath.fgetcsv(_stream, 0, delimiter, enclosure, escape).AsArray();

        [return: CastToFalse]
        public virtual PhpString fgets() => PhpPath.fgets(_stream);

        [return: CastToFalse]
        public virtual string fgetss(string allowable_tags = null) => PhpPath.fgetss(_stream, -1, allowable_tags);

        public virtual bool flock(int operation, ref int wouldblock) => PhpPath.flock(_stream, operation, ref wouldblock);

        [return: CastToFalse]
        public virtual int fpassthru() => PhpPath.fpassthru(_ctx, _stream);

        public virtual int fputcsv(PhpArray fields, char delimiter = ',', char enclosure = '"') => PhpPath.fputcsv(_ctx, _stream, fields, delimiter, enclosure);

        [return: CastToFalse]
        public virtual PhpString fread(int length) => PhpPath.fread(_ctx, _stream, length);

        [return: CastToFalse]
        public virtual PhpValue fscanf(string format) => PhpPath.fscanf(_stream, format);

        [return: CastToFalse]
        public virtual PhpValue fscanf(string format, PhpAlias arg, params PhpAlias[] args) => PhpPath.fscanf(_stream, format, arg, args);

        public virtual int fseek(int offset, int whence = PhpStreams.SEEK_SET) => PhpPath.fseek(_stream, offset, whence);

        public virtual PhpArray fstat() => PhpPath.fstat(_stream);

        [return: CastToFalse]
        public virtual int ftell() => PhpPath.ftell(_stream);

        public virtual bool ftruncate(int size) => PhpPath.ftruncate(_stream, size);

        [return: CastToFalse]
        public virtual int fwrite(PhpString data, int length = -1) => PhpPath.fwrite(_ctx, _stream, data, length);

        public virtual PhpArray getCsvControl() { throw new NotImplementedException(); }
        public virtual int getFlags() { throw new NotImplementedException(); }
        public virtual int getMaxLineLen() { throw new NotImplementedException(); }
        public virtual void setCsvControl(string delimiter = ",", string enclosure = "\"", string escape = "\\") { throw new NotImplementedException(); }
        public virtual void setFlags(int flags) { throw new NotImplementedException(); }
        public virtual void setMaxLineLen(int max_len) { throw new NotImplementedException(); }
        public virtual PhpString getCurrentLine() => fgets();
    }

    /// <summary>
    /// The SplTempFileObject class offers an object oriented interface for a temporary file.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class SplTempFileObject : SplFileObject
    {
        /// <summary>
        /// Helper class to lazily transfer stream contents from memory to a temporary file once the memory limit is reached.
        /// </summary>
        private class MemoryTempFileStream : NativeStream
        {
            public enum State
            {
                AlwaysMemory,
                FileWhenExceeded,
                File
            }

            private string _tmpFilePath;
            private State _state;
            private long _maxMemory;

            public MemoryTempFileStream(Context ctx, State state, long maxMemory, string tmpFilePath) :
                base(ctx, CreateNativeStream(state, maxMemory, tmpFilePath), null, StreamAccessOptions.Read | StreamAccessOptions.Write, string.Empty, StreamContext.Default)
            {
                _tmpFilePath = tmpFilePath;
                _state = state;
                _maxMemory = maxMemory;
            }

            private static Stream CreateNativeStream(State variant, long maxMemory, string tmpFilePath)
            {
                if (variant == State.AlwaysMemory)
                {
                    return new MemoryStream();
                }
                else if (variant == State.FileWhenExceeded)
                {
                    return new MemoryStream(new byte[maxMemory]);
                }
                else
                {
                    Debug.Assert(variant == State.File);
                    return CreateTmpFileStream(tmpFilePath);
                }
            }

            private static Stream CreateTmpFileStream(string tmpFilePath) => File.Open(tmpFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);

            protected override int RawWrite(byte[] buffer, int offset, int count)
            {
                if (_state == State.FileWhenExceeded)
                {
                    // If exceeding the max memory, write all the current data to a temporary file instead
                    if (this.stream.Position + count > _maxMemory)
                    {
                        var ms = (MemoryStream)this.stream;
                        long position = ms.Position;

                        this.stream = CreateTmpFileStream(_tmpFilePath);
                        ms.Position = 0;
                        ms.WriteTo(this.stream);
                        this.stream.Position = position;

                        _state = State.File;
                    }
                }

                return base.RawWrite(buffer, offset, count);
            }

            protected override void FreeUnmanaged()
            {
                base.FreeUnmanaged();

                try
                {
                    if (File.Exists(_tmpFilePath))
                    {
                        File.Delete(_tmpFilePath);
                    }
                }
                catch (Exception)
                {
                    PhpException.Throw(PhpError.Warning, Resources.Resources.file_cannot_delete, _tmpFilePath);
                }
            }
        }

        [PhpFieldsOnlyCtor]
        protected SplTempFileObject()
        {
        }

        /// <summary>
        /// Construct a new temporary file object.
        /// </summary>
        /// <param name="ctx">The current runtime context.</param>
        /// <param name="max_memory">The maximum amount of memory (in bytes, default is 2 MB) for the temporary file to use.
        /// If the temporary file exceeds this size, it will be moved to a file in the system's temp directory.
        /// If max_memory is negative, only memory will be used. If max_memory is zero, no memory will be used.</param>
        public SplTempFileObject(Context ctx, long max_memory = 2 * 1024 * 1024 /*2MB*/)
        {
            __construct(ctx, max_memory);
        }

        public sealed override void __construct(Context ctx, string file_name, string open_mode = "r", bool use_include_path = false, PhpResource context = null)
        {
            __construct(ctx);
        }

        /// <summary>
        /// Construct a new temporary file object.
        /// </summary>
        /// <param name="ctx">The current runtime context.</param>
        /// <param name="max_memory">The maximum amount of memory (in bytes, default is 2 MB) for the temporary file to use.
        /// If the temporary file exceeds this size, it will be moved to a file in the system's temp directory.
        /// If max_memory is negative, only memory will be used. If max_memory is zero, no memory will be used.</param>
        public virtual void __construct(Context ctx, long max_memory = 2 * 1024 * 1024 /*2MB*/)
        {
            _ctx = ctx;
            _fullpath = max_memory >= 0 ? $"php://temp/maxmemory:{max_memory}" : "php://memory";

            var streamState =
                max_memory > 0 ? MemoryTempFileStream.State.FileWhenExceeded :
                max_memory == 0 ? MemoryTempFileStream.State.File :
                MemoryTempFileStream.State.AlwaysMemory;
            _stream = new MemoryTempFileStream(ctx, streamState, max_memory, Path.GetTempFileName());
            _ctx.RegisterDisposable(_stream);
        }

        public override string getExtension() => string.Empty;

        public override string getFilename() => _fullpath;

        public override string getPath() => string.Empty;

        public override string getPathname() => _fullpath;

        [return: CastToFalse]
        public override string getRealPath(Context ctx) => null;

        public override bool isDir() => false;

        public override bool isFile() => false;
    }
}
