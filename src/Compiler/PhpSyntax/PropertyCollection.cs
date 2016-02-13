using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Hashtable = System.Collections.Generic.Dictionary<object, object>;

namespace Pchp.Syntax
{
    /// <summary>
    /// Provides set of keyed properties.
    /// </summary>
    public interface IPropertyCollection
    {
        /// <summary>
        /// Sets property into collection.
        /// </summary>
        /// <param name="key">Key to the property, cannot be <c>null</c>.</param>
        /// <param name="value">Value.</param>
        void SetProperty(object key, object value);

        /// <summary>
        /// Sets property into collection under the key <c>typeof(T)</c>.
        /// </summary>
        /// <typeparam name="T">Type of the value and property key.</typeparam>
        /// <param name="value">Value.</param>
        void SetProperty<T>(T value);

        /// <summary>
        /// Gets property from the collection.
        /// </summary>
        /// <param name="key">Key to the property, cannot be <c>null</c>.</param>
        /// <returns>Property value or <c>null</c> if property does not exist.</returns>
        object GetProperty(object key);

        /// <summary>
        /// Gets property of type <typeparamref name="T"/> from the collection.
        /// </summary>
        /// <typeparam name="T">Type and key of the property.</typeparam>
        /// <returns>Property value.</returns>
        T GetProperty<T>();

        /// <summary>
        /// Tries to get property from the container.
        /// </summary>
        bool TryGetProperty(object key, out object value);

        /// <summary>
        /// Tries to get property from the container.
        /// </summary>
        bool TryGetProperty<T>(out T value);

        /// <summary>
        /// Removes property from the collection.
        /// </summary>
        /// <param name="key">Key to the property.</param>
        /// <returns><c>True</c> if property was found and removed, otherwise <c>false</c>.</returns>
        bool RemoveProperty(object key);

        /// <summary>
        /// Removes property from the collection.
        /// </summary>
        /// <typeparam name="T">Key to the property.</typeparam>
        /// <returns><c>True</c> if property was found and removed, otherwise <c>false</c>.</returns>
        bool RemoveProperty<T>();

        /// <summary>
        /// Clear the collection of properties.
        /// </summary>
        void ClearProperties();

        /// <summary>
        /// Gets or sets property.
        /// </summary>
        /// <param name="key">Property key, cannot be <c>null</c>.</param>
        /// <returns>Property value or <c>null</c> if property does not exist.</returns>
        object this[object key] { get; set; }
    }

    /// <summary>
    /// Manages list of properties, organized by a key.
    /// </summary>
    public struct PropertyCollection : IPropertyCollection
    {
        #region Fields & Properties

        /// <summary>
        /// Reference to actual collection of properties.
        /// </summary>
        /// <remarks>
        /// This mechanism saves memory for small properties sets.
        /// type of this object depends on amount of properties in the set.
        /// </remarks>
        private object _obj;

        /// <summary>
        /// Type of the hybrid table.
        /// </summary>
        private object _type;

        private static readonly object TypeHashtable = new object();
        private static readonly object TypeList = new object();
        
        /// <summary>
        /// If amount of properties exceeds this number, hashtable will be used instead of an array.
        /// </summary>
        private const int MaxListSize = 8;

        #endregion

        #region Nested class: DictionaryNode

