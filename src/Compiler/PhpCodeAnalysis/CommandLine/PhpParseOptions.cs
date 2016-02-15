using Microsoft.CodeAnalysis;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis
{
    /// <summary>
    /// This class stores several source parsing related options and offers access to their values.
    /// </summary>
    public sealed class PhpParseOptions : ParseOptions, IEquatable<PhpParseOptions>
    {
        /// <summary>
        /// The default parse options.
        /// </summary>
        public static PhpParseOptions Default { get; } = new PhpParseOptions();

        private ImmutableDictionary<string, string> _features;

        public PhpParseOptions(
            DocumentationMode documentationMode = DocumentationMode.Parse,
            SourceCodeKind kind = SourceCodeKind.Regular)
            :base(kind, documentationMode)
        {
            if (!kind.IsValid())
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        internal PhpParseOptions(
            DocumentationMode documentationMode,
            SourceCodeKind kind,
            ImmutableDictionary<string, string> features)
            : this(documentationMode, kind)
        {
            if (features == null)
            {
                throw new ArgumentNullException(nameof(features));
            }

            _features = features;
        }

        private PhpParseOptions(PhpParseOptions other)
            : this(documentationMode: other.DocumentationMode, kind: other.Kind)
        {
        }
        
        public new PhpParseOptions WithKind(SourceCodeKind kind)
        {
            if (kind == this.Kind)
            {
                return this;
            }

            if (!kind.IsValid())
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            return new PhpParseOptions(this) { Kind = kind };
        }

        public new PhpParseOptions WithDocumentationMode(DocumentationMode documentationMode)
        {
            if (documentationMode == this.DocumentationMode)
            {
                return this;
            }

            if (!documentationMode.IsValid())
            {
                throw new ArgumentOutOfRangeException(nameof(documentationMode));
            }

            return new PhpParseOptions(this) { DocumentationMode = documentationMode };
        }

        public override ParseOptions CommonWithKind(SourceCodeKind kind)
        {
            return WithKind(kind);
        }

        protected override ParseOptions CommonWithDocumentationMode(DocumentationMode documentationMode)
        {
            return WithDocumentationMode(documentationMode);
        }

        protected override ParseOptions CommonWithFeatures(IEnumerable<KeyValuePair<string, string>> features)
        {
            return WithFeatures(features);
        }

        /// <summary>
        /// Enable some experimental language features for testing.
        /// </summary>
        public new PhpParseOptions WithFeatures(IEnumerable<KeyValuePair<string, string>> features)
        {
            if (features == null)
            {
                throw new ArgumentNullException(nameof(features));
            }

            return new PhpParseOptions(this) { _features = features.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase) };
        }

        public override IReadOnlyDictionary<string, string> Features
        {
            get
            {
                return _features;
            }
        }

        public override IEnumerable<string> PreprocessorSymbolNames
        {
            get
            {
                return ImmutableArray<string>.Empty;
            }
        }

        //internal bool IsFeatureEnabled(Syntax.LanguageFeatures feature)
        //{
        //    return true;
        //}

        public override bool Equals(object obj)
        {
            return this.Equals(obj as PhpParseOptions);
        }

        public bool Equals(PhpParseOptions other)
        {
            if (object.ReferenceEquals(this, other))
            {
                return true;
            }

            if (!base.EqualsHelper(other))
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return
                Hash.Combine(base.GetHashCodeHelper(), 0);
        }
    }
}
