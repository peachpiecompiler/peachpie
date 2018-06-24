using System;

namespace Peachpie.Library.PDO
{
    //TODO: Probably a similar class is needed for the other DBMS
    class PDOMySQLWrapper
    {
        public static string convertDSN(string dsn)
        {
            if (dsn == null || dsn.Length == 0)
            {
                return dsn;
            }

            dsn = dsn.Replace("host", "Host");
            dsn = dsn.Replace("port", "Port");
            dsn = dsn.Replace("dbname", "Database");
            //TODO: if unix_socket is provided the protocol probably needs to be set as well.
            // but scince I have no way of testing this I do not touch it.
            //dsn = dsn.Replace("unix_socket", "..."); 
            dsn = dsn.Replace("charset", "CharSet");

            return dsn;
        }
    }
}
