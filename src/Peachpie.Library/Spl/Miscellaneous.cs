using Pchp.Core;
using Pchp.Core.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Pchp.Library.Spl
{
    /// <summary>
    /// The SplObserver interface is used alongside <see cref="SplSubject"/> to implement the Observer Design Pattern.
    /// </summary>
    [PhpType("[name]"), PhpExtension("SPL")]
    public interface SplObserver
    {
        /// <summary>
        /// This method is called when any <see cref="SplSubject"/> to which the observer is attached calls <see cref="SplSubject.notify"/>.
        /// </summary>
        void update(SplSubject subject);
    }

    /// <summary>
    /// The SplSubject interface is used alongside <see cref="SplObserver"/> to implement the Observer Design Pattern.
    /// </summary>
    [PhpType("[name]"), PhpExtension("SPL")]
    public interface SplSubject
    {
        /// <summary>
        /// Attaches an <see cref="SplObserver"/> so that it can be notified of updates.
        /// </summary>
        void attach(SplObserver observer);

        /// <summary>
        /// Detaches an observer from the subject to no longer notify it of updates.
        /// </summary>
        void detach(SplObserver observer);

        /// <summary>
        /// Notifies all attached observers.
        /// </summary>
        void notify();
    }

    /// <summary>
    /// This class allows objects to work as arrays.
    /// </summary>
    [PhpType("[name]"), PhpExtension("SPL")]
    public class ArrayObject : IteratorAggregate, ArrayAccess, Serializable, Countable, IPhpEnumerable
    {
        #region Constants

        /// <summary>
        /// Properties of the object have their normal functionality when accessed as list (var_dump, foreach, etc.).
        /// </summary>
        public const int STD_PROP_LIST = 1;

        /// <summary>
        /// Entries can be accessed as properties (read and write).
        /// </summary>
        public const int ARRAY_AS_PROPS = 2;

        #endregion

        #region Fields & Properties

        readonly protected Context @_ctx;

        PhpArray _underlayingArray;
        object _underlayingObject;

        /// <summary>
        /// Name of the class instantiated by <see cref="getIterator"/>. The class must inherit from <see cref="Iterator"/>.
        /// Default value is <see cref="ArrayIterator"/>.
        /// </summary>
        string _iteratorClass;

        const string DefaultIteratorClass = "ArrayIterator";

        int _flags;

        PhpValue UnderlayingValue
        {
            get
            {
                return (_underlayingArray != null) ? PhpValue.Create(_underlayingArray) : PhpValue.FromClass(_underlayingObject);
            }
            set
            {
                //
                var arr = value.ArrayOrNull();
                if (arr != null)
                {
                    _underlayingArray = arr;
                    _underlayingObject = null;
                }
                else
                {
                    var obj = value.AsObject();
                    if (obj != null)
                    {
                        if (obj.GetType() == typeof(stdClass))
                        {
                            _underlayingArray = ((stdClass)obj).GetRuntimeFields();
                            _underlayingObject = null;
                        }
                        else
                        {
                            _underlayingArray = null;
                            _underlayingObject = obj;
                        }
                    }
                    else
                    {
                        throw new ArgumentException(nameof(value));
                    }
                }
            }
        }

        #endregion

        #region Construction

        /// <summary>
        /// Used in case user overrides the class in PHP and calls <see cref="__construct(PhpValue, int, string)"/> by itself.
        /// </summary>
        [PhpFieldsOnlyCtor]
        protected ArrayObject(Context ctx)
        {
            _ctx = ctx;
        }

        public ArrayObject(Context ctx, PhpValue input, int flags = 0, string iterator_class = null/*ArrayIterator*/)
            : this(ctx)
        {
            __construct(input, flags, iterator_class);
        }

        #endregion

        #region ArrayAccess

        public virtual bool offsetExists(PhpValue index)
        {
            if (_underlayingArray != null)
            {
                return _underlayingArray.ContainsKey(index.ToIntStringKey());
            }
            else
            {
                return Operators.PropertyExists(default(RuntimeTypeHandle), _underlayingObject, index);
            }
        }
        public virtual PhpValue offsetGet(PhpValue index)
        {
            if (_underlayingArray != null)
            {
                return _underlayingArray[index.ToIntStringKey()];
            }
            else
            {
                return Operators.PropertyGetValue(default(RuntimeTypeHandle), _underlayingObject, index);
            }
        }
        public virtual void offsetSet(PhpValue index, PhpValue newval)
        {
            if (_underlayingArray != null)
            {
                _underlayingArray[index.ToIntStringKey()] = newval.DeepCopy();
            }
            else
            {
                Operators.PropertySetValue(default(RuntimeTypeHandle), _underlayingObject, index, newval);
            }
        }
        public virtual void offsetUnset(PhpValue index)
        {
            if (_underlayingArray != null)
            {
                _underlayingArray.RemoveKey(index.ToIntStringKey());
            }
            else
            {
                Operators.PropertyUnset(default(RuntimeTypeHandle), _underlayingObject, index);
            }
        }

        #endregion

        #region Countable

        public virtual long count()
        {
            if (_underlayingArray != null)
            {
                // array size
                return _underlayingArray.Count;
            }
            else
            {
                // public (visible) instance properties + runtime fields
                return TypeMembersUtils.EnumerateVisibleInstanceFields(_underlayingObject).LongCount();
            }
        }

        #endregion

        #region Serializable

        public virtual PhpString serialize() { throw new NotImplementedException(); }
        public virtual void unserialize(PhpString serialized) { throw new NotImplementedException(); }

        #endregion

        #region IteratorAggregate

        public virtual Traversable getIterator()
        {
            if (_iteratorClass == null)
            {
                return new ArrayIterator(_ctx, UnderlayingValue);
            }
            else
            {
                return (Iterator)_ctx.Create(_iteratorClass, UnderlayingValue);
            }
        }

        #endregion

        #region IPhpEnumerable

        IPhpEnumerator IPhpEnumerable.IntrinsicEnumerator
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        IPhpEnumerator IPhpEnumerable.GetForeachEnumerator(bool aliasedValues, RuntimeTypeHandle caller)
        {
            throw new NotImplementedException();
        }

        #endregion

        public void __construct(PhpValue input, int flags = 0, string iterator_class = null/*ArrayIterator*/)
        {
            this.UnderlayingValue = input;
            this.setIteratorClass(iterator_class);
            this.setFlags(flags);
        }

        public virtual void __set(PhpValue prop, PhpValue value)
        {
            if (_underlayingArray != null)
            {
                if ((_flags & ARRAY_AS_PROPS) != 0)
                {
                    _underlayingArray[prop.ToIntStringKey()] = value.DeepCopy();
                }
                else
                {
                    // TODO: err
                }
            }
            else if (_underlayingObject != null)
            {
                Operators.PropertySetValue(default(RuntimeTypeHandle), _underlayingObject, prop, value);
            }
        }

        public virtual PhpValue __get(PhpValue prop)
        {
            if (_underlayingArray != null)
            {
                if ((_flags & ARRAY_AS_PROPS) != 0)
                {
                    return _underlayingArray[prop.ToIntStringKey()];
                }
            }
            else if (_underlayingObject != null)
            {
                return Operators.PropertyGetValue(default(RuntimeTypeHandle), _underlayingObject, prop);
            }

            // TODO: err
            return PhpValue.Void;
        }

        public string getIteratorClass() => _iteratorClass ?? DefaultIteratorClass;
        public void setIteratorClass(string iterator_class)
        {
            if (iterator_class == null || string.Equals(iterator_class, DefaultIteratorClass, StringComparison.OrdinalIgnoreCase))
            {
                iterator_class = null;
            }
            else
            {
                // TODO: check the class is valid and inherits from Iterator
            }

            _iteratorClass = iterator_class;
        }

        public int getFlags()
        {
            return _flags;
        }

        public void setFlags(int flags)
        {
            _flags = flags;
        }

        public void append(PhpValue value)
        {
            if (_underlayingArray != null)
            {
                _underlayingArray.Add(value);
            }
            else
            {
                PhpException.Throw(PhpError.E_RECOVERABLE_ERROR, "Cannot append properties to objects, use %s::offsetSet() instead");   // TODO: Resources
            }
        }
        public PhpValue exchangeArray(PhpValue input)
        {
            var oldvalue = this.UnderlayingValue;
            this.UnderlayingValue = input;

            //
            return oldvalue;
        }
        public PhpArray getArrayCopy()
        {
            if (_underlayingArray != null)
            {
                // array size
                return _underlayingArray.DeepCopy();
            }
            else
            {
                // public (visible) instance properties + runtime fields
                return new PhpArray(TypeMembersUtils.EnumerateVisibleInstanceFields(_underlayingObject));
            }
        }

        public void asort() { throw new NotImplementedException(); }
        public void ksort() { throw new NotImplementedException(); }
        public void natcasesort() { throw new NotImplementedException(); }
        public void natsort() { throw new NotImplementedException(); }
        public void uasort(IPhpCallable cmp_function) { throw new NotImplementedException(); }
        public void uksort(IPhpCallable cmp_function) { throw new NotImplementedException(); }
    }
}
