using Pchp.Core;
using Pchp.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Library.Database
{
    [PhpHidden]
    public abstract class ResultResource : PhpResource
    {
        /// <summary>
		/// Represents a single result set returned by query.
		/// </summary>
		protected sealed class ResultSet
        {
            /// <summary>
            /// Rows.
            /// </summary>
            public List<object[]> Rows;

            /// <summary>
            /// Names of columns in query.
            /// </summary>
            public string[] Names;

            /// <summary>
            /// Names of SQL types of columns in query.
            /// </summary>
            public string[] DataTypes;

            /// <summary>
            /// Number of records affected by the query.
            /// </summary>
            public int RecordsAffected = -1;

            /// <summary>
            /// Custom data obtained from the row by <see cref="GetCustomData"/> callback function of specific PhpDbResult implementation.
            /// </summary>
            public object CustomData;
        }

        /// <summary>
        /// Source data reader.
        /// </summary>
        internal protected IDataReader Reader { get; internal set; }

        /// <summary>
        /// Gets underlaying connection.
        /// </summary>
        protected ConnectionResource Connection { get; private set; }
        private List<ResultSet> resultSets;

        #region Fields and Properties

        /// <summary>
        /// Command whose result is represented by this instance.
        /// </summary>
        public IDbCommand Command { get { return command; } }
        internal IDbCommand command; // GENERICS: internal set 

        /// <summary>
        /// Gets the index of the current result set. Initialized to 0.
        /// </summary>
        public int CurrentSetIndex { get { return currentSetIndex; } }
        private int currentSetIndex;

        /// <summary>
        /// Gets the index of the current row or -1 if no row has been fetched yet.
        /// </summary>
        public int CurrentRowIndex { get { return currentRowIndex; } }
        private int currentRowIndex;

        /// <summary>
        /// Gets the index of the current field. Initialized to 0.
        /// </summary>
        public int CurrentFieldIndex { get { return currentFieldIndex; } }
        private int currentFieldIndex;

        /// <summary>
        /// Gets the index of the last fetched field. Initialized to -1.
        /// </summary>
        public int LastFetchedField { get { return lastFetchedField; } }
        private int lastFetchedField = -1;

        /// <summary>
        /// Gets the number of rows of the result.
        /// </summary>
        public int RowCount { get { Debug.Assert(CurrentSet.Rows != null); return CurrentSet.Rows.Count; } }

        /// <summary>
        /// Gets the number of fields of the result. Returns 0 if data are not loaded.
        /// </summary>
        public int FieldCount { get { Debug.Assert(CurrentSet.Names != null); return CurrentSet.Names.Length; } }

        /// <summary>
        /// Gets the number of records affected by the query that generates this result.
        /// Contains minus one for select queries.
        /// </summary>
        public int RecordsAffected => CurrentSet.RecordsAffected;

        #endregion

        #region Result Sets

        /// <summary>
        /// Gets the current result set.
        /// </summary>
        protected ResultSet/*!*/ CurrentSet => resultSets[currentSetIndex];

        /// <summary>
        /// Gets the number of results sets.
        /// </summary>
        public int ResultSetCount => resultSets.Count;

        /// <summary>
        /// Advances the current result set index.
        /// </summary>
        /// <returns>Whether the index has been advanced.</returns>
        public bool NextResultSet()
        {
            if (currentSetIndex < resultSets.Count - 1)
            {
                currentSetIndex++;
                currentRowIndex = -1;
                currentFieldIndex = 0;
                return true;
            }

            return false;
        }

        #endregion

        #region Constructors, Population, Release

        /// <summary>
        /// Creates an instance of a result resource.
        /// </summary>
        /// <param name="connection">Database connection.</param>
        /// <param name="reader">Data reader from which to load results.</param>
        /// <param name="name">Resource name.</param>
        /// <param name="convertTypes">Whether to convert resulting values to PHP types.</param>
        /// <exception cref="ArgumentNullException">Argument is a <B>null</B> reference.</exception>
        protected ResultResource(ConnectionResource/*!*/ connection, IDataReader/*!*/ reader, string/*!*/ name, bool convertTypes)
            : base(name)
        {
            Reader = reader ?? throw new ArgumentNullException(nameof(reader));
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));

            LoadData(convertTypes);
        }

        /// <summary>
        /// Loads all data from the reader to arrays.
        /// </summary>
        /// <remarks>This method should be called before any other method.</remarks>
        private void LoadData(bool convertTypes)
        {
            this.resultSets = new List<ResultSet>(16);

            var reader = this.Reader;

            do
            {
                int fieldsCount;
                try
                {
                    fieldsCount = reader.FieldCount;
                }
                catch (Exception ex)
                {
                    // some DataReader implementations (i.e. SqliteDataReader)
                    // throws an exception when there are no fields in the reader:
                    Debug.WriteLine($"{reader.GetType().Name}.FieldCount: {ex.Message}");
                    fieldsCount = 0;
                }
                
                var result_set = new ResultSet()
                {
                    Rows = new List<object[]>(),
                    Names = GetNames(fieldsCount),
                    DataTypes = GetDataTypes(fieldsCount),
                    RecordsAffected = reader.RecordsAffected,
                    CustomData = GetCustomData(),
                };

                while (reader.Read())
                {
                    result_set.Rows.Add(this.GetValues(result_set.DataTypes, convertTypes));
                }

                resultSets.Add(result_set);
            }
            while (reader.NextResult());

            this.currentSetIndex = 0;
            this.currentRowIndex = -1;
            this.currentFieldIndex = 0;
        }

        /// <summary>
        /// Disposes the resource.
        /// </summary>
        protected override void FreeManaged()
        {
            base.FreeManaged();
            this.Reader?.Close();
        }

        internal void ReleaseConnection()
        {
            this.Connection = null;
        }

        #endregion

        #region Virtual Methods

        /// <summary>
        /// Retrieves column names from the reader.
        /// </summary>
        /// <returns>An array of column names.</returns>
        protected virtual string[]/*!*/ GetNames(int fieldsCount)
        {
            if (fieldsCount == 0)
            {
                return Array.Empty<string>();
            }
            else if (fieldsCount < 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            
            var names = new string[fieldsCount];
            for (int i = 0; i < fieldsCount; i++)
            {
                names[i] = Reader.GetName(i);
            }

            return names;
        }

        /// <summary>
        /// Retrieves column type names from the reader.
        /// </summary>
        /// <returns>An array of column type names.</returns>
        protected virtual string[]/*!*/ GetDataTypes(int fieldsCount)
        {
            if (fieldsCount == 0)
            {
                return Array.Empty<string>();
            }
            else if (fieldsCount < 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            
            var names = new string[fieldsCount];
            for (int i = 0; i < fieldsCount; i++)
            {
                names[i] = Reader.GetDataTypeName(i);
            }

            return names;
        }

        /// <summary>
        /// Get custom data of current row of <see cref="Reader"/>. Used when loading data from database.
        /// </summary>
        /// <returns>Custom object associated with current row.</returns>
        protected virtual object GetCustomData()
        {
            return null;
        }

        /// <summary>
        /// Gets values of the current row from the reader.
        /// </summary>
        /// <param name="dataTypes">Column type names.</param>
        /// <param name="convertTypes">Whether to convert types of values to PHP types.</param>
        /// <returns>An array of values of cells in the current row.</returns>
        protected abstract object[]/*!*/ GetValues(string[] dataTypes, bool convertTypes);

        /// <summary>
        /// Maps SQL type name to PHP type name.
        /// </summary>
        /// <param name="typeName">SQL type name.</param>
        /// <returns>PHP type name.</returns>
        protected abstract string/*!*/ MapFieldTypeName(string typeName);

        #endregion

        #region SeekRow, SeekField, FetchNextField, FetchArray, FetchStdClass, FetchFields

        /// <summary>
        /// Moves the internal cursor to the specified row. 
        /// </summary>
        /// <returns>Whether the cursor moved and there are data available.</returns>
        public bool SeekRow(int rowIndex)
        {
            if (!CheckRowIndex(rowIndex)) return false;
            currentRowIndex = rowIndex - 1;
            currentFieldIndex = 0;
            return true;
        }

        /// <summary>
        /// Seeks to a specified field.
        /// </summary>
        /// <param name="fieldIndex">An index of the field.</param>
        /// <returns>Whether the index is in the range.</returns>
        public bool SeekField(int fieldIndex)
        {
            CheckFieldIndex(fieldIndex);
            currentFieldIndex = fieldIndex;
            return true;
        }

        /// <summary>
        /// Advances <see cref="LastFetchedField"/> counter and gets its value.
        /// </summary>
        /// <returns>Index of field to be fetched.</returns>
        public int FetchNextField()
        {
            if (lastFetchedField < FieldCount - 1) lastFetchedField++;
            return lastFetchedField;
        }

        /// <summary>
        /// Moves cursor in internal cache one ahead. Reads data from IDataReader if necessary.
        /// </summary>
        /// <returns>Whether the cursor moved and there are data available.</returns>
        private bool ReadRow()
        {
            if (currentRowIndex < RowCount - 1)
            {
                currentRowIndex++;
                currentFieldIndex = 0;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns a PhpArray containing data from collumns in the row and move to the next row.
        /// Returns false if there are no more rows.
        /// </summary>
        /// <param name="intKeys">Whether to add integer keys.</param>
        /// <param name="stringKeys">Whether to add string keys.</param>
        /// <returns>A PHP array containing the data.</returns>
        public PhpArray FetchArray(bool intKeys, bool stringKeys)
        {
            if (TryReadRow(out object[] oa, out string[] names))
            {
                var array = new PhpArray(names.Length);
                for (int i = 0; i < names.Length; i++)
                {
                    var quoted = PhpValue.FromClr(oa[i]); //  Core.Utilities.StringUtils.AddDbSlashes(oa[i].ToString());

                    if (intKeys) array[i] = quoted;
                    if (stringKeys) array[names[i]] = quoted;
                }

                return array;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// An <see cref="object"/> with properties that correspond to the fetched row, 
        /// or false if there are no more rows. 
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Works like FetchArray but instead of storing data to associative array,
        /// FetchObject use object fields. Note, that field names are case sensitive.
        /// </remarks>
        public stdClass FetchStdClass()
        {
            return FetchAssocArray()?.AsStdClass();
        }

        /// <summary>
        /// Gets fields as PHP array with associative string keys.
        /// </summary>
        public PhpArray FetchAssocArray()
        {
            if (TryReadRow(out object[] oa, out string[] names))
            {
                var array = new PhpArray(names.Length);
                for (int i = 0; i < names.Length; i++)
                {
                    array[names[i]] = PhpValue.FromClr(oa[i]);
                }

                return array;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Reads next row in the result.
        /// </summary>
        /// <param name="oa">Row CLR values.</param>
        /// <param name="names">Column names.</param>
        /// <returns>Whether the row was fetched.</returns>
        public bool TryReadRow(out object[] oa, out string[] names)
        {
            // no more data
            if (!this.ReadRow())
            {
                oa = null;
                names = null;
                return false;
            }

            Debug.Assert(currentRowIndex >= 0 && currentRowIndex < RowCount);

            var set = CurrentSet;

            oa = set.Rows[currentRowIndex];
            names = set.Names;

            return true;
        }

        #endregion

        #region GetSchemaTable, GetSchemaRowInfo, GetFieldName, GetFieldType, GetFieldLength, GetFieldValue

        private List<DataTable> _schemaTables = null;

        /// <summary>
        /// Gets information about schema of the current result set.
        /// </summary>
        /// <returns>Schema table.</returns>
        public DataTable GetSchemaTable()
        {
            // loads schema if not loaded yet:
            if (_schemaTables == null)
            {
                Connection.ReexecuteSchemaQuery(this);
                if (Reader.IsClosed)
                {
                    PhpException.Throw(PhpError.Warning, Resources.LibResources.cannot_retrieve_schema);
                    return null;
                }

                _schemaTables = new List<DataTable>();
                do
                {
                    _schemaTables.Add(Reader.GetSchemaTable());
                }
                while (Reader.NextResult());
            }

            return _schemaTables[currentSetIndex];
        }

        ///// <summary>
        ///// Gets schema information for a specified field.
        ///// </summary>
        ///// <param name="fieldIndex">Field index.</param>
        ///// <returns>Data row containing column schema.</returns>
        //public DataRow GetSchemaRowInfo(int fieldIndex)
        //{
        //    if (!CheckFieldIndex(fieldIndex)) return null;
        //    return GetSchemaTable().Rows[fieldIndex];
        //}

        /// <summary>
        /// Gets a name of the current field.
        /// </summary>
        /// <returns>The field name.</returns>
        public string GetFieldName() => GetFieldName(currentFieldIndex);

        /// <summary>
        /// Gets a name of a specified field.
        /// </summary>
        /// <param name="fieldIndex">An index of the field.</param>
        /// <returns>The field name or a <B>null</B> reference if index is out of range.</returns>
        public string GetFieldName(int fieldIndex)
        {
            return CheckFieldIndex(fieldIndex) ? CurrentSet.Names[fieldIndex] : null;
        }

        /// <summary>
        /// Gets a type of the current field.
        /// </summary>
        /// <returns>The type name.</returns>
        public string GetFieldType() => GetFieldType(currentFieldIndex);

        /// <summary>
        /// Gets a PHP name of the current field type.
        /// </summary>
        /// <returns>PHP type name.</returns>
        public string GetPhpFieldType()
        {
            return MapFieldTypeName(GetFieldType());
        }

        /// <summary>
        /// Gets a PHP name of a specified field type.
        /// </summary>
        /// <param name="fieldIndex">Field index.</param>
        /// <returns>PHP type name.</returns>
        public string GetPhpFieldType(int fieldIndex)
        {
            return MapFieldTypeName(GetFieldType(fieldIndex));
        }

        /// <summary>
        /// Gets a type of specified field.
        /// </summary>
        /// <param name="fieldIndex">An index of the field.</param>
        /// <returns>The type name.</returns>
        public virtual string GetFieldType(int fieldIndex)
        {
            return CheckFieldIndex(fieldIndex)
                ? CurrentSet.DataTypes[fieldIndex]
                : null;
        }

        /// <summary>
        /// Gets length of the current field.
        /// </summary>
        /// <returns>The field length.</returns>
        public virtual int GetFieldLength()
        {
            return GetFieldLength(currentFieldIndex);
        }

        /// <summary>
        /// Gets length of a specified field.
        /// </summary>
        /// <param name="fieldIndex">An index of the field.</param>
        /// <returns>The field length or 0.</returns>
        public virtual int GetFieldLength(int fieldIndex)
        {
            //var info = GetSchemaRowInfo(fieldIndex);
            //return (info != null) ? (int)info["ColumnSize"] : 0;
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a value of a specified field of the result.
        /// </summary>
        /// <param name="rowIndex">Row index.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>The value or a <B>null</B> reference if row or index are out of range.</returns>
        public object GetFieldValue(int rowIndex, string fieldName)
        {
            if (!CheckRowIndex(rowIndex)) return false;

            var names = CurrentSet.Names;

            for (int i = 0; i < names.Length; i++)
            {
                if (string.Equals(names[i], fieldName, StringComparison.OrdinalIgnoreCase))
                //if (string.Compare(CurrentSet.Names[i], fieldName, true) == 0)
                {
                    return CurrentSet.Rows[rowIndex][i];
                }
            }

            PhpException.Throw(PhpError.Notice, Resources.LibResources.field_not_exists, fieldName);
            return null;
        }

        /// <summary>
        /// Gets a value of a specified field of the result.
        /// </summary>
        /// <param name="rowIndex">Row index.</param>
        /// <param name="fieldIndex">Index of the field.</param>
        /// <returns>The value or a <B>null</B> reference if row or index are out of range.</returns>
        public object GetFieldValue(int rowIndex, int fieldIndex)
        {
            if (CheckRowIndex(rowIndex) && CheckFieldIndex(fieldIndex))
            {
                return CurrentSet.Rows[rowIndex][fieldIndex];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Get custom data associated with current set.
        /// </summary>
        /// <returns></returns>
        public object GetRowCustomData()
        {
            return CurrentSet.CustomData;
        }

        #endregion

        #region Checks

        /// <summary>
        /// Checks whether a field index is valid for the current result set.
        /// </summary>
        /// <param name="fieldIndex">Field index to check.</param>
        /// <returns>Whether the index is in the range [0, <see cref="FieldCount"/>).</returns>
        /// <exception cref="PhpException">Invalid field index (Warning).</exception>
        public bool CheckFieldIndex(int fieldIndex)
        {
            if (fieldIndex < 0 || fieldIndex >= FieldCount)
            {
                PhpException.Throw(PhpError.Warning, Resources.LibResources.invalid_data_result_field_index, fieldIndex.ToString(), this.TypeName, this.Id.ToString());
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks whether a row index is valid for the current result set.
        /// </summary>
        /// <param name="rowIndex">Row index to check.</param>
        /// <returns>Whether the index is in the range [0, <see cref="RowCount"/>).</returns>
        /// <exception cref="PhpException">Invalid row index (Warning).</exception>
        public bool CheckRowIndex(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= RowCount)
            {
                PhpException.Throw(PhpError.Warning, Resources.LibResources.invalid_data_result_row_index, rowIndex.ToString(), this.TypeName, this.Id.ToString());
                return false;
            }

            return true;
        }

        #endregion
    }
}
