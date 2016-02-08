using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using PHP.Core.Parsers;
using PHP.Core.Text;
using System.IO;

namespace PHP.Core
{
    #region SourceUnit

    /// <summary>
    /// Represents single source document.
    /// </summary>
    public abstract class SourceUnit : ILineBreaks, IPropertyCollection
    {
        #region Fields & Properties

        /// <summary>
        /// Source file containing the unit. For evals, it can be even a non-php source file.
        /// Used for emitting debug information and error reporting.
        /// </summary>
        public string/*!*/ FilePath { get { return _filePath; } }
        readonly string/*!*/ _filePath;

        public AST.GlobalCode Ast { get { return ast; } }
        protected AST.GlobalCode ast;

        /// <summary>
        /// Set of object properties.
        /// </summary>
        private PropertyCollection innerProps;

        /// <summary>
        /// Gets line breaks for this source unit.
        /// </summary>
        /// <remarks>Line breaks are used to resolve line and column number from given position.</remarks>
        public ILineBreaks/*!*/LineBreaks { get { return (ILineBreaks)this; } }

        /// <summary>
        /// Line breaks managed internally.
        /// </summary>
        protected ILineBreaks _innerLineBreaks;

        /// <summary>
        /// Naming context defining aliases.
        /// </summary>
        public NamingContext/*!*/ Naming
        {
            get { return this._naming; }
            internal set
            {
                if (value == null) throw new ArgumentNullException();
                this._naming = value;
            }
        }
        private NamingContext/*!*/_naming;

        /// <summary>
        /// Current namespace (in case we are compiling through eval from within namespace).
        /// </summary>
        public QualifiedName? CurrentNamespace { get { return this._naming.CurrentNamespace; } }
        
        public List<QualifiedName>/*!*/ImportedNamespaces { get { return importedNamespaces; } }
        private readonly List<QualifiedName>/*!*/importedNamespaces = new List<QualifiedName>();
        public bool HasImportedNamespaces { get { return this.importedNamespaces != null && this.importedNamespaces.Count != 0; } }

        /// <summary>
        /// Encoding of the file or the containing file.
        /// </summary>
        public Encoding/*!*/ Encoding { get { return _encoding; } }
        protected readonly Encoding/*!*/ _encoding;

        /// <summary>
        /// Gets value indicating whether we are in pure mode.
        /// </summary>
        public virtual bool IsPure { get { return false; } }

        /// <summary>
        /// Gets value indicating whether we are processing transient unit.
        /// </summary>
        public virtual bool IsTransient { get { return false; } }

        #endregion

        #region Construction

        public SourceUnit(string/*!*/ filePath, Encoding/*!*/ encoding, ILineBreaks/*!*/lineBreaks)
        {
            Debug.Assert(filePath != null && encoding != null);
            Debug.Assert(lineBreaks != null);

            _filePath = filePath;
            _encoding = encoding;
            _innerLineBreaks = lineBreaks;
            _naming = new NamingContext(null, null);
        }

        #endregion

        #region Abstract Methods

        public abstract void Parse(
            ErrorSink/*!*/ errors, IReductionsSink/*!*/ reductionsSink,
            LanguageFeatures features);

        public abstract void Close();

        public abstract string GetSourceCode(Text.Span span);

        #endregion

        #region ILineBreaks Members

        int ILineBreaks.Count
        {
            get { return this._innerLineBreaks.Count; }
        }

        int ILineBreaks.TextLength
        {
            get { return this._innerLineBreaks.TextLength; }
        }

        int ILineBreaks.EndOfLineBreak(int index)
        {
            return this._innerLineBreaks.EndOfLineBreak(index);
        }

        public virtual int GetLineFromPosition(int position)
        {
            return this._innerLineBreaks.GetLineFromPosition(position);
        }

        public virtual void GetLineColumnFromPosition(int position, out int line, out int column)
        {
            this._innerLineBreaks.GetLineColumnFromPosition(position, out line, out column);
        }

        #endregion

        #region IPropertyCollection Members

        void IPropertyCollection.SetProperty(object key, object value)
        {
            innerProps.SetProperty(key, value);
        }

        void IPropertyCollection.SetProperty<T>(T value)
        {
            innerProps.SetProperty<T>(value);
        }

