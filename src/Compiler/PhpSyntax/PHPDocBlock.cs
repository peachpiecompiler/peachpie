using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;

using Pchp.Syntax.Text;
using System.Reflection;

namespace Pchp.Syntax
{
    /// <summary>
    /// Structuralized representation of PHPDoc DocBlock.
    /// </summary>
    /// <remarks>define() statements, functions, classes, class methods, and class vars, include() statements, and global variables can all be documented.
    /// See http://en.wikipedia.org/wiki/PHPDoc for specifications.</remarks>
    public sealed class PHPDocBlock : AST.LangElement
    {
        #region Nested classes: Element

        public abstract class Element
        {
            #region Constants

            /// <summary>
            /// String sequence starting the PHPDoc block on the first line.
            /// </summary>
            private const string PhpDocStartString = "/**";

            /// <summary>
            /// Every PHPDoc line not starting with this character is ignored.
            /// </summary>
            private const char PHPDocFirstChar = '*';

            /// <summary>
            /// Every PHPDoc tag starts with this character.
            /// </summary>
            private const char PHPDocTagChar = '@';

            /// <summary>
            /// String representing new line between PHPDoc comment lines.
            /// </summary>
            protected const string NewLineString = "\n";

            #endregion

            #region Properties

            /// <summary>
            /// Element position within the source code.
            /// </summary>
            public Span Span { get; internal set; }

            #endregion

            #region Tags

            /// <summary>
            /// Tag elements initialized using reflection.
            /// </summary>
            private static Dictionary<string, Func<string, string, Element>>/*!!*/elementFactories;

            static Element()
            {
                // initilize dictionary of known tags and their factories:
                elementFactories = new Dictionary<string, Func<string, string, Element>>(20, StringComparer.OrdinalIgnoreCase);
                var types = typeof(PHPDocBlock).GetTypeInfo().DeclaredNestedTypes.Where(t => t.IsPublic);
                var eltype = typeof(Element).GetTypeInfo();
                foreach (var t in types)
                {
                    if (t.IsSealed && !t.IsAbstract && eltype.IsAssignableFrom(t))
                    {
                        // add to the dictionary according to its Name:
                        var fld = t.GetDeclaredField("Name");
                        if (fld != null)
                        {
                            var factory = CreateElementFactory(t);
                            elementFactories.Add(TagNameHelper(fld), factory);
                        }
                        else
                        {
                            var f1 = t.GetDeclaredField("Name1");
                            var f2 = t.GetDeclaredField("Name2");
                            var f3 = t.GetDeclaredField("Name3");

                            if (f1 != null && f2 != null)
                            {
                                var factory = CreateElementFactory(t);
                                elementFactories.Add(TagNameHelper(f1), factory);
                                elementFactories.Add(TagNameHelper(f2), factory);

                                if (f3 != null)
                                    elementFactories.Add(TagNameHelper(f3), factory);
                            }
                            else
                            {
                                // only these Elements do not represent a tag:
                                Debug.Assert(
                                    t.Name == typeof(ShortDescriptionElement).Name ||
                                    t.Name == typeof(LongDescriptionElement).Name ||
                                    t.Name == typeof(UnknownTextTag).Name);
                            }
                        }
                    }
                }

                // ensure we have some tags:
                Debug.Assert(elementFactories.ContainsKey("@param"));
                Debug.Assert(elementFactories.ContainsKey("@ignore"));
                Debug.Assert(elementFactories.ContainsKey("@var"));
                // ...
            }

            private static Func<string, string, Element>/*!*/CreateElementFactory(TypeInfo/*!*/elementType)
            {
                Debug.Assert(elementType != null && typeof(Element).GetTypeInfo().IsAssignableFrom(elementType));

                var ctors = elementType.DeclaredConstructors.ToArray();
                Debug.Assert(ctors != null && ctors.Length  == 1);
                var ctor = ctors[0];

                var args = ctor.GetParameters();
                Debug.Assert(args != null && args.Length <= 2);

                // create function that creates the Element 't':
                if (args.Length == 0)
                {
                    return (tagName, line) => (Element)ctor.Invoke(null);
                }
                else if (args.Length == 1)
                {
                    Debug.Assert(args[0].Name == "line");
                    return (tagName, line) => (Element)ctor.Invoke(new object[] { line });
                }
                else
                {
                    Debug.Assert(args[0].Name == "tagName");
                    Debug.Assert(args[1].Name == "line");
                    return (tagName, line) => (Element)ctor.Invoke(new object[] { tagName, line });
                }
            }

            /// <summary>
            /// Reads value of given field, assuming it is string constant, which value starts with <see cref="PHPDocTagChar"/>.
            /// </summary>
            private static string TagNameHelper(System.Reflection.FieldInfo fld)
            {
                Debug.Assert(fld != null);

                var tagname = fld.GetValue(null) as string;

                Debug.Assert(!string.IsNullOrEmpty(tagname));
                Debug.Assert(tagname[0] == PHPDocTagChar);

                return tagname;
            }

            private static KeyValuePair<string, Func<string, string, Element>> FindTagInfo(string/*!*/line)
            {
                Debug.Assert(!string.IsNullOrEmpty(line));
                Debug.Assert(line[0] == PHPDocTagChar);

                int endIndex = 1;
                char c;
                while (endIndex < line.Length && !char.IsWhiteSpace(c = line[endIndex]) && c != ':' && c != '(' && c != ';' && c != '.')
                    endIndex++;

                string tagName = (endIndex < line.Length) ? line.Remove(endIndex) : line;

                Func<string,string,Element> tmp;
                if (elementFactories.TryGetValue(tagName, out tmp))
                    return new KeyValuePair<string, Func<string, string, Element>>(tagName, tmp);
                else
                    return new KeyValuePair<string, Func<string, string, Element>>(tagName, (_name, _line) => new UnknownTextTag(_name, _line));
            }

            #endregion

            #region Parsing

