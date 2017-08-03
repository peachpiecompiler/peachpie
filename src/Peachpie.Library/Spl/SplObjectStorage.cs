using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Pchp.Core;

namespace Pchp.Library.Spl
{
    /// <summary>
    /// The class provides a map from objects to data or, by ignoring data, an object set.
    /// This dual purpose can be useful in many cases involving the need to uniquely identify objects.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public class SplObjectStorage : Countable, Iterator, Serializable, ArrayAccess
    {
        private readonly Dictionary<object, PhpValue> _storage = new Dictionary<object, PhpValue>();
        private IEnumerator<KeyValuePair<object, PhpValue>> _enumerator;
        private int _index = -1;

        protected readonly Context _ctx;

        /// <summary>
        /// See <see cref="stdClass"/>.
        /// Allows for storing runtime fields to this object.
        /// </summary>
        [System.Runtime.CompilerServices.CompilerGenerated]
        internal PhpArray __peach__runtimeFields;

        public SplObjectStorage(Context ctx) { _ctx = ctx; }

        public void addAll(SplObjectStorage storage)
        {
            foreach (var pair in storage._storage)
            {
                _storage[pair.Key] = pair.Value.DeepCopy();
            }
            // TODO: reset _enumerator to previous position
        }
        public void attach(object @object, PhpValue data = default(PhpValue))
        {
            _storage[@object] = data.IsSet ? data.DeepCopy() : PhpValue.Void;
            // TODO: reset _enumerator to previous position
        }
        public virtual bool contains(object @object) => _storage.ContainsValue(PhpValue.FromClass(@object));
        public void detach(object @object)
        {
            _storage.Remove(@object);
            // TODO: reset _enumerator to previous position
        }
        public string getHash(object @object) => (@object != null) ? @object.GetHashCode().ToString("x32") : string.Empty;   // see spl_object_hash()
        public PhpValue getInfo()
        {
            EnsureEnumerator();
            return (_index >= 0) ? _enumerator.Current.Value : PhpValue.Null;
        }
        public void setInfo(PhpValue data)
        {
            EnsureEnumerator();
            if (_index >= 0)
            {
                _storage[_enumerator.Current.Key] = data.DeepCopy();
                // TODO: reset _enumerator to previous position
            }
            else
            {
                throw new InvalidOperationException();  // TODO: Err
            }
        }
        public void removeAll(SplObjectStorage storage)
        {
            foreach (var obj in storage._storage.Keys)
            {
                _storage.Remove(obj);
            }
            // TODO: reset _enumerator to previous position
        }
        public void removeAllExcept(SplObjectStorage storage)
        {
            var set = new HashSet<object>(this._storage.Keys);
            set.ExceptWith(storage._storage.Keys);

            foreach (var obj in set)
            {
                _storage.Remove(obj);
            }
            // TODO: reset _enumerator to previous position
        }

        #region Countable

        public long count() => _storage.Count;

        #endregion

        #region Iterator

        private void EnsureEnumerator()
        {
            if (_enumerator == null)
            {
                _enumerator = _storage.GetEnumerator();
                _index = _enumerator.MoveNext() ? 0 : -1;
            }
        }

        public PhpValue current()
        {
            EnsureEnumerator();
            return (_index >= 0) ? PhpValue.FromClass(_enumerator.Current.Key) : PhpValue.Null;
        }

        public PhpValue key()
        {
            EnsureEnumerator();
            return (_index >= 0) ? PhpValue.Create(_index) : PhpValue.Null;
        }

        public void next()
        {
            EnsureEnumerator();
            if (_index >= 0 && _enumerator.MoveNext())
            {
                _index++;
            }
            else
            {
                _index = -1;
            }
        }

        public void rewind()
        {
            _enumerator = null;
        }

        public bool valid()
        {
            EnsureEnumerator();
            return _index >= 0;
        }

        #endregion

        #region ArrayAccess

        public bool offsetExists(PhpValue offset)
        {
            return _storage.ContainsKey(offset.AsObject());
        }

        public PhpValue offsetGet(PhpValue offset)
        {
            var obj = offset.AsObject();
            if (obj != null && _storage.TryGetValue(obj, out PhpValue value))
            {
                return value;
            }
            else
            {
                // TODO: Err
                return PhpValue.Null;
            }
        }

        public void offsetSet(PhpValue offset, PhpValue value)
        {
            var obj = offset.AsObject();
            if (obj != null)
            {
                _storage[obj] = value.DeepCopy();
                // TODO: reset _enumerator to previous position
            }
            else
            {
                throw new ArgumentException(nameof(offset));
            }
        }

        public void offsetUnset(PhpValue offset)
        {
            var obj = offset.AsObject();
            if (obj != null)
            {
                _storage.Remove(obj);
            }
        }

        #endregion

        #region Serializable

        public PhpString serialize()
        {
            // x:{count_int};{item0},{value0};;...;;m:{members_array}

            var result = new PhpString();
            var serializer = PhpSerialization.PhpSerializer.Instance;

            // x:i:{count};
            result.Append("x:");
            result.Append(serializer.Serialize(_ctx, (PhpValue)_storage.Count, default(RuntimeTypeHandle)));

            // {item},{value};
            foreach (var pair in _storage)
            {
                result.Append(serializer.Serialize(_ctx, PhpValue.FromClass(pair.Key), default(RuntimeTypeHandle)));
                result.Append(",");
                result.Append(serializer.Serialize(_ctx, pair.Value, default(RuntimeTypeHandle)));
                result.Append(";");
            }

            // m:{array of runtime members}
            result.Append("m:");
            result.Append(serializer.Serialize(_ctx, (PhpValue)(__peach__runtimeFields ?? PhpArray.Empty), default(RuntimeTypeHandle)));

            //
            return result;
        }

        public void unserialize(PhpString serialized)
        {
            // x:{count_int};{item0},{value0};...;m:{members_array}
            if (serialized.Length < 12) throw new ArgumentException(nameof(serialized)); // quick check

            var stream = new MemoryStream(serialized.ToBytes(_ctx));
            try
            {
                PhpValue tmp;
                var reader = new PhpSerialization.PhpSerializer.ObjectReader(_ctx, stream, default(RuntimeTypeHandle));

                // x:
                if (stream.ReadByte() != 'x' || stream.ReadByte() != ':') throw new InvalidDataException();
                // i:{count};
                tmp = reader.Deserialize();
                if (tmp.TypeCode != PhpTypeCode.Long) throw new InvalidDataException();
                var count = tmp.ToLong();
                if (count < 0) throw new InvalidDataException(nameof(count));

                stream.Seek(-1, SeekOrigin.Current);    // back to `;`

                // {item},{value}
                while (count-- > 0)
                {
                    // ;obj
                    if (stream.ReadByte() != ';') throw new InvalidDataException();
                    var obj = reader.Deserialize();
                    // ,data
                    PhpValue data;
                    if (stream.ReadByte() == ',')
                    {
                        data = reader.Deserialize();
                    }
                    else
                    {
                        data = PhpValue.Void;
                    }

                    //
                    _storage[obj.AsObject()] = data;
                }

                // ;
                if (stream.ReadByte() != ';') throw new InvalidDataException();

                // m:{array}
                if (stream.ReadByte() != 'm' || stream.ReadByte() != ':') throw new InvalidDataException();

                tmp = reader.Deserialize();
                if (tmp.IsArray)
                {
                    var arr = tmp.AsArray();
                    if (arr != null && arr.Count != 0)  // do not leak empty arrays
                    {
                        __peach__runtimeFields = arr;
                    }
                }
                else
                {
                    throw new InvalidDataException();
                }
            }
            catch (Exception e)
            {
                PhpException.Throw(PhpError.Notice,
                    Resources.LibResources.deserialization_failed, e.Message, stream.Position.ToString(), stream.Length.ToString());
            }
        }

        #endregion
    }
}
