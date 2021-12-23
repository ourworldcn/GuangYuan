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

        private Lazy<ConcurrentDictionary<Guid, GameShoppingTemplate>> _Id2Shopping;

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
            var pn = $"{ProjectConstant.LevelPropertyName}{seqPropName}";
            if (template.Properties.ContainsKey(pn))
                return pn;
            return ProjectConstant.LevelPropertyName;
        }

    }

    public static class GameItemTemplateManagerExtensions
    {
        /// <summary>
        /// 获取该物品的价格，指钻石计价的价格。
        /// </summary>
        /// <param name="templat"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal? GetPriceWithDiamond(this GameItemTemplate templat) =>
            templat.TryGetPropertyValue("bd", out var sdObj) && OwConvert.TryGetDecimal(sdObj, out var sd) ? new decimal?(sd) : null;

        /// <summary>
        /// 获取这个模板指出的物品是否是免费的（钻石计价）。
        /// </summary>
        /// <param name="templat"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFree(this GameItemTemplate templat) =>
            !templat.TryGetPropertyValue("bd", out var sdObj) || !OwConvert.TryGetDecimal(sdObj, out var sd) || sd <= 0;

    }

}