            /// <summary>
            /// Prepares given <paramref name="line"/>.
            /// 
            /// If the line creates new PHPDoc element, new <see cref="Element"/>
            /// is instantiated and returned in <paramref name="next"/>.
            /// </summary>
            /// <param name="line">Line to parse. Cannot be <c>null</c> reference.</param>
            /// <param name="next">Outputs new element that will follow current element. Parsing will continue using this element.</param>
            /// <param name="lineIndex">Index of the line within PHPDoc token.</param>
            /// <param name="startCharIndex">Gets index of first content character within <paramref name="line"/>.</param>
            /// <param name="endCharIndex">Gets index of last content character within <paramref name="line"/>.</param>
            /// <returns>If the line can be parsed, method returns <c>true</c>.</returns>
            internal static bool TryParseLine(ref string/*!*/line, out Element next, int lineIndex, out int startCharIndex, out int endCharIndex)
            {
                if (line == null)
                    throw new ArgumentNullException("line");

                next = null;
                startCharIndex = endCharIndex = 0;

                int startIndex = 0;
                while (startIndex < line.Length && char.IsWhiteSpace(line[startIndex])) startIndex++;   // skip whitespaces

                // we souhldn't, but we allow first line to contain text after the /** sequence:
                if (lineIndex == 0 && line.StartsWith(PhpDocStartString, StringComparison.Ordinal))
                {
                    startIndex = PhpDocStartString.Length - 1;  // jump to the '*' character
                    Debug.Assert(line[startIndex] == PHPDocFirstChar);
                }

                // invalid PHPDoc line (not starting with '*'):
                if (startIndex == line.Length || line[startIndex] != PHPDocFirstChar)
                    return false;

                // trim starting '*' and whitespaces
                startIndex++;   // skip '*'
                while (startIndex < line.Length && char.IsWhiteSpace(line[startIndex])) startIndex++;   // skip whitespaces

                if (startIndex == line.Length)
                {
                    line = string.Empty;
                }
                else
                {
                    // trim end
                    int endIndex = line.Length;
                    while (endIndex > startIndex && char.IsWhiteSpace(line[endIndex - 1])) endIndex--;  // skip whitespaces from end
                    line = line.Substring(startIndex, endIndex - startIndex).Replace("{@*}", "*/");
                }

                // check "*/" at the end
                if (line.Length == 1 && line[0] == '/')
                    return false;   // empty line
                if (line.Length >= 2 && line[line.Length - 1] == '/' && line[line.Length - 2] == '*')  // "*/" found at the end
                    line = line.Remove(line.Length - 2);

                // TODO: any whitespace sequence is converted into single space, but only outside <pre> and {} blocks
                // TODO: handle "{@tag ...}" for @link, @see etc...

                // check tags:
                next = CreateElement(line);
                startCharIndex = startIndex;
                endCharIndex = startIndex + line.Length;

                // 
                return true;
            }

            /// <summary>
            /// Parses given <paramref name="line"/> and updates current content.
            /// </summary>
            /// <param name="line">Line to parse. Line is trimmed and does not start with '*'. Cannot be <c>null</c> reference.</param>
            /// <param name="next">Next element to continue parsing with.</param>
            internal abstract void ParseLine(string/*!*/line, out Element next);

            /// <summary>
            /// Reads tag at the beginning of line and tries to create corresponding <see cref="Element"/> instance.
            /// </summary>
            /// <param name="line">PHPDoc comment line. Assuming the line starts with a PHPDoc tag. Otherwise, or if tag is not recognized, <c>null</c> is returned..</param>
            private static Element CreateElement(string/*!*/line)
            {
                Debug.Assert(line != null);

                if (line.Length == 0 || line[0] != PHPDocTagChar)
                    return null;

                // try to match known tags:
                var tagInfo = FindTagInfo(line);
                if (tagInfo.Key != null)
                {
                    Debug.Assert(tagInfo.Value != null);
                    
                    // initialize new tag element
                    return tagInfo.Value(tagInfo.Key, line);
                }
                
                // unrecognized tag:
                return null;
            }

            /// <summary>
            /// Returns <c>true</c> if current element does not contain any information and can be ignored.
            /// </summary>
            internal virtual bool IsEmpty { get { return false; } }

            /// <summary>
            /// Called when parsing of this element ended.
            /// </summary>
            internal virtual void OnEndParsing() { }

            #endregion
        }

        /// <summary>
        /// Short description.
        /// </summary>
        public sealed class ShortDescriptionElement : Element
        {
            /// <summary>
            /// Character defining the end of PHPDoc short description.
            /// </summary>
            private const char EndChar = '.';

            public string Text { get; private set; }

            public ShortDescriptionElement()
            {

            }

            internal override void ParseLine(string/*!*/line, out Element next)
            {
                next = null;

                // ignore first line of length 0 (empty space after /**)
                if (this.Text == null && string.IsNullOrWhiteSpace(line))
                    return;

                // Short Description can be followed by Long Description.
                // It can be only 3 lines long, otherwise only the first line is taken
                // It is terminated by empty line or a dot.

                if (this.Text != null && (this.Text.LastCharacter() == (int)EndChar))
                {
                    next = new LongDescriptionElement(line);
                }
                else if (line.Length == 0)
                {
                    next = new LongDescriptionElement(null);
                }
                else if (this.Text.CharsCount('\n') >= 2)
                {
                    // short description has already 3 lines,
                    // only first line is taken, the rest is for LongDescriptionElement
                    int firstLineEndIndex = this.Text.IndexOf('\n');
                    Debug.Assert(firstLineEndIndex != -1);

                    next = new LongDescriptionElement(this.Text.Substring(firstLineEndIndex + 1) + NewLineString + line);
                    this.Text = this.Text.Remove(firstLineEndIndex);
                }
                else
                {
                    this.Text = (this.Text != null) ? (this.Text + NewLineString + line) : line;
                }
            }

            internal override bool IsEmpty { get { return string.IsNullOrWhiteSpace(this.Text); } }

            internal override void OnEndParsing()
            {
                base.OnEndParsing();
                if (this.Text != null)
                    this.Text = this.Text.Trim();
            }

            public override string ToString()
            {
                return this.Text ?? string.Empty;
            }
        }

        /// <summary>
        /// Long description.
        /// </summary>
        public sealed class LongDescriptionElement : Element
        {
            public string Text { get; private set; }

            public LongDescriptionElement(string initialText)
            {
                this.Text = string.IsNullOrWhiteSpace(initialText) ? null : initialText;
            }

            internal override void ParseLine(string line, out Element next)
            {
                // Long Description can only be followed by PHPDoc tag (handled in TryParseLine)

                next = null;
                this.Text = (this.Text != null) ? (this.Text + NewLineString + line) : line;
            }

            internal override bool IsEmpty { get { return string.IsNullOrWhiteSpace(this.Text); } }

