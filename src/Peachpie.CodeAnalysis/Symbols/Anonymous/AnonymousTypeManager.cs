using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    internal sealed class AnonymousTypeManager : CommonAnonymousTypeManager
    {
        internal AnonymousTypeManager(PhpCompilation compilation)
        {
            Debug.Assert(compilation != null);
            this.Compilation = compilation;
        }

        /// <summary> 
        /// Current compilation.
        /// </summary>
        public PhpCompilation Compilation { get; }

        /// <summary>
        /// Returns all templates owned by this type manager
        /// </summary>
        internal ImmutableArray<NamedTypeSymbol> GetAllCreatedTemplates()
        {
            // NOTE: templates may not be sealed in case metadata is being emitted without IL

            var builder = ArrayBuilder<NamedTypeSymbol>.GetInstance();

            //var anonymousTypes = ArrayBuilder<AnonymousTypeTemplateSymbol>.GetInstance();
            //GetCreatedAnonymousTypeTemplates(anonymousTypes);
            //builder.AddRange(anonymousTypes);
            //anonymousTypes.Free();

            var synthesizedDelegates = ArrayBuilder<SynthesizedDelegateSymbol>.GetInstance();
            GetCreatedSynthesizedDelegates(synthesizedDelegates);
            builder.AddRange(synthesizedDelegates);
            synthesizedDelegates.Free();

            if (_lazySynthesizedTypes != null)
            {
                builder.AddRange(_lazySynthesizedTypes);
            }

            return builder.ToImmutableAndFree();
        }

        #region Delegates

        /// <summary>
        /// Maps delegate signature shape (number of parameters and their ref-ness) to a synthesized generic delegate symbol.
        /// Unlike anonymous types synthesized delegates are not available through symbol APIs. They are only used in lowered bound trees.
        /// Currently used for dynamic call-site sites whose signature doesn't match any of the well-known Func or Action types.
        /// </summary>
        private ConcurrentDictionary<SynthesizedDelegateKey, SynthesizedDelegateValue> _lazySynthesizedDelegates;

        private struct SynthesizedDelegateKey : IEquatable<SynthesizedDelegateKey>
        {
            private readonly BitVector _byRefs;
            private readonly ushort _parameterCount;
            private readonly byte _returnsVoid;

            public SynthesizedDelegateKey(int parameterCount, BitVector byRefs, bool returnsVoid)
            {
                _parameterCount = (ushort)parameterCount;
                _returnsVoid = (byte)(returnsVoid ? 1 : 0);
                _byRefs = byRefs;
            }

            /// <summary>
            /// Produces name of the synthesized delegate symbol that encodes the parameter byref-ness and return type of the delegate.
            /// The arity is appended via `N suffix since in MetadataName calculation since the delegate is generic.
            /// </summary>
            public string MakeTypeName()
            {
                var pooledBuilder = PooledStringBuilder.GetInstance();
                pooledBuilder.Builder.Append(_returnsVoid != 0 ? "<>A" : "<>F");

                if (!_byRefs.IsNull)
                {
                    pooledBuilder.Builder.Append("{");

                    int i = 0;
                    foreach (int byRefIndex in _byRefs.Words())
                    {
                        if (i > 0)
                        {
                            pooledBuilder.Builder.Append(",");
                        }

                        pooledBuilder.Builder.AppendFormat("{0:x8}", byRefIndex);
                        i++;
                    }

                    pooledBuilder.Builder.Append("}");
                    Debug.Assert(i > 0);
                }

                return pooledBuilder.ToStringAndFree();
            }

            public override bool Equals(object obj)
            {
                return obj is SynthesizedDelegateKey && Equals((SynthesizedDelegateKey)obj);
            }

            public bool Equals(SynthesizedDelegateKey other)
            {
                return _parameterCount == other._parameterCount
                    && _returnsVoid == other._returnsVoid
                    && _byRefs.Equals(other._byRefs);
            }

            public override int GetHashCode()
            {
                return Hash.Combine((int)_parameterCount, Hash.Combine((int)_returnsVoid, _byRefs.GetHashCode()));
            }
        }

        private struct SynthesizedDelegateValue
        {
            public readonly SynthesizedDelegateSymbol Delegate;

            // the manager that created this delegate:
            public readonly AnonymousTypeManager Manager;

            public SynthesizedDelegateValue(AnonymousTypeManager manager, SynthesizedDelegateSymbol @delegate)
            {
                Debug.Assert(manager != null && (object)@delegate != null);
                this.Manager = manager;
                this.Delegate = @delegate;
            }
        }

        private class SynthesizedDelegateSymbolComparer : IComparer<SynthesizedDelegateSymbol>
        {
            public static readonly SynthesizedDelegateSymbolComparer Instance = new SynthesizedDelegateSymbolComparer();

            public int Compare(SynthesizedDelegateSymbol x, SynthesizedDelegateSymbol y)
            {
                return x.MetadataName.CompareTo(y.MetadataName);
            }
        }

        /// <summary>
        /// The set of synthesized delegates created by
        /// this AnonymousTypeManager.
        /// </summary>
        private void GetCreatedSynthesizedDelegates(ArrayBuilder<SynthesizedDelegateSymbol> builder)
        {
            Debug.Assert(!builder.Any());
            var delegates = _lazySynthesizedDelegates;
            if (delegates != null)
            {
                foreach (var template in delegates.Values)
                {
                    if (ReferenceEquals(template.Manager, this))
                    {
                        builder.Add(template.Delegate);
                    }
                }
                builder.Sort(SynthesizedDelegateSymbolComparer.Instance);
            }
        }

        private ConcurrentDictionary<SynthesizedDelegateKey, SynthesizedDelegateValue> SynthesizedDelegates
        {
            get
            {
                if (_lazySynthesizedDelegates == null)
                {
                    Interlocked.CompareExchange(ref _lazySynthesizedDelegates,
                                                new ConcurrentDictionary<SynthesizedDelegateKey, SynthesizedDelegateValue>(),
                                                null);
                }

                return _lazySynthesizedDelegates;
            }
        }

        internal SynthesizedDelegateSymbol SynthesizeDelegate(int parameterCount, BitVector byRefParameters, bool returnsVoid)
        {
            // parameterCount doesn't include return type
            Debug.Assert(byRefParameters.IsNull || parameterCount == byRefParameters.Capacity);

            var key = new SynthesizedDelegateKey(parameterCount, byRefParameters, returnsVoid);

            SynthesizedDelegateValue result;
            if (this.SynthesizedDelegates.TryGetValue(key, out result))
            {
                return result.Delegate;
            }

            // NOTE: the newly created template may be thrown away if another thread wins
            return this.SynthesizedDelegates.GetOrAdd(key,
                new SynthesizedDelegateValue(
                    this,
                    new SynthesizedDelegateSymbol(
                        (NamespaceOrTypeSymbol)this.Compilation.SourceAssembly.GlobalNamespace,
                        key.MakeTypeName(),
                        this.System_Object,
                        Compilation.GetSpecialType(SpecialType.System_IntPtr),
                        returnsVoid ? Compilation.GetSpecialType(SpecialType.System_Void) : null,
                        parameterCount,
                        byRefParameters))).Delegate;
        }

        #endregion

        #region Types

        private ConcurrentBag<NamedTypeSymbol> _lazySynthesizedTypes;

        public SynthesizedTypeSymbol SynthesizeType(string name, Accessibility accessibility = Accessibility.Internal)
        {
            var type = new SynthesizedTypeSymbol(Compilation, name, null, accessibility);

            if (_lazySynthesizedTypes == null)
            {
                Interlocked.CompareExchange(ref _lazySynthesizedTypes, new ConcurrentBag<NamedTypeSymbol>(), null);
            }

            _lazySynthesizedTypes.Add(type);

            return type;
        }

        #endregion

        #region Symbols

        public NamedTypeSymbol System_Object
        {
            get { return Compilation.GetSpecialType(SpecialType.System_Object); }
        }

        public NamedTypeSymbol System_Void
        {
            get { return Compilation.GetSpecialType(SpecialType.System_Void); }
        }

        public NamedTypeSymbol System_Boolean
        {
            get { return Compilation.GetSpecialType(SpecialType.System_Boolean); }
        }

        public NamedTypeSymbol System_String
        {
            get { return Compilation.GetSpecialType(SpecialType.System_String); }
        }

        public NamedTypeSymbol System_Int32
        {
            get { return Compilation.GetSpecialType(SpecialType.System_Int32); }
        }

        public NamedTypeSymbol System_Diagnostics_DebuggerBrowsableState
        {
            get { return Compilation.GetWellKnownType(WellKnownType.System_Diagnostics_DebuggerBrowsableState); }
        }

        public MethodSymbol System_Object__Equals
        {
            get { return this.Compilation.GetSpecialTypeMember(SpecialMember.System_Object__Equals) as MethodSymbol; }
        }

        public MethodSymbol System_Object__ToString
        {
            get { return this.Compilation.GetSpecialTypeMember(SpecialMember.System_Object__ToString) as MethodSymbol; }
        }

        public MethodSymbol System_Object__GetHashCode
        {
            get { return this.Compilation.GetSpecialTypeMember(SpecialMember.System_Object__GetHashCode) as MethodSymbol; }
        }

        public MethodSymbol System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor
        {
            get { return this.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor) as MethodSymbol; }
        }

        public MethodSymbol System_Diagnostics_DebuggerHiddenAttribute__ctor
        {
            get { return this.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Diagnostics_DebuggerHiddenAttribute__ctor) as MethodSymbol; }
        }

        public MethodSymbol System_Diagnostics_DebuggerBrowsableAttribute__ctor
        {
            get { return this.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Diagnostics_DebuggerBrowsableAttribute__ctor) as MethodSymbol; }
        }

        public MethodSymbol System_Collections_Generic_EqualityComparer_T__Equals
        {
            get { return this.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_EqualityComparer_T__Equals) as MethodSymbol; }
        }

        public MethodSymbol System_Collections_Generic_EqualityComparer_T__GetHashCode
        {
            get { return this.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_EqualityComparer_T__GetHashCode) as MethodSymbol; }
        }

        public MethodSymbol System_Collections_Generic_EqualityComparer_T__get_Default
        {
            get { return this.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Collections_Generic_EqualityComparer_T__get_Default) as MethodSymbol; }
        }

        public MethodSymbol System_String__Format_IFormatProvider
        {
            get { return this.Compilation.GetWellKnownTypeMember(WellKnownMember.System_String__Format_IFormatProvider) as MethodSymbol; }
        }

        #endregion
    }
}
