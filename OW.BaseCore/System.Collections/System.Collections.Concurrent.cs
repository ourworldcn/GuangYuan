using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Concurrent
{
    public class ConcurrentSet<T> : IProducerConsumerCollection<T>
    {
        HashSet<T> _Data = new HashSet<T>();
        #region IProducerConsumerCollection接口相关
        
        public int Count => _Data.Count;

        public bool IsSynchronized => true;

        public object SyncRoot => _Data;

        public void CopyTo(T[] array, int index)
        {
            _Data.CopyTo(array, index);
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _Data.GetEnumerator();
        }

        public T[] ToArray()
        {
            throw new NotImplementedException();
        }

        public bool TryAdd(T item)
        {
            throw new NotImplementedException();
        }

        public bool TryTake([MaybeNullWhen(false)] out T item)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
        #endregion IProducerConsumerCollection接口相关
    }
}