using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace OW
{
    public class StringLocker
    {

        public StringLocker()
        {

        }

        ConcurrentDictionary<(string, StringComparer), HashSet<string>> _Datas = new ConcurrentDictionary<(string, StringComparer), HashSet<string>>();


        public bool TryEnter(ref string str, string region, StringComparer comparer, TimeSpan timeout)
        {
            var hs = _Datas.GetOrAdd((region, comparer), c => new HashSet<string>(comparer));
            lock (hs)
                if (hs.TryGetValue(str, out str))
                {

                }
                else
                {
                    hs.Add(str);
                }
            return Monitor.TryEnter(str, timeout);
        }

        public void Exit(string str, string region, StringComparer comparer)
        {

        }
    }
}
