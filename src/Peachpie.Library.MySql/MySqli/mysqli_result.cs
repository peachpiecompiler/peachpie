using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Pchp.Core;
using Pchp.Core.Reflection;

namespace Peachpie.Library.MySql.MySqli
{
    /// <summary>
    /// Represents the result set obtained from a query against the database.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    [PhpExtension(Constants.ExtensionName)]
    public sealed class mysqli_result : Iterator
    {
        readonly MySqlResultResource _result;

        internal mysqli_result(MySqlResultResource result)
        {
            Debug.Assert(result != null);
            _result = result;
        }

        /* Properties */

        /// <summary>
        /// Get current field offset of a result pointer.
        /// </summary>
        public int current_field => _result.CurrentFieldIndex;

        /// <summary>
        /// Get the number of fields in a result.
        /// </summary>
        public int field_count => _result.FieldCount;

        //array $lengths;

        /// <summary>
        /// Gets the number of rows in a result.
        /// </summary>
        public int num_rows => _result.RowCount;

        /* Methods */

        /// <summary>
        /// Adjusts the result pointer to an arbitrary row in the result.
        /// </summary>
        public bool data_seek(int offset) => _result.SeekRow(offset);

        //mixed fetch_all([ int $resulttype = MYSQLI_NUM ] )

        /// <summary>
        /// Fetch a result row as an associative, a numeric array, or both.
        /// </summary>
        public PhpArray fetch_array(int resulttype = Constants.MYSQLI_BOTH)
        {
            switch (resulttype)
            {
                case Constants.MYSQLI_ASSOC: return _result.FetchAssocArray();
                case Constants.MYSQLI_NUM: return _result.FetchArray(true, false);
                case Constants.MYSQLI_BOTH: return _result.FetchArray(true, true);
            }

            return null;
        }

        /// <summary>
        /// Fetch a result row as an associative array.
        /// </summary>
        public PhpArray fetch_assoc() => _result.FetchAssocArray();

        //object fetch_field_direct(int $fieldnr )
        //object fetch_field(void )
        //array fetch_fields(void )

        /// <summary>
        /// Returns the current row of a result set as an object.
        /// </summary>
        public object fetch_object(string class_name = null, PhpArray class_params = null)
        {
            if (string.IsNullOrEmpty(class_name) || string.Equals(class_name, "stdClass", StringComparison.OrdinalIgnoreCase))
            {
                return _result.FetchStdClass();
            }

            if (_result.TryReadRow(out object[] oa, out string[] names))
            {
                // instantiate class dynamically:
                var ctx = _result.Connection.Context;
                var phpt = ctx.GetDeclaredTypeOrThrow(class_name, autoload: true);
                var obj = phpt.Creator(ctx, (class_params == null) ? Array.Empty<PhpValue>() : class_params.GetValues());

                // set object properties using reflection:
                for (int i = 0; i < names.Length; i++)
                {
                    var p =
                        TypeMembersUtils.GetDeclaredProperty(phpt, names[i]) ??
                        TypeMembersUtils.GetRuntimeProperty(phpt, names[i], obj);

                    p.SetValue(ctx, obj, PhpValue.FromClr(oa[i]));
                }

                //
                return obj;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Get a result row as an enumerated array.
        /// </summary>
        public PhpArray fetch_row() => _result.FetchArray(true, false);

        /// <summary>
        /// Set result pointer to a specified field offset.
        /// </summary>
        public bool field_seek(int fieldnr) => _result.SeekField(fieldnr);

        /// <summary>
        /// Frees the memory associated with a result.
        /// </summary>
        public void free() => close();

        /// <summary>
        /// Frees the memory associated with a result.
        /// </summary>
        public void close()
        {
            _result.Dispose();
        }

        #region Iterator

        PhpValue Iterator.current()
        {
            throw new NotImplementedException();
        }

        PhpValue Iterator.key()
        {
            throw new NotImplementedException();
        }

        void Iterator.next()
        {
            throw new NotImplementedException();
        }

        void Iterator.rewind()
        {
            throw new NotImplementedException();
        }

        bool Iterator.valid()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