        private sealed class DictionaryNode
        {
            public object key;
            public object value;
            public PropertyCollection.DictionaryNode next;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Sets property into the container.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        public void SetProperty(object key, object value)
        {
            CheckKey(key);

            //
            object p = _type;
            object o = _obj;

            // empty list
            if (p == null)
            {
                _type = key;
                _obj = value;
            }
            // one item list, with the same key
            else if (object.Equals(p, key))
            {
                _obj = value;
            }
            // linked list
            else if (object.ReferenceEquals(p, TypeList))
            {
                Debug.Assert(o is DictionaryNode);

                // replaces value if key already in collection,
                // counts items
                int count = 0;
                for (var node = (DictionaryNode)o; node != null; node = node.next)
                {
                    if (object.Equals(node.key, key))
                    {
                        node.value = value;
                        return;
                    }
                    count++;
                }

                // add new item
                if (count < MaxListSize)
                {
                    _obj = new DictionaryNode() { key = key, value = value, next = (DictionaryNode)o };
                }
                else
                {
                    // upgrade to hashtable
                    var hashtable = ToHashtable((DictionaryNode)o);
                    hashtable.Add(key, value);

                    _obj = hashtable;
                    _type = TypeHashtable;
                }
            }
            // hashtable
            else if (object.ReferenceEquals(p, TypeHashtable))
            {
                Debug.Assert(o is Hashtable);
                ((Hashtable)o)[key] = value;
            }
            // one item list,
            // upgrade to linked list
            else
            {
                _obj = new DictionaryNode()
                {
                    key = key,
                    value = value,
                    next = new DictionaryNode()
                    {
                        key = p,
                        value = o,
                        next = null,
                    }
                };
                _type = TypeList;
            }
        }

        /// <summary>
        /// Sets property into the container.
        /// </summary>
        public void SetProperty<T>(T value)
        {
            SetProperty(typeof(T), (object)value);
        }

        /// <summary>
        /// Tries to get property from the container.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Out. Value of the property.</param>
        /// <returns><c>true</c> if the property was found, otherwise <c>false</c>.</returns>
        public bool TryGetProperty(object key, out object value)
        {
            CheckKey(key);

            object p = _type;
            object o = _obj;

            // empty container
            if (p != null)
            {
                if (object.Equals(p, key))
                {
                    value = o;
                    return true;
                }
                else if (object.ReferenceEquals(p, TypeList))
                {
                    Debug.Assert(o is DictionaryNode);
                    for (var node = (DictionaryNode)o; node != null; node = node.next)
                        if (object.Equals(node.key, key))
                        {
                            value = node.value;
                            return true;
                        }
                }
                else if (object.ReferenceEquals(p, TypeHashtable))
                {
                    Debug.Assert(o is Hashtable);
                    value = ((Hashtable)o)[key];
                    return value != null || ((Hashtable)o).ContainsKey(key);
                }
            }

            // nothing found
            value = default(object);
            return false;
        }

        /// <summary>
        /// Tries to get property from the container.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns><c>null</c> or property value.</returns>
        public object GetProperty(object key)
        {
            object value;
            TryGetProperty(key, out value);
            return value;
        }

        /// <summary>
        /// Tries to get property from the container.
        /// </summary>
        public T GetProperty<T>()
        {
            return (T)GetProperty(typeof(T));
        }

        /// <summary>
        /// Tries to get property from the container.
        /// </summary>
        public bool TryGetProperty<T>(out T value)
        {
            object tmp;
            if (TryGetProperty(typeof(T), out tmp))
            {
                value = (T)tmp;
                return true;
            }

            value = default(T);
            return false;
        }

