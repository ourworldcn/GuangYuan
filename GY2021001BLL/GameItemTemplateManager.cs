using Gy2021001Template;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OwGame;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GY2021001BLL
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

        private GameTemplateContext _TemplateContext;

        /// <summary>
        /// 使用该上下文加载所有模板对象，以保证其单例性。
        /// </summary>
        protected GameTemplateContext TemplateContext
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
            if (!template.Properties.TryGetValue(seqPropName, out object obj) || !(obj is decimal[] seq))
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
        static public decimal? GetPriceWithDiamond(this GameItemTemplate templat) =>
            templat.TryGetPropertyValue("sd", out var sdObj) && OwHelper.TryGetDecimal(sdObj, out var sd) ? new decimal?(sd) : null;

        /// <summary>
        /// 获取这个模板指出的物品是否是免费的（钻石计价）。
        /// </summary>
        /// <param name="templat"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsFree(this GameItemTemplate templat) =>
            !templat.TryGetPropertyValue("sd", out var sdObj) || !OwHelper.TryGetDecimal(sdObj, out var sd) || sd <= 0;

    }
}
