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
        struct Keys
        {
            public static readonly IntStringKey Object = new IntStringKey("obj");
            public static readonly IntStringKey Info = new IntStringKey("inf");
            public static IntStringKey MakeKey(object obj) => new IntStringKey(SplObjects.object_hash_internal(obj));
        }

        /// <summary>
        /// Storage array "storage" as it is in PHP.
        /// <code>{ hash_i => [object_i, info_i] }</code>
        /// </summary>
        private readonly PhpArray storage = new PhpArray();
        private int _index = 0;

        protected readonly Context _ctx;

        /// <summary>
        /// See <see cref="stdClass"/>.
        /// Allows for storing runtime fields to this object.
        /// </summary>
        [System.Runtime.CompilerServices.CompilerGenerated]
        internal PhpArray __peach__runtimeFields;

        public SplObjectStorage(Context ctx) { _ctx = ctx; }

        public virtual void addAll(SplObjectStorage storage)
        {
            using (var e = storage.storage.GetFastEnumerator())
                while (e.MoveNext())
                {
                    this.storage[e.CurrentKey] = e.CurrentValue.DeepCopy();
                }
        }
        public virtual void attach(object @object, PhpValue data = default(PhpValue))
        {
            // hash => { "obj" => object, "inf" => data }

            this.storage[Keys.MakeKey(@object)] = (PhpValue)new PhpArray(2)
            {
                {Keys.Object,  PhpValue.FromClass(@object)},
                {Keys.Info,  data.IsSet ? data : PhpValue.Null}
            };
        }
        public virtual bool contains(object @object) => storage.ContainsKey(Keys.MakeKey(@object));
        public virtual void detach(object @object)
        {
            storage.RemoveKey(Keys.MakeKey(@object));
        }
        public virtual string getHash(object @object) => (@object != null) ? SplObjects.object_hash_internal_string(@object) : string.Empty;   // see spl_object_hash()
        public virtual PhpValue getInfo()
        {
            return storage.IntrinsicEnumerator.AtEnd
                ? PhpValue.Null
                : storage.IntrinsicEnumerator.CurrentValue.Array[Keys.Info];
        }
        public virtual void setInfo(PhpValue data)
        {
            if (!storage.IntrinsicEnumerator.AtEnd)
            {
                storage.IntrinsicEnumerator.CurrentValue.Array[Keys.Info] = data.DeepCopy();
            }
        }
        public virtual void removeAll(SplObjectStorage storage)
        {
            using (var e = storage.storage.GetFastEnumerator())
                while (e.MoveNext())
                {
                    this.storage.RemoveKey(e.CurrentKey);
                }
        }
        public virtual void removeAllExcept(SplObjectStorage storage)
        {
            using (var e = this.storage.GetFastEnumerator())
                while (e.MoveNext())
                {
                    if (!storage.storage.ContainsKey(e.CurrentKey))
                    {
                        this.storage.RemoveKey(e.CurrentKey);
                    }
                }
        }

        #region Countable

        public virtual long count() => storage.Count;

        #endregion

        #region Iterator

        public virtual PhpValue current()
        {
            return storage.IntrinsicEnumerator.AtEnd
                ? PhpValue.Null
                : storage.IntrinsicEnumerator.CurrentValue.Array[Keys.Object];
        }

        public virtual PhpValue key()
        {
            return PhpValue.Create(_index);
        }

        public virtual void next()
        {
            _index++;   // PHP behavior, increasing key even if the enumerater reached the end of storage
            storage.IntrinsicEnumerator.MoveNext();
        }

        public virtual void rewind()
        {
            _index = 0;
            storage.IntrinsicEnumerator.MoveFirst();
        }

        public virtual bool valid()
        {
            return !storage.IntrinsicEnumerator.AtEnd;
        }

        #endregion

        #region ArrayAccess

        public virtual bool offsetExists(PhpValue offset)
        {
            var obj = offset.AsObject();
            return obj != null && storage.ContainsKey(Keys.MakeKey(obj));
        }

        public virtual PhpValue offsetGet(PhpValue offset)
        {
            var obj = offset.AsObject();
            if (obj != null && this.storage.TryGetValue(Keys.MakeKey(obj), out PhpValue value))
            {
                return value.Array[Keys.Info];
            }
            else
            {
                throw new UnexpectedValueException();
            }
        }

        public virtual void offsetSet(PhpValue offset, PhpValue value)
        {
            var obj = offset.AsObject();
            if (obj != null)
            {
                attach(obj, value);
            }
            else
            {
                throw new ArgumentException(nameof(offset));
            }
        }

        public virtual void offsetUnset(PhpValue offset)
        {
            var obj = offset.AsObject();
            if (obj != null)
            {
                this.storage.Remove(Keys.MakeKey(obj));
            }
        }

        #endregion

        #region Serializable

        public virtual PhpString serialize()
        {
            // x:{count_int};{item0},{value0};;...;;m:{members_array}

            var result = new PhpString();
            var serializer = PhpSerialization.PhpSerializer.Instance;

            // x:i:{count};
            result.Append("x:");
            result.Append(serializer.Serialize(_ctx, (PhpValue)storage.Count, default(RuntimeTypeHandle)));

            // {item},{value};
            using (var e = storage.GetFastEnumerator())
                while (e.MoveNext())
                {
                    result.Append(serializer.Serialize(_ctx, e.CurrentValue.Array[Keys.Object], default(RuntimeTypeHandle)));
                    result.Append(",");
                    result.Append(serializer.Serialize(_ctx, e.CurrentValue.Array[Keys.Info], default(RuntimeTypeHandle)));
                    result.Append(";");
                }

            // m:{array of runtime members}
            result.Append("m:");
            result.Append(serializer.Serialize(_ctx, (PhpValue)(__peach__runtimeFields ?? PhpArray.Empty), default(RuntimeTypeHandle)));

            //
            return result;
        }

        public virtual void unserialize(PhpString serialized)
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
                    attach(obj.AsObject(), data);
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