            internal override void OnEndParsing()
            {
                base.OnEndParsing();
                if (this.Text != null)
                    this.Text = this.Text.Trim();
            }

            public override string ToString()
            {
                return this.Text ?? string.Empty;
            }
        }

        public abstract class EmptyTag : Element
        {
            internal override void ParseLine(string line, out Element next)
            {
                next = null;
                // ignored
            }
        }

        /// <summary>
        /// Documents an abstract class, class variable or method.
        /// </summary>
        public sealed class AbstractTag : EmptyTag
        {
            public const string Name = "@abstract";

            public override string ToString()
            {
                return Name;
            }
        }

        /// <summary>
        /// Documents access control for an element. @access private indicates that documentation of element be prevented.
        /// </summary>
        public sealed class AccessTag : Element
        {
            public const string Name1 = "@access";
            public const string Name2 = "@private";

            private const string IsPublic = "public";
            private const string IsPrivate = "private";
            private const string IsProtected = "protected";

            /// <summary>
            /// Resolved access modifier. (public, private or protected)
            /// </summary>
            public PhpMemberAttributes Access { get { return attributes & PhpMemberAttributes.VisibilityMask; } }
            private readonly PhpMemberAttributes attributes;

            private string AccessString
            {
                get
                {
                    switch (Access)
                    {
                        case PhpMemberAttributes.Private: return IsPrivate;
                        case PhpMemberAttributes.Protected: return IsProtected;
                        default: return IsPublic;
                    }
                }
            }

            public AccessTag(string/*!*/line)
            {
                if (line.StartsWith(Name1))
                {
                    if (line.Length > Name1.Length)
                    {
                        var access = line.Substring(Name1.Length + 1).Trim().ToLowerInvariant();

                        // public, private or protected
                        switch (access)
                        {
                            case IsPublic: attributes = PhpMemberAttributes.Public; break;
                            case IsPrivate: attributes = PhpMemberAttributes.Private; break;
                            case IsProtected: attributes = PhpMemberAttributes.Protected; break;
                            default:
                                Debug.WriteLine("Unexpected access modifier in PHPDoc @access tag, line:" + line);
                                break;
                        }
                    }
                    else
                    {
                        attributes = PhpMemberAttributes.Public;
                    }
                }
                else if (line.StartsWith(Name2))
                {
                    attributes = PhpMemberAttributes.Private;
                }
                else
                {
                    Debug.Assert(false, "Unexpected " + line);
                }
            }

            internal override void ParseLine(string line, out Element next)
            {
                next = null;
                // ignored
            }

            public override string ToString()
            {
                return Name1 + " " + AccessString;
            }
        }

        public abstract class SingleLineTag : Element
        {
            protected readonly string text;

            internal SingleLineTag(string/*!*/tagName, string/*!*/line)
            {
                Debug.Assert(line.StartsWith(tagName));

                if (line.Length > tagName.Length)
                    this.text = line.Substring(tagName.Length + 1).Trim();
            }

            internal override void ParseLine(string line, out Element next)
            {
                next = null;
                // other lines are ignored
            }

            internal override bool IsEmpty
            {
                get
                {
                    return string.IsNullOrWhiteSpace(text);
                }
            }
        }

        /// <summary>
        /// Documents the author of the current element.
        /// </summary>
        public sealed class AuthorTag : SingleLineTag
        {
            public const string Name = "@author";

            /// <summary>
            /// author name &lt;author@email&gt;
            /// </summary>
            public string Author { get { return text; } }

            public AuthorTag(string/*!*/line)
                :base(Name, line)
            {
                
            }

            public override string ToString()
            {
                return Name + " " + Author;
            }
        }

        /// <summary>
        /// Documents copyright information.
        /// </summary>
        public sealed class CopyrightTag : SingleLineTag
        {
            public const string Name = "@copyright";

            /// <summary>
            /// name date
            /// </summary>
            public string Copyright { get { return text; } }

            public CopyrightTag(string/*!*/line)
                : base(Name, line)
            {

            }

            public override string ToString()
            {
                return Name + " " + Copyright;
            }
        }

        /// <summary>
        /// Documents a method as deprecated.
        /// </summary>
        public sealed class DeprecatedTag : SingleLineTag
        {
            public const string Name1 = "@deprecated";
            public const string Name2 = "@deprec";

            /// <summary>
            /// version
            /// </summary>
            public string Version { get { return text; } }

            public DeprecatedTag(string tagName, string/*!*/line)
                : base(tagName, line)
            {

            }

            public override string ToString()
            {
                return Name1 + " " + Version;
            }
        }

        /// <summary>
        /// Documents the location of an external saved example file.
        /// </summary>
        public sealed class ExampleTag : SingleLineTag
        {
            public const string Name = "@example";
            
            /// <summary>
            /// /path/to/example
            /// </summary>
            public string Example { get { return text; } }

            public ExampleTag(string/*!*/line)
                : base(Name, line)
            {

            }

            public override string ToString()
            {
                return Name + " " + Example;
            }
        }

        /// <summary>
        /// Documents an exception thrown by a method.
        /// </summary>
        public sealed class ExceptionTag : TypeVarDescTag
        {
            public const string Name1 = "@exception";
            public const string Name2 = "@throws";

            /// <summary>
            /// version
            /// </summary>
            public string Exception { get { return this.TypeNames; } }

            public ExceptionTag(string tagName, string/*!*/line)
                : base(tagName, line, false)
            {

            }

            public override string ToString()
            {
                return Name2 + " " + this.Exception;
            }
        }

        /// <summary>
        /// Documents any tag in a form of "type [$varname] [multilined-description]".
        /// </summary>
        public abstract class TypeVarDescTag : Element
        {
            /// <summary>
            /// Character separating type names within <see cref="TypeNames"/> property.
            /// </summary>
            public const char TypeNamesSeparator = '|';

            /// <summary>
            /// Optional. Type names separated by '|'.
            /// </summary>
            public string TypeNames
            {
                get
                {
                    var names = _typeNames;
                    if (names == null || names.Length == 0)
                        return null;

                    return string.Join(TypeNamesSeparator.ToString(), names);
                }
            }

            /// <summary>
            /// Position of the <see cref="TypeNames"/> information.
            /// </summary>
            public Span TypeNamesSpan
            {
                get
                {
                    var positions = _typeNamesPos;
                    if (positions == null || positions.Length == 0)
                        return Span.Invalid;
                    var names = _typeNames;

                    var offset = this.Span.Start;
                    return Span.FromBounds(offset + positions[0], offset + positions[positions.Length - 1] + names[names.Length - 1].Length);
                }
            }

