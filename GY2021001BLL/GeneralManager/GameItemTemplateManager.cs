using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using Microsoft.EntityFrameworkCore;
using OW.Game;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace GuangYuan.GY001.BLL
{
    public class GameItemTemplateManagerOptions
    {
        public GameItemTemplateManagerOptions()
        {

        }

        /// <summary>
        /// 当模板加载后调用该委托。
        /// </summary>
        public Func<DbContext, bool> Loaded { get; set; }

        public string TestString { get; set; }
    }

    public class GameItemTemplateManager : GameManagerBase<GameItemTemplateManagerOptions>
    {
        #region 构造函数

        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameItemTemplateManager()
        {
            Initialize();
        }

        public GameItemTemplateManager(IServiceProvider service) : base(service)
        {
            Initialize();
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="service">所使用的服务容器。</param>
        public GameItemTemplateManager(IServiceProvider service, GameItemTemplateManagerOptions options) : base(service, options)
        {
            Initialize();
        }
        #endregion 构造函数

        #region 属性及相关

        private GY001TemplateContext _TemplateContext;

        /// <summary>
        /// 使用该上下文加载所有模板对象，以保证其单例性。
        /// </summary>
        protected GY001TemplateContext TemplateContext
        {
            get
            {
                lock (ThisLocker)
                    return _TemplateContext ??= World.CreateNewTemplateDbContext();
            }
        }

        private Lazy<ConcurrentDictionary<Guid, GameItemTemplate>> _Id2Template;

        /// <summary>
        /// 所有模板的字典。键是模板Id,值是模板对象。
        /// </summary>
        public ConcurrentDictionary<Guid, GameItemTemplate> Id2Template
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _Id2Template.Value;
        }

        ConcurrentDictionary<Guid, GameItemTemplate> _HeadTemplates;
        /// <summary>
        /// 获取所有坐骑/野兽头的模板。
        /// </summary>
        public ConcurrentDictionary<Guid, GameItemTemplate> HeadTemplates
        {
            get
            {
                if (_HeadTemplates is null)
                    lock (ThisLocker)
                        if (_HeadTemplates is null)
                        {
                            _HeadTemplates = new ConcurrentDictionary<Guid, GameItemTemplate>(Id2Template.Values.Where(c => c.CatalogNumber == 3).ToDictionary(c => c.Id));
                        }
                return _HeadTemplates;
            }
        }

        ConcurrentDictionary<Guid, GameItemTemplate> _BodyTemplates;
        /// <summary>
        /// 获取所有坐骑/野兽身体的模板。
        /// </summary>
        public ConcurrentDictionary<Guid, GameItemTemplate> BodyTemplates
        {
            get
            {
                if (_BodyTemplates is null)
                    lock (ThisLocker)
                        if (_BodyTemplates is null)
                        {
                            _BodyTemplates = new ConcurrentDictionary<Guid, GameItemTemplate>(Id2Template.Values.Where(c => c.CatalogNumber == 4).ToDictionary(c => c.Id));
                        }
                return _BodyTemplates;
            }
        }

        Lazy<ConcurrentDictionary<Guid, GameCardPoolTemplate>> _Id2CardPool;
        /// <summary>
        /// 获取卡池设置数据。
        /// </summary>
        public ConcurrentDictionary<Guid, GameCardPoolTemplate> Id2CardPool => _Id2CardPool.Value;

        private Lazy<ConcurrentDictionary<Guid, GameShoppingTemplate>> _Id2Shopping;
        /// <summary>
        /// 获取商城设置数据。
        /// </summary>
        public ConcurrentDictionary<Guid, GameShoppingTemplate> Id2Shopping => _Id2Shopping.Value;

        private Lazy<ConcurrentDictionary<Guid, GameMissionTemplate>> _Id2Mission;
        /// <summary>
        /// 任务定义模板。
        /// </summary>
        public ConcurrentDictionary<Guid, GameMissionTemplate> Id2Mission => _Id2Mission.Value;

        #endregion 属性及相关

        private void Initialize()
        {
            _Id2Template = new Lazy<ConcurrentDictionary<Guid, GameItemTemplate>>(() =>
            {
                var db = TemplateContext;
                return new ConcurrentDictionary<Guid, GameItemTemplate>(db.ItemTemplates.AsNoTracking().ToDictionary(c => c.Id));
            }, LazyThreadSafetyMode.ExecutionAndPublication);
            _Id2Shopping = new Lazy<ConcurrentDictionary<Guid, GameShoppingTemplate>>(() =>
            {
                using var db = World.CreateNewTemplateDbContext();
                return new ConcurrentDictionary<Guid, GameShoppingTemplate>(db.ShoppingTemplates.AsNoTracking().ToDictionary(c => c.Id));
            }, LazyThreadSafetyMode.ExecutionAndPublication);
            _Id2Mission = new Lazy<ConcurrentDictionary<Guid, GameMissionTemplate>>(() =>
            {
                using var db = World.CreateNewTemplateDbContext();
                return new ConcurrentDictionary<Guid, GameMissionTemplate>(db.MissionTemplates.AsNoTracking().ToDictionary(c => c.Id));
            }, LazyThreadSafetyMode.ExecutionAndPublication);
            _Id2CardPool = new Lazy<ConcurrentDictionary<Guid, GameCardPoolTemplate>>(() =>
            {
                using var db = World.CreateNewTemplateDbContext();
                return new ConcurrentDictionary<Guid, GameCardPoolTemplate>(db.CardPoolTemplates.AsNoTracking().ToDictionary(c => c.Id));
            }, LazyThreadSafetyMode.ExecutionAndPublication);
            //_Id2RequireLevel = new Lazy<ILookup<Guid, GameItemTemplate>>(() =>
            //{
            //    const string rqlv = "rqlv";
            //    var coll = from tmp in Id2Template.Values
            //               let keyName = tmp.Properties.Keys.FirstOrDefault(c => c.StartsWith(rqlv))
            //               where !string.IsNullOrEmpty(keyName) && Guid.TryParse(keyName[rqlv.Length..], out _)
            //               select (tid: Guid.Parse(keyName[rqlv.Length..]), tmp);
            //    var result = coll.ToLookup(c => c.tid, c => c.tmp);
            //    return result;
            //}, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        /// 按指定Id获取模板对象。
        /// </summary>
        /// <param name="id"></param>
        /// <returns>没有找到则返回null</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public GameItemTemplate GetTemplateFromeId(Guid id) => Id2Template.GetValueOrDefault(id, null);

        /// <summary>
        /// 获取符合条件的一组模板。
        /// </summary>
        /// <param name="conditional"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public IEnumerable<GameItemTemplate> GetTemplates(Func<GameItemTemplate, bool> conditional) => Id2Template.Values.Where(c => conditional(c));

        /// <summary>
        /// 获取指定名字序列属性的索引属性名，如果没有找到则考虑使用lv。
        /// </summary>
        /// <param name="template"></param>
        /// <param name="seqPropName">序列属性的名称。</param>
        /// <returns>null如果没有找到指定的<paramref name="seqPropName"/>名称的属性或，该属性不是序列属性。</returns>
        public string GetIndexPropName(GameItemTemplate template, string seqPropName)
        {
            if (!template.Properties.TryGetValue(seqPropName, out object obj) || !(obj is decimal[]))
                return null;
            var pn = $"{World.PropertyManager.LevelPropertyName}{seqPropName}";
            if (template.Properties.ContainsKey(pn))
                return pn;
            return World.PropertyManager.LevelPropertyName;
        }

    }

    public static class GameItemTemplateManagerExtensions
    {

    }

}
