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
    public class DirectoryIterator : SplFileInfo, SeekableIterator
    {
        int _index; // where we are pointing right now
        FileSystemInfo[] _children;
        DirectoryInfo _original;

        int Position
        {
            get => _index;
            set
            {
                _index = value;

                // . always listed
                if (value == 0)
                {
                    _entry = _original;
                }
                else
                {
                    // .. listed ?
                    var hasparent = (_original.Parent != null);
                    if (hasparent && value == 1)
                    {
                        _entry = _original.Parent;
                    }
                    else
                    {
                        // the rest
                        var index = hasparent ? value - 2 : value - 1;

                        if (index < 0) throw new ArgumentException();

                        if (index >= _children.Length)
                        {
                            _entry = _original;
                            _fullpath = string.Empty; // not valid()
                            return;
                        }

                        _entry = _children[index];
                    }
                }

                _fullpath = _entry.FullName;
            }
        }

        public DirectoryIterator(Context ctx, string path) : base(ctx, path)
        {
        }

        protected internal DirectoryIterator(FileSystemInfo/*!*/entry)
            : base(entry)
        {
        }

        [PhpFieldsOnlyCtor]
        protected DirectoryIterator()
        {
        }

        public override void __construct(Context ctx, string path)
        {
            base.__construct(ctx, path);

            _original = new DirectoryInfo(_fullpath);

            if (!_original.Exists)
            {
                throw new UnexpectedValueException();
            }

            _children = _original.GetFileSystemInfos();

            //
            Position = 0;
        }

        public override string getFilename()
        {
            if (_entry == _original) return ".";
            if (_entry.FullName.Length < _original.FullName.Length) return ".."; // do not compare with _original.Parent

            //
            return base.getFilename();
        }

        public virtual bool isDot()
        {
            return
                _entry == _original ||
                _entry.FullName.Length < _original.FullName.Length; // do not compare with _original.Parent
        }

        #region SeekableIterator

        public virtual DirectoryIterator current() => this;

        PhpValue Iterator.current() => PhpValue.FromClass(current());

        public virtual PhpValue key() => Position;

        public virtual void next()
        {
            if (valid())
            {
                Position++;
            }
        }

        public virtual void rewind()
        {
            Position = 0;
        }

        public virtual void seek(long position)
        {
            Position = (int)position;
        }

        public virtual bool valid() => !string.IsNullOrEmpty(_fullpath);

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
        protected FilesystemIterator()
        {
        }

        public FilesystemIterator(Context ctx, string path, int flags = KEY_AS_PATHNAME | CURRENT_AS_FILEINFO | SKIP_DOTS)
            : base(ctx, path)
        {
            __construct(ctx, path, flags);
        }

        public override sealed void __construct(Context ctx, string file_name)
        {
            this.__construct(ctx, file_name);
        }

        public virtual void __construct(Context ctx, string path, int flags = KEY_AS_PATHNAME | CURRENT_AS_FILEINFO | SKIP_DOTS)
        {
            base.__construct(ctx, path);
            _flags = flags;
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
        protected RecursiveDirectoryIterator()
        {
        }

        public override void __construct(Context ctx, string path, int flags = KEY_AS_PATHNAME | CURRENT_AS_FILEINFO)
        {
            base.__construct(ctx, path, flags);
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
