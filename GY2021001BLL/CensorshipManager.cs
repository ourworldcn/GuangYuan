using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GuangYuan.GY001.BLL
{
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class CensorshipManager
    {
        public CensorshipManager()
        {
            Initialize();
        }

        HashSet<string> _Datas;

        public void Initialize()
        {
            var dir = AppContext.BaseDirectory;
            var path = Path.Combine(dir, "敏感词库.txt");
            using var stream = File.OpenRead(path);
            using var sr = new StreamReader(stream);
            var guts = sr.ReadToEnd();
            var ary = guts.Split(Environment.NewLine);
            var coll = ary.Skip(1).Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Split('\t')?[1]).Where(c => !string.IsNullOrWhiteSpace(c));

            _Datas = new HashSet<string>(coll);
        }

        public bool HasKey(string str)
        {
            return _Datas.Any(c => str.Contains(c));
        }
    }
}
