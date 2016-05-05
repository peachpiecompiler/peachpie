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
        /// Signature of the scripts main method.
        /// </summary>
        /// <param name="ctx">Reference to current context. Cannot be <c>null</c>.</param>
        /// <param name="locals">Reference to variables scope. Cannot be <c>null</c>. Can refer to either globals or new array locals.</param>
        /// <returns>Result of the main method call.</returns>
        public delegate PhpValue MainDelegate(Context ctx, PhpArray locals);

        /// <summary>
        /// Script descriptor.
        /// </summary>
        struct ScriptInfo
        {
            readonly RuntimeMethodHandle MainMethodHandle;
            readonly int Index;
            readonly string FullPath;
            readonly MainDelegate MainMethod;
        }

        /// <summary>
        /// Manages map of known scripts and bit array of already included.
        /// </summary>
        class ScriptsMap
        {
            readonly ElasticBitArray array = new ElasticBitArray(_count);
            static int _count;  // ~_scriptsMap.Count

            /// <summary>
            /// Map of full script path to its script_id.
            /// </summary>
            static Dictionary<string, int> _scriptsMap;

            /// <summary>
            /// Scripts descriptors corresponding to script_id.
            /// </summary>
            static ScriptInfo[] _scripts;

            //public bool IsIncluded<TScript>()
            //{

            //}

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
