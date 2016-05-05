using Pchp.Core.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pchp.Core
{
    partial class Context
    {
        /// <summary>
        /// Manages map of known scripts and bit array of already included.
        /// </summary>
        class ScriptsMap
        {
            ElasticBitArray array = new ElasticBitArray(_count);
            static int _count;
            //static Dictionary<string, int> _scriptMap;

            public bool IsIncluded(int script_id) => array[script_id - 1];

            public void SetIncluded(int script_id) => array.SetTrue(script_id - 1);

            public static int EnsureIndex(ref int script_id)
            {
                if (script_id <= 0)
                    script_id = Interlocked.Increment(ref _count);

                return script_id;
            }
        }
    }
}
