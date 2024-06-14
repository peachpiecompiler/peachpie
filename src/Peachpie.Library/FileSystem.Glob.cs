using Pchp.Core;
using Pchp.Core.Collections;
using Pchp.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Library
{
    [PhpExtension(PhpExtensionAttribute.KnownExtensionNames.Standard)]
    public static partial class PhpPath
    {
        #region GlobOptions, FnMatchOptions

        /// <summary>
        /// Flags used in call to <c>glob()</c>.
        /// </summary>
        [Flags, PhpHidden]
        public enum GlobOptions
        {
            /// <summary>
            /// No flags.
            /// </summary>
            None = 0,

            /// <summary>
            /// List directories only.
            /// </summary>
            StopOnError = 0x0004,

            /// <summary>
            /// Append system directory separator (slash) to matching directories.
            /// </summary>
            Mark = 0x0008,

            /// <summary>
            /// Return pattern itself if nothing matches.
            /// </summary>
            NoCheck = 0x0010,

            /// <summary>
            /// Don't sort.
            /// </summary>
            NoSort = 0x0020,

            /// <summary>
            /// Expand braces ala csh.
            /// </summary>
            Brace = 0x0080,

            /// <summary>
            /// Disable backslash escaping.
            /// </summary>
            NoEscape = 0x1000,

            /// <summary>
            /// List directories only.
            /// </summary>
            OnlyDir = 0x40000000,

            /// <summary>
            /// Gets bit mask of all available flags.
            /// </summary>
            SupportedMask = StopOnError | Mark | NoCheck | NoSort | Brace | NoEscape | OnlyDir,
        }

        public const int GLOB_MARK = (int)GlobOptions.Mark;
        public const int GLOB_NOCHECK = (int)GlobOptions.NoCheck;
        public const int GLOB_NOSORT = (int)GlobOptions.NoSort;
        public const int GLOB_BRACE = (int)GlobOptions.Brace;
        public const int GLOB_NOESCAPE = (int)GlobOptions.NoEscape;
        public const int GLOB_ONLYDIR = (int)GlobOptions.OnlyDir;
        public const int GLOB_ERR = (int)GlobOptions.StopOnError;
        public const int GLOB_AVAILABLE_FLAGS = (int)GlobOptions.SupportedMask;

        /// <summary>
        /// Flags used in call to <c>fnmatch()</c>.
        /// </summary>
        [Flags, PhpHidden]
        public enum FnMatchOptions
        {
            /// <summary>
            /// No flags.
            /// </summary>
            None = 0,

            /// <summary>
            /// Disable backslash escaping. 
            /// </summary>
            NoEscape = 0x0001,

            /// <summary>
            /// Slash in string only matches slash in the given pattern. 
            /// </summary>
            PathName = 0x0002,

            /// <summary>
            /// Leading period in string must be exactly matched by period in the given pattern. 
            /// </summary>
            Period = 0x0004,

            /// <summary>
            /// Caseless match. Part of the GNU extension. 
            /// </summary>
            CaseFold = 0x0010,
        }

        public const int FNM_NOESCAPE = (int)FnMatchOptions.NoEscape;
        public const int FNM_PATHNAME = (int)FnMatchOptions.PathName;
        public const int FNM_PERIOD = (int)FnMatchOptions.Period;
        public const int FNM_CASEFOLD = (int)FnMatchOptions.CaseFold;

        #endregion

        #region Helpers

        sealed class CharClass
        {
            readonly StringBuilder/*!*/ _chars = new StringBuilder();

            public void Add(char c)
            {
                if (c == ']' || c == '\\')
                {
                    _chars.Append('\\');
                }
                _chars.Append(c);
            }

            public string MakeString()
            {
                if (_chars.Length == 0)
                {
                    return null;
                }

                if (_chars.Length == 1 && _chars[0] == '^')
                {
                    _chars.Insert(0, "\\");
                }
                _chars.Insert(0, "[");
                _chars.Append(']');
                return _chars.ToString();
            }
        }

        sealed class GlobMatcher
        {
            readonly Context/*!*/_ctx;
            readonly string/*!*/ _pattern;
            readonly GlobOptions _flags;
            readonly List<string>/*!*/ _result;
            readonly bool _dirOnly;
            bool _stripTwo;
            bool _relative;
            FnMatchOptions _fnMatchFlags;

            private bool NoEscapes => (_flags & GlobOptions.NoEscape) != 0;

            private bool StopOnError => (_flags & GlobOptions.StopOnError) != 0;

            private bool Mark => (_flags & GlobOptions.Mark) != 0;

            public GlobMatcher(Context ctx, string/*!*/pattern, GlobOptions flags)
            {
                Debug.Assert(ctx != null);

                _ctx = ctx;
                _pattern = pattern;
                _flags = flags;
                _result = new List<string>();
                _dirOnly = PathUtils.IsDirectorySeparator((char)_pattern.LastCharacter()) || (flags & GlobOptions.OnlyDir) != 0;

                _fnMatchFlags = NoEscapes ? FnMatchOptions.NoEscape : FnMatchOptions.None;
            }


            private static string/*!*/ Unescape(string/*!*/ path, int start)
            {
                var unescaped = ObjectPools.GetStringBuilder();
                var inEscape = false;

                for (int i = start; i < path.Length; i++)
                {
                    var c = path[i];
                    if (inEscape)
                    {
                        inEscape = false;
                    }
                    else if (c == '\\' && i + 1 < path.Length && (IsMetaCharacter(path[i + 1]) || PathUtils.IsDirectorySeparator(path[i + 1])))
                    {
                        inEscape = true;
                        continue;
                    }
                    unescaped.Append(c);
                }

                //
                return ObjectPools.GetStringAndReturn(unescaped);
            }

            private void TestPath(string path, int patternEnd, bool isLastPathSegment)
            {
                if (!isLastPathSegment)
                {
                    DoGlob(path, patternEnd);
                    return;
                }

                if (!NoEscapes)
                {
                    path = Unescape(path, _stripTwo ? 2 : 0);
                }
                else if (_stripTwo)
                {
                    path = path.Substring(2);
                }

                var resultPath = path;

                if (_relative)//we have to remove CWD before adding to results list
                {
                    resultPath = path.Substring(_ctx.WorkingDirectory.Length + 1);
                }

                if (System.IO.Directory.Exists(path))
                {
                    if (Mark)
                        _result.Add(resultPath + Path.DirectorySeparatorChar.ToString());
                    else
                        _result.Add(resultPath);

                }
                else if (!_dirOnly && File.Exists(path))
                {
                    _result.Add(resultPath);
                }
            }

            internal List<string>/*!*/ DoGlob()
            {
                if (_pattern.Length == 0)
                {
                    return _result;
                }

                int pos = 0;
                string baseDirectory = ".";
                if (Path.IsPathRooted(_pattern))
                {
                    pos = FindNextSeparator(0, false, out var containsWildcard);
                    if (pos == _pattern.Length)
                    {
                        TestPath(_pattern, pos, true);
                        return _result;
                    }
                    if (pos > 0 || _pattern[0] == '/')
                    {
                        baseDirectory = _pattern.Substring(0, pos);
                    }
                }
                else
                {
                    _relative = true;
                    baseDirectory = _ctx.WorkingDirectory;
                }

                _stripTwo = (baseDirectory == ".");

                try
                {
                    DoGlob(baseDirectory, pos);
                }
                catch (Exception)
                {
                    //
                }

                return _result;
            }

            private void DoGlob(string/*!*/ baseDirectory, int position)
            {
                if (!System.IO.Directory.Exists(baseDirectory))
                {
                    return;
                }

                int patternEnd = FindNextSeparator(position, true, out var containsWildcard);
                bool isLastPathSegment = (patternEnd == _pattern.Length);
                var dirSegment = _pattern.AsSpan(position, patternEnd - position);

                if (!isLastPathSegment)
                {
                    patternEnd++;
                }

                if (!containsWildcard)
                {
                    var path = Path.Combine(baseDirectory, dirSegment.ToString());
                    TestPath(path, patternEnd, isLastPathSegment);
                    return;
                }

                try
                {
                    foreach (var file in System.IO.Directory.GetFileSystemEntries(baseDirectory, "*"))
                    {
                        var filename = Path.GetFileName(file);
                        if (fnmatch(dirSegment, filename, _fnMatchFlags))
                        {
                            TestPath(file, patternEnd, isLastPathSegment);
                        }
                    }
                }
                catch (Exception)
                {
                    if (StopOnError)
                    {
                        throw;
                    }
                }

                if (isLastPathSegment && dirSegment[0] == '.')
                {
                    if (fnmatch(dirSegment, ".", _fnMatchFlags))
                    {
                        var directory = baseDirectory + CurrentPlatform.DirectorySeparatorString + ".";
                        if (_dirOnly)
                        {
                            directory += CurrentPlatform.DirectorySeparatorString;
                        }
                        TestPath(directory, patternEnd, true);
                    }
                    if (fnmatch(dirSegment, "..", _fnMatchFlags))
                    {
                        var directory = baseDirectory + CurrentPlatform.DirectorySeparatorString + "..";
                        if (_dirOnly)
                        {
                            directory += CurrentPlatform.DirectorySeparatorString;
                        }
                        TestPath(directory, patternEnd, true);
                    }
                }

            }

            static bool IsMetaCharacter(char c) => c == '*' || c == '?' || c == '[';

            private int FindNextSeparator(int position, bool allowWildcard, out bool containsWildcard)
            {
                var pattern = _pattern;
                var lastSlash = -1;

                containsWildcard = false;

                for (int i = position; i < pattern.Length; i++)
                {
                    var c = pattern[i];

                    // skip the escaped character
                    if (c == '\\' && CurrentPlatform.DirectorySeparator != '\\' && NoEscapes == false && i + 1 < pattern.Length)
                    {
                        if (IsMetaCharacter(pattern[i + 1]) || PathUtils.IsDirectorySeparator(pattern[i + 1]))
                        {
                            i++;
                            continue;
                        }
                    }

                    if (IsMetaCharacter(c))
                    {
                        if (!allowWildcard)
                        {
                            return lastSlash + 1;
                        }
                        else if (lastSlash >= 0)
                        {
                            return lastSlash;
                        }
                        containsWildcard = true;
                    }
                    else if (PathUtils.IsDirectorySeparator(c) || c == ':')
                    {
                        if (containsWildcard)
                        {
                            return i;
                        }
                        lastSlash = i;
                    }
                }
                return pattern.Length;
            }
        }

        sealed class GlobUngrouper
        {
            abstract class GlobNode
            {
                public readonly GlobNode/*!*/ _parent;
                protected GlobNode(GlobNode parentNode)
                {
                    _parent = parentNode ?? this;
                }
                abstract public GlobNode/*!*/ AddChar(char c);
                abstract public GlobNode/*!*/ StartLevel();
                abstract public GlobNode/*!*/ AddGroup();
                abstract public GlobNode/*!*/ FinishLevel();
                abstract public ValueList<string>/*!*/ Flatten();
            }

            class TextNode : GlobNode
            {
                readonly StringBuilder/*!*/ _builder;

                public TextNode(GlobNode/*!*/ parentNode)
                    : base(parentNode)
                {
                    _builder = new StringBuilder();
                }

                public override GlobNode/*!*/ AddChar(char c)
                {
                    if (c != 0)
                    {
                        _builder.Append(c);
                    }
                    return this;
                }

                public override GlobNode/*!*/ StartLevel()
                {
                    return _parent.StartLevel();
                }

                public override GlobNode/*!*/ AddGroup()
                {
                    return _parent.AddGroup();
                }

                public override GlobNode/*!*/ FinishLevel()
                {
                    return _parent.FinishLevel();
                }

                public override ValueList<string>/*!*/ Flatten()
                {
                    return new ValueList<string>(1) { _builder.ToString() };
                }
            }

            class ChoiceNode : GlobNode
            {
                private readonly List<SequenceNode>/*!*/ _nodes;

                public ChoiceNode(GlobNode/*!*/ parentNode)
                    : base(parentNode)
                {
                    _nodes = new List<SequenceNode>();
                }

                public override GlobNode/*!*/ AddChar(char c)
                {
                    var node = new SequenceNode(this);
                    _nodes.Add(node);
                    return node.AddChar(c);
                }

                public override GlobNode/*!*/ StartLevel()
                {
                    var node = new SequenceNode(this);
                    _nodes.Add(node);
                    return node.StartLevel();
                }

                public override GlobNode/*!*/ AddGroup()
                {
                    AddChar('\0');
                    return this;
                }

                public override GlobNode/*!*/ FinishLevel()
                {
                    AddChar('\0');
                    return _parent;
                }

                public override ValueList<string>/*!*/ Flatten()
                {
                    var result = new ValueList<string>();

                    foreach (var node in _nodes)
                    {
                        result.AddRange(node.Flatten());
                    }

                    return result;
                }
            }

            class SequenceNode : GlobNode
            {
                readonly List<GlobNode>/*!*/_nodes;

                public SequenceNode(GlobNode parentNode)
                    : base(parentNode)
                {
                    _nodes = new List<GlobNode>();
                }

                public override GlobNode/*!*/ AddChar(char c)
                {
                    var node = new TextNode(this);
                    _nodes.Add(node);
                    return node.AddChar(c);
                }

                public override GlobNode/*!*/ StartLevel()
                {
                    var node = new ChoiceNode(this);
                    _nodes.Add(node);
                    return node;
                }

                public override GlobNode/*!*/ AddGroup()
                {
                    return _parent;
                }

                public override GlobNode/*!*/ FinishLevel()
                {
                    return _parent._parent;
                }

                public override ValueList<string>/*!*/ Flatten()
                {
                    var result = new ValueList<string>(); // root
                    var first = true;

                    foreach (var node in _nodes)
                    {
                        var node_flattern = node.Flatten();

                        if (first)
                        {
                            first = false;
                            result = node_flattern;
                            continue;
                        }

                        var tmp = new ValueList<string>(node_flattern.Count);

                        foreach (var next in node_flattern)
                        {
                            foreach (var current in result)
                            {
                                tmp.Add(current + next);
                            }
                        }

                        result = tmp;
                    }

                    return result;
                }
            }

            readonly SequenceNode/*!*/ _rootNode;
            GlobNode/*!*/ _currentNode;
            int _level;

            public GlobUngrouper(int patternLength)
            {
                _rootNode = new SequenceNode(null);
                _currentNode = _rootNode;
                _level = 0;
            }

            public void AddChar(char c)
            {
                _currentNode = _currentNode.AddChar(c);
            }

            public void StartLevel()
            {
                _currentNode = _currentNode.StartLevel();
                _level++;
            }

            public void AddGroup()
            {
                _currentNode = _currentNode.AddGroup();
            }

            public void FinishLevel()
            {
                _currentNode = _currentNode.FinishLevel();
                _level--;
            }

            public int Level
            {
                get { return _level; }
            }

            public ValueList<string>/*!*/ Flatten()
            {
                if (_level != 0)
                {
                    return ValueList<string>.Empty;
                }

                return _rootNode.Flatten();
            }
        }

        static void AppendExplicitRegexChar(StringBuilder/*!*/ builder, char c)
        {
            builder.Append('[');
            if (c == '^' || c == '\\')
            {
                builder.Append('\\');
            }
            builder.Append(c);
            builder.Append(']');
        }

        static string/*!*/ PatternToRegex(ReadOnlySpan<char>/*!*/pattern, bool pathName, bool noEscape)
        {
            var result = ObjectPools.GetStringBuilder();
            result.Append("\\G");

            bool inEscape = false;
            CharClass charClass = null;

            for (int i = 0; i < pattern.Length; i++)
            {
                var c = pattern[i];

                if (inEscape)
                {
                    inEscape = false;

                    if (charClass != null)
                    {
                        charClass.Add(c);
                    }
                    else
                    {
                        AppendExplicitRegexChar(result, c);
                    }
                }
                else if (c == '\\' && !noEscape)
                {
                    inEscape = true;
                }
                else if (charClass != null)
                {
                    if (c == ']')
                    {
                        var set = charClass.MakeString();
                        if (set == null)
                        {
                            // PHP regex "[]" matches nothing
                            // CLR regex "[]" throws exception
                            return string.Empty;
                        }

                        result.Append(set);
                        charClass = null;
                    }
                    else
                    {
                        charClass.Add(c);
                    }
                }
                else
                {
                    switch (c)
                    {
                        case '*':
                            result.Append(pathName ? "[^/]*" : ".*");
                            break;

                        case '?':
                            result.Append('.');
                            break;

                        case '[':
                            charClass = new CharClass();
                            break;

                        default:
                            AppendExplicitRegexChar(result, c);
                            break;
                    }
                }
            }

            if (charClass == null)
            {
                return ObjectPools.GetStringAndReturn(result);
            }
            else
            {
                ObjectPools.Return(result);
                return string.Empty;
            }
        }

        static ValueList<string> UngroupGlobs(string/*!*/ pattern, bool noEscape, bool brace)
        {
            var ungrouper = new GlobUngrouper(pattern.Length);
            var inEscape = false;

            for (int i = 0; i < pattern.Length; i++)
            {
                var c = pattern[i];

                if (inEscape)
                {
                    inEscape = false;
                    if (c != ',' && c != '{' && c != '}')
                    {
                        ungrouper.AddChar('\\');
                    }
                    ungrouper.AddChar(c);
                }
                else if (c == '\\' && !noEscape)
                {
                    inEscape = true;
                }
                else
                {
                    switch (c)
                    {
                        case '{':
                            if (!brace)
                            {
                                return ValueList<string>.Empty;
                            }

                            ungrouper.StartLevel();
                            break;

                        case ',':
                            if (ungrouper.Level < 1)
                            {
                                ungrouper.AddChar(c);
                            }
                            else
                            {
                                ungrouper.AddGroup();
                            }
                            break;

                        case '}':
                            if (ungrouper.Level < 1)
                            {
                                // Unbalanced closing bracket matches nothing
                                return ValueList<string>.Empty;
                            }
                            ungrouper.FinishLevel();
                            break;

                        default:
                            ungrouper.AddChar(c);
                            break;
                    }
                }
            }
            return ungrouper.Flatten();
        }

        internal static IEnumerable<string>/*!*/ GetMatches(Context ctx, string/*!*/pattern, GlobOptions flags)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                yield break;
            }

            var noEscape = (flags & GlobOptions.NoEscape) != 0;
            var brace = (flags & GlobOptions.Brace) != 0;

            var groups = UngroupGlobs(pattern, noEscape, brace);

            foreach (string group in groups)
            {
                var matcher = new GlobMatcher(ctx, group, flags);

                foreach (string filename in matcher.DoGlob())
                {
                    //yield return CurrentPlatform.NormalizeSlashes(filename); // NOTE: PHP leave slashes as they were specified in pattern
                    yield return filename;
                }
            }
        }

        #endregion

        #region fnmatch, glob

        /// <summary>
        /// Matches the given path against a pattern.
        /// </summary>
        /// <param name="pattern">A <see cref="string"/> containing a wildcard.</param>
        /// <param name="path">The <see cref="string"/> to be matched.</param>
        /// <param name="flags">Additional flags.</param>
        /// <returns><c>true</c> if the <paramref name="path"/> matches with the given 
        /// wildcard <paramref name="pattern"/>.</returns>
        public static bool fnmatch(ReadOnlySpan<char>/*!*/pattern, string/*!*/ path, FnMatchOptions flags = FnMatchOptions.None)
        {
            if (pattern.IsEmpty)
            {
                return string.IsNullOrEmpty(path);
            }

            var pathName = (flags & FnMatchOptions.PathName) != 0;
            var noEscape = (flags & FnMatchOptions.NoEscape) != 0;
            var regexPattern = PatternToRegex(pattern, pathName, noEscape);
            if (regexPattern.Length == 0)
            {
                return false;
            }

            if ((flags & FnMatchOptions.Period) == 0 && path.Length != 0 && path[0] == '.')
            {
                // Starting dot requires an explicit dot in the pattern
                if (regexPattern.Length < 4 || regexPattern[2] != '[' || regexPattern[3] != '.')
                {
                    return false;
                }
            }

            var options = System.Text.RegularExpressions.RegexOptions.CultureInvariant | System.Text.RegularExpressions.RegexOptions.Singleline;

            if ((flags & FnMatchOptions.CaseFold) != 0)
            {
                options |= System.Text.RegularExpressions.RegexOptions.IgnoreCase;
            }

            var match = System.Text.RegularExpressions.Regex.Match(path, regexPattern, options);
            return match != null && match.Success && (match.Length == path.Length);
        }

        /// <summary>
        /// Find path names matching a pattern.
        /// </summary>
        [return: CastToFalse]
        public static PhpArray glob(Context ctx, string pattern, GlobOptions flags = GlobOptions.None)
        {
            if ((flags & ~GlobOptions.SupportedMask) != 0)
            {
                PhpException.InvalidArgument(nameof(flags), string.Format(Resources.Resources.glob_invalid_flags, (flags & ~GlobOptions.SupportedMask).ToString()));
                return null; // FALSE
            }

            if (string.IsNullOrEmpty(pattern))
            {
                return PhpArray.NewEmpty();
            }

            var result = new PhpArray();

            result.AddRange(GetMatches(ctx, pattern, flags));

            if (result.Count == 0 && (flags & GlobOptions.NoCheck) != 0)
            {
                result.Add(pattern);
            }

            return result;
        }

        #endregion
    }
}
