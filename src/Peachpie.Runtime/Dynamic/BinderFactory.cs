using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Pchp.CodeAnalysis.Semantics;

namespace Pchp.Core.Dynamic
{
    /// <summary>
    /// Constructs callsite binders.
    /// </summary>
    public static class BinderFactory
    {
        public static CallSiteBinder Function(string name, string nameOpt, RuntimeTypeHandle returnType, int genericParams)
        {
            return new CallFunctionBinder(name, nameOpt, returnType, genericParams);
        }

        public static CallSiteBinder InstanceFunction(string name, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType, int genericParams)
        {
            return new CallInstanceMethodBinder(name, classContext, returnType, genericParams);
        }

        public static CallSiteBinder StaticFunction(RuntimeTypeHandle type, string name, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType, int genericParams)
        {
            return new CallStaticMethodBinder(type, name, classContext, returnType, genericParams);
        }

        public static CallSiteBinder GetField(string name, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType, AccessMask access)
        {
            return new GetFieldBinder(name, classContext, returnType, access);
        }

        public static CallSiteBinder GetClassConst(string name, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType, AccessMask access)
        {
            return new GetClassConstBinder(name, classContext, returnType, access);
        }

        public static CallSiteBinder SetField(string name, RuntimeTypeHandle classContext, AccessMask access)
        {
            return new SetFieldBinder(name, classContext, access);
        }
    }

}
