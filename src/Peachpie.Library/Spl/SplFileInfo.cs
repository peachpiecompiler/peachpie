using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Pchp.Core;
using Pchp.Library.Streams;

namespace Pchp.Library.Spl
{
    [PhpType(PhpTypeAttribute.InheritName)]
    public class SplFileInfo
    {
        protected readonly Context _ctx;

        protected internal string _filename;

        private string _info_class = "SplFileInfo";
        private string _file_class = "SplFileObject";

        private SplFileInfo CreateFileInfo(string class_name, string file_name)
        {
            if (string.IsNullOrEmpty(class_name) ||
                string.Equals(class_name, "SplFileInfo", StringComparison.OrdinalIgnoreCase))
            {
                return new SplFileInfo(_ctx, file_name);
            }
            else
            {
                return (SplFileInfo)_ctx.Create(class_name, (PhpValue)file_name);
            }
        }

        public SplFileInfo(Context ctx, string file_name)
            : this(ctx)
        {
            __construct(file_name);
        }

        [PhpFieldsOnlyCtor]
        protected SplFileInfo(Context ctx)
        {
            _ctx = ctx;
        }

        public virtual void __construct(string file_name)
        {
            _filename = file_name;
        }

        public virtual long getATime() { throw new NotImplementedException(); }
        public virtual string getBasename(string suffix = null) => PhpPath.basename(_filename, suffix);
        public virtual long getCTime() { throw new NotImplementedException(); }
        public virtual string getExtension()
        {
            var ext = System.IO.Path.GetExtension(_filename);
            if (string.IsNullOrEmpty(ext))
            {
                return string.Empty;
            }
            Debug.Assert(ext[0] == '.');
            return ext.Substring(1);
        }
        public virtual SplFileInfo getFileInfo(string class_name = null) => CreateFileInfo(class_name, _filename);
        public virtual string getFilename() => _filename;
        public virtual long getGroup() { throw new NotImplementedException(); }
        public virtual long getInode() { throw new NotImplementedException(); }
        public virtual string getLinkTarget() { throw new NotImplementedException(); }
        public virtual long getMTime() { throw new NotImplementedException(); }
        public virtual long getOwner() { throw new NotImplementedException(); }
        public virtual string getPath() => PhpPath.dirname(_filename);
        public virtual SplFileInfo getPathInfo(string class_name = null) => CreateFileInfo(class_name, PhpPath.dirname(_filename));
        public virtual string getPathname() => _filename;
        public virtual long getPerms() { throw new NotImplementedException(); }
        public virtual string getRealPath() => PhpPath.realpath(_ctx, _filename);
        public virtual long getSize() { throw new NotImplementedException(); }
        public virtual string getType() { throw new NotImplementedException(); }
        public virtual bool isDir() { throw new NotImplementedException(); }
        public virtual bool isExecutable() { throw new NotImplementedException(); }
        public virtual bool isFile() { throw new NotImplementedException(); }
        public virtual bool isLink() { throw new NotImplementedException(); }
        public virtual bool isReadable() { throw new NotImplementedException(); }
        public virtual bool isWritable() { throw new NotImplementedException(); }
        public virtual SplFileObject openFile(string open_mode = "r", bool use_include_path = false, PhpResource context = null) { throw new NotImplementedException(); }
        public virtual void setFileClass(string class_name = "SplFileObject") => _file_class = class_name;
        public virtual void setInfoClass(string class_name = "SplFileInfo") => _info_class = class_name;
        public virtual string __toString() => _filename;
        public override string ToString() => _filename;
    }

    [PhpType(PhpTypeAttribute.InheritName)]
    public class SplFileObject : SplFileInfo, SeekableIterator, RecursiveIterator
    {
        public const long DROP_NEW_LINE = 1;
        public const long READ_AHEAD = 2;
        public const long SKIP_EMPTY = 4;
        public const long READ_CSV = 8;

        public SplFileObject(Context ctx, string file_name)
            : base(ctx, file_name)
        {
        }

        [PhpFieldsOnlyCtor]
        protected SplFileObject(Context ctx)
            : base(ctx)
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

        public virtual bool eof() { throw new NotImplementedException(); }
        public virtual bool fflush() { throw new NotImplementedException(); }
        public virtual string fgetc() { throw new NotImplementedException(); }
        public virtual PhpArray fgetcsv(string delimiter = ",", string enclosure = "\"", string escape = "\\") { throw new NotImplementedException(); }
        public virtual string fgets() { throw new NotImplementedException(); }
        public virtual string fgetss(string allowable_tags = null) { throw new NotImplementedException(); }
        public virtual bool flock(int operation, ref int wouldblock) { throw new NotImplementedException(); }
        public virtual int fpassthru() { throw new NotImplementedException(); }
        public virtual int fputcsv(PhpArray fields, string delimiter = ",", string enclosure = "\"", string escape = "\\") { throw new NotImplementedException(); }
        public virtual string fread(int length) { throw new NotImplementedException(); }
        public virtual PhpValue fscanf(string format, params PhpValue[] args) { throw new NotImplementedException(); }
        public virtual int fseek(int offset, int whence = PhpStreams.SEEK_SET) { throw new NotImplementedException(); }
        public virtual PhpArray fstat() { throw new NotImplementedException(); }
        public virtual int ftell() { throw new NotImplementedException(); }
        public virtual bool ftruncate(int size) { throw new NotImplementedException(); }
        public virtual int fwrite(string str, int length = -1) { throw new NotImplementedException(); }
        public virtual PhpArray getCsvControl() { throw new NotImplementedException(); }
        public virtual int getFlags() { throw new NotImplementedException(); }
        public virtual int getMaxLineLen() { throw new NotImplementedException(); }
        public virtual void setCsvControl(string delimiter = ",", string enclosure = "\"", string escape = "\\") { throw new NotImplementedException(); }
        public virtual void setFlags(int flags) { throw new NotImplementedException(); }
        public virtual void setMaxLineLen(int max_len) { throw new NotImplementedException(); }
    }

    [PhpType(PhpTypeAttribute.InheritName)]
    public class SplTempFileObject : SplFileObject
    {
        public SplTempFileObject(Context ctx, long max_memory = 2 * 1024 * 1024 /*2MB*/)
            : base(ctx)
        {
            throw new NotImplementedException();
        }
    }
}
