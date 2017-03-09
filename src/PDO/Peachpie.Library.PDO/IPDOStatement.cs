using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peachpie.Library.PDO
{
    /// <summary>
    /// Interface of the PDOStatement class
    /// </summary>
    public interface IPDOStatement
    {
        /// <summary>
        /// Bind a column to a PHP variable.
        /// </summary>
        /// <param name="colum">Number of the column (1-indexed) or name of the column in the result set. If using the column name, be aware that the name should match the case of the column, as returned by the driver</param>
        /// <param name="param">Name of the PHP variable to which the column will be bound.</param>
        /// <param name="type">Data type of the parameter, specified by the PDO::PARAM_* constants.</param>
        /// <param name="maxlen">A hint for pre-allocation.</param>
        /// <param name="driverdata">Optional parameter(s) for the driver.</param>
        /// <returns>Returns TRUE on success or FALSE on failure</returns>
        bool bindColumn(PhpValue colum, ref PhpValue param, int? type = null, int? maxlen = null, PhpValue? driverdata = null);

        /// <summary>
        /// Binds a parameter to the specified variable name
        /// </summary>
        /// <param name="parameter">Parameter identifier. For a prepared statement using named placeholders, this will be a parameter name of the form :name. For a prepared statement using question mark placeholders, this will be the 1-indexed position of the parameter.</param>
        /// <param name="variable">Name of the PHP variable to bind to the SQL statement parameter.</param>
        /// <param name="data_type">Explicit data type for the parameter using the PDO::PARAM_* constants. To return an INOUT parameter from a stored procedure, use the bitwise OR operator to set the PDO::PARAM_INPUT_OUTPUT bits for the data_type parameter.</param>
        /// <param name="length">Length of the data type. To indicate that a parameter is an OUT parameter from a stored procedure, you must explicitly set the length.</param>
        /// <param name="driver_options"></param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        bool bindParam(PhpValue parameter, ref PhpValue variable, int data_type = PDO.PARAM_STR, int? length = null, PhpValue? driver_options = null);

        /// <summary>
        /// Binds a value to a parameter.
        /// </summary>
        /// <param name="parameter">Parameter identifier. For a prepared statement using named placeholders, this will be a parameter name of the form :name. For a prepared statement using question mark placeholders, this will be the 1-indexed position of the parameter.</param>
        /// <param name="value">The value to bind to the parameter.</param>
        /// <param name="data_type">Explicit data type for the parameter using the PDO::PARAM_* constants.</param>
        /// <returns>Returns TRUE on success or FALSE on failure</returns>
        bool bindValue(PhpValue parameter, PhpValue value, int data_type = PDO.PARAM_STR);

        /// <summary>
        /// Closes the cursor, enabling the statement to be executed again.
        /// </summary>
        /// <returns>Returns TRUE on success or FALSE on failure</returns>
        bool closeCursor();

        /// <summary>
        /// Returns the number of columns in the result set
        /// </summary>
        /// <returns>Returns the number of columns in the result set represented by the PDOStatement object, even if the result set is empty. If there is no result set, PDOStatement::columnCount() returns 0</returns>
        int columnCount();

        /// <summary>
        /// Dump an SQL prepared command.
        /// </summary>
        void debugDumpParams();

        /// <summary>
        /// Fetch the SQLSTATE associated with the last operation on the statement handle
        /// </summary>
        /// <returns>Identical to PDO::errorCode(), except that PDOStatement::errorCode() only retrieves error codes for operations performed with PDOStatement objects</returns>
        string errorCode();

        /// <summary>
        /// Fetch extended error information associated with the last operation on the statement handle.
        /// </summary>
        /// <returns>PDOStatement::errorInfo() returns an array of error information about the last operation performed by this statement handle.</returns>
        PhpArray errorInfo();

        /// <summary>
        /// Executes a prepared statement
        /// </summary>
        /// <param name="input_parameters">An array of values with as many elements as there are bound parameters in the SQL statement being executed. All values are treated as PDO::PARAM_STR.</param>
        /// <returns>Returns TRUE on success or FALSE on failure</returns>
        bool execute(PhpArray input_parameters = null);

        /// <summary>
        /// Fetches the specified fetch style.
        /// </summary>
        /// <param name="fetch_style">Controls how the next row will be returned to the caller. This value must be one of the PDO::FETCH_* constants.</param>
        /// <param name="cursor_orientation">This value determines which row will be returned to the caller.</param>
        /// <param name="cursor_offet">Relative or absolute position move for the cursor.</param>
        /// <returns>The return value of this function on success depends on the fetch type. In all cases, FALSE is returned on failure.</returns>
        PhpValue fetch(int? fetch_style = null, int cursor_orientation = PDO.FETCH_ORI_NEXT, int cursor_offet = 0);

        /// <summary>
        /// Controls the contents of the returned array as documented in PDOStatement::fetch()
        /// </summary>
        /// <param name="fetch_style">The fetch style.</param>
        /// <param name="fetch_argument">This argument has a different meaning depending on the value of the fetch_style parameter.</param>
        /// <param name="ctor_args">Arguments of custom class constructor when the fetch_style parameter is PDO::FETCH_CLASS.</param>
        /// <returns>An array containing all of the remaining rows in the result set</returns>
        PhpArray fetchAll(int? fetch_style = null, PhpValue? fetch_argument = null, PhpArray ctor_args = null);

        /// <summary>
        /// Returns a single column from the next row of a result set.
        /// </summary>
        /// <param name="column_number">0-indexed number of the column you wish to retrieve from the row. If no value is supplied, PDOStatement::fetchColumn() fetches the first column</param>
        /// <returns>Single column from the next row of a result set or FALSE if there are no more rows</returns>
        PhpValue fetchColumn(int column_number = 0);

        /// <summary>
        /// Fetches the next row and returns it as an object.
        /// </summary>
        /// <param name="class_name">Name of the created class.</param>
        /// <param name="ctor_args">Elements of this array are passed to the constructor.</param>
        /// <returns>Returns an instance of the required class with property names that correspond to the column names or FALSE on failure</returns>
        PhpValue fetchObject(string class_name = "stdClass", PhpArray ctor_args = null);

        /// <summary>
        /// Retrieve a statement attribute
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns>Returns the attribute value</returns>
        PhpValue getAttribute(int attribute);

        /// <summary>
        /// Returns metadata for a column in a result set.
        /// </summary>
        /// <param name="column">The 0-indexed column in the result set.</param>
        /// <returns>Returns an associative array containing the values representing the metadata for a single column</returns>
        PhpArray getColumnMeta(int column);

        /// <summary>
        /// Advances to the next rowset in a multi-rowset statement handle.
        /// </summary>
        /// <returns>Returns TRUE on success or FALSE on failure</returns>
        bool nextRowset();

        /// <summary>
        /// Returns the number of rows affected by the last SQL statement
        /// </summary>
        /// <returns></returns>
        int rowCount();

        /// <summary>
        /// Set a statement attribute.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <param name="value">The value.</param>
        /// <returns>Returns TRUE on success or FALSE on failure</returns>
        bool setAttribute(int attribute, PhpValue value);

        /// <summary>
        /// Set the default fetch mode for this statement
        /// </summary>
        /// <param name="mode">The fetch mode must be one of the PDO::FETCH_* constants.</param>
        /// <param name="param1">For FETCH_COLUMN : column number. For FETCH_CLASS : the class name. For FETCH_INTO, the object</param>
        /// <param name="param2">For FETCH_CLASS : the constructor arguments.</param>
        /// <returns>Returns TRUE on success or FALSE on failure</returns>
        bool setFetchMode(int mode, PhpValue param1, PhpValue param2);
    }
}
