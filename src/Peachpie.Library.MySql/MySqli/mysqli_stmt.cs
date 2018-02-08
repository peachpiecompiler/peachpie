using System;
using System.Collections.Generic;
using System.Text;
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
        //__construct(mysqli $link[, string $query] )
        //int attr_get(int $attr )
        //bool attr_set(int $attr , int $mode )
        //bool bind_param(string $types , mixed &$var1[, mixed &$... ] )
        //bool bind_result(mixed &$var1[, mixed &$... ] )
        //bool close(void )
        //void data_seek(int $offset )
        //bool execute(void )
        //bool fetch(void )
        //void free_result(void )
        //mysqli_result get_result(void )
        //object get_warnings(mysqli_stmt $stmt )
        //int num_rows(void )
        //mixed prepare(string $query )
        //bool reset(void )
        //mysqli_result result_metadata(void )
        //bool send_long_data(int $param_nr , string $data )
        //bool store_result(void )
    }
}
