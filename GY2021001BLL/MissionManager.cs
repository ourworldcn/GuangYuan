using Game.Social;
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
using System.Threading.Tasks;

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
        /// 异步扫描成就的变化情况。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        public Task<bool> ScanAsync(GameChar gameChar)
        {
            return Task.Factory.StartNew(ScanCore, gameChar);
        }

        /// <summary>
        /// 扫描成就变化的函数。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        private bool ScanCore(object gameChar)
        {
            bool result = false;
            var gc = gameChar as GameChar;
            var gu = gc?.GameUser;
            if (gu is null)
                return result;
            using var dwUser = World.CharManager.LockAndReturnDisposer(gu);
            if (dwUser is null)
                return result;
            /*等级成就
玩家等级达到LV1|LV2|LV3|LV4|LV5|LV6|LV7|lv8|lv9|lv10*/

            /*坐骑等级成就
玩家提升坐骑等级最高达到
LV2|LV3|LV5|LV7|LV10|LV12|LV14|LV16|LV18|LV20*/

            /*满级坐骑数量成就
拥有LV20的坐骑数量1，2，3，4，6，8，10，15，20，25*/

            /*战力成就
总战力达到10000，20000，30000，40000，50000，60000，70000，80000，100000，120000*/

            /*获取坐骑成就
获得坐骑（含杂交）1只，4只，10只，16只，28只，50只，100只，200只，300只，400只*/

            /*纯种坐骑获取成就（不含杂交）
获得纯种坐骑1只，2只，3只，4只，5只，8只，10只，12只，14只，17只*/

            /*孵化成就
成功孵化次数1次，5次，10次，15次，20次，30次，50次，100次，200次，400次*/

            /*坐骑资质成就
最高拥有坐骑资质总和达到，60，90，120，150，180，210，250，270，290，300*/

            /*神纹成就
最高拥有神纹等级达到10，20，30，40，50，60，70，80，90，100*/

            /*神纹突破成就
神纹累计突破次数达到：1次，3次，5次，10次，27次，54次，81次，108次，135次，216次*/

            /*访问好友天次成就
累计访问好友家园1天，2天，3天，5天，7天，10天，15天，20天，30天，60天*/

            /*关卡成就
打通大章1，大章2，3，4，5，6，7，8，9，10*/

            /*累计进行塔防模式次数
5次，10次，20次，30次,50次，70次，100次，130次，160次，280次*/

            /*PVP进攻成就
PVP进攻获胜1次，3次，5次，10次，20次，50次，100次，200次，300次，500次*/

            /*PVP防御
PVP防御获胜1次，3次，5次，10次，20次，50次，100次，200次，300次，500次*/

            /*助战成就
PVP助战获胜1次，2次，3次，5次，10次，20次，50次，100次，150次，200次*/

            /*炮塔成就
最高级别炮塔等级1，2，3，4，5，6，7，8，9，10*/

            /*陷阱成就 最高级别陷阱等级1，2，3，4，5，6，7，8，9，10*/

            /*旗帜成就
最高级别旗帜等级1，2，3，4，5，6，7，8，9，10*/

            /*方舟等级成就
最高级别主基地等级1，2，3，4，5，6，7，8，9，10*/
            return result;
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
            List<GameItem> remainder = new List<GameItem>();
            foreach (var item in obj_keys)  //遍历每个要领取奖励的成就对象
            {
                var zhibiaos = item.Item3.Split(OwHelper.SemicolonArrayWithCN, StringSplitOptions.RemoveEmptyEntries).Select(c => decimal.Parse(c));  //可领取的成就指标
                var view = TId2Views[item.tmp.TemplateId];
                foreach (var zhibiao in zhibiaos)   //对每个完成的成就获取物品
                {
                    remainder.Clear();
                    var tuple = view.Metrics.First(c => c.Item1 == zhibiao);
                    var gi = gim.Clone(tuple.Item2);  //复制新物品
                    var container = gim.GetDefaultContainer(datas.GameChar, gi);
                    gim.AddItem(gi, container, remainder, datas.ChangeItems);//自动加入对应容器
                    if (remainder.Count > 0)
                    {
                        var mail = new GameMail() { };
                        World.SocialManager.SendMail(mail, new Guid[] { datas.GameChar.Id }, SocialConstant.FromSystemId, remainder.Select(c => (c, gim.GetDefaultContainer(datas.GameChar, c).TemplateId)));
                        datas.MailIds.Add(mail.Id);
                    }
                }
            }
            ChangeItem.Reduce(datas.ChangeItems);
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

        /// <summary>
        /// 
        /// </summary>
        public List<Guid> ItemIds { get; } = new List<Guid>();

        /// <summary>
        /// 发送邮件的Id集合。如果无法获取奖品则发送邮件。
        /// </summary>
        public List<Guid> MailIds { get; } = new List<Guid>();

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

    }

}
