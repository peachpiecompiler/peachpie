using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Pchp.Core;

namespace Pchp.Library.Spl
{
    /// <summary>
    /// The class provides a map from objects to data or, by ignoring data, an object set.
    /// This dual purpose can be useful in many cases involving the need to uniquely identify objects.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName), PhpExtension(SplExtension.Name)]
    public class SplObjectStorage : Countable, Iterator, Serializable, ArrayAccess
    {
        struct Keys
        {
            public static readonly IntStringKey Object = new IntStringKey("obj");
            public static readonly IntStringKey Info = new IntStringKey("inf");
            public static IntStringKey MakeKey(object obj) => new IntStringKey(SplObjects.object_hash_internal(obj));
            public static void AttachImpl(PhpArray storage, object @object, PhpValue data)
            {
                Debug.Assert(@object != null);

                // hash => { "obj" => object, "inf" => data }

                storage[MakeKey(@object)] = (PhpValue)new PhpArray(2)
                {
                    { Object, PhpValue.FromClass(@object) },
                    { Info, data.IsSet ? data : PhpValue.Null },
                };
            }
        }

        /// <summary>
        /// Storage array "storage" as it is in PHP.
        /// <code>{ hash_i => ["obj" => object_i, "inf" => info_i] }</code>
        /// </summary>
        private readonly PhpArray storage = new PhpArray();
        internal int _index = 0;

        protected readonly Context _ctx;

        /// <summary>
        /// See <see cref="stdClass"/>.
        /// Allows for storing runtime fields to this object.
        /// </summary>
        [System.Runtime.CompilerServices.CompilerGenerated]
        internal PhpArray __peach__runtimeFields;

        public SplObjectStorage(Context ctx) { _ctx = ctx; }

        /// <summary>
        /// Adds all objects-data pairs from a different storage in the current storage.
        /// </summary>
        public virtual void addAll(SplObjectStorage storage)
        {
            var e = storage.storage.GetFastEnumerator();
            while (e.MoveNext())
            {
                this.storage[e.CurrentKey] = e.CurrentValue.DeepCopy();
            }
        }

        /// <summary>
        /// Adds an object inside the storage, and optionally associate it to some data.
        /// </summary>
        public virtual void attach(object @object, PhpValue data = default(PhpValue)) => Keys.AttachImpl(storage, @object, data);

        /// <summary>
        /// Checks if the storage contains the object provided.
        /// </summary>
        public virtual bool contains(object @object) => @object != null && storage.ContainsKey(Keys.MakeKey(@object));

        /// <summary>
        /// Removes the object from the storage.
        /// </summary>
        public virtual void detach(object @object)
        {
            storage.RemoveKey(Keys.MakeKey(@object));
        }

        /// <summary>
        /// This method calculates an identifier for the objects added to an SplObjectStorage object.
        /// </summary>
        /// <remarks>
        /// The implementation in SplObjectStorage returns the same value as spl_object_hash().
        /// 
        /// The storage object will never contain more than one object with the same identifier.
        /// As such, it can be used to implement a set(a collection of unique values) where
        /// the quality of an object being unique is determined by the value returned by this function being unique.
        /// </remarks>
        public virtual string getHash(object @object) => (@object != null) ? SplObjects.object_hash_internal_string(@object) : string.Empty;   // see spl_object_hash()

        /// <summary>
        /// Returns the data, or info, associated with the object pointed by the current iterator position.
        /// </summary>
        public virtual PhpValue getInfo()
        {
            return storage.IntrinsicEnumerator.AtEnd
                ? PhpValue.Null
                : storage.IntrinsicEnumerator.CurrentValue.Array[Keys.Info];
        }

        /// <summary>
        /// Associates data, or info, with the object currently pointed to by the iterator.
        /// </summary>
        public virtual void setInfo(PhpValue data)
        {
            if (!storage.IntrinsicEnumerator.AtEnd)
            {
                storage.IntrinsicEnumerator.CurrentValue.Array[Keys.Info] = data.DeepCopy();
            }
        }

        /// <summary>
        /// Removes objects contained in another storage from the current storage.
        /// </summary>
        public virtual void removeAll(SplObjectStorage storage)
        {
            var e = storage.storage.GetFastEnumerator();
            while (e.MoveNext())
            {
                this.storage.RemoveKey(e.CurrentKey);
            }
        }

        /// <summary>
        /// Removes all objects except for those contained in another storage from the current storage.
        /// </summary>
        public virtual void removeAllExcept(SplObjectStorage storage)
        {
            var e = this.storage.GetFastEnumerator();
            while (e.MoveNext())
            {
                if (!storage.storage.ContainsKey(e.CurrentKey))
                {
                    // NOTE: deleting element under the enumerator current entry, FastEnumerator survives
                    this.storage.RemoveKey(e.CurrentKey);
                }
            }
        }

        #region Countable

        /// <summary>
        /// Counts the number of objects in the storage.
        /// </summary>
        public virtual long count() => storage.Count;

        #endregion

        #region Iterator

