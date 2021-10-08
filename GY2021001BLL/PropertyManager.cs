using GuangYuan.GY001.TemplateDb;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OW.Game
{
    public class PropertyManagerOptions
    {
        public PropertyManagerOptions()
        {

        }
    }

    /// <summary>
    /// 属性管理器。
    /// </summary>
    public class PropertyManager : GameManagerBase<PropertyManagerOptions>, IGamePropertyManager
    {
        public PropertyManager()
        {
            Initializer();
        }

        public PropertyManager(IServiceProvider service) : base(service)
        {
            Initializer();
        }

        public PropertyManager(IServiceProvider service, PropertyManagerOptions options) : base(service, options)
        {
            Initializer();
        }

        private void Initializer()
        {
            _Alls = new Lazy<Dictionary<string, GamePropertyTemplate>>(() =>
            {
                using var db = World.CreateNewTemplateDbContext();
                return db.Set<GamePropertyTemplate>().AsNoTracking().ToDictionary(c => c.PName);
            }, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="pNmaes"></param>
        /// <returns></returns>
        public IEnumerable<string> Filter(IEnumerable<string> pNmaes) =>
            pNmaes.Where(c => !NoCopyNames.Contains(c) && !NoCopyPrefixNames.Any(c1 => c.StartsWith(c1))   //寻找匹配的设置项
            );

        public IEnumerable<KeyValuePair<string, object>> Filter(IReadOnlyDictionary<string, object> dic)
        {
            return dic.Where(c => !NoCopyNames.Contains(c.Key) && !NoCopyPrefixNames.Any(c1 => c.Key.StartsWith(c1)));
        }

        private Lazy<Dictionary<string, GamePropertyTemplate>> _Alls;

        public IReadOnlyDictionary<string, GamePropertyTemplate> Id2Datas => _Alls.Value;

        private HashSet<string> _NoCopyNames;

        /// <summary>
        /// 不必复制的属性全名集合。
        /// </summary>
        public ISet<string> NoCopyNames => _NoCopyNames ??= new HashSet<string>(Id2Datas.Values.Where(c => c.IsFix && !c.IsPrefix).Select(c => string.IsNullOrEmpty(c.FName) ? c.PName : c.FName));

        private HashSet<string> _NoCopyPrefixNames;
        /// <summary>
        /// 不必复制的属性属性名前缀集合。
        /// </summary>
        public ISet<string> NoCopyPrefixNames => _NoCopyPrefixNames ??= new HashSet<string>(Id2Datas.Values.Where(c => c.IsFix && c.IsPrefix).Select(c => string.IsNullOrEmpty(c.FName) ? c.PName : c.FName));

    }

}
