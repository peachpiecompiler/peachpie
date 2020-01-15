using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peachpie.Library.PDO
{
    partial class PDO
    {
        /// <summary>
        /// 
        /// </summary>
        [PhpHidden]
        public enum PARAM
        {
            /// <summary>
            /// The value is NULL.
            /// </summary>
            PARAM_NULL = 0,

            /// <summary>
            /// int as in long (the php native int type)
            /// </summary>
            PARAM_INT = 1,

            /// <summary>
            /// get_col ptr should point to start of the string buffer
            /// </summary>
            PARAM_STR = 2,

            /// <summary>
            /// The pdo parameter lob
            /// </summary>
            PARAM_LOB = 3,

            /// <summary>
            /// The pdo parameter statement
            /// </summary>
            PARAM_STMT = 4,

            /// <summary>
            /// The pdo parameter bool
            /// </summary>
            PARAM_BOOL = 5,

            /// <summary>
            /// The pdo parameter zval
            /// </summary>
            PARAM_ZVAL = 6,

            /// <summary>
            /// The pdo parameter input output
            /// </summary>
            PARAM_INPUT_OUTPUT = int.MinValue, // 0x80000000
        }

        /// <summary>
        /// 
        /// </summary>
        public const int PARAM_NULL = (int)PARAM.PARAM_NULL;
        /// <summary>
        /// int as in long (the php native int type). If you mark a column as an int, PDO expects get_col to return a pointer to a long
        /// </summary>
        public const int PARAM_INT = (int)PARAM.PARAM_INT;
        /// <summary>
        /// get_col ptr should point to start of the string buffer
        /// </summary>
        public const int PARAM_STR = (int)PARAM.PARAM_STR;
        /// <summary>
        /// get_col: when len is 0 ptr should point to a php_stream *, otherwise it should behave like a string. Indicate a NULL field value by setting the ptr to NULL
        /// </summary>
        public const int PARAM_LOB = (int)PARAM.PARAM_LOB;
        /// <summary>
        /// get_col: will expect the ptr to point to a new PDOStatement object handle, but this isn't wired up yet
        /// </summary>
        public const int PARAM_STMT = (int)PARAM.PARAM_STMT;
        /// <summary>
        /// get_col ptr should point to a zend_bool
        /// </summary>
        public const int PARAM_BOOL = (int)PARAM.PARAM_BOOL;
        /// <summary>
        /// get_col ptr should point to a zval* and the driver is responsible for adding correct type information to get_column_meta()
        /// </summary>
        public const int PARAM_ZVAL = (int)PARAM.PARAM_ZVAL;
        /// <summary>
        /// magic flag to denote a parameter as being input/output
        /// </summary>
        public const int PARAM_INPUT_OUTPUT = (int)PARAM.PARAM_INPUT_OUTPUT;
    }
}
