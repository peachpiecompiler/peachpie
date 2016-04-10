using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Dynamic
{
    public abstract class CallMethodBinder : DynamicMetaObjectBinder
    {
        protected readonly Type _returnType;
        protected readonly int _genericParamsCount;
        protected readonly Type _classContext;

        protected abstract string ResolveName(DynamicMetaObject[] args, BindingRestrictions restrictions);

        #region Factory

        protected CallMethodBinder(RuntimeTypeHandle classContext, RuntimeTypeHandle returnType, int genericParams)
        {
            _returnType = Type.GetTypeFromHandle(returnType);
            _genericParamsCount = genericParams;
            _classContext = Type.GetTypeFromHandle(classContext);
        }

        public static CallMethodBinder Create(string name, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType, int genericParams)
        {
            if (name != null)
            {
                // direct method call
                return new DirectCallMethodBinder(name, classContext, returnType, genericParams);
            }

            throw new NotImplementedException();
        }

        #endregion

        #region DynamicMetaObjectBinder

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    sealed class DirectCallMethodBinder : CallMethodBinder
    {
        readonly string _name;

        public DirectCallMethodBinder(string name, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType, int genericParams)
            :base(classContext, returnType, genericParams)
        {
            Debug.Assert(name != null);
            _name = name;
        }

        protected override string ResolveName(DynamicMetaObject[] args, BindingRestrictions restrictions)
        {
            return _name;
        }
    }
}
