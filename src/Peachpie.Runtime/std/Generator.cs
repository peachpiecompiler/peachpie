using Pchp.Core.Reflection;
using System;

namespace Pchp.Core.std
{
    [PhpType("Generator")]
    public class Generator : Iterator
    {
        readonly protected Context _ctx;

        readonly PhpArray _local;
        readonly internal RoutineInfo routine;

        //Might get replaced by _state logic
        bool _runToFirstYield = false;
        bool _runAfterFirstYield = false;

        /// <summary>
        /// State of the state machine
        ///  -  0: before first yield
        ///  - -1: running
        ///  - -2: closed
        ///  - +x: valid state
        /// </summary>
        internal int _state = 0;

        int _currNumericalKey = 0;
        internal PhpValue _currValue, _currKey, _currSendItem, _returnValue;
        internal Exception _currException;

        public void rewind()
        {
            checkIfRunToFirstYield();
            if (_runAfterFirstYield) { throw new Exception("Cannot rewind a generator that was already run"); }
        }

        public void next()
        {
            if(!valid()) { return; }

            checkIfMovingFromFirstYeild();
            checkIfRunToFirstYield();

            routine.PhpCallable(_ctx, PhpValue.FromClass(this));
            throw new NotImplementedException();
        }

        public bool valid()
        {
            checkIfRunToFirstYield();
            return (_state >= 0);
        }

        public PhpValue key()
        {
            checkIfRunToFirstYield();
            return _currKey;
        }

        public PhpValue current()
        {
            checkIfRunToFirstYield();
            return _currValue;
        }

        public PhpValue getReturn()
        {
            if (_state != -2) { throw new Exception("Cannot get return value of a generator that hasn't returned"); }
            return _returnValue;
        }

        public PhpValue send(PhpValue value)
        {
            checkIfRunToFirstYield();

            _currSendItem = value;
            next();
            _currSendItem = PhpValue.Null;

            return current();
        }

        public PhpValue @throw(Exception ex)
        {
            if(!valid()) { throw ex; }

            _currException = ex;
            next();
            _currException = null;

            return current();
        }

        private void checkIfMovingFromFirstYeild()
        {
            if (_runToFirstYield && !_runAfterFirstYield) { _runAfterFirstYield = true; }
        }


        private void checkIfRunToFirstYield()
        {
            if(!_runToFirstYield) { _runToFirstYield = true; this.next(); }
            
        }

    }
}
