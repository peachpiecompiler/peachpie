using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Symbols;
using Pchp.CodeAnalysis.Semantics.Graph;
using System.Reflection.Metadata;
using System.Diagnostics;
using System.Collections.Immutable;
using Cci = Microsoft.Cci;
using Pchp.CodeAnalysis.Semantics;

namespace Pchp.CodeAnalysis.CodeGen
{
    internal partial class CodeGenerator : IDisposable
    {
        #region LocalScope

        internal class LocalScope
        {
            #region Fields

            readonly CodeGenerator _codegen;
            readonly LocalScope _parent;
            readonly ScopeType _type;
            readonly int _from, _to;

            SortedSet<BoundBlock> _blocks;

            #endregion

            #region Contruction

            public LocalScope(CodeGenerator codegen, LocalScope parent, ScopeType type, int from, int to)
            {
                Contract.ThrowIfNull(codegen);
                Debug.Assert(from < to);
                _codegen = codegen;
                _parent = parent;
                _type = type;
                _from = from;
                _to = to;
            }

            #endregion

            /// <summary>
            /// Gets underlaying <see cref="ILBuilder"/>.
            /// </summary>
            public ILBuilder IL => _codegen.Builder;

            /// <summary>
            /// Gets parent scope. Can be <c>null</c> in case of a root scope.
            /// </summary>
            public LocalScope Parent => _parent;

            public bool IsIn(BoundBlock block)
            {
                return block.Ordinal >= _from && block.Ordinal < _to;
            }

            public void Enqueue(BoundBlock block)
            {
                if (IsIn(block))
                {
                    if (_blocks == null)
                    {
                        _blocks = new SortedSet<BoundBlock>(BoundBlock.EmitOrderComparer.Instance);
                    }

                    _blocks.Add(block);
                }
                else
                {
                    if (Parent == null)
                        throw new ArgumentOutOfRangeException();

                    Parent.Enqueue(block);
                }
            }

            public void ContinueWith(BoundBlock block)
            {
                if (_codegen.IsGenerated(block))
                {
                    // backward edge;
                    // or the block was already emitted, branch there:
                    IL.EmitBranch(ILOpCode.Br, block);
                    return;
                }

                if (block.IsDead || block.FlowState == null)
                {
                    // ignore dead/unreachable blocks
                    return;
                }

                if (block.Ordinal < _from)
                {
                    throw new InvalidOperationException("block miss");
                }

                if (IsIn(block))
                {
                    // TODO: avoid branching to a guarded scope // e.g. goto x; try { x: }

                    if (_blocks == null || _blocks.Count == 0 || _blocks.Comparer.Compare(block, _blocks.First()) < 0)
                    {
                        if (_blocks != null)
                        {
                            _blocks.Remove(block);
                        }

                        // continue with the block
                        _codegen.GenerateBlock(block);
                        return;
                    }
                }

                // forward edge:
                // note: if block will follow immediately, .br will be ignored
                IL.EmitBranch(ILOpCode.Br, block);
                this.Enqueue(block);
            }

            internal BoundBlock Dequeue()
            {
                BoundBlock block = null;

                if (_blocks != null && _blocks.Count != 0)
                {
                    block = _blocks.First();
                    _blocks.Remove(block);
                }

                //
                return block;
            }

            internal void BlockGenerated(BoundBlock block)
            {
                // remove block from parent todo list
                var scope = this.Parent;
                while (scope != null)
                {
                    if (scope._blocks != null && scope._blocks.Remove(block))
                    {
                        return;
                    }

                    scope = scope.Parent;
                }
            }
        }

        #endregion

        #region Fields

        readonly ILBuilder _il;
        readonly SourceRoutineSymbol _routine;
        readonly PEModuleBuilder _moduleBuilder;
        readonly PhpOptimizationLevel _optimizations;
        readonly bool _emitPdbSequencePoints;
        readonly DiagnosticBag _diagnostics;
        readonly DynamicOperationFactory _factory;

