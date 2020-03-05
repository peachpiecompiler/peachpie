using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using MySql.Data.MySqlClient;
using Pchp.Core;

namespace Peachpie.Library.MySql.MySqli
{
    /// <summary>
    /// Represents a prepared statement.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    [PhpExtension(Constants.ExtensionName)]
    public class mysqli_stmt
    {
        private struct BoundType
        {
            /// <summary>
            /// corresponding variable has type integer
            /// </summary>
            public const char Integer = 'i';

            /// <summary>
            /// corresponding variable has type double
            /// </summary>
            public const char Double = 'd';

            /// <summary>
            /// corresponding variable has type string
            /// </summary>
            public const char String = 's';

            /// <summary>
            /// corresponding variable is a blob and will be sent in packets
            /// </summary>
            public const char Blob = 'b';
        }

        /// <summary>
        /// Associated <see cref="mysqli"/> connection.
        /// </summary>
        internal MySqlConnectionResource/*!*/Connection { get; private set; }

        /// <summary>
        /// Prepared command.
        /// </summary>
        private protected MySqlCommand Command { get; private set; }

        /// <summary>
        /// Result of the command execute command.
        /// </summary>
        [PhpHidden]
        MySqlResultResource Result { get; set; }

        /// <summary>
        /// Lazily bound params.
        /// </summary>
        [PhpHidden]
        private PhpAlias[] _bound_params = null;

        /// <summary>
        /// Lazily bound params type.
        /// </summary>
        private string _bound_params_type = null;

        /// <summary>
        /// Constructs the object.
        /// </summary>
        public mysqli_stmt([NotNull]mysqli link)
            : this(link, null)
        {
        }

        /// <summary>
        /// Constructs the object.
        /// </summary>
        public mysqli_stmt([NotNull]mysqli link, string query)
        {
            __construct(link, query);
        }

        /* Properties */

        /// <summary>
        /// Returns the total number of rows changed, deleted, or inserted by the last executed statement.
        /// </summary>
        public int affected_rows { get; private set; }

        //int $errno;

        //array $error_list;

        //string $error;

        //int $field_count;

        /// <summary>
        /// Get the ID generated from the previous INSERT operation.
        /// </summary>
        public long insert_id => Command != null ? Command.LastInsertedId : throw new InvalidOperationException();

        //int $num_rows;

        //int $param_count;

        //string $sqlstate;

        /* Methods */

        /// <summary>
        /// Constructs the object.
        /// </summary>
        public void __construct([NotNull]mysqli link, string query = null)
        {
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }

            Connection = link.Connection;

            if (query != null)
            {
                prepare(query);
            }
        }

        //int attr_get(int $attr )

        //bool attr_set(int $attr , int $mode )

        /// <summary>
        /// Binds variables to a prepared statement as parameters.
        /// </summary>
        public bool bind_param(string types, params PhpAlias[] variables)
        {
            if (types == null || variables == null || types.Length != variables.Length)
            {
                return false;
            }

            _bound_params_type = types;
            _bound_params = variables;

            return true;
        }

        /// <summary>
        /// Send data in blocks.
        /// </summary>
        public bool send_long_data(int param_nr, PhpString data)
        {
            if (param_nr >= 0 && _bound_params_type != null && param_nr < _bound_params_type.Length)
            {
                if (_bound_params_type[param_nr] == BoundType.Blob)
                {
                    //
                    var alias = _bound_params[param_nr];
                    var str = alias.ToPhpString(this.Connection.Context);
                    str.EnsureWritable().Add(data);
                    alias.Value = str;

                    return true;
                }
            }

            // ERR
            return false;
        }

        //bool bind_result(mixed &$var1[, mixed &$... ] )

        /// <summary>
        /// Closes a prepared statement.
        /// </summary>
        public bool close()
        {
            Connection.ClosePendingReader();

            if (Command != null)
            {
                Command.Dispose();
                Command = null;
            }

            if (Result != null)
            {
                Result.Dispose();
                Result = null;
            }

            _bound_params = null;
            _bound_params_type = null;

            return true;
        }

        /// <summary>
        /// Seeks to an arbitrary row in statement result set.
        /// </summary>
        /// <param name="offset">Must be between zero and the total number of rows minus one.</param>
        public void data_seek(int offset)
        {
            if (Result == null)
            {
                throw new InvalidOperationException();
            }

            Result.SeekRow(offset);
        }

        /// <summary>
        /// Executes a prepared Query.
        /// </summary>
        public bool execute()
        {
            if (Command == null)
            {
                // ERR: not prepared
                throw new InvalidOperationException();
            }

            IDataParameter[] parameters;

            if (_bound_params == null || _bound_params.Length == 0)
            {
                parameters = Array.Empty<IDataParameter>();
            }
            else
            {
                parameters = new IDataParameter[_bound_params.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    var variable = _bound_params[i];

                    // convert the type
                    object boxed;
                    if (variable.Value.IsNull)
                    {
                        boxed = null;
                    }
                    else
                    {
                        switch (_bound_params_type[i])
                        {
                            case BoundType.Integer:
                                boxed = variable.ToLong();
                                break;
                            case BoundType.Double:
                                boxed = variable.ToDouble();
                                break;
                            case BoundType.String:
                            case BoundType.Blob:
                                var phpstr = variable.Value.ToPhpString(Connection.Context);
                                if (phpstr.ContainsBinaryData)
                                {
                                    boxed = phpstr.ToBytes(Connection.Context);
                                }
                                else
                                {
                                    boxed = phpstr.ToString();
                                }
                                break;

                            default:
                                throw new InvalidOperationException();
                        }
                    }

                    //
                    parameters[i] = new MySqlParameter()
                    {
                        Value = boxed,
                        IsNullable = ReferenceEquals(boxed, null),
                    };
                }
            }

            // execute and store the result:
            this.Result = (MySqlResultResource)Connection.ExecuteCommandInternal(Command, true, parameters, false);

            if (this.Result == null)
            {
                return false;
            }

            //
            this.affected_rows = Connection.LastAffectedRows;

            return true;
        }

        //bool fetch(void )

        //void free_result(void )

        /// <summary>
        /// Gets a result set from a prepared statement.
        /// </summary>
        [return: CastToFalse]
        public mysqli_result get_result()
        {
            return Result != null
                ? new mysqli_result(Result)
                : null; // FALSE
        }

        //object get_warnings(mysqli_stmt $stmt )
        //int num_rows(void )

        /// <summary>
        /// Prepare an SQL statement for execution.
        /// </summary>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public bool prepare(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return false;
            }

            Connection.ClosePendingReader();    // needs to be closed before creating the command

            try
            {
                Command = Connection.CreateCommandInternal(query);
                Command.Prepare();
            }
            catch (Exception e)
            {
                PhpException.Throw(PhpError.Warning, Connection.GetExceptionMessage(e));
                return false;
            }

            //
            return true;
        }

        //bool reset(void )

        //mysqli_result result_metadata(void )

        //bool store_result(void )
    }
}
