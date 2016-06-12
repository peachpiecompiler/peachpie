using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.CodeGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    partial class SynthesizedStaticFieldsHolder
    {
        internal void EmitCtors(Emit.PEModuleBuilder module)
        {
            EnsureMembers();

            // .ctor()

            var tt = DeclaringCompilation.CoreTypes;
            var diagnostic = DiagnosticBag.GetInstance();

            var ctor = new SynthesizedCtorSymbol(this);
            ctor.SetParameters();// empty params (default ctor)

            var body = MethodGenerator.GenerateMethodBody(module, ctor, (il) =>
            {
                var cg = new CodeGenerator(il, module, diagnostic, OptimizationLevel.Release, false, this, null, new ArgPlace(this, 0));

                GetMembers().OfType<SourceFieldSymbol>().Where(f => !f.InitializerRequiresContext).ForEach(f => f.EmitInit(cg));

                //
                il.EmitRet(true);
            },
            null, diagnostic, false);
            module.SetMethodBody(ctor, body);

            //
            _lazyMembers = _lazyMembers.Add(ctor);
        }

        internal void EmitInit(Emit.PEModuleBuilder module)
        {
            EnsureMembers();

            // void Init(Context)

            var tt = DeclaringCompilation.CoreTypes;
            var diagnostic = DiagnosticBag.GetInstance();

            // override IStaticInit.Init(Context) { .. }

            var initMethod = new SynthesizedMethodSymbol(this, "Init", false, true, tt.Void, Accessibility.Public);
            initMethod.SetParameters(new SynthesizedParameterSymbol(initMethod, tt.Context, 0, RefKind.None, "ctx"));

            var body = MethodGenerator.GenerateMethodBody(module, initMethod, (il) =>
            {
                var cg = new CodeGenerator(il, module, diagnostic, OptimizationLevel.Release, false, this, new ArgPlace(tt.Context, 1), new ArgPlace(this, 0));

                GetMembers().OfType<SourceFieldSymbol>().Where(f => f.InitializerRequiresContext).ForEach(f => f.EmitInit(cg));

                //
                il.EmitRet(true);
            },
            null, diagnostic, false);
            module.SetMethodBody(initMethod, body);

            //
            _lazyMembers = _lazyMembers.Add(initMethod);
        }
    }
}