        object IPropertyCollection.GetProperty(object key)
        {
            return innerProps.GetProperty(key);
        }

        T IPropertyCollection.GetProperty<T>()
        {
            return innerProps.GetProperty<T>();
        }

        bool IPropertyCollection.TryGetProperty(object key, out object value)
        {
            return innerProps.TryGetProperty(key, out value);
        }

        bool IPropertyCollection.TryGetProperty<T>(out T value)
        {
            return innerProps.TryGetProperty<T>(out value);
        }

        bool IPropertyCollection.RemoveProperty(object key)
        {
            return innerProps.RemoveProperty(key);
        }

        bool IPropertyCollection.RemoveProperty<T>()
        {
            return innerProps.RemoveProperty<T>();
        }

        void IPropertyCollection.ClearProperties()
        {
            innerProps.ClearProperties();
        }

        object IPropertyCollection.this[object key]
        {
            get
            {
                return innerProps[key];
            }
            set
            {
                innerProps[key] = value;
            }
        }

        #endregion
    }

    #endregion

    #region CodeSourceUnit

    /// <summary>
    /// Source unit from string representation of code.
    /// </summary>
    public class CodeSourceUnit : SourceUnit
    {
        #region Fields & Properties

        public string/*!*/ Code { get { return code; } }
        private readonly string/*!*/ code;

        /// <summary>
        /// Initial state of source code parser. Used by <see cref="Parse"/>.
        /// </summary>
        private readonly Lexer.LexicalStates initialState;

        #endregion

        #region SourceUnit

        public CodeSourceUnit(string/*!*/ code, string/*!*/ filePath,
            Encoding/*!*/ encoding, Lexer.LexicalStates initialState)
            : base(filePath, encoding, Text.LineBreaks.Create(code))
        {
            this.code = code;
            this.initialState = initialState;
        }

        public override void Parse(ErrorSink/*!*/ errors, IReductionsSink/*!*/ reductionsSink, LanguageFeatures features)
        {
            Parser parser = new Parser();

            using (StringReader source_reader = new StringReader(code))
            {
                ast = parser.Parse(this, source_reader, errors, reductionsSink, initialState, features);
            }
        }

        /// <summary>
        /// Initializes <c>Ast</c> with empty <see cref="AST.GlobalCode"/>.
        /// </summary>
        internal void SetEmptyAst()
        {
            this.ast = new AST.GlobalCode(new List<AST.Statement>(), this);
        }

        public override string GetSourceCode(Text.Span span)
        {
            return span.GetText(code);
        }

        public override void Close()
        {

        }

        #endregion

        #region Helpers

        /// <summary>
        /// Creates source unit and parses given <paramref name="code"/>.
        /// </summary>
        /// <param name="code">Source code to be parsed.</param>
        /// <param name="filePath">Source file used for error reporting.</param>
        /// <param name="errors">Errors sink. Can be <c>null</c>.</param>
        /// <param name="reductionsSink">Reduction sink. Can be <c>null</c>.</param>
        /// <param name="features">Optional. Language features.</param>
        /// <param name="initialState">
        /// Optional. Initial parser state.
        /// This allows e.g. to parse PHP code without encapsulating the code into opening and closing tags.</param>
        /// <returns></returns>
        public static SourceUnit/*!*/ParseCode(string code, string filePath,
            ErrorSink/*!*/ errors,
            IReductionsSink/*!*/ reductionsSink = null,
            LanguageFeatures features = LanguageFeatures.Php5,
            Lexer.LexicalStates initialState = Lexer.LexicalStates.INITIAL)
        {
            var/*!*/unit = new CodeSourceUnit(code, filePath, Encoding.UTF8, initialState);
            unit.Parse(errors, reductionsSink, features);
            unit.Close();

            //
            return unit;
        }

        #endregion

        //#region ILineBreaks Members

        //public override int GetLineFromPosition(int position)
        //{
        //    // shift the position
        //    return base.GetLineFromPosition(position) + this.Line;
        //}

        //public override void GetLineColumnFromPosition(int position, out int line, out int column)
        //{
        //    // shift the position
        //    base.GetLineColumnFromPosition(position, out line, out column);
        //    if (line == 0)
        //        column += this.Column;
        //    line += this.Line;
        //}

        //#endregion
    }

    #endregion
}
