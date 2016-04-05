using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.CodeGen
{
    internal class DynamicOperationFactory
    {
        readonly PhpCompilation _compilation;

        public DynamicOperationFactory(PhpCompilation compilation)
        {
            Contract.ThrowIfNull(compilation);
            _compilation = compilation;
        }

        internal NamedTypeSymbol GetCallSiteDelegateType(
            BoundExpression loweredReceiver,
            RefKind receiverRefKind,
            ImmutableArray<BoundExpression> loweredArguments,
            ImmutableArray<RefKind> refKinds,
            BoundExpression loweredRight,
            TypeSymbol resultType)
        {
            Debug.Assert(refKinds.IsDefaultOrEmpty || refKinds.Length == loweredArguments.Length);

            var callSiteType = _compilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_CallSite);
            if (callSiteType.IsErrorType())
            {
                return null;
            }

            var delegateSignature = MakeCallSiteDelegateSignature(callSiteType, loweredReceiver, loweredArguments, loweredRight, resultType);
            bool returnsVoid = resultType.SpecialType == SpecialType.System_Void;
            bool hasByRefs = receiverRefKind != RefKind.None || !refKinds.IsDefaultOrEmpty;

            if (!hasByRefs)
            {
                var wkDelegateType = returnsVoid ?
                    WellKnownTypes.GetWellKnownActionDelegate(invokeArgumentCount: delegateSignature.Length) :
                    WellKnownTypes.GetWellKnownFunctionDelegate(invokeArgumentCount: delegateSignature.Length - 1);

                if (wkDelegateType != WellKnownType.Unknown)
                {
                    var delegateType = _compilation.GetWellKnownType(wkDelegateType);
                    if (delegateType != null && !delegateType.IsErrorType())
                    {
                        return delegateType.Construct(delegateSignature);
                    }
                }
            }

            BitVector byRefs;
            if (hasByRefs)
            {
                byRefs = BitVector.Create(1 + (loweredReceiver != null ? 1 : 0) + loweredArguments.Length + (loweredRight != null ? 1 : 0));

                int j = 1;
                if (loweredReceiver != null)
                {
                    byRefs[j++] = receiverRefKind != RefKind.None;
                }

                if (!refKinds.IsDefault)
                {
                    for (int i = 0; i < refKinds.Length; i++, j++)
                    {
                        if (refKinds[i] != RefKind.None)
                        {
                            byRefs[j] = true;
                        }
                    }
                }
            }
            else
            {
                byRefs = default(BitVector);
            }

            int parameterCount = delegateSignature.Length - (returnsVoid ? 0 : 1);

            //return _compilation.AnonymousTypeManager.SynthesizeDelegate(parameterCount, byRefs, returnsVoid).Construct(delegateSignature);
            throw new NotImplementedException();
        }

        internal TypeSymbol[] MakeCallSiteDelegateSignature(TypeSymbol callSiteType, BoundExpression receiver, ImmutableArray<BoundExpression> arguments, BoundExpression right, TypeSymbol resultType)
        {
            var systemObjectType = (TypeSymbol)_compilation.GetSpecialType(SpecialType.System_Object);
            var result = new TypeSymbol[1 + (receiver != null ? 1 : 0) + arguments.Length + (right != null ? 1 : 0) + (resultType.SpecialType == SpecialType.System_Void ? 0 : 1)];
            int j = 0;

            // CallSite:
            result[j++] = callSiteType;

            // receiver:
            if (receiver != null)
            {
                result[j++] = receiver.ResultType ?? systemObjectType;
            }

            // argument types:
            for (int i = 0; i < arguments.Length; i++)
            {
                result[j++] = arguments[i].ResultType ?? systemObjectType;
            }

            // right hand side of an assignment:
            if (right != null)
            {
                result[j++] = right.ResultType ?? systemObjectType;
            }

            // return type:
            if (j < result.Length)
            {
                result[j++] = resultType ?? systemObjectType;
            }

            return result;
        }
    }
}