        /// <summary>
        /// Place for loading a reference to <c>Pchp.Core.Context</c>.
        /// </summary>
        readonly IPlace _contextPlace;

        /// <summary>
        /// Place referring array of locals variables.
        /// This is valid for global scope, local scope with unoptimized locals and generators.
        /// </summary>
        readonly IPlace _localsPlaceOpt;

        /// <summary>
        /// Place referring array to temporal local variables.
        /// </summary>
        /// <remarks>
        /// Must not be null if method contains any synthesized temporal local variables as they need to be indirect.
        /// </remarks>
        readonly IPlace _tmpLocalsPlace;

        /// <summary>
        /// Are locals initilized externally.
        /// </summary>
        readonly bool _localsInitialized;

        /// <summary>
        /// Place for loading a reference to <c>this</c>.
        /// </summary>
        public IPlace ThisPlaceOpt => _thisPlace;
        readonly IPlace _thisPlace;

        /// <summary>
        /// In case code generator emits body of a generator SM method,
        /// gets reference to synthesized method symbol with additional information.
        /// </summary>
        internal SourceGeneratorSymbol GeneratorStateMachineMethod { get => _smmethod; set => _smmethod = value; }
        SourceGeneratorSymbol _smmethod;

        /// <summary>
        /// BoundBlock.Tag value indicating the block was emitted.
        /// </summary>
        readonly int _emmittedTag;

        LocalScope _scope = null;

        #endregion

        #region Properties

        /// <summary>
        /// Gets underlaying <see cref="ILBuilder"/>.
        /// </summary>
        public ILBuilder Builder => _il;

        /// <summary>
        /// Module builder.
        /// </summary>
        public PEModuleBuilder Module => _moduleBuilder;

        /// <summary>
        /// Gets the routine we are emitting.
        /// </summary>
        public SourceRoutineSymbol Routine => _routine;

        /// <summary>
        /// For debug purposes.
        /// Current routine being generated.
        /// </summary>
        internal MethodSymbol DebugRoutine { get; set; }

        /// <summary>
        /// Type context of currently emitted expressions. Can be <c>null</c>.
        /// </summary>
        internal TypeRefContext TypeRefContext
        {
            get => _typeRefContext ?? this.Routine?.TypeRefContext;
            set => _typeRefContext = value;
        }
        TypeRefContext _typeRefContext;

        public DiagnosticBag Diagnostics => _diagnostics;

        /// <summary>
        /// Whether to emit debug assertions.
        /// </summary>
        public bool IsDebug => _optimizations.IsDebug();

        /// <summary>
        /// Whether to emit sequence points (PDB).
        /// </summary>
        public bool EmitPdbSequencePoints => _emitPdbSequencePoints;

        /// <summary>
        /// Gets a reference to compilation object.
        /// </summary>
        public PhpCompilation DeclaringCompilation => _moduleBuilder.Compilation;

        /// <summary>Gets <see cref="BoundTypeRefFactory"/> instance.</summary>
        BoundTypeRefFactory BoundTypeRefFactory => DeclaringCompilation.TypeRefFactory;

        /// <summary>
        /// Gets conversions helper class.
        /// </summary>
        public Conversions Conversions => DeclaringCompilation.Conversions;

        /// <summary>
        /// Well known types.
        /// </summary>
        public CoreTypes CoreTypes => DeclaringCompilation.CoreTypes;

        /// <summary>
        /// Well known methods.
        /// </summary>
        public CoreMethods CoreMethods => DeclaringCompilation.CoreMethods;

        /// <summary>
        /// Factory for dynamic and anonymous types.
        /// </summary>
        public DynamicOperationFactory Factory => _factory;

        /// <summary>
        /// Whether the generator corresponds to a global scope.
        /// </summary>
        public bool IsGlobalScope => _routine is SourceGlobalMethodSymbol;

        /// <summary>
        /// Whether the code is generated inside method in a trait type.
        /// </summary>
        public bool IsInTrait => _routine is SourceMethodSymbol && _routine.ContainingType.IsTraitType();

