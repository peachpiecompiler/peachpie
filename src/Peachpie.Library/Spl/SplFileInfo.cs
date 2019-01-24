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

        protected internal SplFileInfo(FileSystemInfo/*!*/entry)
        {
            _entry = entry;
            _fullpath = entry.FullName;
        }

        public virtual void __construct(Context ctx, string file_name)
        {
            _fullpath = FileSystemUtils.AbsolutePath(ctx, file_name);
        }

        public virtual long getATime() { throw new NotImplementedException(); }
        public virtual string getBasename(string suffix = null) => PhpPath.basename(_fullpath, suffix);
        public virtual long getCTime() { throw new NotImplementedException(); }
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
        public virtual string getFilename() => ResolvedInfo.Name;
        public virtual long getGroup() { throw new NotImplementedException(); }
        public virtual long getInode() { throw new NotImplementedException(); }
        public virtual string getLinkTarget() { throw new NotImplementedException(); }
        public virtual long getMTime() { throw new NotImplementedException(); }
        public virtual long getOwner() { throw new NotImplementedException(); }
        public virtual string getPath() => PhpPath.dirname(_fullpath);
        public virtual SplFileInfo getPathInfo(Context ctx, string class_name = null) => CreateFileInfo(ctx, class_name ?? _info_class, PhpPath.dirname(_fullpath));
        public virtual string getPathname() => _fullpath;
        public virtual long getPerms() { throw new NotImplementedException(); }
        public virtual string getRealPath(Context ctx) => ResolvedInfo.FullName;
        public virtual long getSize() { throw new NotImplementedException(); }
        public virtual string getType() { throw new NotImplementedException(); }
        public virtual bool isDir() => ResolvedInfo.Exists && ResolvedInfo is DirectoryInfo;
        public virtual bool isExecutable() { throw new NotImplementedException(); }
        public virtual bool isFile() => ResolvedInfo.Exists && ResolvedInfo is FileInfo;
        public virtual bool isLink() { throw new NotImplementedException(); }
        public virtual bool isReadable() { throw new NotImplementedException(); }
        public virtual bool isWritable() { throw new NotImplementedException(); }
        public virtual SplFileObject openFile(string open_mode = "r", bool use_include_path = false, PhpResource context = null) { throw new NotImplementedException(); /* NOTE: use _file_class */ }
        public virtual void setFileClass(string class_name = "SplFileObject") => _file_class = class_name;
        public virtual void setInfoClass(string class_name = "SplFileInfo") => _info_class = class_name;
        public virtual string __toString() => _fullpath;
        public override string ToString() => _fullpath;
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
    }

    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class SplTempFileObject : SplFileObject
    {
        public SplTempFileObject(long max_memory = 2 * 1024 * 1024 /*2MB*/)
        {
            throw new NotImplementedException();
        }
    }
}
