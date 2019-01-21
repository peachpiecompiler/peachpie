using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Pchp.Core;
using Pchp.Core.Utilities;
using Pchp.Library.Streams;

namespace Pchp.Library.Spl
{
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class DirectoryIterator : SplFileInfo, SeekableIterator
    {
        private protected int _index; // where we are pointing right now
        private protected string _dotname; // in case we are pointing at . or .., otheriwse null

        private protected FileSystemInfo[] _children; // list of child elements
        private protected DirectoryInfo _original; // the directory we are iterating

        /// <summary>Gets or sets the internal iterator position.</summary>
        private protected int Position
        {
            get
            {
                return _index;
            }
            set
            {
                MoveTo(_index = value);
            }
        }

        private protected bool TryGetEntryOnPosition(int position, bool dots, out FileSystemInfo entry, out string dotname)
        {
            int dotscount = 0;

            if (dots)
            {
                // . always listed
                if (position == 0)
                {
                    entry = _original;
                    dotname = ".";
                    return true;
                }
                else
                {
                    // .. listed if parent
                    var parent = _original.Parent;
                    var hasparent = parent != null;

                    // ..
                    if (position == 1 && hasparent)
                    {
                        entry = parent;
                        dotname = "..";
                        return true;
                    }

                    dotscount = hasparent ? 2 : 1;
                }
            }

            dotname = null;

            // the rest
            var index = position - dotscount;

            if (index < 0)
            {
                throw new ArgumentException();
            }

            if (index >= _children.Length)
            {
                entry = default;
                return false;
            }

            entry = _children[index];

            //
            return true;
        }

        private protected virtual bool MoveTo(int position)
        {
            if (TryGetEntryOnPosition(position, true, out _entry, out _dotname))
            {
                _fullpath = _entry.FullName;
                return true;
            }
            else
            {
                _fullpath = string.Empty;
                _entry = _original;
                return false;
            }
        }

        private protected void __construct(DirectoryInfo entry, FileSystemInfo[] children = null)
        {
            _entry = entry;
            _original = entry ?? throw new ArgumentNullException();

            if (!_original.Exists)
            {
                throw new UnexpectedValueException();
            }

            _children = children ?? _original.GetFileSystemInfos();

            //
            Position = 0;
        }

        public DirectoryIterator(Context ctx, string path)
            : base(ctx, path)
        {
        }

        protected internal DirectoryIterator(FileSystemInfo/*!*/entry)
            : base(entry)
        {
            __construct((DirectoryInfo)entry);
        }

        [PhpFieldsOnlyCtor]
        protected DirectoryIterator()
        {
        }

        public override void __construct(Context ctx, string path)
        {
            base.__construct(ctx, path); // init _fullpath
            __construct(new DirectoryInfo(_fullpath));
        }

        public override string getFilename() => _dotname ?? base.getFilename();

        public virtual bool isDot() => _dotname != null;

        #region SeekableIterator

        public virtual PhpValue current() => PhpValue.FromClass(this);

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
        public const int CURRENT_AS_FILEINFO = 0;
        public const int CURRENT_AS_SELF = 16;
        public const int CURRENT_AS_PATHNAME = 32;
        public const int CURRENT_MODE_MASK = 240;

        public const int KEY_AS_PATHNAME = 0;
        public const int KEY_AS_FILENAME = 256;
        public const int KEY_MODE_MASK = 3840;

        public const int FOLLOW_SYMLINKS = 512;

        /// <summary>Combination of <see cref="KEY_AS_FILENAME"/> | <see cref="CURRENT_AS_FILEINFO"/></summary>
        public const int NEW_CURRENT_AND_KEY = KEY_AS_FILENAME | CURRENT_AS_FILEINFO;
        public const int SKIP_DOTS = 4096;
        public const int UNIX_PATHS = 8192;

        private protected int _flags;
        private protected FileSystemInfo _current;

        private protected override bool MoveTo(int position)
        {
            _index = position;

            if (!TryGetEntryOnPosition(position, (_flags & SKIP_DOTS) == 0, out _current, out _dotname))
            {
                _current = null;
            }

            //

            if ((_flags & CURRENT_AS_SELF) != 0)
            {
                _entry = _current ?? _original;
                _fullpath = _entry.FullName;
            }
            else
            {
                _dotname = null;
                _entry = _original;
                _fullpath = _original.FullName;
            }

            //
            return _current != null;
        }

        [PhpFieldsOnlyCtor]
        protected FilesystemIterator()
        {
        }

        public FilesystemIterator(Context ctx, string path, int flags = KEY_AS_PATHNAME | CURRENT_AS_FILEINFO | SKIP_DOTS)
            : base(ctx, path)
        {
            __construct(ctx, path, flags);
        }

        protected internal FilesystemIterator(DirectoryInfo/*!*/entry, int flags)
        {
            _flags = flags;
            _fullpath = entry.FullName;

            __construct(entry);
        }

        public override sealed void __construct(Context ctx, string file_name)
        {
            this.__construct(ctx, file_name);
        }

        public virtual void __construct(Context ctx, string path, int flags = KEY_AS_PATHNAME | CURRENT_AS_FILEINFO | SKIP_DOTS)
        {
            base.__construct(ctx, path);
            _flags = flags;

            //
            Position = 0;
        }

        public virtual int getFlags() => _flags;

        public virtual void setFlags(int flags) => _flags = flags;

        public override bool isDot() => false;

        #region Iterator

        public override PhpValue current()
        {
            if (_current == null)
            {
                return PhpValue.Null;
            }

            switch (_flags & CURRENT_MODE_MASK)
            {
                case CURRENT_AS_FILEINFO: return PhpValue.FromClass(new SplFileInfo(_current));
                case CURRENT_AS_PATHNAME: return _current.FullName;
                case CURRENT_AS_SELF: return PhpValue.FromClass(this);
                default: throw new InvalidOperationException();
            }
        }

        public override PhpValue key()
        {
            if (_current == null)
            {
                return PhpValue.Null;
            }

            switch (_flags & KEY_MODE_MASK)
            {
                case KEY_AS_PATHNAME: return _current.FullName;
                case KEY_AS_FILENAME: return _current.Name;
                default: throw new InvalidOperationException();
            }
        }

        public override bool valid() => _current != null;

        #endregion
    }

    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class RecursiveDirectoryIterator : FilesystemIterator, SeekableIterator, RecursiveIterator
    {
        private string subPath =  string.Empty;
        
        public RecursiveDirectoryIterator(Context ctx, string file_name, int flags = KEY_AS_PATHNAME | CURRENT_AS_FILEINFO | SKIP_DOTS)
            : base(ctx, file_name, flags)
        {
        }

        [PhpFieldsOnlyCtor]
        protected RecursiveDirectoryIterator()
        {
        }

        private protected RecursiveDirectoryIterator(DirectoryInfo entry, int flags, string subPath)
            : base(entry, flags)
        {
            this.subPath = subPath;
        }

        public override void __construct(Context ctx, string path, int flags = KEY_AS_PATHNAME | CURRENT_AS_FILEINFO)
        {
            base.__construct(ctx, path, flags);
        }

        public RecursiveIterator getChildren()
        {
            if (_current is DirectoryInfo dinfo)
            {
                return new RecursiveDirectoryIterator(
                    dinfo,
                    _flags,
                    string.IsNullOrEmpty(subPath) ? dinfo.Name : Path.Combine(subPath, dinfo.Name));
            }
            else
            {
                throw new UnexpectedValueException(); // the directory name is invalid
            }
        }

        public bool hasChildren() => _current is DirectoryInfo && _dotname == null;

        public virtual string getSubPath() => subPath;

        public virtual string getSubPathname() => string.IsNullOrEmpty(subPath) ? string.Empty : Path.GetFileName(subPath);
    }

    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class GlobIterator : FilesystemIterator, SeekableIterator, Countable
    {
        #region Construction

        public GlobIterator(Context ctx, string file_name, int flags = KEY_AS_PATHNAME | CURRENT_AS_FILEINFO)
            : base(ctx, file_name, flags)
        {
        }

        [PhpFieldsOnlyCtor]
        protected GlobIterator()
        {
        }

        public override void __construct(Context ctx, string path, int flags = KEY_AS_PATHNAME | CURRENT_AS_FILEINFO)
        {
            var children =
                PhpPath.GetMatches(ctx, path, PhpPath.GlobOptions.None)
                .Select(f => new FileInfo(f))
                .ToArray();

            string dir = children.FirstOrDefault()?.DirectoryName ?? ctx.WorkingDirectory;
            var dirInfo = new DirectoryInfo(dir);

            _fullpath = dir;
            __construct(dirInfo, children);
            _flags = flags | SKIP_DOTS;

            Position = 0;
        }

        #endregion

        public long count() => _children.LongLength;
    }
}
