using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Pchp.Core;
using Pchp.Core.Reflection;
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
        private protected string _originalRelativePath; // relative path to it

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
                _relativePath = Path.Combine(_originalRelativePath, _dotname ?? _entry.Name);
                return true;
            }
            else
            {
                _relativePath = _fullpath = string.Empty;
                _entry = _original;
                return false;
            }
        }

        private protected void __construct(DirectoryInfo entry, string relativePath, FileSystemInfo[] children = null)
        {
            _entry = entry;
            _original = entry ?? throw new ArgumentNullException();
            _originalRelativePath = relativePath;

            if (!_original.Exists)
            {
                throw new UnexpectedValueException();
            }

            _children = children ?? _original.GetFileSystemInfos();

            //
            Position = 0;
        }

        public DirectoryIterator(Context ctx, string path)
        {
            __construct(ctx, path);
        }

        [PhpFieldsOnlyCtor]
        protected DirectoryIterator()
        {
        }

        public override void __construct(Context ctx, string path)
        {
            base.__construct(ctx, path); // init _fullpath
            __construct(new DirectoryInfo(_fullpath), path);
        }

        public override string getFilename() => _dotname ?? base.getFilename();

        public override string getPath() => (_dotname != null) ? _originalRelativePath : base.getPath();

        public virtual bool isDot() => _dotname != null;

        public override string __toString() => getFilename();

        public override string ToString() => getFilename();

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

        public const int OTHER_MODE_MASK = SKIP_DOTS | UNIX_PATHS;

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
            _entry = _current ?? _original;
            _fullpath = _entry.FullName;
            _relativePath = (_current != null)
                ? Path.Combine(_originalRelativePath, _dotname ?? _entry.Name)
                : _originalRelativePath;

            //
            return _current != null;
        }

        [PhpFieldsOnlyCtor]
        protected FilesystemIterator()
        {
        }

        public FilesystemIterator(Context ctx, string path, int flags = KEY_AS_PATHNAME | CURRENT_AS_FILEINFO | SKIP_DOTS)
        {
            __construct(ctx, path, flags);
        }

        private protected FilesystemIterator(DirectoryInfo/*!*/entry, string relativePath, int flags)
        {
            __construct(entry, relativePath, flags);
        }

        private protected void __construct(DirectoryInfo/*!*/entry, string relativePath, int flags)
        {
            _flags = flags;
            _fullpath = entry.FullName;

            base.__construct(entry, relativePath);
        }

        public override sealed void __construct(Context ctx, string file_name)
        {
            this.__construct(ctx, file_name);
        }

        public virtual void __construct(Context ctx, string path, int flags = KEY_AS_PATHNAME | CURRENT_AS_FILEINFO | SKIP_DOTS)
        {
            _flags = flags;
            base.__construct(ctx, path);
        }

        public virtual int getFlags() => _flags;

        public virtual void setFlags(int flags) => _flags = flags;

        #region Iterator

        public override PhpValue current()
        {
            if (_current == null)
            {
                return PhpValue.Null;
            }

            switch (_flags & CURRENT_MODE_MASK)
            {
                case CURRENT_AS_FILEINFO: return PhpValue.FromClass(new SplFileInfo(_current, _relativePath));
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

            if ((_flags & KEY_AS_FILENAME) != 0)
            {
                return getFilename();
            }
            else
            {
                return getPathname();
            }
        }

        public override bool valid() => _current != null;

        #endregion
    }

    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class RecursiveDirectoryIterator : FilesystemIterator, SeekableIterator, RecursiveIterator
    {
        private string subPath = string.Empty;

        readonly protected Context _ctx;

        public RecursiveDirectoryIterator(Context ctx, string file_name, int flags = KEY_AS_PATHNAME | CURRENT_AS_FILEINFO)
            : this(ctx)
        {
            __construct(ctx, file_name, flags);
        }

        [PhpFieldsOnlyCtor]
        protected RecursiveDirectoryIterator(Context ctx)
        {
            _ctx = ctx;
        }

        private protected RecursiveDirectoryIterator(Context ctx, DirectoryInfo entry, string relativePath, int flags, string subPath)
            : this(ctx)
        {
            __construct(entry, relativePath, flags, subPath);
        }

        private protected void __construct(DirectoryInfo entry, string relativePath, int flags, string subPath)
        {
            base.__construct(entry, relativePath, flags);
            this.subPath = subPath;
        }

        public override void __construct(Context ctx, string path, int flags = KEY_AS_PATHNAME | CURRENT_AS_FILEINFO)
        {
            base.__construct(ctx, path, flags);
        }

        RecursiveIterator RecursiveIterator.getChildren() => getChildren();

        public virtual RecursiveDirectoryIterator getChildren()
        {
            if (_current is DirectoryInfo dinfo)
            {
                var relativePath = Path.Combine(_originalRelativePath, dinfo.Name);
                if (this.GetType() == typeof(RecursiveDirectoryIterator))
                {
                    return new RecursiveDirectoryIterator(
                        _ctx,
                        dinfo,
                        relativePath,
                        _flags,
                        string.IsNullOrEmpty(subPath) ? dinfo.Name : Path.Combine(subPath, dinfo.Name));
                }
                else
                {
                    // In case of a derived class we must create an instance of this class
                    return (RecursiveDirectoryIterator)_ctx.Create(default, this.GetPhpTypeInfo(), relativePath, _flags);
                }
            }
            else
            {
                throw new UnexpectedValueException(); // the directory name is invalid
            }
        }

        public virtual bool hasChildren() => _current is DirectoryInfo && _dotname == null;

        public virtual string getSubPath() => subPath ?? string.Empty;

        public virtual string getSubPathname() => Path.Combine(subPath ?? string.Empty, getFilename());
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
            __construct(dirInfo, path, children);
            _flags = flags | SKIP_DOTS;

            Position = 0;
        }

        #endregion

        public long count() => _children.LongLength;
    }
}