        /// <summary>
        /// Gets or sets value determining <see cref="BoundArrayEx"/> is being emitted.
        /// When set, array expression caching is disabled.
        /// </summary>
        internal bool IsInCachedArrayExpression { get; set; }

        /// <summary>
        /// Type of the caller context (the class declaring current method) or null.
        /// </summary>
        public TypeSymbol CallerType
        {
            get => GetSelfType(_callerType ?? (_routine is SourceMethodSymbol ? _routine.ContainingType : null));
            set => _callerType = value;
        }
        TypeSymbol _callerType;
        IPlace _callerTypePlace;

        static TypeSymbol GetSelfType(TypeSymbol scope) => scope is SourceTraitTypeSymbol t ? t.TSelfParameter : scope;

        public SourceFileSymbol ContainingFile
        {
            get => _containingFile;
            internal set => _containingFile = value;
        }
        SourceFileSymbol _containingFile;

        #endregion

        #region Construction

        public CodeGenerator(ILBuilder il, PEModuleBuilder moduleBuilder, DiagnosticBag diagnostics, PhpOptimizationLevel optimizations, bool emittingPdb,
            NamedTypeSymbol container, IPlace contextPlace, IPlace thisPlace, MethodSymbol routine = null, IPlace locals = null, bool localsInitialized = false, IPlace tempLocals = null)
        {
            Contract.ThrowIfNull(il);
            Contract.ThrowIfNull(moduleBuilder);

            if (localsInitialized) { Debug.Assert(locals != null); }

            _il = il;
            _moduleBuilder = moduleBuilder;
            _optimizations = optimizations;
            _diagnostics = diagnostics;

            _localsPlaceOpt = locals;
            _tmpLocalsPlace = tempLocals;
            _localsInitialized = localsInitialized;

            _emmittedTag = 0;

            _contextPlace = contextPlace;
            _thisPlace = thisPlace;

            _factory = new DynamicOperationFactory(this, container);

            _emitPdbSequencePoints = emittingPdb;

            _routine = routine as SourceRoutineSymbol;
            _containingFile = GetContainingFile(routine);

            if (EmitPdbSequencePoints && _containingFile != null)
            {
                il.SetInitialDebugDocument(_containingFile.SyntaxTree);
            }
        }

        /// <summary>
        /// Copy ctor with different routine content (and TypeRefContext).
        /// Used for emitting in a context of a different routine (parameter initializer).
        /// </summary>
        public CodeGenerator(CodeGenerator cg, SourceRoutineSymbol routine)
            : this(cg._il, cg._moduleBuilder, cg._diagnostics, cg._optimizations, cg._emitPdbSequencePoints, routine.ContainingType, cg.ContextPlaceOpt, cg.ThisPlaceOpt, routine, cg._localsPlaceOpt, cg.InitializedLocals)
        {
            _emmittedTag = cg._emmittedTag;
        }

        public CodeGenerator(SourceRoutineSymbol routine, ILBuilder il, PEModuleBuilder moduleBuilder, DiagnosticBag diagnostics, PhpOptimizationLevel optimizations, bool emittingPdb)
            : this(il, moduleBuilder, diagnostics, optimizations, emittingPdb, routine.ContainingType, routine.GetContextPlace(moduleBuilder), routine.GetThisPlace(), routine)
        {
            Contract.ThrowIfNull(routine);

            _emmittedTag = (routine.ControlFlowGraph != null) ? routine.ControlFlowGraph.NewColor() : -1;

            bool localsAlreadyInited;
            _localsPlaceOpt = GetLocalsPlace(routine, out localsAlreadyInited);
            _localsInitialized = localsAlreadyInited;


            // Emit sequence points unless
            // - the PDBs are not being generated
            // - debug information for the method is not generated since the method does not contain
            //   user code that can be stepped through, or changed during EnC.
            // 
            // This setting only affects generating PDB sequence points, it shall not affect generated IL in any way.
            _emitPdbSequencePoints = emittingPdb && true; // routine.GenerateDebugInfo;
        }