            /// <summary>
            /// Array of type names. Cannot be <c>null</c>. Can be an empty array.
            /// </summary>
            public string[]/*!!*/TypeNamesArray { get { return _typeNames; } }
            private readonly string[]/*!!*/_typeNames;

            /// <summary>
            /// Array of type names span within the source code.
            /// </summary>
            public Span[]/*!*/TypeNameSpans
            {
                get
                {
                    var positions = _typeNamesPos;
                    var names = _typeNames;
                    Debug.Assert(names.Length == positions.Length);
                    Span[] spans = new Span[positions.Length];
                    var offset = this.Span.Start;
                    for (int i = 0; i < spans.Length; i++)
                        spans[i] = new Span(offset + positions[i], names[i].Length);

                    return spans;
                }
            }
            private readonly int[]/*!!*/_typeNamesPos;

            /// <summary>
            /// Optional. Variable name, starts with '$'.
            /// </summary>
            public readonly string VariableName;

            /// <summary>
            /// Starting column of the <see cref="VariableName"/> within the element.
            /// </summary>
            private readonly int _variableNameOffset = -1;

            /// <summary>
            /// Position of the <see cref="VariableName"/> information.
            /// </summary>
            public Span VariableNameSpan
            {
                get
                {
                    if (this._variableNameOffset < 0)
                        return Span.Invalid;

                    Debug.Assert(this.VariableName != null);
                    return new Span(this.Span.Start + this._variableNameOffset, this.VariableName.Length);
                }
            }

            /// <summary>
            /// Optional. Element description.
            /// </summary>
            public string Description { get; private set; }

            protected TypeVarDescTag(string/*!*/tagName, string/*!*/line, bool allowVariableName)
            {
                Debug.Assert(line.StartsWith(tagName));

                // [type] [$varname] [type] [description]

                int index = tagName.Length; // current index within line
                
                // try to find [type]
                TryReadTypeName(line, ref index, out _typeNames, out _typeNamesPos);
                
                if (allowVariableName)
                {
                    // try to find [$varname]
                    if (TryReadVariableName(line, ref index, out this.VariableName, out this._variableNameOffset))
                    {
                        // try to find [type] if it was not found yet, user may specified it after variable name
                        if (_typeNames == null || _typeNames.Length == 0)
                        {
                            TryReadTypeName(line, ref index, out _typeNames, out _typeNamesPos);
                        }
                    }
                }

                if (index < line.Length)
                {
                    this.Description = line.Substring(index).TrimStart(null/*default whitespace characters*/);
                    if (string.IsNullOrEmpty(this.Description))
                        this.Description = string.Empty;
                }
            }

            #region Helpers

            private static string NextWord(string/*!*/text, ref int index)
            {   
                // skip whitespaces:
                while (index < text.Length && char.IsWhiteSpace(text[index]))
                    index++;

                // read word:
                int startIndex = index;
                while (index < text.Length && !char.IsWhiteSpace(text[index]))
                    index++;

                // cut off the word:
                if (startIndex < index)
                    return text.Substring(startIndex, index - startIndex);
                else
                    return null;
            }

            /// <summary>
            /// Tries to recognize a type name starting at given <paramref name="index"/>.
            /// </summary>
            /// <param name="text">Source text.</param>
            /// <param name="index">Index within <paramref name="text"/> to start read.</param>
            /// <param name="typenames">Resulting type name(s) separated by <c>|</c>.</param>
            /// <param name="typenamesPos">Type names span or invalid span.</param>
            /// <returns>Whether the type name was parsed.</returns>
            internal static bool TryReadTypeName(string/*!*/text, ref int index, out string[] typenames, out int[] typenamesPos)
            {
                // [type]
                
                var typenameend = index;
                var typename = NextWord(text, ref typenameend);
                if (IsTypeName(typename))
                {
                    List<int> positions = new List<int>(1);
                    List<string> names = new List<string>(1);

                    int typenameOffset = typenameend - typename.Length;
                    index = typenameend;

                    var split = typename.Split(new char[] { TypeNamesSeparator });
                    int splitat = typenameOffset;
                    foreach (var s in split)
                    {
                        if (!string.IsNullOrEmpty(s))
                        {
                            names.Add(s);
                            positions.Add(splitat);
                            splitat += s.Length;    // type name length
                        }
                        splitat++;  // separator
                    }

                    // [type] or [type]
                    var orend = typenameend;
                    var or = NextWord(text, ref orend);
                    if (or == "or")
                    {
                        var nextend = orend;
                        var next = NextWord(text, ref nextend);
                        if (IsTypeName(next) && next.IndexOf(TypeNamesSeparator) == -1)
                        {
                            index = nextend;
                            names.Add(next);
                            positions.Add(nextend - next.Length);
                        }
                    }

                    typenames = names.ToArray();
                    typenamesPos = positions.ToArray();
                    return true;
                }

                //
                typenames = EmptyArray<string>.Instance;
                typenamesPos = EmptyArray<int>.Instance;
                return false;
            }

            /// <summary>
            /// tries to read a variable name starting at given <paramref name="index"/>.
            /// </summary>
            /// <param name="text">Source text.</param>
            /// <param name="index">Index within <paramref name="text"/> to start read.</param>
            /// <param name="variableName">Result variable name.</param>
            /// <param name="variableNameOffset">Variable name start index within text.</param>
            /// <returns>Whether the variable name was parsed.</returns>
            private static bool TryReadVariableName(string/*!*/text, ref int index, out string variableName, out int variableNameOffset)
            {
                var wordend = index;
                var word = NextWord(text, ref wordend);
                if (word != null /* => word.Length != 0 */ && word[0] == '$')
                {
                    index = wordend;
                    variableName = word;
                    variableNameOffset = wordend - word.Length;
                    return true;
                }

                variableName = null;
                variableNameOffset = -1;
                return false;
            }

            /// <summary>
            /// Checks whether given <paramref name="str"/> may be a type name.
            /// </summary>
            /// <param name="str">String to check.</param>
            /// <returns>Whether given string may be a PHP type name.</returns>
            internal static bool IsTypeName(string str)
            {
                if (string.IsNullOrEmpty(str))
                    return false;

                if (str[0] != '_' && !char.IsLetter(str[0]) && str[0] != QualifiedName.Separator)
                    return false;

                for (int i = 1; i < str.Length; i++)
                {
                    char c = str[i];
                    if (c != '_' && !char.IsLetterOrDigit(c) && c != '[' && c != ']' && c != TypeNamesSeparator && c != QualifiedName.Separator)
                        return false;
                }

                // ok
                return true;
            }

