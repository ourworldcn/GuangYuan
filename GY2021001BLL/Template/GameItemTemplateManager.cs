using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using Microsoft.EntityFrameworkCore;
using OW.Game;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
        #endregion 属性及相关

        private void Initialize()
        {
            _Id2Template = new Lazy<ConcurrentDictionary<Guid, GameItemTemplate>>(() =>
            {
                var db = TemplateContext;
                return new ConcurrentDictionary<Guid, GameItemTemplate>(db.ItemTemplates.AsNoTracking().ToDictionary(c => c.Id));
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
            templat.TryGetPropertyValue("bd", out var sdObj) && OwHelper.TryGetDecimal(sdObj, out var sd) ? new decimal?(sd) : null;

        /// <summary>
        /// 获取这个模板指出的物品是否是免费的（钻石计价）。
        /// </summary>
        /// <param name="templat"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFree(this GameItemTemplate templat) =>
            !templat.TryGetPropertyValue("bd", out var sdObj) || !OwHelper.TryGetDecimal(sdObj, out var sd) || sd <= 0;

    }

    /// <summary>
    /// 升级数据工作的数据块。
    /// </summary>
    public class LevelUpDatas : GameCharWorkDataBase
    {

        public LevelUpDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public LevelUpDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public LevelUpDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        private GameItem _GameItem;

        /// <summary>
        /// 要升级的物品。
        /// </summary>
        public GameItem GameItem { get => _GameItem; set => _GameItem = value; }

        /// <summary>
        /// 获取物品升级所需的代价。
        /// </summary>
        /// <returns>升级需要的资源及消耗量，null表示不可升级。</returns>
        public List<(GameItem, decimal)> GetCost()
        {
            var lv = GameItem.Properties.GetDecimalOrDefault(ProjectConstant.LevelPropertyName);
            
            var result = new List<(GameItem, decimal)>();

            var dic = GameItem.Properties;
            var bag = GameChar.GetCurrencyBag();

            var dia = dic.GetDecimalOrDefault("lud", decimal.Zero); //钻石
            if (dia != decimal.Zero)   //若有钻石消耗
            {
                var gi = GameChar.GetZuanshi();
                result.Add((gi, dia));
            }

            var gold = dic.GetDecimalOrDefault("lug");  //金币
            if (gold != decimal.Zero)   //若有金币消耗
            {
                var gi = GameChar.GetJinbi();
                result.Add((gi, gold));
            }

            var wood = dic.GetDecimalOrDefault("luw");  //木材
            if (wood != decimal.Zero)   //若有金币消耗
            {
                var gi = GameChar.GetMucai();
                result.Add((gi, wood));
            }
            return result;
        }

        /// <summary>
        /// 消耗资源。
        /// </summary>
        /// <param name="cost"></param>
        /// <returns>true,消耗资源成功，false至少有一种资源不足。</returns>
        public bool Deplete(List<(GameItem, decimal)> cost)
        {
            var errItem = cost.FirstOrDefault(item => item.Item1.Count + item.Item2 < 0);
            if (null != errItem.Item1)  //若有资源不足
            {
                VWorld.SetLastError(ErrorCodes.RPC_S_OUT_OF_RESOURCES);
                VWorld.SetLastErrorMessage($"{errItem.Item1.Template.DisplayName} 不足。");
                return false;
            }
            cost.ForEach(c => c.Item1.Count += c.Item2);    //应用资源损耗
            return true;
        }

        /// <summary>
        /// 获取升级所需的时间。
        /// <see cref="TimeSpan.Zero"/>表示升级立即完成。
        /// </summary>
        /// <returns></returns>
        public TimeSpan GetColdown()
        {
            return TimeSpan.FromSeconds((double)GameItem.Properties.GetDecimalOrDefault("lut"));
        }

        internal void Apply(GameChar gameChar, IResultWorkData datas)
        {
            throw new NotImplementedException();
        }
    }
}