        /// <summary>
        /// Removes property from the container.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns><c>True</c> if property was found and removed. otherwise <c>false</c>.</returns>
        public bool RemoveProperty(object key)
        {
            CheckKey(key);

            var p = _type;
            var o = _obj;

            if (p != null)
            {
                if (object.Equals(p, key))
                {
                    _type = null;
                    _obj = null;
                    return true;
                }
                else if (object.ReferenceEquals(p, TypeList))
                {
                    Debug.Assert(o is DictionaryNode);
                    DictionaryNode prev = null;
                    for (var node = (DictionaryNode)o; node != null; node = node.next)
                    {
                        if (object.Equals(node.key, key))
                        {
                            if (prev == null)
                            {
                                if ((_obj = node.next) == null)
                                {
                                    _type = null;   // empty list
                                }
                            }
                            else
                            {
                                prev.next = node.next;
                            }
                            
                            return true;
                        }

                        //
                        prev = node;
                    }
                }
                else if (object.ReferenceEquals(p, TypeHashtable))
                {
                    Debug.Assert(o is Hashtable);
                    var hashtable = (Hashtable)o;
                    int count = hashtable.Count;
                    hashtable.Remove(key);
                    if (hashtable.Count != count)
                    {
                        if (hashtable.Count <= MaxListSize)
                        {
                            _obj = ToList(hashtable);
                            _type = TypeList;
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Removes property from the container.
        /// </summary>
        public bool RemoveProperty<T>()
        {
            return RemoveProperty(typeof(T));
        }

        /// <summary>
        /// Clears the container.
        /// </summary>
        public void ClearProperties()
        {
            _obj = _type = null;
        }

        /// <summary>
        /// Gets amount of properties in the container.
        /// </summary>
        public int Count
        {
            get
            {
                var p = _type;
                var o = _obj;

                if (p == null) return 0;
                if (object.ReferenceEquals(p, TypeList)) return CountItems((PropertyCollection.DictionaryNode)o);
                if (object.ReferenceEquals(p, TypeHashtable)) return ((Hashtable)o).Count;
                return 1;
            }
        }

        /// <summary>
        /// Gets or sets named property.
        /// </summary>
        /// <param name="key">Property key.</param>
        /// <returns>Property value or <c>null</c>.</returns>
        public object this[object key]
        {
            get
            {
                return this.GetProperty(key);
            }
            set
            {
                this.SetProperty(key, value);
            }
        }
        
        #endregion

        #region Helper functions

        private static void CheckKey(object key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
        }

        /// <summary>
        /// Counts items in the linked list.
        /// </summary>
        private static int CountItems(DictionaryNode head)
        {
            int count = 0;
            for (var p = head; p != null; p = p.next)
                count++;
            return count;
        }

        private static Hashtable/*!*/ToHashtable(DictionaryNode/*!*/node)
        {
            var hashtable = new Hashtable(13);

            for (var p = node; p != null; p = p.next)
                hashtable.Add(p.key, p.value);

            return hashtable;
        }
        private static DictionaryNode ToList(Hashtable/*!*/hashtable)
        {
            DictionaryNode list = null;
            foreach (var p in hashtable)
            {
                list = new DictionaryNode() { key = p.Key, value = p.Value, next = list };
            }
            return list;
        }

        #endregion
    }

    /// <summary>
    /// Helper reference object implementing <see cref="IPropertyCollection"/>
    /// </summary>
    [DebuggerDisplay("Count = {_properties.Count}")]
    public class PropertyCollectionClass : IPropertyCollection
    {
        /// <summary>
        /// Internbal collection (struct).
        /// </summary>
        private PropertyCollection _properties = new PropertyCollection();

        public object this[object key]
        {
            get
            {
                return _properties.GetProperty(key);
            }

            set
            {
                _properties.SetProperty(key, value);
            }
        }

        public void ClearProperties()
        {
            _properties.ClearProperties();
        }

        public object GetProperty(object key)
        {
            return _properties.GetProperty(key);
        }

        public T GetProperty<T>()
        {
            return _properties.GetProperty<T>();
        }

        public bool RemoveProperty(object key)
        {
            return _properties.RemoveProperty(key);
        }

        public bool RemoveProperty<T>()
        {
            return _properties.RemoveProperty<T>();
        }

        public void SetProperty(object key, object value)
        {
            _properties.SetProperty(key, value);
        }

        public void SetProperty<T>(T value)
        {
            _properties.SetProperty<T>(value);
        }

        public bool TryGetProperty(object key, out object value)
        {
            return _properties.TryGetProperty(key, out value);
        }

        public bool TryGetProperty<T>(out T value)
        {
            return _properties.TryGetProperty<T>(out value);
        }

        /// <summary>
        /// Gets value from collection. If value is not set yet, it is created using provided factory.
        /// </summary>
        public T GetOrCreateProperty<T>(Func<T>/*!*/factory)
        {
            T value;
            if (this.TryGetProperty<T>(out value) == false)
            {
                this.SetProperty<T>(value = factory());
            }

            return value;
        }
    }
}