            #endregion

            internal override void ParseLine(string line, out Element next)
            {
                next = null;

                // add the line into description:
                Description = string.IsNullOrWhiteSpace(Description) ? line : (Description + NewLineString + line);
            }

            internal override void OnEndParsing()
            {
                base.OnEndParsing();

                if (string.IsNullOrWhiteSpace(this.Description))
                    this.Description = null;
                else
                    this.Description = this.Description.Trim();

                // TODO: compress TypeNames, VariableName, Description
            }

            internal override bool IsEmpty
            {
                get
                {
                    return _typeNames.Length == 0 && string.IsNullOrEmpty(this.VariableName) && string.IsNullOrWhiteSpace(this.Description);
                }
            }
        }

        /// <summary>
        /// Documents a global variable or its use in a function or method.
        /// @global	type $globalvarname
        /// </summary>
        public sealed class GlobalTag : TypeVarDescTag
        {
            public const string Name = "@global";

            public GlobalTag(string/*!*/line)
                :base(Name, line, true)
            {
            }

            public override string ToString()
            {
                string result = Name;

                var type = this.TypeNames;
                if (type != null)
                    result += " " + type;

                var varname = this.VariableName;
                if (varname != null)
                    result += " " + varname;
                
                return result;
            }
        }

        /// <summary>
        /// Prevents the documentation of an element.
        /// </summary>
        public sealed class IgnoreTag : EmptyTag
        {
            public const string Name = "@ignore";

            public override string ToString()
            {
                return Name;
            }
        }

        public abstract class TextTag : Element
        {
            /// <summary>
            /// Tag text information.
            /// </summary>
            public string Text { get; private set; }

            public TextTag(string/*!*/tagName, string/*!*/line)
            {
                Debug.Assert(line.StartsWith(tagName));
                int index = tagName.Length;

                if (index < line.Length)
                {
                    var c = line[index];
                    if (c == ':' || c == '(' || c == ';' || c == '.') index++;
                }

                // trim leading whitespaces
                while (index < line.Length && char.IsWhiteSpace(line[index]))
                    index++;

                this.Text = (index < line.Length) ? line.Substring(index) : string.Empty;
            }

            internal override void  ParseLine(string line, out Element next)
            {
                next = null;
                this.Text = string.IsNullOrEmpty(this.Text) ? line : (this.Text + NewLineString + line);
            }

            internal override void OnEndParsing()
            {
                base.OnEndParsing();

                if (string.IsNullOrWhiteSpace(this.Text))
                    this.Text = string.Empty;
                else
                    this.Text = this.Text.Trim();
            }
        }

        /// <summary>
        /// Represents an unknown PHPDoc tag followed by text.
        /// </summary>
        public sealed class UnknownTextTag : TextTag
        {
            /// <summary>
            /// Tag name.
            /// </summary>
            public string TagName { get; private set; }

            internal UnknownTextTag(string tagName, string/*!*/line)
                : base(tagName, line)
            {
                this.TagName = tagName;
            }

            public override string ToString()
            {
                return string.IsNullOrEmpty(Text) ? (TagName) : (TagName + " " + Text);
            }
        }

        /// <summary>
        /// Private information for advanced developers.
        /// </summary>
        public sealed class InternalTag : TextTag
        {
            public const string Name = "@internal";

            public InternalTag(string/*!*/line)
                :base(Name, line)
            {                
            }

            public override string ToString()
            {
                return Name + " " + Text;
            }
        }

        ///// <summary>
        ///// URL information.
        ///// </summary>
        //public sealed class LinkTag : SingleLineTag
        //{
        //    public const string Name = "@link";

        //    /// <summary>
        //    /// URL
        //    /// </summary>
        //    public string Url { get { return this.text; } }

        //    public LinkTag(string/*!*/line)
        //        :base(Name, line)
        //    {

        //    }
        //}

        /// <summary>
        /// Specifies an alias for a variable. For example, $GLOBALS['myvariable'] becomes $myvariable.
        /// </summary>
        public sealed class NameTag : SingleLineTag
        {
            public const string Name = "@name";

            /// <summary>
            /// Variable name. Empty string or a name starting with '$' character.
            /// </summary>
            public string VariableName { get { return string.IsNullOrEmpty(this.text) ? string.Empty : ((this.text[0] == '$') ? this.text : ('$' + this.text)); } }

            public NameTag(string/*!*/line)
                : base(Name, line)
            {
                
            }
        }

        /// <summary>
        /// phpdoc.de compatibility "phpDocumentor tags".
        /// </summary>
        public sealed class MagicTag : EmptyTag
        {
            public const string Name = "@magic";

            public override string ToString()
            {
                return Name;
            }
        }

        /// <summary>
        /// Documents a group of related classes and functions.
        /// </summary>
        public sealed class PackageTag : SingleLineTag
        {
            public const string Name = "@package";

            /// <summary>
            /// Name of the package.
            /// </summary>
            public string PackageName { get { return this.text; } }

            public PackageTag(string/*!*/line)
                : base(Name, line)
            {

            }
        }

        /// <summary>
        /// Documents a group of related classes and functions within a package.
        /// </summary>
        public sealed class SubPackageTag : SingleLineTag
        {
            public const string Name = "@subpackage";

            /// <summary>
            /// Name of the sub-package.
            /// </summary>
            public string SubPackageName { get { return this.text; } }

            public SubPackageTag(string/*!*/line)
                : base(Name, line)
            {

            }
        }

        /// <summary>
        /// Documents a parameter.
        /// @param type [$varname] description
        /// </summary>
        public sealed class ParamTag : TypeVarDescTag
        {
            public const string Name = "@param";

            public ParamTag(string/*!*/line)
                : base(Name, line, true)
            {
            }

            public override string ToString()
            {
                StringBuilder result = new StringBuilder(Name, Name.Length + ((this.Description != null) ? this.Description.Length : 0) + 16);

                if (this.TypeNames != null)
                {
                    result.Append(' ');
                    result.Append(this.TypeNames);
                }
                if (this.VariableName != null)
                {
                    result.Append(' ');
                    result.Append(this.VariableName);
                }
                if (this.Description != null)
                {
                    result.Append(' ');
                    result.Append(this.Description);
                }
                //
                return result.ToString();
            }
        }

