using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;

namespace Pchp.Core.Utilities
{
    public class ListPool<T>
    {
        sealed class ListPoolPolicy : PooledObjectPolicy<List<T>>
        {
            public override List<T> Create() => new List<T>();

            public override bool Return(List<T> obj)
            {
                if (obj.Count > 1024*1024)
                {
                    return false;
                }

                obj.Clear();
                return true;
            }
        }

        public static readonly ObjectPool<List<T>> Pool = new DefaultObjectPool<List<T>>(new ListPoolPolicy());
    }
}
