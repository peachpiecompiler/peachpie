#nullable enable

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Pchp.Core;
using Pchp.Core.Dynamic;
using Pchp.Core.Reflection;

namespace Peachpie.Runtime.Dynamic
{
    internal static class EmitHelpers
    {
        private static readonly Lazy<ModuleBuilder> s_lazyDynamicModuleBuilder = new Lazy<ModuleBuilder>(() =>
        {
            var aname = "dynamic`1";
            var ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(aname), AssemblyBuilderAccess.Run);
            var mb = ab.DefineDynamicModule(aname);

            return mb;
        });

        static ConstructorInfo? ResolveFieldInitCtor(Type type)
        {
            ConstructorInfo? basector = null;

            foreach (var c in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (c.IsPhpFieldsOnlyCtor())
                {
                    return c; // best
                }

                var ps = c.GetParameters();
                switch (ps.Length)
                {
                    case 0:
                    case 1 when ps[0].IsContextParameter():
                        basector = c;
                        break;
                }
            }

            return basector;
        }

        /// <summary>
        /// Defines non-abstract class implementing given abstract class.
        /// </summary>
        /// <param name="type">Abstract class prototype.</param>
        /// <param name="ctor_context">Resulting constructor that instantiates the defined type.</param>
        /// <returns></returns>
        public static Type CreatDefaultAbstractClassImplementation(Type type, out ConstructorInfo ctor_context)
        {
            if (type.IsInterface) throw new ArgumentException();
            if (type.IsAbstract == false) throw new ArgumentException();

            // 
            var tb = s_lazyDynamicModuleBuilder.Value.DefineType(type.FullName + "#" + type.GetHashCode(), TypeAttributes.Public, parent: type);

            // implement abstract methods:
            foreach (var m in type.GetRuntimeMethods().Where(m => m.IsAbstract && !m.IsPrivate))
            {
                var method = tb.DefineMethod(
                    m.Name,
                    (m.Attributes & MethodAttributes.MemberAccessMask) | MethodAttributes.Virtual | MethodAttributes.Final,
                    CallingConventions.Standard,
                    m.ReturnType,
                    m.GetParametersType());

                var il = method.GetILGenerator();

                il.ThrowException(typeof(NotImplementedException));

                tb.DefineMethodOverride(method, m);
            }

            // find default [FieldsOnly] .ctor
            var basector = ResolveFieldInitCtor(type);
            if (basector != null)
            {
                var cb = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(Context) });
                // cb.DefineParameter(1, ParameterAttributes.None, "ctx");
                cb.InitLocals = true;
                var il = cb.GetILGenerator();

                // void .ctor(Context ctx) {

                // base..ctor(...)
                il.Emit(OpCodes.Ldarg_0); // this
                var ps = basector.GetParameters();
                foreach (var p in ps)
                {
                    if (p.IsContextParameter())
                    {
                        il.Emit(OpCodes.Ldarg_1); // LOAD ctx
                    }
                    else if (p.ParameterType.IsValueType)
                    {
                        var tmploc = il.DeclareLocal(p.ParameterType);
                        il.Emit(OpCodes.Ldloc, tmploc); // default
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldnull); // null
                    }
                }

                il.Emit(OpCodes.Call, basector); // .ctor // non-virtual // void

                il.Emit(OpCodes.Ret);

                // }
            }
            else
            {
                throw new NotSupportedException("Cannot determine base .ctor");
            }

            //
            var newtype = tb.CreateTypeInfo() ?? throw new InvalidOperationException();
            ctor_context = newtype.DeclaredConstructors.Single();
            return newtype;
        }
    }
}