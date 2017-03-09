using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace Peachpie.Library.PDO
{
    internal static class Extensions
    {
        //public static NameValueCollection ToNameValueCollection(this PhpArray arr, Context ctx)
        //{
        //    if (arr == null)
        //        return null;

        //    NameValueCollection result = new NameValueCollection();
        //    foreach (var key in arr.Keys)
        //    {
        //        var nKey = key.ToString();
        //        var nValue = arr[key].ToString(ctx);
        //        result.Set(nKey, nValue);
        //    }
        //    return result;
        //}

        public static void Set<TKey, TValue>(this IDictionary<TKey,TValue> dic, TKey key, TValue value)
        {
            if(dic.ContainsKey(key))
            {
                dic[key] = value;
            }
            else
            {
                dic.Add(key, value);
            }
        }
    }
}
