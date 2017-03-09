using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Pchp.Core;

namespace Peachpie.Library.PDO
{
    /// <summary>
    /// PDOStatement class
    /// </summary>
    /// <seealso cref="IPDOStatement" />
    public class PDOStatement : IPDOStatement, IDisposable
    {
        private readonly PDO m_pdo;
        private readonly string m_stmt;
        private readonly PhpArray m_options;

        private readonly DbCommand m_cmd;
        private DbDataReader m_dr;
        private readonly Dictionary<PDO.PDO_ATTR, object> m_attributes = new Dictionary<PDO.PDO_ATTR, object>();
        private string[] m_dr_names;


        /// <summary>
        /// Initializes a new instance of the <see cref="PDOStatement" /> class.
        /// </summary>
        /// <param name="pdo">The pdo.</param>
        /// <param name="statement">The statement.</param>
        /// <param name="driver_options">The driver options.</param>
        internal PDOStatement(PDO pdo, string statement, PhpArray driver_options)
        {
            this.m_pdo = pdo;
            this.m_stmt = statement;
            this.m_options = driver_options ?? PhpArray.Empty;

            this.m_cmd = pdo.CreateCommand(this.m_stmt);

            this.SetDefaultAttributes();
        }

        private void SetDefaultAttributes()
        {
            this.m_attributes.Set(PDO.PDO_ATTR.ATTR_CURSOR, PDO.PDO_CURSOR.CURSOR_FWDONLY);
        }

        /// <inheritDoc />
        void IDisposable.Dispose()
        {
            this.m_dr?.Dispose();
            this.m_cmd.Dispose();
        }

        private void OpenReader()
        {
            if (this.m_dr == null)
            {
                PDO.PDO_CURSOR cursor = (PDO.PDO_CURSOR)this.m_attributes[PDO.PDO_ATTR.ATTR_CURSOR];
                this.m_dr = this.m_pdo.Driver.OpenReader(this.m_pdo, this.m_cmd, cursor);
                switch (cursor)
                {
                    case PDO.PDO_CURSOR.CURSOR_FWDONLY:
                        this.m_dr = this.m_cmd.ExecuteReader();
                        break;
                    case PDO.PDO_CURSOR.CURSOR_SCROLL:
                        this.m_dr = this.m_cmd.ExecuteReader();
                        break;
                    default:
                        throw new InvalidProgramException();
                }
                this.m_dr_names = new string[this.m_dr.FieldCount];
                for (int i = 0; i < this.m_dr_names.Length; i++)
                {
                    this.m_dr_names[i] = this.m_dr.GetName(i);
                }
            }
        }

        /// <inheritDoc />
        public bool bindColumn(PhpValue colum, ref PhpValue param, int? type = default(int?), int? maxlen = default(int?), PhpValue? driverdata = default(PhpValue?))
        {
            throw new NotImplementedException();
        }

        /// <inheritDoc />
        public bool bindParam(PhpValue parameter, ref PhpValue variable, int data_type = 2, int? length = default(int?), PhpValue? driver_options = default(PhpValue?))
        {
            throw new NotImplementedException();
        }

        /// <inheritDoc />
        public bool bindValue(PhpValue parameter, PhpValue value, int data_type = 2)
        {
            throw new NotImplementedException();
        }

        /// <inheritDoc />
        public bool closeCursor()
        {
            if (this.m_dr != null)
            {
                ((IDisposable)this.m_dr).Dispose();
                this.m_dr = null;
                return true;
            }
            return false;
        }

        /// <inheritDoc />
        public int columnCount()
        {
            if (this.m_dr == null)
            {
                return 0;
            }

            return this.m_dr.FieldCount;
        }

        /// <inheritDoc />
        public void debugDumpParams()
        {
            throw new NotImplementedException();
        }

        /// <inheritDoc />
        public string errorCode()
        {
            throw new NotImplementedException();
        }

        /// <inheritDoc />
        public PhpArray errorInfo()
        {
            throw new NotImplementedException();
        }

