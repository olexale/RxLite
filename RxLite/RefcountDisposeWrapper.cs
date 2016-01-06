using System;
using System.Threading;

namespace RxLite
{
    internal sealed class RefcountDisposeWrapper
    {
        private IDisposable _inner;
        private int _refCount = 1;

        public RefcountDisposeWrapper(IDisposable inner)
        {
            _inner = inner;
        }

        public void AddRef()
        {
            Interlocked.Increment(ref _refCount);
        }

        public void Release()
        {
            if (Interlocked.Decrement(ref _refCount) == 0)
            {
                var inner = Interlocked.Exchange(ref _inner, null);
                inner.Dispose();
            }
        }
    }
}