using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// Represents an aliased value.
    /// </summary>
    public class PhpAlias
    {
        #region Fields

        /// <summary>
        /// Underlaying value.
        /// </summary>
        PhpValue _value;

        /// <summary>
        /// References count.
        /// </summary>
        int _refcount;

        #endregion

        #region Properties

        /// <summary>
        /// Gets references count.
        /// </summary>
        public int ReferenceCount => _refcount;

        /// <summary>
        /// Gets or sets underlaying value.
        /// </summary>
        public PhpValue Value
        {
            get { return _value; }
            set { _value = value; }
        }

        #endregion

        #region Methods

        public void AddRef()
        {
            _refcount++;
        }

        public void ReleaseRef()
        {
            if (--_refcount == 0)
            {
                // TODO: dispose implicitly
            }
        }

        #endregion
    }
}
