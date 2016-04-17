using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Dynamic
{
    public class SetFieldBinder : DynamicMetaObjectBinder
    {
        readonly string _name;
        readonly Type _classContext;
        readonly int _flags;

        protected SetFieldBinder(string name, RuntimeTypeHandle classContext, int flags)
        {
            _name = name;
            _classContext = Type.GetTypeFromHandle(classContext);
            _flags = flags;
        }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            throw new NotImplementedException();
        }
    }
}