        /// <summary>
        /// Returns the current storage entry.
        /// </summary>
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

        /// <summary>
        /// Moves the iterator to the next object in the storage.
        /// </summary>
        public virtual void next()
        {
            _index++;   // PHP behavior, increasing key even if the enumerater reached the end of storage
            storage.IntrinsicEnumerator.MoveNext();
        }

        /// <summary>
        /// Rewind the iterator to the first storage element.
        /// </summary>
        public virtual void rewind()
        {
            _index = 0;
            storage.IntrinsicEnumerator.MoveFirst();
        }

        /// <summary>
        /// Returns if the current iterator entry is valid.
        /// </summary>
        public virtual bool valid()
        {
            return !storage.IntrinsicEnumerator.AtEnd;
        }

        #endregion

        #region ArrayAccess

        /// <summary>
        /// Checks whether an object exists in the storage.
        /// </summary>
        /// <remarks>Alias for <see cref="contains(object)"/>.</remarks>
        public virtual bool offsetExists(PhpValue offset) => contains(offset.AsObject());

        /// <summary>
        /// Returns the data associated with an object in the storage.
        /// </summary>
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

        /// <summary>
        /// Associate data to an object in the storage.
        /// </summary>
        public virtual void offsetSet(PhpValue offset, PhpValue value = default(PhpValue))
        {
            var obj = offset.AsObject();
            if (obj != null)
            {
                Keys.AttachImpl(storage, obj, value);
            }
            else
            {
                throw new ArgumentException(nameof(offset));
            }
        }

        /// <summary>
        /// Removes an object from the storage.
        /// </summary>
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

        /// <summary>
        /// Returns a string representation of the storage.
        /// </summary>
        public virtual PhpString serialize()
        {
            // x:{count_int};{item0},{value0};;...;;m:{members_array}

            var result = new PhpString.Blob();
            var serializer = PhpSerialization.PhpSerializer.Instance;

            // x:i:{count};
            result.Append("x:");
            result.Append(serializer.Serialize(_ctx, storage.Count, default));

            // {item},{value};
            var e = storage.GetFastEnumerator();
            while (e.MoveNext())
            {
                result.Append(serializer.Serialize(_ctx, e.CurrentValue.Array[Keys.Object], default));
                result.Append(",");
                result.Append(serializer.Serialize(_ctx, e.CurrentValue.Array[Keys.Info], default));
                result.Append(";");
            }

            // m:{array of runtime members}
            result.Append("m:");
            result.Append(serializer.Serialize(_ctx, (__peach__runtimeFields ?? PhpArray.Empty), default));

            //
            return new PhpString(result);
        }

        /// <summary>
        /// Unserializes storage entries and attach them to the current storage.
        /// </summary>
        public virtual void unserialize(PhpString serialized)
        {
            // x:{count_int};{item0},{value0};...;m:{members_array}
            if (serialized.Length < 12) throw new ArgumentException(nameof(serialized)); // quick check

            var stream = new MemoryStream(serialized.ToBytes(_ctx));
            try
            {
                var reader = new PhpSerialization.PhpSerializer.ObjectReader(_ctx, stream, default);

                // x:
                if (stream.ReadByte() != 'x' || stream.ReadByte() != ':') throw new InvalidDataException();
                // i:{count};
                var tmp = reader.Deserialize();
                if (!tmp.IsLong(out var count) || count < 0)
                {
                    throw new InvalidDataException();
                }

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
                        // backward compatibility with data created with old PHP SplObjectStorage
                        data = PhpValue.Void;
                        stream.Seek(-1, SeekOrigin.Current);    // back to `;`
                    }

                    //
                    Keys.AttachImpl(storage, obj.AsObject(), data);
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

        public virtual PhpArray __serialize()
        {

            var flattenedStorage = new PhpArray(storage.Count * 2);
            int i = 0;
            var e = storage.GetFastEnumerator();
            while (e.MoveNext())
            {
                flattenedStorage[i++] = e.CurrentValue.Array[Keys.Object];
                flattenedStorage[i++] = e.CurrentValue.Array[Keys.Info];
            }

            var array = new PhpArray(2);
            array.AddValue(flattenedStorage);
            array.AddValue(__peach__runtimeFields ?? PhpArray.NewEmpty());

            return array;
        }

        public virtual void __unserialize(PhpArray array)
        {
            var flattenedStorage = array.TryGetValue(0, out var storageVal) && storageVal.IsPhpArray(out var storageArray)
                ? storageArray : throw new InvalidDataException();

            var e = flattenedStorage.GetFastEnumerator();
            while (e.MoveNext())
            {
                var key = e.CurrentValue.IsObject ? e.CurrentValue.Object : throw new InvalidDataException();

                if (!e.MoveNext())
                    throw new InvalidDataException();
                var val = e.CurrentValue;

                Keys.AttachImpl(storage, key, val);
            }

            __peach__runtimeFields = array.TryGetValue(1, out var propsVal) && propsVal.IsPhpArray(out var propsArray)
                ? propsArray : throw new InvalidDataException();
        }

        #endregion
    }
}
