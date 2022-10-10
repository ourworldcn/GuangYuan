using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OW.Microsoft.Extensions.ObjectPool
{
    public class CollectionPool<T> : DefaultObjectPool<T> where T : class
    {
        public CollectionPool(IPooledObjectPolicy<T> policy) : base(policy)
        {
        }

        public CollectionPool(IPooledObjectPolicy<T> policy, int maximumRetained) : base(policy, maximumRetained)
        {
        }

        public override void Return(T obj)
        {
            base.Return(obj);
        }
    }
}
