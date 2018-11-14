using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.CodeGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection.Metadata;

namespace Pchp.CodeAnalysis.Symbols
{
    partial class SynthesizedStaticFieldsHolder
    {
        internal void EmitCtors(Emit.PEModuleBuilder module)
        {
            bool requiresInit = false;

            // .ctor()

            var tt = DeclaringCompilation.CoreTypes;
            var diagnostic = DiagnosticBag.GetInstance();

            var ctor = new SynthesizedCtorSymbol(this);

            var body = MethodGenerator.GenerateMethodBody(module, ctor, (il) =>
            {
                var cg = new CodeGenerator(il, module, diagnostic, module.Compilation.Options.OptimizationLevel, false, this, null, new ArgPlace(this, 0))
                {
                    CallerType = this.ContainingType,
                    ContainingFile = _class.ContainingFile,
                };

                // base..ctor()
                cg.EmitThis();   // this
                il.EmitCall(module, diagnostic, ILOpCode.Call, this.BaseType.InstanceConstructors.Single());   // .ctor()

                //
                foreach (var p in this.Fields.Cast<IPhpPropertySymbol>())
                {
                    if (p.RequiresContext)
                    {
                        requiresInit = true;
                    }
                    else
                    {
                        p.EmitInit(cg);
                    }
                }

                //
                il.EmitRet(true);
            },
            null, diagnostic, false);
            module.SetMethodBody(ctor, body);
            module.SynthesizedManager.AddMethod(this, ctor);

            //
            if (requiresInit)
            {
                EmitInit(module);
            }
        }

        void EmitInit(Emit.PEModuleBuilder module)
        {
            // void Init(Context)

            var tt = DeclaringCompilation.CoreTypes;
            var diagnostic = DiagnosticBag.GetInstance();

            // override IStaticInit.Init(Context) { .. }

            var initMethod = new SynthesizedMethodSymbol(this, "Init", false, true, tt.Void, Accessibility.Public);
            initMethod.SetParameters(new SynthesizedParameterSymbol(initMethod, tt.Context, 0, RefKind.None, SpecialParameterSymbol.ContextName));

            var body = MethodGenerator.GenerateMethodBody(module, initMethod, (il) =>
            {
                var cg = new CodeGenerator(il, module, diagnostic, module.Compilation.Options.OptimizationLevel, false, this, new ArgPlace(tt.Context, 1), new ArgPlace(this, 0), initMethod)
                {
                    CallerType = this.ContainingType,
                    ContainingFile = _class.ContainingFile,
                };

                foreach (var p in this.Fields.Cast<IPhpPropertySymbol>())
                {
                    if (p.RequiresContext)
                    {
                        p.EmitInit(cg);
                    }
                }

                //
                il.EmitRet(true);
            },
            null, diagnostic, false);
            module.SetMethodBody(initMethod, body);
            module.SynthesizedManager.AddMethod(this, initMethod);
        }
    }
}
