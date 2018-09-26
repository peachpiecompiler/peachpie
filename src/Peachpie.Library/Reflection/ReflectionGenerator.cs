using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Pchp.Core;
using Pchp.Core.Reflection;
using Pchp.Library.Spl;

namespace Pchp.Library.Reflection
{
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(ReflectionUtils.ExtensionName)]
    public class ReflectionGenerator
    {
        Generator _g;
        Context _ctx => Operators.GetGeneratorContext(_g);

        [PhpFieldsOnlyCtor]
        protected ReflectionGenerator() { }

        public ReflectionGenerator(Generator g)
        {
            __construct(g);
        }

        public virtual void __construct(Generator generator)
        {
            _g = generator ?? throw new ArgumentNullException();
        }

        public virtual string getExecutingFile()
        {
            var m = Operators.GetGeneratorMethod(_g).GetMethodInfo();
            var t = m.DeclaringType.GetTypeInfo();

            string path = null;

            // [PhpType(FileName = ...)]
            var phpt = t.GetCustomAttribute<PhpTypeAttribute>();
            if (phpt != null)
            {
                path = phpt.FileName;
            }
            else
            {
                // [Script(Path = ...)]
                var scrt = Core.Reflection.ReflectionUtils.GetScriptAttribute(t);
                if (scrt != null)
                {
                    path = scrt.Path;
                }
            }

            //
            return (path != null) ? Path.Combine(_ctx.RootPath, path) : string.Empty;
        }
        public virtual Generator getExecutingGenerator() => _g;
        public virtual int getExecutingLine() { throw new NotImplementedException(); }
        public virtual ReflectionFunctionAbstract getFunction()
        {
            var owner = Operators.GetGeneratorOwnerMethod(_g);
            if (owner != null)
            {

                var container = owner.DeclaringType.GetTypeInfo();
                var phpt = container.GetCustomAttribute<PhpTypeAttribute>();
                if (phpt != null) // declared inside class
                {
                    // method
                    var tinfo = container.GetPhpTypeInfo();
                    var routine = tinfo.RuntimeMethods[owner.Name];

                    return new ReflectionMethod(tinfo, routine);
                }
                else if (owner.IsStatic) // global function (?)
                {
                    // [Script(Path = ...)] => global function declared inside script
                    var scrt = Core.Reflection.ReflectionUtils.GetScriptAttribute(container);
                    if (scrt != null)
                    {
                        // function
                        return new ReflectionFunction(_ctx.GetDeclaredFunction(owner.Name));
                    }
                }

            }

            //
            throw new NotSupportedException();
        }
        public virtual object getThis() => Operators.GetGeneratorThis(_g);
        public virtual PhpArray getTrace(int options = 1/*DEBUG_BACKTRACE_PROVIDE_OBJECT*/) { throw new NotImplementedException(); }
    }
}