        static SourceFileSymbol GetContainingFile(MethodSymbol method)
        {
            if (ReferenceEquals(method, null)) return null;
            if (method is SourceRoutineSymbol r) return r.ContainingFile;

            for (var t = method.ContainingType; t != null; t = t.ContainingType)
            {
                if (t is SourceFileSymbol s) return s;
                if (t is SourceTypeSymbol st) return st.ContainingFile;
            }

            return null;
        }

        //Gets appropriate locals place and whether it's inicialized externally or not.
        IPlace GetLocalsPlace(SourceRoutineSymbol routine, out bool localsAlreadyInitialized)
        {
            if (routine is SourceGlobalMethodSymbol)
            {
                // second parameter
                Debug.Assert(routine.ParameterCount >= 2 && routine.Parameters[1].Name == SpecialParameterSymbol.LocalsName);

                localsAlreadyInitialized = true;
                return new ParamPlace(routine.Parameters[1]);
            }
            else if ((routine.Flags & RoutineFlags.RequiresLocalsArray) != 0)
            {
                // declare PhpArray <locals>
                var symbol = new SynthesizedLocalSymbol(Routine, "<locals>", CoreTypes.PhpArray);
                var localsDef = this.Builder.LocalSlotManager.DeclareLocal((Cci.ITypeReference)symbol.Type, symbol, symbol.Name, SynthesizedLocalKind.OptimizerTemp, LocalDebugId.None, 0, LocalSlotConstraints.None, ImmutableArray<bool>.Empty, ImmutableArray<string>.Empty, false);

                localsAlreadyInitialized = false;
                return new LocalPlace(localsDef);
            }

            //
            localsAlreadyInitialized = false;
            return null;
        }

        #endregion

        #region CFG Emitting

        /// <summary>
        /// Emits routines body.
        /// </summary>
        internal void Generate()
        {
            Debug.Assert(_routine != null && _routine.ControlFlowGraph != null);
            _routine.Generate(this);
        }

        internal void GenerateScope(BoundBlock block, int to)
        {
            GenerateScope(block, ScopeType.Variable, to);
        }

        internal void GenerateScope(BoundBlock block, ScopeType type, int to)
        {
            Contract.ThrowIfNull(block);

            // open scope
            _scope = new LocalScope(this, _scope, type, block.Ordinal, to);
            _scope.ContinueWith(block);

            while ((block = _scope.Dequeue()) != null)
            {
                GenerateBlock(block);
            }

            // close scope
            _scope = _scope.Parent;

            //
            _il.AssertStackEmpty();
        }

        /// <summary>
        /// Gets a reference to the current scope.
        /// </summary>
        internal LocalScope Scope => _scope;

        void GenerateBlock(BoundBlock block)
        {
            // mark the block as emitted
            Debug.Assert(block.Tag != _emmittedTag);
            block.Tag = _emmittedTag;

            // mark location as a label
            // to allow branching to the block
            _il.MarkLabel(block);

            //
            _scope.BlockGenerated(block);

            //
            Generate(block);
        }

        /// <summary>
        /// Invokes <see cref="IGenerator.Generate"/>.
        /// </summary>
        internal void Generate(IGenerator element)
        {
            element?.Generate(this);
        }

        /// <summary>
        /// Gets value indicating whether the given block was already emitted.
        /// </summary>
        internal bool IsGenerated(BoundBlock block)
        {
            Contract.ThrowIfNull(block);
            return block.Tag == _emmittedTag;
        }

        #endregion

        #region IDisposable

        void IDisposable.Dispose()
        {

        }

        #endregion
    }

    /// <summary>
    /// Represents a semantic element that can be emitted.
    /// </summary>
    internal interface IGenerator
    {
        /// <summary>
        /// Emits IL into the underlaying <see cref="ILBuilder"/>.
        /// </summary>
        void Generate(CodeGenerator cg);
    }

    [DebuggerDisplay("Label {_name}")]
    internal sealed class NamedLabel
    {
        readonly string _name;

        public NamedLabel(string name) { _name = name; }
    }
}
