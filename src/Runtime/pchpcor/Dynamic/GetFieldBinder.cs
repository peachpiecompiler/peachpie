using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Dynamic
{
    public class GetFieldBinder : DynamicMetaObjectBinder
    {
        readonly string _name;
        readonly Type _classContext;
        readonly Type _returnType;
        readonly int _flags;

        protected GetFieldBinder(string name, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType, int flags)
        {
            _name = name;
            _returnType = Type.GetTypeFromHandle(returnType);
            _classContext = Type.GetTypeFromHandle(classContext);
            _flags = flags;
        }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            throw new NotImplementedException();
        }
    }
}
