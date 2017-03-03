/*

 Copyright (c) 2005-2006 Tomas Matousek.  
 Copyright (c) 20012-2017 Jakub Misek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using Pchp.Core;
using Pchp.Library.Database;
using Pchp.Library.Resources;

namespace Peachpie.Library.MsSql
{
	/// <summary>
	/// Represets a stored procedure statement.
	/// </summary>
	internal sealed class PhpSqlDbProcedure : StatementResource
	{
		/// <summary>
		/// Procedure name.
		/// </summary>
		public string/*!*/ ProcedureName { get { return procedureName; } }
		private string/*!*/ procedureName;

		/// <summary>
		/// Creates a stored procedure statement.
		/// </summary>
		/// <param name="connection">Database connection.</param>
		/// <param name="procedureName">Procedure name.</param>
		/// <exception cref="ArgumentNullException">Argument is a <B>null</B> reference.</exception>
		public PhpSqlDbProcedure(ConnectionResource/*!*/ connection, string/*!*/ procedureName)
			: base("mssql statement", connection)
		{
			if (procedureName == null)
				throw new ArgumentNullException("procedureName");

			this.procedureName = procedureName;
		}

		internal static PhpSqlDbProcedure ValidProcedure(PhpResource handle)
		{
			PhpSqlDbProcedure result = handle as PhpSqlDbProcedure;
			if (result != null && result.IsValid) return result;

			PhpException.Throw(PhpError.Warning, Resources.invalid_stored_procedure_resource);
			return null;
		}

		internal static ParameterType VariableTypeToParamType(MsSql.VariableType type)
		{
			switch (type)
			{
				case MsSql.VariableType.Char:
				case MsSql.VariableType.Text:
				case MsSql.VariableType.VarChar:
				return ParameterType.String;

				case MsSql.VariableType.Double:
				case MsSql.VariableType.Float:
				case MsSql.VariableType.FloatN:
				return ParameterType.Double;

				case MsSql.VariableType.Bit:
				case MsSql.VariableType.Int8:
				case MsSql.VariableType.Int16:
				case MsSql.VariableType.Int32:
				return ParameterType.Integer;

				default:
				return ParameterType.Invalid;
			}
		}

		/// <summary>
		/// Executes the procedure.
		/// </summary>
		/// <param name="skipResults">Whether to load the results.</param>
		/// <param name="success">Whether the execution succeeded.</param>
		/// <returns>Results or a <B>null</B> reference if results are not loaded or an error occured.</returns>
		public PhpSqlDbResult Execute(bool skipResults, out bool success)
		{
			var result = (PhpSqlDbResult)connection.ExecuteProcedure(procedureName, PrepareParameters(), skipResults);

			success = connection.LastException == null;

            if (success)
            {
                WriteParametersBack();
            }

			return result;
		}
	}
}
