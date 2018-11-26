using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Pchp.Core;
using Pchp.Library.Streams;

namespace Pchp.Library.Spl
{
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class DirectoryIterator : SplFileInfo, SeekableIterator
    {
        public DirectoryIterator(Context ctx, string file_name)
            : base(ctx, file_name)
        {
        }

        [PhpFieldsOnlyCtor]
        protected DirectoryIterator(Context ctx)
            : base(ctx)
        {
        }

        public override void __construct(string file_name)
        {
            base.__construct(file_name);
        }

        #region SeekableIterator

        public virtual DirectoryIterator current()
        {
            throw new NotImplementedException();
        }

        PhpValue Iterator.current() => PhpValue.FromClass(current());

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

        public virtual void seek(long position)
        {
            throw new NotImplementedException();
        }

        public virtual bool valid()
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class FilesystemIterator : DirectoryIterator
    {
        public const int CURRENT_AS_PATHNAME = 32;
        public const int CURRENT_AS_FILEINFO = 0;
        public const int CURRENT_AS_SELF = 16;
        public const int CURRENT_MODE_MASK = 240;
        public const int KEY_AS_PATHNAME = 0;
        public const int KEY_AS_FILENAME = 256;
        public const int FOLLOW_SYMLINKS = 512;
        public const int KEY_MODE_MASK = 3840;
        public const int NEW_CURRENT_AND_KEY = 256;
        public const int SKIP_DOTS = 4096;
        public const int UNIX_PATHS = 8192;

        int _flags;

        [PhpFieldsOnlyCtor]
        protected FilesystemIterator(Context ctx)
            : base(ctx)
        {
        }

        public FilesystemIterator(Context ctx, string file_name, int flags = KEY_AS_PATHNAME | CURRENT_AS_FILEINFO | SKIP_DOTS)
            : this(ctx)
        {
            __construct(file_name, flags);
        }

        public override sealed void __construct(string file_name)
        {
            this.__construct(file_name);
        }

        public virtual void __construct(string path, int flags = KEY_AS_PATHNAME | CURRENT_AS_FILEINFO | SKIP_DOTS)
        {
            _flags = flags;
            base.__construct(path);
        }

        public virtual int getFlags() => _flags;
        public virtual void setFlags(int flags) => _flags = flags;
    }

    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class RecursiveDirectoryIterator : FilesystemIterator, SeekableIterator, RecursiveIterator
    {
        public RecursiveDirectoryIterator(Context ctx, string file_name, int flags = KEY_AS_PATHNAME | CURRENT_AS_FILEINFO)
            : base(ctx, file_name, flags)
        {
        }

        [PhpFieldsOnlyCtor]
        protected RecursiveDirectoryIterator(Context ctx)
            : base(ctx)
        {
        }

        public override void __construct(string path, int flags = KEY_AS_PATHNAME | CURRENT_AS_FILEINFO)
        {
            base.__construct(path, flags);

            //
        }

        public RecursiveIterator getChildren()
        {
            throw new NotImplementedException();
        }

        public bool hasChildren()
        {
            throw new NotImplementedException();
        }

        public virtual string getSubPath() => throw new NotImplementedException();

        public virtual string getSubPathname() => throw new NotImplementedException();
    }
}
