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
        /// <summary>
        /// Associated <see cref="mysqli"/> connection.
        /// </summary>
        internal MySqlConnectionResource/*!*/Connection { get; private set; }

        /// <summary>
        /// Prepared command.
        /// </summary>
        protected internal MySqlCommand Command { get; private set; }

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
        //int $affected_rows;
        //int $errno;
        //array $error_list;
        //string $error;
        //int $field_count;
        //int $insert_id;
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
                if (_bound_params_type[param_nr] == 'b')
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
        //bool close(void )
        //void data_seek(int $offset )

        /// <summary>
        /// Executes a prepared Query.
        /// </summary>
        public bool execute()
        {
            if (Command == null)
            {
                // ERR: not prepared
                return false;
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
                    // TODO: check type

                    parameters[i] = new MySqlParameter()
                    {
                        Value = _bound_params[i].Value.ToClr(),
                    };
                }
            }

            var result = Connection.ExecuteCommandInternal(Command, true, parameters, false);
            if (result != null)
            {
                //

                return true;
            }

            return false;
        }

        //bool fetch(void )
        //void free_result(void )
        //mysqli_result get_result(void )
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
