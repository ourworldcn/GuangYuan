using GuangYuan.GY001.BLL;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OW.Game.Item;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace OW.Game.Mission
{
    /// <summary>
    /// 任务成就管理器的配置数据类。
    /// </summary>
    public class GameMissionManagerOptions
    {
        public GameMissionManagerOptions()
        {

        }
    }

    /// <summary>
    /// 成就的辅助视图数据对象。
    /// </summary>
    public class GameaChieveView
    {
        /// <summary>
        /// 送物品的前缀。
        /// </summary>
        private const string TidPrefix = "mtid";

        public Guid Id { get => _Template.Id; }

        public GameaChieveView(VWorld world, GameItemTemplate template)
        {
            _Template = template;
            _World = world;
        }

        private readonly VWorld _World;

        /// <summary>
        /// 存储模板对象。
        /// </summary>
        private readonly GameItemTemplate _Template;

        private List<(decimal, GameItem)> _Metrics;
        /// <summary>
        /// 指标值的数据。按 Item1升序排序。
        /// Item1=指标值。Item2=送的物品对象。
        /// </summary>
        public List<(decimal, GameItem)> Metrics
        {
            get
            {
                if (_Metrics is null)
                    lock (this)
                        if (_Metrics is null)
                        {
                            var gim = _World.ItemManager;
                            //Factory
                            var vals = _Template.Properties.Keys.Where(c => c.StartsWith(TidPrefix)).Select(c => decimal.Parse(c[TidPrefix.Length..])); //指标值的集合
                            _Metrics = new List<(decimal, GameItem)>();
                            Dictionary<string, object> dic = new Dictionary<string, object>();
                            foreach (var item in vals)  //对每个指标值给出辅助元组
                            {
                                dic["tid"] = _Template.Properties[$"mtid{item}"];
                                dic["count"] = _Template.Properties[$"mcount{item}"];
                                if (_Template.Properties.TryGetValue($"mhtid{item}", out var mhtid))  //若有头模板id
                                {
                                    dic["htid"] = mhtid;
                                    dic["mbtid"] = _Template.Properties[$"mbtid{item}"];    //必须有身体模板id
                                }
                                var gi = gim.CreateItemFromDictionary(dic); //创建对象
                                _Metrics.Add((item, gi));
                            }
                            _Metrics.Sort(Comparer<(decimal, GameItem)>.Create((l, r) => decimal.Compare(l.Item1, r.Item1)));
                        }
                return _Metrics;
            }
        }

        /// <summary>
        /// 返回指定指标值，属于的成就项的从零开始的索引；没有找到则为 -1。
        /// </summary>
        /// <param name="count">指标值。</param>
        /// <returns></returns>
        public int GetIndex(decimal count)
        {
            return Metrics.FindLastIndex(c => c.Item1 < count);
        }
    }

    /// <summary>
    /// 任务/成就管理器。
    /// </summary>
    public class GameMissionManager : GameManagerBase<GameMissionManagerOptions>
    {
        public GameMissionManager()
        {
            Initialize();
        }

        public GameMissionManager(IServiceProvider service) : base(service)
        {
            Initialize();
        }

        public GameMissionManager(IServiceProvider service, GameMissionManagerOptions options) : base(service, options)
        {
            Initialize();
        }

        private void Initialize()
        {
            _MissionTemplates = new Lazy<List<GameItemTemplate>>(() => World.ItemTemplateManager.GetTemplates(c => c.CatalogNumber == 51).ToList(),
                LazyThreadSafetyMode.ExecutionAndPublication);


        }

        private Lazy<List<GameItemTemplate>> _MissionTemplates;
        /// <summary>
        /// 与成就任务相关的所有模板。
        /// </summary>
        public List<GameItemTemplate> MissionTemplates => _MissionTemplates.Value;

        private Dictionary<Guid, GameaChieveView> _TId2Views;

        public Dictionary<Guid, GameaChieveView> TId2Views
        {
            get
            {
                if (_TId2Views is null)
                    lock (ThisLocker)
                        _TId2Views ??= MissionTemplates.Select(c => new GameaChieveView(World, c)).ToDictionary(c => c.Id);
                return _TId2Views;
            }
        }

        /// <summary>
        /// 扫描发生变化的任务数据。
        /// </summary>
        /// <param name="gChar"></param>
        /// <param name="tid">相关的任务/成就模板Id。</param>
        /// <param name="newCount">新的指标值。</param>
        /// <returns>成功更新了成就的指标值则返回true,false没有找到指标值或指标值没有变化。</returns>
        public bool SetMetricsChange(GameChar gChar, Guid tid, decimal newCount)
        {
            var result = false;
            var logger = Service.GetService<ILogger<GameMissionManager>>();
            var gu = gChar?.GameUser;
            using var dwUser = World.CharManager.LockAndReturnDisposer(gu);
            if (dwUser is null)
            {
                logger?.LogWarning($"无法扫描指定角色的成就变化数据,ErrorCode={VWorld.GetLastError()},ErrorMessage={VWorld.GetLastErrorMessage()}");
                return false;
            }
            var slot = gChar.GetRenwuSlot();    //任务/成就槽对象
            var missionObj = slot.Children.FirstOrDefault(c => c.TemplateId == tid);    //任务/成就的数据对象
            if (missionObj.Count != newCount)   //若确实发生变化了
            {
                result = SetNewValue(slot, tid, newCount);
            }
            if (true) //若不认识该槽
            {

            }
            return result;
        }

        /// <summary>
        /// 获取成就的奖励。
        /// </summary>
        /// <param name="datas"></param>
        /// <returns></returns>
        public void GetRewarding(GetRewardingDatas datas)
        {
            using var dwUser = datas.LockUser();
            if (dwUser is null)
            {
                datas.HasError = true;
                datas.FillErrorFromWorld();
                return;
            }
            var slot = datas.GameChar.GetRenwuSlot();
            var coll = from id in datas.ItemIds
                       join obj in slot.Children
                       on id equals obj.Id
                       select obj;
            var objs = coll.ToList();
            if (objs.Count != datas.ItemIds.Count()) //若长度不等
            {
                datas.HasError = true;
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.ErrorMessage = "至少有一个成就找不到对应的对象。";
                return;
            }
            var obj_keys = (from tmp in objs    //工作数据，Item1是成就对象，Item2是槽扩展属性的键名，Item3=槽扩展属性的值
                            let keyName = $"mcid{tmp.Id}"
                            select (tmp, keyName, slot.Properties.GetStringOrDefault(keyName))).ToArray();
            if (obj_keys.Any(c => string.IsNullOrWhiteSpace(c.Item3)))
            {
                datas.HasError = true;
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.ErrorMessage = "至少有一个成就没有可以领取的奖励。";
                return;
            }
            var gim = World.ItemManager;
            foreach (var item in obj_keys)  //遍历每个要领取奖励的成就对象
            {
                var zhibiaos = item.Item3.Split(OwHelper.SemicolonArrayWithCN, StringSplitOptions.RemoveEmptyEntries).Select(c => decimal.Parse(c));  //可领取的成就指标
                var view = TId2Views[item.tmp.TemplateId];
                foreach (var zhibiao in zhibiaos)   //对每个完成的成就获取物品
                {
                    var tuple = view.Metrics.First(c => c.Item1 == zhibiao);
                    var gi = gim.Clone(tuple.Item2);  //复制新物品
                    ;//自动加入对应容器
                }
            }
            return;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="missionSlot">任务/成就槽对象。</param>
        /// <param name="tid">任务/成就模板Id。</param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        private bool SetNewValue(GameItem missionSlot, Guid tid, decimal newValue)
        {
            //mcid5af7a4f2-9ba9-44e0-b368-1aa1bd9aed6d=10;50;100,...
            var mObj = missionSlot.Children.FirstOrDefault(c => c.TemplateId == tid);   //任务/成就对象
            var keyName = $"mcid{mObj.Id}"; //键名
            var template = missionSlot.Template;    //模板数据
            var oldVal = missionSlot.Properties.GetStringOrDefault(keyName);   //原值
            var lst = oldVal.Split(OwHelper.SemicolonArrayWithCN, StringSplitOptions.RemoveEmptyEntries).Select(c => decimal.Parse(c)).ToList(); //级别完成且未领取的指标值。
            if (!TId2Views.TryGetValue(tid, out var view))
            {
                throw new InvalidOperationException("找不到指定模板id的对象。");
            }
            var coll = view.Metrics.Where(c => newValue >= c.Item1).Select(c => c.Item1).Except(lst).ToArray(); //所有应新加入的值
            if (coll.Length > 0)   //若确实有新成就
            {
                lst.AddRange(coll);
                missionSlot.Properties[keyName] = string.Join(';', lst.Select(c => c.ToString()));
                return true;
            }
            else
                return false;
        }

    }

    /// <summary>
    /// 与任务/成就系统相关的扩展方法封装类。
    /// </summary>
    public static class GameMissionExtensions
    {
        /// <summary>
        /// 获取任务/成就槽。
        /// </summary>
        /// <param name="gChar"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameItem GetRenwuSlot(this GameChar gChar) => gChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.RenwuSlotTId);
    }

    /// <summary>
    /// 
    /// </summary>
    public class GetRewardingDatas : ChangeItemsWorkDatasBase
    {
        public GetRewardingDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public GetRewardingDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public GetRewardingDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        public IEnumerable<Guid> ItemIds { get; set; }
    }

}
