using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Semantics
{
    partial class BoundExpression
    {
        internal virtual TypeSymbol Emit(CodeGenerator il)
        {
            throw new NotImplementedException();
        }
    }

    partial class BoundBinaryEx
    {
        internal override TypeSymbol Emit(CodeGenerator il)
        {
            Debug.Assert(this.Access == AccessType.Read || this.Access == AccessType.None);

            if (UsesOperatorMethod)
            {
                throw new NotImplementedException();    // call this.Operator(Left, Right)
            }

            var ltype = Left.Emit(il);
            var rtype = Right.Emit(il);

            switch (this.BinaryOperationKind)
            {
                case Microsoft.CodeAnalysis.Semantics.BinaryOperationKind.OperatorEquals:
                    if (ltype.SpecialType == SpecialType.System_Object && rtype.SpecialType == SpecialType.System_Object)
                    {
                        il.Builder.EmitOpCode(ILOpCode.Call, stackAdjustment: -1);    // 2 out, 1 return value on
                        il.Builder.EmitToken(il.CoreMethods.Operators.Equal_Object_Object.Symbol, null, il.Diagnostics);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                    return (TypeSymbol)il.Routine.DeclaringCompilation.GetSpecialType(SpecialType.System_Boolean);

                default:
                    throw new NotImplementedException();
            }
        }
    }

    partial class BoundLiteral
    {
        internal override TypeSymbol Emit(CodeGenerator il)
        {
            Debug.Assert(this.Access == AccessType.Read || this.Access == AccessType.None);

            // do nothing
            if (this.Access == AccessType.None)
            {
                return il.CoreTypes.Void;
            }

            // push value onto the evaluation stack
            if (!ConstantValue.HasValue)
                throw new InvalidOperationException();

            // TOOD: use ConstantValue

            var value = ConstantValue.Value;
            if (value == null)
            {
                il.Builder.EmitNullConstant();
                return il.CoreTypes.Object;
            }
            else
            {
                if (value is int)
                {
                    il.Builder.EmitLongConstant((int)value);
                    return il.CoreTypes.Long;
                }
                else if (value is long)
                {
                    il.Builder.EmitLongConstant((long)value);
                    return il.CoreTypes.Long;
                }
                else if (value is string)
                {
                    il.Builder.EmitStringConstant((string)value);
                    return il.CoreTypes.String;
                }
                else if (value is bool)
                {
                    il.Builder.EmitBoolConstant((bool)value);
                    return il.CoreTypes.Boolean;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }
    }

    partial class BoundVariableRef
    {
        internal override TypeSymbol Emit(CodeGenerator il)
        {
            if (this.Variable == null)
                throw new InvalidOperationException(); // variable was not resolved

            if (Access == AccessType.Read)
            {
                return il.EmitLoad(this.Variable);
            }
            else if (Access == AccessType.None)
            {
                // do nothing
                return il.CoreTypes.Void;
            }

            throw new NotImplementedException();
        }
    }

    partial class BoundEcho
    {
        internal override TypeSymbol Emit(CodeGenerator il)
        {
            Debug.Assert(Access == AccessType.None);

            foreach (var arg in _arguments)
            {
                il.EmitEcho(arg.Value);
            }

            return il.CoreTypes.Void;
        }
    }
}
