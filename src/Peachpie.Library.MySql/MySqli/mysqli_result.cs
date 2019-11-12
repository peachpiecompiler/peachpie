using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Pchp.Core;
using Pchp.Core.Reflection;
using Pchp.Core.Utilities;

namespace Peachpie.Library.MySql.MySqli
{
    /// <summary>
    /// Represents the result set obtained from a query against the database.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    [PhpExtension(Constants.ExtensionName)]
    [DebuggerDisplay("mysqli_result")]
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

        stdClass fetch_field_internal(int field)
        {
            if (!_result.CheckFieldIndex(field))
                return null;

            //DataRow info = result.GetSchemaRowInfo(fieldIndex);
            //if (info == null) return null;

            var col = _result.GetColumnSchema(field);

            //PhpMyDbResult.FieldCustomData data = ((PhpMyDbResult.FieldCustomData[])result.GetRowCustomData())[fieldIndex];
            //ColumnFlags flags = data.Flags;//result.GetFieldFlags(fieldIndex);

            //name The name of the column
            //orgname Original column name if an alias was specified
            //table   The name of the table this field belongs to(if not calculated)
            //orgtable Original table name if an alias was specified
            //def Reserved for default value, currently always ""
            //db  Database(since PHP 5.3.6)
            //catalog The catalog name, always "def"(since PHP 5.3.6)
            //max_length  The maximum width of the field for the result set.
            //length  The width of the field, as specified in the table definition.
            //charsetnr   The character set number for the field.
            //flags   An integer representing the bit - flags for the field.
            //type    The data type used for this field
            //decimals    The number of decimals used(for integer fields)

            // create an array of runtime fields with specified capacity:
            var objFields = new PhpArray(16)
            {
                { "name", col.ColumnName },
                { "orgname", col.BaseColumnName },
                { "table", col.BaseTableName ?? string.Empty },
                //{ "orgtable", col.BaseTableName ?? string.Empty },
                { "def", "" }, // undocumented
                { "db", col.BaseSchemaName },
                { "catalog", col.BaseCatalogName },
                //{ "max_length", /*result.GetFieldLength(fieldIndex)*/data.ColumnSize },
                { "length", col.ColumnSize.GetValueOrDefault() },
                //{ "charsetnr", ??? },
                //{ "flags", (int)flags },
                { "type", (int)col.ProviderType },
                //{ "decimals", ??? },
            };

            // create new stdClass with runtime fields initialized above:
            return objFields.AsStdClass();
        }

        /// <summary>
        /// Fetch meta-data for a single field.
        /// </summary>
        [return: CastToFalse]
        public stdClass fetch_field_direct(int fieldnr) => fetch_field_internal(fieldnr);

        /// <summary>
        /// Returns the next field in the result set.
        /// </summary>
        [return: CastToFalse]
        public stdClass fetch_field() => fetch_field_internal(_result.FetchNextField());

        /// <summary>
        /// Returns an array of objects representing the fields in a result set.
        /// </summary>
        public PhpArray fetch_fields()
        {
            var n = _result.FieldCount;
            var arr = new PhpArray(n);

            for (int i = 0; i < n; i++)
            {
                arr.Add(PhpValue.FromClass(fetch_field_internal(i)));
            }

            return arr;
        }

        /// <summary>
        /// Returns the current row of a result set as an object.
        /// </summary>
        public object fetch_object(string class_name = null, PhpArray class_params = null)
        {
            if (string.IsNullOrEmpty(class_name) || nameof(stdClass).Equals(class_name, StringComparison.OrdinalIgnoreCase))
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
                    // TODO: Operators.PropertySetValue( obj, names[i], FromClr(oa[i]) );

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
        /// Alias to <see cref="close"/>.
        /// </summary>
        public void free() => close();

        /// <summary>
        /// Alias to <see cref="close"/>.
        /// </summary>
        public void free_result() => close();

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