        /// <summary>
        /// Documents function return value. This tag should not be used for constructors or methods defined with a void return type
        /// @return type [description]
        /// </summary>
        public sealed class ReturnTag : TypeVarDescTag
        {
            public const string Name = "@return";

            public ReturnTag(string/*!*/line)
                : base(Name, line, false)
            {
            }

            public override string ToString()
            {
                return Name + " " + TypeNames + NewLineString + Description;
            }
        }

        /// <summary>
        /// Documents an association to any element (global variable, include, page, class, function, define, method, variable).
        /// </summary>
        public sealed class SeeTag : SingleLineTag
        {
            public const string Name = "@see";

            /// <summary>
            /// element
            /// </summary>
            public string ElementName { get { return this.text; } }

            public SeeTag(string/*!*/line)
                : base(Name, line)
            {

            }
        }

        /// <summary>
        /// Documents when a method was added to a class.
        /// </summary>
        public sealed class SinceTag : SingleLineTag
        {
            public const string Name = "@since";

            /// <summary>
            /// version
            /// </summary>
            public string Version { get { return this.text; } }

            public SinceTag(string/*!*/line)
                : base(Name, line)
            {

            }
        }

        /// <summary>
        /// Documents a static class or method.
        /// </summary>
        public sealed class StaticTag : EmptyTag
        {
            public const string Name = "@static";

            public override string ToString()
            {
                return Name;
            }
        }

        /// <summary>
        /// Documents a static variable's use in a function or class.
        /// </summary>
        public sealed class StaticVarTag : TypeVarDescTag
        {
            public const string Name = "@staticvar";

            public StaticVarTag(string/*!*/line)
                :base(Name, line, false)
            {

            }

            public override string ToString()
            {
                return Name + " " + this.TypeNames;
            }
        }

        /// <summary>
        /// Documents things that need to be done to the code at a later date.
        /// </summary>
        public sealed class TodoTag : TextTag
        {
            public const string Name = "@todo";

            public TodoTag(string/*!*/line)
                :base(Name, line)
            {
            }

            public override string ToString()
            {
                return Name + " " + Text;
            }
        }

        /// <summary>
        /// Documents a link to an external source.
        /// </summary>
        public sealed class LinkTag : TextTag
        {
            public const string Name = "@link";

            public LinkTag(string/*!*/line)
                : base(Name, line)
            {
            }

            public override string ToString()
            {
                return Name + " " + Text;
            }
        }

        /// <summary>
        /// Documents a license information.
        /// </summary>
        public sealed class LicenseTag : TextTag
        {
            public const string Name = "@license";

            public LicenseTag(string/*!*/line)
                : base(Name, line)
            {
            }

            public override string ToString()
            {
                return Name + " " + Text;
            }
        }

        public sealed class VarTag : TypeVarDescTag
        {
            public const string Name = "@var";

            public VarTag(string/*!*/line)
                : base(Name, line, true)
            {
            }

            public override string ToString()
            {
                return Name + " " + this.TypeNames;
            }
        }

        /// <summary>
        /// Dynamic property description within a class.
        /// </summary>
        public sealed class PropertyTag : TypeVarDescTag
        {
            public const string Name1 = "@property";
            public const string Name2 = "@property-read";
            public const string Name3 = "@property-write";

            public PropertyTag(string tagName, string/*!*/line)
                : base(tagName, line, true)
            {
            }

            public override string ToString()
            {
                return Name1 + " " + this.TypeNames;
            }
        }

        /// <summary>
        /// Dynamic method description within a class.
        /// </summary>
        public sealed class MethodTag : Element
        {
            public const string Name = "@method";

            /// <summary>
            /// Optional. Type names separated by '|'.
            /// </summary>
            public string TypeNames
            {
                get
                {
                    var names = _typeNames;
                    if (names == null || names.Length == 0)
                        return null;

                    return string.Join(TypeVarDescTag.TypeNamesSeparator.ToString(), names);
                }
            }

            /// <summary>
            /// Array of type names. Cannot be <c>null</c>. Can be an empty array.
            /// </summary>
            public string[]/*!!*/TypeNamesArray { get { return _typeNames; } }
            private readonly string[]/*!*/_typeNames;

            /// <summary>
            /// Array of type names span within the source code.
            /// </summary>
            public Span[]/*!*/TypeNameSpans
            {
                get
                {
                    var positions = _typeNamesPos;
                    var names = _typeNames;
                    Debug.Assert(names.Length == positions.Length);
                    Span[] spans = new Span[positions.Length];
                    var offset = this.Span.Start;
                    for (int i = 0; i < spans.Length; i++)
                        spans[i] = new Span(offset + positions[i], names[i].Length);

                    return spans;
                }
            }
            private readonly int[]/*!*/_typeNamesPos;

            /// <summary>
            /// Array of method parameters;
            /// </summary>
            public readonly AST.FormalParam[]/*!*/Parameters;

            /// <summary>
            /// Method name.
            /// </summary>
            public readonly string MethodName;

            /// <summary>
            /// Span within the source code of the method name.
            /// </summary>
            public Span MethodNameSpan
            {
                get
                {
                    var pos = _methodNamePos;
                    if (pos < 0)
                        return Span.Invalid;
                    Debug.Assert(MethodName != null);
                    return new Span(pos + this.Span.Start, this.MethodName.Length);
                }            
            }
            private readonly int _methodNamePos;

            /// <summary>
            /// Optional. Element description.
            /// </summary>
            public string Description { get; private set; }