        /// <inheritDoc />
        public bool execute(PhpArray input_parameters = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritDoc />
        public PhpValue fetch(int? fetch_style = default(int?), int cursor_orientation = default(int), int cursor_offet = 0)
        {
            this.m_pdo.ClearError();
            try
            {
                PDO.PDO_FETCH style = PDO.PDO_FETCH.FETCH_BOTH;
                if (fetch_style.HasValue && Enum.IsDefined(typeof(PDO.PDO_FETCH), fetch_style.Value))
                {
                    style = (PDO.PDO_FETCH)fetch_style.Value;
                }
                PDO.PDO_FETCH_ORI ori = PDO.PDO_FETCH_ORI.FETCH_ORI_NEXT;
                if (Enum.IsDefined(typeof(PDO.PDO_FETCH_ORI), cursor_orientation))
                {
                    ori = (PDO.PDO_FETCH_ORI)cursor_orientation;
                }

                switch (ori)
                {
                    case PDO.PDO_FETCH_ORI.FETCH_ORI_NEXT:
                        break;
                    default:
                        throw new NotSupportedException();
                }

                if (!this.m_dr.Read())
                    return PhpValue.False;

                switch (style)
                {
                    case PDO.PDO_FETCH.FETCH_OBJ:
                        return this.ReadObj();
                    case PDO.PDO_FETCH.FETCH_ASSOC:
                        return PhpValue.Create(this.ReadArray(true, false));
                    case PDO.PDO_FETCH.FETCH_BOTH:
                    case PDO.PDO_FETCH.FETCH_USE_DEFAULT:
                        return PhpValue.Create(this.ReadArray(true, true));
                    case PDO.PDO_FETCH.FETCH_NUM:
                        return PhpValue.Create(this.ReadArray(false, true));
                    default:
                        throw new NotImplementedException();
                }
            }
            catch (System.Exception ex)
            {
                this.m_pdo.HandleError(ex);
                return PhpValue.False;
            }
        }

        private PhpValue ReadObj()
        {
            return PhpValue.FromClass(this.ReadArray(true, false).ToClass());
        }


        private PhpArray ReadArray(bool assoc, bool num)
        {
            PhpArray arr = new PhpArray();
            for (int i = 0; i < this.m_dr.FieldCount; i++)
            {
                if (this.m_dr.IsDBNull(i))
                {
                    if (assoc)
                        arr.Add(this.m_dr_names[i], PhpValue.Null);
                    if (num)
                        arr.Add(i, PhpValue.Null);
                }
                else
                {
                    var value = PhpValue.FromClr(this.m_dr.GetValue(i));
                    if (assoc)
                        arr.Add(this.m_dr_names[i], value);
                    if (num)
                        arr.Add(i, value);
                }
            }
            return arr;
        }

        /// <inheritDoc />
        public PhpArray fetchAll(int? fetch_style = default(int?), PhpValue? fetch_argument = default(PhpValue?), PhpArray ctor_args = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritDoc />
        public PhpValue fetchColumn(int column_number = 0)
        {
            throw new NotImplementedException();
        }

        /// <inheritDoc />
        public PhpValue fetchObject(string class_name = "stdClass", PhpArray ctor_args = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritDoc />
        public PhpValue getAttribute(int attribute)
        {
            throw new NotImplementedException();
        }

        /// <inheritDoc />
        [return: CastToFalse]
        public PhpArray getColumnMeta(int column)
        {
            if (this.m_dr == null)
            {
                return null;
            }

            if (column < 0 || column >= this.m_dr.FieldCount)
                return null;

            PhpArray meta = new PhpArray();
            meta.Add("native_type", this.m_dr.GetFieldType(column).FullName);
            meta.Add("driver:decl_type", this.m_dr.GetDataTypeName(column));
            //meta.Add("flags", PhpValue.Null);
            meta.Add("name", this.m_dr_names[column]);
            //meta.Add("table", PhpValue.Null);
            //meta.Add("len", PhpValue.Null);
            //meta.Add("prevision", PhpValue.Null);
            //meta.Add("pdo_type", (int)PDO.PARAM.PARAM_NULL);
            return meta;
        }

        /// <inheritDoc />
        public bool nextRowset()
        {
            if (this.m_dr == null)
            {
                return false;
            }
            if (this.m_dr.NextResult())
            {
                this.m_dr_names = new string[this.m_dr.FieldCount];
                for (int i = 0; i < this.m_dr_names.Length; i++)
                {
                    this.m_dr_names[i] = this.m_dr.GetName(i);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <inheritDoc />
        public int rowCount()
        {
            throw new NotImplementedException();
        }

        /// <inheritDoc />
        public bool setAttribute(int attribute, PhpValue value)
        {
            throw new NotImplementedException();
        }

        /// <inheritDoc />
        public bool setFetchMode(int mode, PhpValue param1, PhpValue param2)
        {
            throw new NotImplementedException();
        }
    }
}
