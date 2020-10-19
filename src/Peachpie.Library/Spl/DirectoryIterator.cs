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

        private protected List<string> _children; // list of child elements

        private protected string _dirFullPath; // original full path to the directory
        private protected string _dirOriginalPath; // original provided path to the directory

        /// <summary>Gets or sets the internal iterator position.</summary>
        private protected int Position
        {
            get => _index;
            set => MoveTo(_index = value);
        }

        private protected virtual bool SkipDots => false;

        private protected bool TryGetChildOnPosition(int position, out string child)
        {
            if (position >= 0 && position < _children.Count)
            {
                child = _children[position];
                return true;
            }

            //
            child = default;
            return false;
        }

        private protected virtual bool MoveTo(int position)
        {
            _lazystat = default;

            if (TryGetChildOnPosition(position, out var child))
            {
                _fullpath = Path.GetFullPath(Path.Combine(_dirFullPath, child));
                _originalPath = Path.Combine(_dirOriginalPath, child);
                return true;
            }
            else
            {
                _originalPath = _fullpath = string.Empty;
                return false;
            }
        }

        /// <summary>
        /// Populates internal list of children and validates the directory exists.
        /// </summary>
        private protected void Initialize(List<string> children = null)
        {
            _dirFullPath = _fullpath;
            _dirOriginalPath = _originalPath;
            _children = children ?? StreamWrapper.GetFileStreamWrapper().Listing(_root, _fullpath, StreamListingOptions.Empty, StreamContext.Default);

            if (SkipDots)
            {
                _children.RemoveAll(p => IsDotImpl(p));
            }

            //
            if (isDir() == false)
            {
                // the system cannot find the specified directory
                throw new UnexpectedValueException(string.Format(Resources.Resources.directory_not_found, _fullpath));
            }

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

        public new void __construct(Context ctx, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                // Throws a RuntimeException if the path is an empty string.
                throw new RuntimeException(string.Format(Resources.Resources.arg_null_or_empty, nameof(path)));
            }

            // resolve _fullpath
            base.__construct(ctx, path);

            //
            Initialize();
        }

        // public override string getFilename() => PhpPath.basename(_originalPath); // base.getFilename();

        public override string getPath() => _dirOriginalPath; // (_dotname != null) ? _originalRelativePath : base.getPath();

        private protected static bool IsDotImpl(string path)
        {
            var fname = Core.Utilities.PathUtils.GetFileName(path.AsSpan());

            return
                fname.Equals(".".AsSpan(), StringComparison.Ordinal) ||
                fname.Equals("..".AsSpan(), StringComparison.Ordinal);
        }

        public virtual bool isDot() => IsDotImpl(_originalPath);

        public override string __toString() => getFilename(); // TODO: test

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

        /// <summary>
        /// Current child.
        /// </summary>
        private protected string _current;

        private protected override bool SkipDots => (_flags & SKIP_DOTS) == SKIP_DOTS;

        private protected override bool MoveTo(int position)
        {
            _lazystat = default;
            _index = position;

            if (TryGetChildOnPosition(position, out _current))
            {
                _fullpath = Path.GetFullPath(Path.Combine(_dirFullPath, _current));
                _originalPath = Path.Combine(_dirOriginalPath, _current);
            }
            else
            {
                _fullpath = _dirFullPath;
                _originalPath = _dirOriginalPath;
            }

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

        public void __construct(Context ctx, string path, int flags = KEY_AS_PATHNAME | CURRENT_AS_FILEINFO | SKIP_DOTS)
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
                case CURRENT_AS_FILEINFO: return new SplFileInfo(_root, _fullpath, _originalPath);
                case CURRENT_AS_PATHNAME: return _fullpath;
                case CURRENT_AS_SELF: return this;
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
        private protected string subPath = string.Empty;

        readonly protected Context _ctx;

        [PhpFieldsOnlyCtor]
        protected RecursiveDirectoryIterator(Context ctx)
        {
            _ctx = ctx;
        }

        public RecursiveDirectoryIterator(Context ctx, string path, int flags = KEY_AS_PATHNAME | CURRENT_AS_FILEINFO)
            : this(ctx)
        {
            __construct(ctx, path, flags);
        }

        RecursiveIterator RecursiveIterator.getChildren() => getChildren();

        public virtual RecursiveDirectoryIterator getChildren()
        {
            if (Stat.IsDirectory)
            {
                var relativePath = Path.Combine(_dirOriginalPath, _current);
                var iterator = this.GetType() == typeof(RecursiveDirectoryIterator)
                    ? new RecursiveDirectoryIterator(_ctx, relativePath, _flags)
                    : (RecursiveDirectoryIterator)_ctx.Create(default, this.GetPhpTypeInfo(), relativePath, _flags);    // In case of a derived class we must create an instance of this class

                iterator.subPath = Path.Combine(subPath, _current);

                return iterator;
            }
            else
            {
                throw new UnexpectedValueException(); // the directory name is invalid
            }
        }

        public virtual bool hasChildren() => !IsDotImpl(_current) && Stat.IsDirectory;

        public virtual string getSubPath() => subPath;

        public virtual string getSubPathname() => Path.Combine(subPath, getFilename());
    }

    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class GlobIterator : FilesystemIterator, SeekableIterator, Countable
    {
        #region Construction

        [PhpFieldsOnlyCtor]
        protected GlobIterator()
        {
        }

        public GlobIterator(Context ctx, string path, int flags = KEY_AS_PATHNAME | CURRENT_AS_FILEINFO)
        {
            __construct(ctx, path, flags);
        }

        public new void __construct(Context ctx, string path, int flags = KEY_AS_PATHNAME | CURRENT_AS_FILEINFO)
        {
            _flags = flags | SKIP_DOTS;

            var children = PhpPath.GetMatches(ctx, path, PhpPath.GlobOptions.None).ToList();

            //foreach (var child in children)
            //{
            // find working directory
            //}

            _fullpath = ctx.WorkingDirectory;
            _originalPath = path;

            Initialize(children);
        }

        #endregion

        public long count() => _children.Count;
    }
}