            public MethodTag(string/*!*/tagName, string/*!*/line)
            {
                Debug.Assert(line.StartsWith(tagName));

                _methodNamePos = -1;

                // [type] [name()] [name(params ...)] [description]

                int index = tagName.Length; // current index within line
                int descStart = index;  // start of description, moved when [type] or [name] found

                // try to find [type]
                TypeVarDescTag.TryReadTypeName(line, ref index, out _typeNames, out _typeNamesPos);

                descStart = index;
                string word = NextWord(line, ref index);

                // [name()]
                if (word != null && word.EndsWith("()", StringComparison.Ordinal))
                {
                    this.MethodName = word.Remove(word.Length - 2);
                    _methodNamePos = index - word.Length;
                    descStart = index;
                    word = NextWord(line, ref index);
                }

                // [name(params ...)]
                while (descStart < line.Length && char.IsWhiteSpace(line[descStart]))
                    descStart++;    // skip whitespaces

                this.Parameters = null;
                
                int nameStart = descStart;
                int paramsFrom = -1;
                // skip [name]
                while (descStart < line.Length && char.IsLetterOrDigit(line[descStart]))
                    descStart++;

                // parse parameters
                if (descStart < line.Length && line[descStart] == '(')
                {
                    paramsFrom = descStart;
                    if (nameStart < paramsFrom)
                    {
                        if (this.MethodName == null)
                            this.MethodName = line.Substring(nameStart, paramsFrom - nameStart);
                        _methodNamePos = nameStart;
                    }
                }
                else
                {
                    descStart = nameStart;
                }

                if (string.IsNullOrEmpty(this.MethodName))
                    return;

                if (paramsFrom > 0 && paramsFrom < line.Length && line[paramsFrom] == '(')
                {
                    // "name(" found
                    int paramsEnd = line.IndexOf(')', paramsFrom);
                    if (paramsEnd > 0)
                    {
                        descStart = paramsEnd + 1;
                        string[] paramsDecl = line.Substring(paramsFrom + 1, paramsEnd - paramsFrom - 1).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        if (paramsDecl != null && paramsDecl.Length > 0)
                        {
                            this.Parameters = new AST.FormalParam[paramsDecl.Length];
                            for (int i = 0; i < paramsDecl.Length; i++)
                                this.Parameters[i] = ParseParam(paramsDecl[i]);
                        }
                    }
                }
                if (this.Parameters == null) this.Parameters = EmptyArray<AST.FormalParam>.Instance;

                if (descStart < line.Length)
                    this.Description = line.Substring(descStart).TrimStart(null/*default whitespace characters*/);
            }

            /// <summary>
            /// Parses parameter description in a form of [type][$name][=initializer].
            /// </summary>
            /// <param name="paramDecl"></param>
            /// <returns></returns>
            private static AST.FormalParam/*!*/ParseParam(string/*!*/paramDecl)
            {
                Debug.Assert(!string.IsNullOrEmpty(paramDecl));
                
                string typehint = null;
                string paramname = null;
                bool byref = false;
                
                int i = 0;
                var word = NextWord(paramDecl, ref i);
                if (word != null)
                {
                    // [type]
                    if (word.Length > 0 && word[0] != '$')
                    {
                        typehint = word;
                        word = NextWord(paramDecl, ref i);
                    }

                    // [$name][=initializer]
                    if (word != null && word.Length > 0 && word[0] == '$')
                    {
                        int eqIndex = word.IndexOf('=');
                        paramname = ((eqIndex == -1) ? word : word.Remove(eqIndex));

                        byref = paramname.IndexOf('&') != -1;
                        paramname = paramname.TrimStart(new char[]{ '$', '&'});
                    }
                }

                return new AST.FormalParam(
                    Text.Span.Invalid, paramname, typehint,
                    byref ? AST.FormalParam.Flags.IsByRef : AST.FormalParam.Flags.Default,
                    null, null);
            }

            #region Helpers

            private static string NextWord(string/*!*/text, ref int index)
            {   
                // skip whitespaces:
                while (index < text.Length && char.IsWhiteSpace(text[index]))
                    index++;

                // read word:
                int startIndex = index;
                while (index < text.Length && !char.IsWhiteSpace(text[index]))
                    index++;

                // cut off the word:
                if (startIndex < index)
                    return text.Substring(startIndex, index - startIndex);
                else
                    return null;
            }

            #endregion

            internal override void ParseLine(string line, out Element next)
            {
                next = null;

                // add the line into description:
                Description = string.IsNullOrWhiteSpace(Description) ? line : (Description + NewLineString + line);
            }

            internal override void OnEndParsing()
            {
                base.OnEndParsing();

                if (string.IsNullOrWhiteSpace(this.Description))
                    this.Description = null;
                else
                    this.Description = this.Description.Trim();
            }

            internal override bool IsEmpty
            {
                get
                {
                    return _typeNames.Length == 0 && string.IsNullOrEmpty(this.MethodName) && string.IsNullOrWhiteSpace(this.Description);
                }
            }

            public override string ToString()
            {
                return Name + " " + this.MethodName + "()\n" + this.Description;
            }
        }

        public sealed class VersionTag : SingleLineTag
        {
            public const string Name = "@version";

            public string Version { get { return this.text; } }

            public VersionTag(string/*!*/line)
                : base(Name, line)
            {
            }

            public override string ToString()
            {
                return Name + " " + this.Version;
            }
        }

        public sealed class TestTag : EmptyTag
        {
            public const string Name = "@test";

            public TestTag(string/*!*/line)
                : base()
            {
            }

            public override string ToString()
            {
                return Name;
            }
        }

        public sealed class GroupTag : SingleLineTag
        {
            public const string Name = "@group";

            public string Group { get { return this.text; } }

            public GroupTag(string/*!*/line)
                : base(Name, line)
            {
            }

