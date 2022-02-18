using Game.Social;
using GuangYuan.GY001.BLL;
using GuangYuan.GY001.BLL.Homeland;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OW.Game.Item;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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
    public class AchieveTemplateView
    {
        /// <summary>
        /// 送物品的前缀。
        /// </summary>
        private const string TidPrefix = "mtid";

        public Guid Id { get => _Template.Id; }

        public AchieveTemplateView(VWorld world, GameItemTemplate template)
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
                                    dic["btid"] = _Template.Properties.GetValueOrDefault($"mbtid{item}");    //必须有身体模板id
                                }
                                var gi = gim.ToGameItems(dic); //创建对象
                                _Metrics.Add((item, gi.First()));
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

        private Dictionary<Guid, AchieveTemplateView> _TId2Views;

        public Dictionary<Guid, AchieveTemplateView> TId2Views
        {
            get
            {
                if (_TId2Views is null)
                    lock (ThisLocker)
                        _TId2Views ??= MissionTemplates.Select(c => new AchieveTemplateView(World, c)).ToDictionary(c => c.Id);
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
            return Task.Factory.StartNew(ScanCore, gameChar, TaskCreationOptions.LongRunning);
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
            using var dwUser = World.CharManager.LockAndReturnDisposer(gu, World.CharManager.Options.DefaultLockTimeout * 1.5);
            if (dwUser is null)
                return result;

            var bag = gc.GetRenwuSlot();
            decimal metrics = 0;
            var gim = World.ItemManager;

            foreach (var item in bag.Children)
            {
                Guid tid = item.TemplateId;
                var str = tid.ToString();   //其中 GUID 的值表示为一系列小写的十六进制位，这些十六进制位分别以 8 个、4 个、4 个、4 个和 12 个位为一组并由连字符分隔开。 例如，返回值可以是“382c74c3-721d-4f34-80e5-57657b6cbc27”。 
                switch (str)
                {
                    case "814e47cd-8bdf-4efc-bd26-61af57b7fcf8": //	孵化成就	51007

                    case "8bba8a00-e767-4a6a-aa6b-22ef03a3f527": //	关卡模式总战力成就	51005
                    case "42d3236c-ea7c-4444-898e-469aac1fda07": //	累计访问好友天次成就	51011

                    case "6f8f5d48-e4b4-4e37-a48f-f8b6badc6f44": //	pvp进攻成就	51013
                    case "c20cc819-dc76-482f-a3c4-cfd32b8b83c7": //	pvp防御成就	51014
                    case "6817d0d6-ad3d-4dd1-a8f5-4368ac5a568d": //	pvp助战成就	51015
                    case "5c3d9daf-fe89-43a4-93f8-7abdc85418e5": //	累计塔防模式次数成就	51012
                    case "96a36fbe-f79a-4579-932e-588772436da5": //	关卡成就	51002
                        {
                            var diff = item.Properties.GetDecimalOrDefault(ProjectMissionConstant.指标增量属性名); //指标增量值
                            item.Properties[ProjectMissionConstant.指标增量属性名] = decimal.Zero;
                            metrics = item.Count.GetValueOrDefault() + diff;
                        }
                        break;
                    case "25ffbee1-f617-49bd-b0de-32b3e3e975cb": //	玩家等级成就	51001
                        metrics = gc.Properties.GetDecimalOrDefault(ProjectConstant.LevelPropertyName);
                        break;
                    case "2f48528e-fd7f-4269-92c9-dbd6f14ffef0": //	坐骑最高等级成就	51003
                        {
                            var zuoqiBag = gc.GetZuojiBag();
                            metrics = zuoqiBag.Children.Max(c => gim.GetBody(c).Properties.GetDecimalOrDefault(ProjectConstant.LevelPropertyName));
                        }
                        break;
                    case "7d5ad309-2614-434e-b8d3-afe4db93d8b3": //	lv20坐骑数量	51004
                        {
                            var zuoqiBag = gc.GetZuojiBag();
                            metrics = zuoqiBag.Children.Count(c => gim.GetBody(c)?.Properties.GetDecimalOrDefault(ProjectConstant.LevelPropertyName) >= 19);
                        }
                        break;
                    case "49ee3541-3a6e-4d05-85b0-566c6bfecde2": //	纯种坐骑数量成就	51006
                        {
                            var zuoqiBag = gc.GetZuojiBag();
                            metrics = zuoqiBag.Children.Count(c =>
                            {
                                var hgid = gim.GetHeadTemplate(c)?.Sequence;
                                var bgid = gim.GetBodyTemplate(c)?.Sequence;
                                if (!hgid.HasValue || !bgid.HasValue)
                                    return false;
                                return hgid.Value == bgid.Value;
                            });
                        }
                        break;
                    case "0c29f28b-d3ac-4f44-8c41-8d279fd319b5": //	最高资质成就	51008
                        {
                            var zuoqiBag = gc.GetZuojiBag();

                            metrics = zuoqiBag.Children.Max(c =>
                            {
                                var neatk = c.Properties.GetDecimalOrDefault("neatk");
                                var nemhp = c.Properties.GetDecimalOrDefault("nemhp");
                                var neqlt = c.Properties.GetDecimalOrDefault("neqlt");
                                return neatk + nemhp + neqlt;
                            });
                        }
                        break;
                    case "6ffc1f03-1c8e-4f7c-bc88-717e42eae59b": //	最高神纹等级成就	51009
                        {
                            var shenwenBag = gc.GetShenwenBag();

                            metrics = shenwenBag.Children.Max(c =>
                            {
                                var lvatk = c.Properties.GetDecimalOrDefault("lvatk");
                                var lvmhp = c.Properties.GetDecimalOrDefault("lvmhp");
                                var lvqlt = c.Properties.GetDecimalOrDefault("lvqlt");
                                return Math.Max(Math.Max(lvatk, lvmhp), lvqlt);
                            });
                        }
                        break;
                    case "4b708b18-e0a3-4388-866f-56d0c6a6da0d": //	神纹突破次数成就	51010
                        {
                            var shenwenBag = gc.GetShenwenBag();

                            metrics = shenwenBag.Children.Sum(c =>
                            {
                                var sscatk = c.Properties.GetDecimalOrDefault("sscatk");
                                var sscmhp = c.Properties.GetDecimalOrDefault("sscmhp");
                                var sscqlt = c.Properties.GetDecimalOrDefault("sscqlt");
                                return sscatk + sscmhp + sscqlt;
                            });
                        }
                        break;
                    case "530efb1e-fc5d-4638-a728-e069431b197a": //	方舟成就	51016
                        {
                            var mainControlRoom = gc.GetMainControlRoom();
                            metrics = mainControlRoom.Properties.GetDecimalOrDefault(ProjectConstant.LevelPropertyName);
                        }
                        break;
                    case "26c63192-867a-43f4-919b-10a614ee2865": //	炮塔成就	51017
                        {
                            var homeland = gc.GetHomeland();
                            metrics = homeland.GetAllChildren().Where(c => c.GetTemplate().CatalogNumber == 40).Max(c => c.Properties.GetDecimalOrDefault(ProjectConstant.LevelPropertyName));
                        }
                        break;
                    case "03d80847-f273-413b-a2a2-81545ab03a89": //	陷阱成就	51018
                        {
                            var homeland = gc.GetHomeland();
                            metrics = homeland.GetAllChildren().Where(c => c.GetTemplate().CatalogNumber == 41).Max(c => c.Properties.GetDecimalOrDefault(ProjectConstant.LevelPropertyName));
                        }
                        break;
                    case "5af7a4f2-9ba9-44e0-b368-1aa1bd9aed6d": //	旗帜成就	51019
                        {
                            var homeland = gc.GetHomeland();
                            metrics = homeland.GetAllChildren().Where(c => c.GetTemplate().CatalogNumber == 42).Max(c => c.Properties.GetDecimalOrDefault(ProjectConstant.LevelPropertyName));
                        }
                        break;
                    default:
                        continue;
                }
                result = SetNewValue(bag, item.TemplateId, metrics) || result;
            }
            World.CharManager.NotifyChange(gu);
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
            var slot = datas.GameChar.GetRenwuSlot();   //任务槽
            //var tmpId = slot.Children.First(c => c.TemplateId == ProjectMissionConstant.坐骑最高等级成就).Id;
            //datas.ItemIds[0] = tmpId;
            var coll = from id in datas.ItemIds
                       join obj in slot.Children
                       on id equals obj.Id
                       select obj;
            var objs = coll.ToList();
            if (objs.Count != datas.ItemIds.Count) //若长度不等
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
            var keys = objs.Select(c => $"mcid{c.Id}").ToList();
            keys.ForEach(c => slot.Properties.Remove(c));
            ChangeItem.Reduce(datas.ChangeItems);
            return;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="missionSlot">任务/成就槽对象。</param>
        /// <param name="tid">任务/成就模板Id。</param>
        /// <param name="newValue">新的指标值。</param>
        /// <returns></returns>
        private bool SetNewValue(GameItem missionSlot, Guid tid, decimal newValue)
        {
            //mcid5af7a4f2-9ba9-44e0-b368-1aa1bd9aed6d=10;50;100,...
            var mObj = missionSlot.Children.FirstOrDefault(c => c.TemplateId == tid);   //任务/成就对象
            var lst = World.CharManager.GetChangeData(missionSlot.GetGameChar());  //通知数据对象
            var keyName = $"mcid{mObj.Id}"; //键名
            var template = missionSlot.GetTemplate();    //模板数据
            var oldVal = mObj.Count.GetValueOrDefault();   //原值
            var unpickMetrics = missionSlot.Properties.GetStringOrDefault(keyName, string.Empty).Split(OwHelper.SemicolonArrayWithCN, StringSplitOptions.RemoveEmptyEntries)
                .Select(c => decimal.Parse(c)).ToArray();   //未领奖励的指标值
            if (!TId2Views.TryGetValue(tid, out var view))  //若找不到指定成就对象
            {
                return false;   //可能被数据删除，此处容错
            }
            var oldMetrics = view.Metrics.Where(c => oldVal >= c.Item1).Select(c => c.Item1); //级别完成且未领取的指标值。
            var newMetrics = view.Metrics.Where(c => newValue >= c.Item1).Select(c => c.Item1).Except(oldMetrics).Except(unpickMetrics).ToList(); //应加入的新值
            mObj.Count = newValue;  //设置指标值
            //通知数据
            if (newValue > oldVal)
            {
                var np = new ChangeData()
                {
                    ActionId = 2,
                    NewValue = newValue,
                    ObjectId = mObj.Id,
                    OldValue = oldVal,
                    PropertyName = "Count",
                    TemplateId = mObj.TemplateId,
                };
                lst.Add(np);
            }
            if (newMetrics.Count > 0)   //若确实有新成就
            {
                missionSlot.Properties[keyName] = string.Join(';', newMetrics.Union(unpickMetrics).Select(c => c.ToString()));
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// 对指定成就设置新的指标值。
        /// </summary>
        /// <param name="missionSlot">任务/成就槽对象。</param>
        /// <param name="tid">任务/成就模板Id。</param>
        /// <param name="newValue">新的指标值。</param>
        /// <returns></returns>
        public virtual bool SetMetrics(GameItem missionSlot, Guid tid, decimal newValue)
        {
            return SetNewValue(missionSlot, tid, newValue);
        }

        /// <summary>
        /// 获取指定成就当前指标值。
        /// </summary>
        /// <param name="missionSlot">任务/成就槽对象。</param>
        /// <param name="tid">任务/成就模板Id。</param>
        /// <returns>当前指标值。</returns>
        public virtual decimal GetMetrics(GameItem missionSlot, Guid tid)
        {
            var mObj = missionSlot.Children.FirstOrDefault(c => c.TemplateId == tid);   //任务/成就对象
            if (mObj is null)
                return decimal.Zero;
            var oldVal = mObj.Count.GetValueOrDefault();   //原值
            return oldVal;
        }

        /// <summary>
        /// 增加指定成就的指标值。
        /// </summary>
        /// <param name="missionSlot">任务/成就槽对象。</param>
        /// <param name="tid">任务/成就模板Id。</param>
        /// <param name="value">增量值。</param>
        /// <returns></returns>
        public virtual bool AddMetrics(GameItem missionSlot, Guid tid, decimal value)
        {
            var oldValue = GetMetrics(missionSlot, tid);
            return SetMetrics(missionSlot, tid, value + oldValue);
        }
        #region 任务相关

        /// <summary>
        /// 完成任务。
        /// </summary>
        /// <param name="datas"></param>
        public void Complete(MissionCompleteDatas datas)
        {
            using var dwUser = datas.LockUser();
            if (dwUser is null)
                return;
            if (!World.ItemTemplateManager.Id2Mission.TryGetValue(datas.MissionTId, out var template))
            {
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.ErrorMessage = "找不到指定模板";
                return;
            }
            using MissionView view = new MissionView(datas.World, datas.GameChar);
            if (template.PreMissionIds.All(c => view.MissionStates.TryGetValue(c, out var state) && state == MissionState.Completion))   //若可以完成
            {
                if (view.MissionStates.TryGetValue(datas.MissionTId, out var state) && state == MissionState.Completion)  //若已经完成
                {
                    datas.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                    datas.ErrorMessage = "指定任务已经完成。";
                    return;
                }
                var gim = World.ItemManager;
                //送物品
                var gis = gim.ToGameItems(template.Properties, "reward");
                if (gis.Any())
                {
                    //加入物品
                    AddItemsOrMailDatas data = new AddItemsOrMailDatas(datas.World, datas.GameChar);
                    data.Items.AddRange(gis.Select(c =>
                    {
                        if (!c.Properties.Remove("ptid", out var tmp) || !OwConvert.TryToGuid(tmp, out var id))
                            id = Guid.Empty;
                        return (c, id);
                    }));
                    gim.AddItemsOrMail(data);
                    datas.ErrorCode = data.ErrorCode;
                    datas.ErrorMessage = data.ErrorMessage;
                    if (!datas.HasError)
                    {
                        datas.ChangeItems.AddRange(data.ChangeItems);
                        datas.MailIds.AddRange(data.MailIds);
                        ScanAsync(datas.GameChar);
                    }
                }
                //保存数据
                view.MissionStates[datas.MissionTId] = MissionState.Completion;
                view.Save();
                World.CharManager.NotifyChange(datas.GameChar.GameUser);
            }
            else //有错误
            {
                datas.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                datas.ErrorMessage = "至少一个前置任务没有完成";
            }
        }

        /// <summary>
        /// 检查指定的一组任务的状态。
        /// </summary>
        /// <param name="datas"></param>
        public void GetMissionState(GetMissionStateDatas datas)
        {
            using var dwUser = datas.LockUser();
            if (dwUser is null)
                return;
            var gitm = World.ItemTemplateManager;
            List<GameMissionTemplate> list;
            if (datas.TIds.Count == 0) //若获取所有任务状态
            {
                datas.TIds.AddRange(World.ItemTemplateManager.Id2Mission.Keys);
                list = World.ItemTemplateManager.Id2Mission.Values.ToList();
            }
            else
                list = datas.TIds.Select(c =>
                {
                    if (!gitm.Id2Mission.TryGetValue(c, out var tt))
                        return null;
                    return tt;
                }).Where(c => c != null).ToList();
            if (list.Count != datas.TIds.Count)
            {
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.ErrorMessage = "至少一个任务找不到模板";
                return;
            }
            using MissionView view = new MissionView(datas.World, datas.GameChar);
            for (int i = 0; i < list.Count; i++)
            {
                var tid = datas.TIds[i];
                if (view.MissionStates.TryGetValue(tid, out var state))
                    datas.State.Add(state);
                else
                    datas.State.Add(MissionState.WaitingForActivation);
            }
        }

        /// <summary>
        /// 获取所有任务模板数据。
        /// </summary>
        public IEnumerable<GameMissionTemplate> GetMissionTemplates()
        {
            return World.ItemTemplateManager.Id2Mission.Values;
        }

        #endregion 任务相关
    }

    public class GetMissionStateDatas : ComplexWorkDatasBase
    {
        public GetMissionStateDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public GetMissionStateDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public GetMissionStateDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 任务模板Id的集合。空集合表示所有任务状态，返回时会填写所有任务id。
        /// </summary>
        public List<Guid> TIds { get; } = new List<Guid>();

        /// <summary>
        /// 任务状态，索引与TIds对应。当前=9就是完成，否则就是没完成。
        /// </summary>
        public List<MissionState> State { get; } = new List<MissionState>();
    }

    /// <summary>
    /// 任务的状态。
    /// </summary>
    [Flags]
    public enum MissionState
    {
        None = 0,

        /// <summary>
        /// 任务的前置条件不满足。
        /// </summary>
        WaitingForActivation = 1,

        /// <summary>
        /// 任务前置条件满足，但还未开始。
        /// </summary>
        WaitingToRun = 2,

        RunningFlag = 4,
        /// <summary>
        /// 进行中。
        /// </summary>
        Running = RunningFlag | 1,

        /// <summary>
        /// 等待子任务完成。
        /// </summary>
        WaitingForChildrenToComplete = RunningFlag | Running | 2,

        CompletionFlag = 8,

        /// <summary>
        /// 已经完成。
        /// </summary>
        Completion = CompletionFlag | 1,

        /// <summary>
        /// 用户已经取消。
        /// </summary>
        Canceled = CompletionFlag | 2,

        /// <summary>
        /// 任务失败。
        /// </summary>
        Faulted = CompletionFlag | 4,
    }

    public class MissionView : GameCharWorkDataBase
    {
        public MissionView([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public MissionView([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public MissionView([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        Dictionary<Guid, MissionState> _MissionStates;
        const string KeyName = "MissionStates";

        /// <summary>
        /// 任务状态字典。
        /// </summary>
        public Dictionary<Guid, MissionState> MissionStates
        {
            get
            {
                if (_MissionStates is null)
                {
                    _MissionStates = new Dictionary<Guid, MissionState>();
                    var gep = GameChar.ExtendProperties.FirstOrDefault(c => c.Name == KeyName);
                    if (gep != null && gep.ByteArray.Length > 0)
                    {
                        using (var ms = new MemoryStream(gep.ByteArray))
                        using (var br = new BinaryReader(ms))
                        {
                            var count = br.ReadInt32();
                            for (int i = 0; i < count; i++)
                            {
                                var key = br.ReadGuid();
                                var val = (MissionState)br.ReadInt32();
                                _MissionStates[key] = val;
                            }
                        }
                    }
                }
                return _MissionStates;
            }
        }

        public override void Save()
        {
            if (null != _MissionStates)    //若需要存储
            {
                var gep = GameChar.ExtendProperties.FirstOrDefault(c => c.Name == KeyName);
                if (gep is null)
                {
                    gep = new GameExtendProperty(KeyName, GameChar.Id);
                    GameChar.ExtendProperties.Add(gep);
                }
                MemoryStream ms;
                using (ms = new MemoryStream())
                using (var bw = new BinaryWriter(ms))
                {
                    bw.Write(_MissionStates.Count);
                    foreach (var item in _MissionStates)
                    {
                        bw.Write(item.Key);
                        bw.Write((int)item.Value);
                    }
                }
                gep.ByteArray = ms.ToArray();
            }
            base.Save();
        }
    }

    public class MissionCompleteDatas : ChangeItemsAndMailWorkDatsBase
    {
        public MissionCompleteDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public MissionCompleteDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public MissionCompleteDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 要完成任务的模板Id。
        /// </summary>
        public Guid MissionTId { get; set; }
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