            public override string ToString()
            {
                return Name + " " + this.Group;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Empty singleton <see cref="Element"/> array.
        /// </summary>
        private static Element[]/*!*/EmptyElements { get { return EmptyArray<Element>.Instance; } }

        /// <summary>
        /// Original PHPDoc text, including comment tags.
        /// </summary>
        /// <remarks>Used internally for lazy initialization.</remarks>
        private string _docCommentString;

        /// <summary>
        /// Parsed data. Lazily initialized.
        /// </summary>
        private Element[] elements;

        /// <summary>
        /// Elements within the PHPDoc block. Some elements may be ignored due to missing information.
        /// Cannot be <c>null</c> reference.
        /// </summary>
        public Element[]/*!*/Elements
        {
            get
            {
                if (this.elements == null)
                    lock (this)
                        if (this.elements == null)  // double checked lock
                        {
                            var elementsList = ParseNoLock(this._docCommentString, this.Span.Start);
                            if (elementsList != null && elementsList.Count > 0)
                                this.elements = elementsList.ToArray();
                            else
                                this.elements = EmptyElements;

                            // dispose the string
                            this._docCommentString = null;
                        }
                
                return this.elements;
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes new instance of <see cref="PHPDocBlock"/>.
        /// </summary>
        /// <param name="doccomment">PHPDoc token content.</param>
        /// <param name="span">Position of the comment in the source code.</param>
        public PHPDocBlock(string doccomment, Span span)
            :base(span)
        {
            this._docCommentString = doccomment;
        }

        /// <summary>
        /// Parses given <paramref name="doccomment"/> into a list of <see cref="Element"/> instances.
        /// </summary>
        /// <param name="doccomment">Content of the PHPDoc token.</param>
        /// <param name="offset">Start position of <paramref name="doccomment"/> within the source code.</param>
        private static List<Element>/*!*/ParseNoLock(string/*!*/doccomment, int offset)
        {
            Debug.Assert(doccomment != null);

            // initialize line endings information
            var/*!*/lineBreaks = Text.LineBreaks.Create(doccomment);

            //
            var result = new List<Element>();
            Element tmp;
            
            Element/*!*/current = new ShortDescriptionElement();
            current.Span = Span.Invalid;

            for (int lineIndex = 0; lineIndex < lineBreaks.LinesCount; lineIndex++)
            {
                var lineSpan = lineBreaks.GetLineSpan(lineIndex);
                string/*!*/line = lineSpan.GetText(doccomment);
                
                int startCharIndex, endCharIndex;
                if (Element.TryParseLine(ref line, out tmp, lineIndex, out startCharIndex, out endCharIndex))    // validate the line, process tags
                {
                    Debug.Assert(line != null);
                    
                    if (tmp == null)    // no new element created
                    {
                        // pass the line into the current element
                        current.ParseLine(line, out tmp);

                        // update position of the element
                        if (current.Span.IsValid == false)      // ShortDescriptionElement has not initialized Span
                        {
                            if (!current.IsEmpty)   // initialize Start iff element has some text
                                current.Span = new Span(offset + lineSpan.Start + startCharIndex, endCharIndex - startCharIndex);
                        }
                        else                                    // other elements has to update their end position
                        {
                            if (tmp != null)
                                current.Span = new Span(current.Span.Start, offset + lineSpan.Start + endCharIndex - current.Span.Start);   // update its end position                        
                        }
                    }

                    if (tmp != null)    // new element created, it is already initialized with the current line
                    {
                        if (!current.IsEmpty)
                        {
                            current.OnEndParsing();
                            result.Add(current);
                        }

                        tmp.Span = new Span(offset + lineSpan.Start + startCharIndex, endCharIndex - startCharIndex);
                        current = tmp;  // it is current element from now
                    }
                }
            }

            // add the last found element
            if (!current.IsEmpty)
            {
                current.OnEndParsing();
                result.Add(current);
            }

            //
            return result;
        }

        #endregion

        #region Helper access methods

        public T GetElement<T>()  where T: Element
        {
            var elements = this.Elements;
            for (int i = 0; i < elements.Length; i++)
                if (elements[i] is T)
                    return (T)elements[i];

            return null;
        }

        /// <summary>
        /// Enumerate all the '@param' tags.
        /// </summary>
        public IEnumerable<ParamTag> Params
        {
            get
            {
                return this.Elements.OfType<ParamTag>();
            }
        }

        /// <summary>
        /// Gets '@return' tag or <c>null</c>.
        /// </summary>
        public ReturnTag Returns
        {
            get
            {
                return GetElement<ReturnTag>();
            }
        }

        /// <summary>
        /// Whether the PHPDoc block contains '@ignore' tag.
        /// </summary>
        public bool IsIgnored
        {
            get
            {
                return GetElement<IgnoreTag>() != null;
            }
        }

        /// <summary>
        /// Gets short description or <c>null</c>.
        /// </summary>
        public string ShortDescription
        {
            get
            {
                var tag = GetElement<ShortDescriptionElement>();
                return (tag != null) ? tag.Text : null;
            }
        }

        /// <summary>
        /// Gets long description or <c>null</c>.
        /// </summary>
        public string LongDescription
        {
            get
            {
                var tag = GetElement<LongDescriptionElement>();
                return (tag != null) ? tag.Text : null;
            }
        }

        /// <summary>
        /// Gets whole description, as a concatenation of <see cref="ShortDescription"/> and <see cref="LongDescription"/>.
        /// </summary>
        public string Summary
        {
            get
            {
                var shortdesc = ShortDescription;
                var longdesc = LongDescription;

                if (shortdesc != null || longdesc != null)
                {
                    if (string.IsNullOrEmpty(shortdesc))
                        return longdesc;

                    if (string.IsNullOrEmpty(longdesc))
                        return shortdesc;

                    return shortdesc + "\n" + longdesc;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets '@access' value or 'Public' if no such tag is found.
        /// </summary>
        public PhpMemberAttributes Access
        {
            get
            {
                var access = GetElement<AccessTag>();
                return (access != null) ? access.Access : PhpMemberAttributes.Public;
            }
        }

        /// <summary>
        /// Reconstructs PHPDoc block from parsed elements, including comment tags.
        /// </summary>
        public string PHPDocPreview
        {
            get
            {
                var result = new StringBuilder();
                result.AppendLine("/**");

                foreach (var element in this.Elements)
                {
                    var str = element.ToString();
                    if (str == null) continue;

                    foreach (var line in str.Split('\n'))
                    {
                        result.Append(" * ");
                        result.AppendLine(line);
                    }

                }
                result.Append(" */");

                return result.ToString();
            }
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns summary of PHPDoc.
        /// </summary>
        public override string ToString()
        {
            return this.Summary;
        }

        #endregion

        #region LangElement

        public override void VisitMe(AST.TreeVisitor visitor)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    internal static class PHPDocBlockHelper
    {
        /// <summary>
        /// Gets <see cref="PHPDocBlock"/> associated with <paramref name="properties"/>.
        /// </summary>
        public static PHPDocBlock GetPHPDoc(this IPropertyCollection/*!*/properties)
        {
            return properties.GetProperty<PHPDocBlock>();
        }

        /// <summary>
        /// Sets <see cref="PHPDocBlock"/> to <paramref name="properties"/>.
        /// </summary>
        public static void SetPHPDoc(this IPropertyCollection/*!*/properties, PHPDocBlock phpdoc)
        {
            if (phpdoc != null)
            {
                properties.SetProperty<PHPDocBlock>(phpdoc);

                // remember LangElement associated with phpdoc
                var element = properties as AST.LangElement;
                if (element != null)
                    phpdoc.SetProperty<AST.LangElement>(element);
            }
            else
            {
                properties.RemoveProperty<PHPDocBlock>();
            }
        }
    }
}
