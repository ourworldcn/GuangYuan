using Game.Social;
using GuangYuan.GY001.BLL.Homeland;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ObjectPool;
using OW.Extensions.Game.Store;
using OW.Game;
using OW.Game.Item;
using OW.Game.Mission;
using OW.Game.PropertyChange;
using OW.Game.Store;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace GuangYuan.GY001.BLL
{
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
        /// <returns>升级需要的资源及消耗量，null表示不可升级,此时可通过<see cref="VWorld.GetLastError"/>获取详细信息。</returns>
        public List<(GameItem, decimal)> GetCost()
        {
            var lv = GameItem.Properties.GetDecimalOrDefault(ProjectConstant.LevelPropertyName);

            var result = new List<(GameItem, decimal)>();

            var dic = GameItem.Properties;
            var bag = GameChar.GetCurrencyBag();

            #region 计算通用代价
            var collCost = GameItem.Properties.GetValuesWithoutPrefix("luc").ToList();   //获取代价
            foreach (var costItem in collCost)
            {
                var item = costItem.FirstOrDefault();    //获取
                if (!OwConvert.TryToGuid(item.Item1, out var tid) || !OwConvert.TryToDecimal(item.Item2, out var count))    //若没有合法的数据
                    return null;
                var gi = GameChar.AllChildren.FirstOrDefault(c => c.TemplateId == tid);
                if (gi is null) //若找不到指定的道具
                    return null;
                result.Add((gi, -Math.Abs(count)));
            }
            #endregion 计算通用代价

            #region 计算非通用代价
            var dia = dic.GetDecimalOrDefault("lud", decimal.Zero); //钻石
            if (dia != decimal.Zero)   //若有钻石消耗
            {
                dia = -Math.Abs(dia);
                var gi = GameChar.GetZuanshi();
                result.Add((gi, dia));
            }

            var gold = dic.GetDecimalOrDefault("lug");  //金币
            if (gold != decimal.Zero)   //若有金币消耗
            {
                gold = -Math.Abs(gold);
                var gi = GameChar.GetJinbi();
                result.Add((gi, gold));
            }

            var wood = dic.GetDecimalOrDefault("luw");  //木材
            if (wood != decimal.Zero)   //若有金币消耗
            {
                wood = -Math.Abs(wood);
                var gi = GameChar.GetMucai();
                result.Add((gi, wood));
            }
            #endregion 计算非通用代价
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
                VWorld.SetLastErrorMessage($"{errItem.Item1.GetTemplate().DisplayName} 不足。");
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

    /// <summary>
    /// 使用蓝图的数据。
    /// </summary>
    public class ApplyBlueprintDatas : ChangeItemsWorkDatasBase
    {
        public ApplyBlueprintDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public ApplyBlueprintDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public ApplyBlueprintDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 蓝图的模板。
        /// </summary>
        public BlueprintTemplate Blueprint { get; set; }

        /// <summary>
        /// 要执行蓝图制造的对象集合。可以仅给出关键物品，在制造过成中会补足其他所需物品。
        /// </summary>
        public List<GameItem> GameItems { get; } = new List<GameItem>();

        /// <summary>
        /// 键是物料模板的Id,值对应的物料对象。
        /// </summary>
        public Dictionary<Guid, GameItem> Items { get; } = new Dictionary<Guid, GameItem>();

        /// <summary>
        /// 提前计算好每种原料的增量。
        /// 键是原料对象Id,值（最低增量,最高增量,用随机数计算得到的增量）的值元组。
        /// </summary>
        internal Dictionary<Guid, (decimal, decimal, decimal)> Incrementes { get; } = new Dictionary<Guid, (decimal, decimal, decimal)>();

        /// <summary>
        /// 要执行的次数。
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// 获取或设置成功执行的次数。
        /// </summary>
        public int SuccCount { get; set; }

        private string _DebugMessage;
        /// <summary>
        /// 调试信息，如果发生错误，这里给出简要说明。
        /// </summary>
        public string DebugMessage
        {
            get => _DebugMessage;
            set => _DebugMessage = value;
        }

        /// <summary>
        /// 返回命中公式的Id集合。
        /// </summary>
        public List<Guid> FormulaIds { get; } = new List<Guid>();

        private List<Guid> _ErrorItemTIds;

        /// <summary>
        /// 获取或设置，出错虚拟物品的模板Id。具体意义根据不同蓝图区别。
        /// </summary>
        public List<Guid> ErrorItemTIds => _ErrorItemTIds ??= new List<Guid>();

        private List<Guid> _MailIds;

        /// <summary>
        /// 执行蓝图导致发送邮件的Id集合。
        /// </summary>
        public List<Guid> MailIds => _MailIds ??= new List<Guid>();

        /// <summary>
        /// 无法放入指定背包的蓝图制造物集合。
        /// </summary>
        public List<GameItem> Remainder { get; } = new List<GameItem>();

        /// <summary>
        /// 详细的变化信息。
        /// </summary>
        public List<GamePropertyChangeItem<object>> Changes { get; } = new List<GamePropertyChangeItem<object>>();
    }

    /// <summary>
    /// 蓝图管理器配置数据。
    /// </summary>
    public class BlueprintManagerOptions
    {
        public BlueprintManagerOptions()
        {

        }

        public Func<IServiceProvider, ApplyBlueprintDatas, bool> DoApply { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class BlueprintMethodAttribute : Attribute
    {
        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236
        private readonly Guid _BlueprintId;

        // This is a positional argument
        public BlueprintMethodAttribute(string blueprintId)
        {

            // TODO: Implement code here

            _BlueprintId = Guid.Parse(blueprintId);
        }


        // This is a named argument
        public int NamedInt { get; set; }

        public Guid BlueprintId { get => _BlueprintId; }
    }

    /// <summary>
    /// 蓝图管理器。
    /// </summary>
    public class BlueprintManager : GameManagerBase<BlueprintManagerOptions>
    {
        private static Dictionary<Guid, MethodInfo> _Methods;
        /// <summary>
        /// 处理蓝图Id，映射到处理函数。
        /// 函数可以是私有的，但务必是实例函数。
        /// </summary>
        public static IReadOnlyDictionary<Guid, MethodInfo> Id2Handler
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                if (_Methods is null)
                {
                    var coll = from tmp in typeof(BlueprintManager).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                               let attr = tmp.GetCustomAttributes(typeof(BlueprintMethodAttribute), true).FirstOrDefault() as BlueprintMethodAttribute
                               where null != attr
                               select (tmp, attr);
                    _Methods = coll.ToDictionary(c => c.attr.BlueprintId, c => c.tmp);
                }
                return _Methods;
            }
        }

        public BlueprintManager()
        {
            Initialize();
        }

        public BlueprintManager(IServiceProvider service) : base(service)
        {
            Initialize();
        }

        public BlueprintManager(IServiceProvider service, BlueprintManagerOptions options) : base(service, options)
        {
            Initialize();
        }

        private void Initialize()
        {
            lock (ThisLocker)
            {
                _InitializeTask ??= Task.Run(() =>
                {
                    _Id2BlueprintTemplate = Context.Set<BlueprintTemplate>().Include(c => c.FormulaTemplates).ThenInclude(c => c.BptfItemTemplates).ToDictionary(c => c.Id);
                    _Id2Template = _Id2BlueprintTemplate.SelectMany(c => c.Value.FormulaTemplates)
                                                        .SelectMany(c => c.BptfItemTemplates)
                                                        .OfType<GameThingTemplateBase>()
                                                        .Concat(_Id2BlueprintTemplate.SelectMany(c => c.Value.FormulaTemplates).OfType<GameThingTemplateBase>())
                                                        .Concat(_Id2BlueprintTemplate.Select(c => c.Value).OfType<GameThingTemplateBase>())
                                                        .ToDictionary(c => c.Id);
                });
            }
        }

        private Task _InitializeTask;

        private DbContext _DbContext;

        public DbContext Context { get => _DbContext ??= World.CreateNewTemplateDbContext(); }

        private Dictionary<Guid, BlueprintTemplate> _Id2BlueprintTemplate;

        /// <summary>
        /// 记录所有蓝图模板的字典，键是蓝图Id值是蓝图模板对象。
        /// </summary>
        public Dictionary<Guid, BlueprintTemplate> Id2BlueprintTemplate
        {
            get
            {
                _InitializeTask.Wait();
                return _Id2BlueprintTemplate;
            }
        }

        /// <summary>
        /// 所有相关对象的加速字典。
        /// </summary>
        private Dictionary<Guid, GameThingTemplateBase> _Id2Template;

        /// <summary>
        /// 获取指定Id的模板对象。
        /// </summary>
        /// <param name="id">模板对象的Id。</param>
        /// <returns>模板对象，如果没有找到则返回null。</returns>
        public GameThingTemplateBase GetTemplateFromId(Guid id)
        {
            _InitializeTask.Wait();
            return _Id2Template.GetValueOrDefault(id);
        }

        /// <summary>
        /// 分发需要特定处理的蓝图。
        /// </summary>
        /// <param name="datas"></param>
        /// <returns>true蓝图已经处理，false蓝图未处理。</returns>
        private bool Dispatch(ApplyBlueprintDatas datas)
        {
            bool succ;
            try
            {
                switch (datas.Blueprint.IdString)
                {
                    case "7f35cda3-316d-4be6-9ccf-c348bb7dd28b":    //若是取蛋
                        GetFhResult(datas);
                        succ = true;
                        break;
                    case "972c78bf-773c-4de7-95db-5fd685a9a263":  //若是加速孵化
                        JiasuFuhua(datas);
                        succ = true;
                        break;
                    //case "384ed85c-82fd-4f08-86e7-eae5ad6eef2c":    //家园所属虚拟物品内升级
                    //    UpgradeInHomeland(datas);
                    //    succ = true;
                    //    break;
                    case "06bdaa5c-3d88-4279-9826-8f5a554ab588":    //加速主控室/玉米地/树林/炮台/旗子/陷阱/捕兽竿升级
                        HastenOnHomeland(datas);
                        succ = true;
                        break;
                    case "8b26f520-fbf3-4979-831c-398a0150b3da":    // 取得玉米/ 木材放入仓库
                        Harvest(datas);
                        succ = true;
                        break;
                    case "f9262cb1-7357-4027-b742-d0a82f3ad4c1":    //合成
                        Hecheng(datas);
                        succ = true;
                        break;
                    case "dd5095f8-929f-45a5-a86c-4a1792e9d9c8":    //购买Pve次数
                        BuyPveCount(datas);
                        succ = true;
                        break;
                    case "3af71bc0-db28-4f42-a1ca-38fb5b5030cc":    //攻击神纹升级
                    case "d40c1818-06cf-4d19-9f6e-5ba54472b6fc":    //血量神纹升级
                    case "dd999eee-9d4e-40de-bff9-16d5054d139b":    //质量神纹升级
                        ShenwenLevelUp(datas);
                        succ = true;
                        break;
                    case "92f63905-a39f-4e1a-ad17-ea648a99be7a":    //神纹攻击突破
                    case "e35d12f0-c2d6-40bf-a2b0-f04b057ff68c":    //神纹血量突破
                    case "7fc78174-f884-4918-a4fc-b84db5360cf9":    //神纹质量突破
                        ShenwenTupo(datas);
                        succ = true;
                        break;
                    case "c7051e47-0a73-4319-85dc-7b02f26f14f4": //兽栏背包扩容
                        if (datas.GameItems.Count == 0)    //若没指定物品
                        {
                            datas.GameItems.Add(datas.GameChar.GetShoulanBag());
                        }
                        LevelUp(datas);
                        succ = true;
                        break;
                    case "b5288563-0543-4d4b-b466-83386ccf188c":    //孵化槽解锁
                        if (datas.GameItems.Count == 0)    //若没指定物品
                        {
                            datas.GameItems.Add(datas.GameChar.GetFuhuaSlot());
                        }
                        LevelUp(datas);
                        succ = true;
                        break;
                    default:
                        if (Id2Handler.TryGetValue(datas.Blueprint.Id, out var handler))
                        {
                            var paras = new object[] { datas };
                            handler.Invoke(this, paras);
                            succ = true;
                        }
                        else
                            succ = false;
                        break;
                }
            }
            catch (Exception err)
            {
                datas.HasError = true;
                datas.SetDebugMessage(err.Message);
                succ = true;
            }
            return succ;
        }

        const string Day30CountKeyName = "Day30Count";
        /// <summary>
        /// 三十日签到礼包。
        /// </summary>
        /// <param name="datas"></param>
        [BlueprintMethod("c86c1851-2e6e-45ad-9a16-4a77cc81550b")]
        private void SignInOfDay30(ApplyBlueprintDatas datas)
        {
            if (!datas.Verify(datas.GameItems.Count == 1, "物品数量不对"))
                return;
            var gi = datas.GameItems[0];    //物品
            var tt = gi.GetTemplate();
            var gc = datas.GameChar;

            var coll = StringObjectDictionaryExtensions.GetValuesWithoutPrefix(tt.Properties, "use");
            var totalDay = gc.Properties.GetDecimalOrDefault(Day30CountKeyName);
            var day = (int)totalDay % 30; //应该获取哪天的物品
            var indexStr = day.ToString();  //前缀字符串
            var dic = coll.First(c => c.Key == indexStr).ToDictionary(c => c.Item1, c => c.Item2);

            var eveMng = World.EventsManager;
            var dest = new GameItem();
            eveMng.GameItemCreated(dest, dic);    //创建物品

            var gim = World.ItemManager;
            var parent = gc.AllChildren.FirstOrDefault(c => c.TemplateId == dest.Properties.GetGuidOrDefault("ptid"));
            gim.AddItem(dest, parent, null, datas.ChangeItems);
            //送固定物品
            dic = coll.First(c => c.Key == string.Empty).ToDictionary(c => c.Item1, c => c.Item2);
            var dest2 = new GameItem();
            eveMng.GameItemCreated(dest2, dic);    //创建物品
            var parent2 = gc.AllChildren.FirstOrDefault(c => c.TemplateId == dest2.Properties.GetGuidOrDefault("ptid"));
            gim.AddItem(dest2, parent2, null, datas.ChangeItems);

            gc.Properties[Day30CountKeyName] = totalDay + 1; //设置已经获取的天计数
        }

        /// <summary>
        /// 坐骑等级提升。
        /// </summary>
        /// <param name="datas"></param>
        [BlueprintMethod("6a0c5697-4228-4ec9-a69e-28d61bd52b32")]
        private void MountsLevelUp(ApplyBlueprintDatas datas)
        {
            if (datas.GameItems.Count != 1)
            {
                datas.HasError = true;
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                return;
            }
            var gi = datas.GameItems[0];
            var charLv = datas.GameChar.Properties.GetDecimalOrDefault(World.PropertyManager.LevelPropertyName);    //角色等级
            var giLv = World.ItemManager.GetBody(gi).Properties.GetDecimalOrDefault(World.PropertyManager.LevelPropertyName); //坐骑等级
            var innerCount = (charLv + 1) * 2 - (giLv + 1); //计算实际可以升级的次数
            innerCount = Math.Min(datas.Count, innerCount);
            if (innerCount <= 0) //若已经不可再升级
            {
                datas.HasError = true;
                datas.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                return;
            }
            using var datasInner = new ApplyBlueprintDatas(datas.World, datas.GameChar)
            {
                ActionId = datas.ActionId,
                Count = (int)innerCount,
                Blueprint = datas.Blueprint,
                UserContext = datas.UserContext,
            };
            var body = World.ItemManager.GetBody(gi);
            datasInner.GameItems.Add(body);
            LevelUp(datasInner);
            datas.ChangeItems.AddToChanges(gi);
            datas.ChangeItems.AddRange(datasInner.ChangeItems);
            datas.SuccCount = datasInner.SuccCount;
            datas.HasError = datasInner.HasError;
            datas.ErrorCode = datasInner.ErrorCode;
            datas.DebugMessage = datasInner.DebugMessage;
            //设置成就数据
            World.MissionManager.ScanAsync(datas.GameChar);
            //if (body.Properties.GetDecimalWithFcpOrDefault(World.PropertyManager.LevelPropertyName) >= 19)    //达到20级
            //{
            //    var bag = datas.GameChar.GetZuojiBag();
            //    var metrics = bag.Children.Count(c => World.ItemManager.GetBody(c)?.Properties.GetDecimalWithFcpOrDefault(World.PropertyManager.LevelPropertyName) >= 19);
            //    World.MissionManager.SetMetrics(datas.GameChar.GetRenwuSlot(), ProjectMissionConstant.LV20坐骑数量, metrics);
            //}
        }

        /// <summary>
        /// 抽奖物品杂交坐骑包。
        /// </summary>
        /// <param name="datas"></param>
        [BlueprintMethod("20913e62-2f84-4b78-9733-aa91aee4bd94")]
        void Lott1(ApplyBlueprintDatas datas)
        {
            var gitm = World.ItemTemplateManager;
            var gameItem = datas.GameItems[0];
            for (int i = 0; i < datas.Count; i++)
            {
                var head = gitm.HeadTemplates.Values.Skip(VWorld.WorldRandom.Next(gitm.HeadTemplates.Count)).First();
                var coll = gitm.BodyTemplates.Values.Where(c => c.Sequence != head.Sequence);
                var body = coll.Skip(VWorld.WorldRandom.Next(coll.Count())).First();
                var propBag = DictionaryPool<string, object>.Shared.Get();
                OwHelper.Copy(datas.GameItems[0].Properties, propBag);
                propBag["htid"] = head.Id;
                propBag["btid"] = body.Id;
                propBag["tid"] = ProjectConstant.ZuojiZuheRongqi.ToString();
                var gi = new GameItem() { Count = 1 };
                World.EventsManager.GameItemCreated(gi, propBag);
                if (World.ItemManager.IsExistsMounts(gi, datas.GameChar))    //若已经存在此类坐骑
                {
                    World.ItemManager.MoveItem(gi, gi.Count.Value, datas.GameChar.GetShoulanBag(), datas.Remainder, datas.Changes);
                }
                else //若没有此种坐骑
                {
                    gi.Properties["neatk"] = 0m;
                    gi.Properties["nemhp"] = 0m;
                    gi.Properties["neqlt"] = 0m;
                    World.ItemManager.MoveItem(gi, gi.Count.Value, datas.GameChar.GetZuojiBag(), datas.Remainder, datas.Changes);
                }
                DictionaryPool<string, object>.Shared.Return(propBag);
                datas.SuccCount++;
            }
            World.ItemManager.ForcedSetCount(gameItem, gameItem.Count.Value - datas.SuccCount, datas.Changes);
        }
        #region 通用功能

        #endregion 通用功能

        #region 神纹相关

        /// <summary>
        /// 神纹升级。
        /// </summary>
        /// <param name="datas"></param>
        public void ShenwenLevelUp(ApplyBlueprintDatas datas)
        {
            if (!datas.Verify(datas.GameItems.Count == 1, "只能升级一个对象。"))
            {
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                return;
            }
            var gi = datas.GameItems[0];    //升级的神纹
            var gc = datas.GameChar;
            if (!datas.Verify(gc.GetShenwenBag().Children.Contains(gi) && gi.GetTemplate().CatalogNumber == 10, "要升级的不是一个神纹对象。"))
            {
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                return;
            }
            string pname; //主属性名后缀
            int gcode;  //主属性名对应的类型码
            switch (datas.Blueprint.IdString)
            {
                case "3af71bc0-db28-4f42-a1ca-38fb5b5030cc":    //攻击
                    pname = "atk";
                    gcode = 16;
                    break;
                case "d40c1818-06cf-4d19-9f6e-5ba54472b6fc":   //血量
                    pname = "mhp";
                    gcode = 15;
                    break;
                case "dd999eee-9d4e-40de-bff9-16d5054d139b":     //质量
                    pname = "qlt";
                    gcode = 17;
                    break;
                default:
                    datas.HasError = true;
                    datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                    datas.DebugMessage = "不正确的模板";
                    return;
            }
            //道具
            var bodyCode = gi.GetDecimalWithFcpOrDefault("body");
            var daoju = gc.GetItemBag().Children.FirstOrDefault(c => c.Properties.GetDecimalOrDefault("body") == bodyCode && c.GetCatalogNumber() == gcode); //碎片道具
            if (!(daoju?.Count > 0)) //若道具不足
            {
                datas.HasError = true;
                datas.ErrorCode = ErrorCodes.RPC_S_OUT_OF_RESOURCES;
                return;
            }
            //升级次数
            var lvpname = $"lv{pname}"; //主属性名
            var mlvpname = $"mlv{pname}"; //上限属性名
            var lv = gi.GetDecimalWithFcpOrDefault(lvpname); //级别
            var mlv = gi.GetDecimalWithFcpOrDefault(mlvpname); //级别上限
            var maxCount = Math.Min(datas.Count, mlv - lv);   //实际的升级次数
            if (maxCount <= 0) //若已经升级到顶
            {
                datas.HasError = true;
                datas.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                datas.DebugMessage = "不可继续升级。";
                return;
            }
            maxCount = Math.Min(maxCount, daoju.Count.Value);   //实际升级次数
            //修改数据
            World.ItemManager.SetLevel(gi, pname, (int)(lv + maxCount));
            gi.Properties[lvpname] = (lv + maxCount);

            daoju.Count -= maxCount;
            datas.SuccCount = (int)maxCount;
            if (daoju.Count > 0)
                datas.ChangeItems.AddToChanges(gi, daoju);
            else
            {
                datas.ChangeItems.AddToRemoves(gc.GetItemBag().Id, daoju.Id);
                datas.GameChar.GameUser.DbContext.Remove(daoju);
                gc.GetItemBag().Children.Remove(daoju);
            }
            datas.ChangeItems.AddToChanges(gi);
        }

        /// <summary>
        /// 神纹突破。
        /// </summary>
        /// <param name="datas"></param>
        public void ShenwenTupo(ApplyBlueprintDatas datas)
        {
            decimal[] costs = new decimal[] { -1, -3, -5, -8, -11, -15, -19, -24, -30 }; //突破石耗损
            double[] probs = new double[] { 1, 1, 1, 0.95, 0.9, 0.8, 0.7, 0.6, 0.5 };    //突破概率
            if (!datas.Verify(datas.GameItems.Count == 1, "只能升级一个对象。"))
            {
                datas.HasError = true;
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                return;
            }
            var gi = datas.GameItems[0];    //突破的神纹
            var gc = datas.GameChar;
            string pname;
            switch (datas.Blueprint.IdString)
            {
                case "92f63905-a39f-4e1a-ad17-ea648a99be7a":    //神纹攻击突破
                    pname = "atk";
                    break;
                case "e35d12f0-c2d6-40bf-a2b0-f04b057ff68c":    //神纹血量突破
                    pname = "mhp";
                    break;
                case "7fc78174-f884-4918-a4fc-b84db5360cf9":    //神纹质量突破
                    pname = "qlt";
                    break;
                default:
                    datas.HasError = true;
                    datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                    datas.DebugMessage = "不正确的模板";
                    return;
            }
            var lvName = $"lv{pname}";
            var ssc = (int)gi.Properties.GetDecimalOrDefault($"ssc{pname}");    //突破次数
            if (ssc >= costs.Length)
            {
                datas.HasError = true;
                datas.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                return;
            }
            var cost = costs[ssc];  //所需突破石数量
            var itemBag = gc.GetItemBag();
            var tid = new Guid("A9BD0E89-1105-4A52-982A-97E6EE4AD577"); //突破石道具
            var tupoItem = itemBag.Children.FirstOrDefault(c => c.TemplateId == tid);
            if (tupoItem is null || tupoItem.Count + cost < 0)   //若突破石不足
            {
                datas.HasError = true;
                datas.ErrorCode = ErrorCodes.RPC_S_OUT_OF_RESOURCES;
                return;
            }
            //测算是否成功突破
            var prob = probs[ssc];
            bool ok = World.IsHit(prob);
            //修改数据
            tupoItem.Count += cost;
            if (tupoItem.Count > 0)    //若还有剩余
            {
                datas.ChangeItems.AddToChanges(tupoItem);
            }
            else //若已经全部消耗
            {
                datas.ChangeItems.AddToRemoves(tupoItem.GetContainerId().Value, tupoItem.Id);
                gc.GameUser.DbContext.Remove(tupoItem);
                itemBag.Children.Remove(tupoItem);
            }
            if (ok)  //若突破成功
            {
                gi.Properties[$"ssc{pname}"] = ssc + 1;
                gi.Properties[$"mlv{pname}"] = gi.Properties.GetDecimalOrDefault($"mlv{pname}") + 10;
            }
            else //若突破失败
            {
                var lv = gi.Properties.GetDecimalOrDefault(lvName);
                World.ItemManager.SetPropertyValue(gi, lvName, (int)Math.Max(lv - 3, 0));
            }
            datas.ChangeItems.AddToChanges(gi);
        }
        #endregion 神纹相关

        #region 升级相关

        /// <summary>
        /// 某个家园内物品升级结束。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UpgradeCompletedInHomelang(object state)
        {
            if (!(state is ValueTuple<Guid, Guid> ids))
            {
                return;
            }

            GameCharManager cm = World.CharManager;
            GameItemManager gim = World.ItemManager;
            using var dwUser = cm.LockOrLoad(ids.Item1, out var gu);
            if (dwUser is null)   //若无法锁定用户
            {
                return;
            }
            var gc = gu.GameChars.FirstOrDefault(c => c.Id == ids.Item1);
            try
            {
                GameItem gameItem = gc.GetHomeland().GetAllChildren().FirstOrDefault(c => c.Id == ids.Item2);
                if (gameItem is null)
                {
                    return;
                }
                LastChangesItems.Clear();
                int lv = (int)gameItem.GetDecimalWithFcpOrDefault(ProjectConstant.LevelPropertyName, 0m);  //原等级
                //gim.SetPropertyValue(gameItem, ProjectConstant.LevelPropertyName, lv + 1);    //设置新等级
                if (gameItem.TemplateId == ProjectConstant.MainControlRoomSlotId) //如果是主控室升级
                {
                    IEnumerable<MainbaseUpgradePrv> coll = MainbaseUpgradePrv.Alls.Where(c => c.Level == lv);
                    List<GameItem> addItems = new List<GameItem>();
                    foreach (MainbaseUpgradePrv item in coll)
                    {
                        GameItem parent = gc.AllChildren.FirstOrDefault(c => c.TemplateId == item.ParentTId);
                        if (item.PrvTId.HasValue)    //若送物品
                        {
                            GameItem tmp = new GameItem();
                            World.EventsManager.GameItemCreated(tmp, item.PrvTId.Value);
                            gim.AddItem(tmp, parent, null, LastChangesItems);
                        }
                        if (item.Genus.HasValue)   //若送地块
                        {
                            int styleNumber = gc.GetCurrentFenggeNumber();   //激活的风格号
                            var subItem = World.ItemTemplateManager.GetTemplateByNumberAndIndex(styleNumber, item.Genus.Value % 100);
                            GameItem tmp = new GameItem();
                            World.EventsManager.GameItemCreated(tmp, subItem.Id);
                            gim.AddItem(tmp, gc.GetHomeland(), null, LastChangesItems);
                        }
                    }
                }
                if (gameItem.TemplateId == ProjectConstant.MucaiStoreTId)
                {
                    gim.ComputeMucaiStc(gc);
                }
                LastChangesItems.AddToChanges(gameItem.GetContainerId().Value, gameItem);
                var worker = gc.GetHomeland().Children.FirstOrDefault(c => c.TemplateId == ProjectConstant.WorkerOfHomelandTId);
                worker.Count++;
                LastChangesItems.AddToChanges(worker);
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// 通用的升级函数。
        /// </summary>
        /// <param name="datas"></param>
        [BlueprintMethod("{7D17BE11-02A0-473A-A2AF-87029393C530}")]
        public void LevelUp(ApplyBlueprintDatas datas)
        {
            var gim = World.ItemManager;
            if (datas.GameItems.Count <= 0)
            {
                datas.HasError = true;
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.DebugMessage = "应指定一个升级物品。";
                return;
            }
            if (datas.Count <= 0)
            {
                datas.HasError = true;
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.DebugMessage = "升级次数应大于0。";
                return;
            }
            var lut = datas.GameItems[0].Properties.GetDecimalOrDefault("lut");
            if (lut > 0 && datas.Count > 1)
            {
                datas.HasError = true;
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.DebugMessage = "升级冷却时间大于0时，不可以连续升级。";
                return;
            }
            var gi = datas.GameItems[0];
            if (gi.Name2FastChangingProperty.TryGetValue(ProjectConstant.UpgradeTimeName, out var fcp))  //若存在升级冷却
            {
                if (!fcp.IsComplate)    //若尚未完成升级
                {
                    datas.HasError = true;
                    datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                    datas.DebugMessage = "物品还在升级中。";
                    return;
                }
                else //若已经完成升级
                {
                    gi.RemoveFastChangingProperty(ProjectConstant.UpgradeTimeName);
                    fcp = null;
                }
            }

            var template = gi.GetTemplate();
            int lv;
            for (int i = 0; i < datas.Count; i++)
            {
                lv = (int)gi.Properties.GetDecimalOrDefault(ProjectConstant.LevelPropertyName);
                if (template.GetMaxLevel(template.SequencePropertyNames.FirstOrDefault()) <= lv)  //若已达最大等级
                {
                    if (i > 0) //若已经成功升级过至少一次
                    {
                        datas.HasError = true;
                        datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                        datas.ErrorItemTIds.Add(gi.TemplateId);
                    }
                    datas.DebugMessage = "已达最大等级";
                    return;
                }
                var luDatas = new LevelUpDatas(World, datas.GameChar)
                {
                    GameItem = datas.GameItems[0],
                };
                if (gim.IsMounts(gi))
                    luDatas.GameItem = gim.GetBody(gi);
                else
                    luDatas.GameItem = gi;
                var cost = luDatas.GetCost();
                if (cost is null)   //若资源不足以升级
                {
                    VWorld.SetLastError(ErrorCodes.RPC_S_OUT_OF_RESOURCES);
                    datas.HasError = true;
                    datas.ErrorCode = ErrorCodes.RPC_S_OUT_OF_RESOURCES;
                    datas.DebugMessage = VWorld.GetLastErrorMessage();
                    return;
                }
                var succ = luDatas.Deplete(cost);
                if (!succ)  //若资源不足
                {
                    if (datas.SuccCount == 0)
                    {
                        datas.HasError = true;
                        datas.ErrorCode = VWorld.GetLastError();
                        datas.DebugMessage = VWorld.GetLastErrorMessage();
                    }
                    return;
                }
                lut = gi.GetDecimalWithFcpOrDefault("lut");    //升级耗时，单位：秒
                if (lut == decimal.Zero)   //若没有升级延时
                {
                    //升级
                    var oldLv = luDatas.GameItem.Properties.GetDecimalOrDefault(ProjectConstant.LevelPropertyName);
                    gim.SetPropertyValue(luDatas.GameItem, ProjectConstant.LevelPropertyName, oldLv + 1);
                    //记录变化信息
                    datas.ChangeItems.AddToChanges(cost.Select(c => c.Item1).ToArray());
                    datas.SuccCount = i + 1;
                    datas.ChangeItems.AddToChanges(gi);
                }
                else //若有升级延时
                {
                    //记录变化信息
                    datas.ChangeItems.AddToChanges(cost.Select(c => c.Item1).ToArray());
                    datas.SuccCount = i + 1;
                    datas.ChangeItems.AddToChanges(gi);
                    if (fcp is null)
                    {
                        var now = DateTime.UtcNow;
                        fcp = new FastChangingProperty(TimeSpan.FromSeconds(1), 1, lut, 0, now) { Tag = ProjectConstant.UpgradeTimeName };
                        DateTime dtComplated = fcp.GetComplateDateTime();   //预估完成时间
                        gi.Name2FastChangingProperty.Add(fcp.Tag as string, fcp);
                        fcp.ToDictionary(gi.Properties, fcp.Tag as string);
                        //定时任务
                        var scId = Guid.NewGuid();  //定时任务Id
                        var sd = new SchedulerDescriptor(scId)
                        {
                            ComplatedDatetime = dtComplated,
                            MethodName = nameof(LevelUpCompleted),
                            ServiceTypeName = GetType().FullName,
                        };
                        sd.Properties["charId"] = datas.GameChar.Id;
                        sd.Properties["itemId"] = gi.Id;
                        World.SchedulerManager.Scheduler(sd);

                        gi.Properties["UpgradedSchedulerId"] = sd.Id.ToString();
                    }
                    return;
                }
            }
        }

        /// <summary>
        /// 升级完成的处理函数。
        /// </summary>
        /// <param name="sender"></param>
        public void LevelUpCompleted(SchedulerDescriptor sender)
        {
            var gcId = sender.Properties.GetGuidOrDefault("charId");
            var giId = sender.Properties.GetGuidOrDefault("itemId");
            using var dwUser = World.CharManager.LockOrLoad(gcId, out var gu);
            if (dwUser is null)
                return;
            var gc = gu.GameChars.FirstOrDefault(c => c.Id == gcId);
            if (gc is null)
                return;
            LevelUpCompleted((gcId, giId));
            if (LastChangesItems.Count > 0)
                gc.ChangesItems.AddRange(LastChangesItems);

        }

        /// <summary>
        /// 通用升级完成的处理函数。
        /// </summary>
        /// <param name="sender"></param>
        public void LevelUpCompleted(object sender)
        {
            var p = (ValueTuple<Guid, Guid>)sender;
            var gcId = p.Item1;
            using var dwUser = World.CharManager.LockOrLoad(gcId, out var gu);
            if (dwUser is null)  //若无法锁定
            {
                //TO DO
                return;
            }
            var gc = gu.GameChars.FirstOrDefault(c => c.Id == gcId);    //角色对象
            if (gc is null)
            {
                //TO DO
                return;
            }
            var gi = gc.AllChildren.FirstOrDefault(c => c.Id == p.Item2);
            if (gi is null)
            {
                //TO DO
                return;
            }
            if (!gi.Name2FastChangingProperty.TryGetValue(ProjectConstant.UpgradeTimeName, out var fcp))    //若已经处理了
                return;
            //升级
            var oldLv = gi.Properties.GetDecimalOrDefault(ProjectConstant.LevelPropertyName);
            World.ItemManager.SetPropertyValue(gi, ProjectConstant.LevelPropertyName, oldLv + 1);
            //通知属性发生变化
            try
            {
                OnLevelUpCompleted(sender);
                gi.InvokeDynamicPropertyChanged(new DynamicPropertyChangedEventArgs(ProjectConstant.LevelPropertyName, oldLv));
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                gi.RemoveFastChangingProperty(ProjectConstant.UpgradeTimeName);
            }
            //扫描成就
            World.MissionManager.ScanAsync(gc);
        }

        /// <summary>
        /// 升级物品结束时被调用。
        /// </summary>
        /// <param name="sender"></param>
        protected virtual void OnLevelUpCompleted(object sender)
        {
            UpgradeCompletedInHomelang(sender);
        }
        #endregion 升级相关

        #region 家园相关

        /// <summary>
        /// 收获家园中玉米，木材等资源放入仓库。
        /// </summary>
        /// <param name="datas"></param>
        private void Harvest(ApplyBlueprintDatas datas)
        {
            if (!datas.Verify(datas.GameItems.Count == 1, "只能升级一个对象。"))
            {
                return;
            }

            GameItem gameItem = datas.GameItems[0];  //收取的容器
            GameChar gameChar = datas.GameChar;
            GameItem hl = datas.Lookup(gameChar.GameItems, ProjectConstant.HomelandSlotId);    //家园
            if (null == hl)
            {
                return;
            }

            GameItem mainBase = datas.Lookup(hl.Children, ProjectConstant.MainBaseSlotId); //主基地
            if (mainBase is null)
            {
                return;
            }

            GameItem src = datas.Lookup(mainBase.Children, gameItem.TemplateId); //收获物
            if (src is null)
            {
                return;
            }

            GameItem destItem = gameItem.TemplateId switch // //目标对象
            {
                _ when gameItem.TemplateId == ProjectConstant.MucaishuTId => gameChar.GetMucai(),   //木材
                _ when gameItem.TemplateId == ProjectConstant.YumitianTId => gameChar.GetJinbi(), //玉米
                _ => null,
            };
            if (!src.TryGetPropertyValueWithFcp("Count", DateTime.UtcNow, true, out object countObj, out DateTime dt) || !OwConvert.TryToDecimal(countObj, out decimal count))
            {
                datas.DebugMessage = "未知原因无法获取收获数量。";
                datas.HasError = true;
                return;
            }
            decimal stc = World.PropertyManager.GetRemainderStc(destItem);  //剩余可堆叠数
            count = Math.Min(count, stc);   //实际移走数量
            if (src.Name2FastChangingProperty.TryGetValue("Count", out FastChangingProperty fcp))    //若有快速变化属性
            {
                fcp.SetLastValue(fcp.LastValue - count, ref dt);
            }
            else
            {
                src.Count -= count;
            }

            destItem.Count += count;
            datas.ChangeItems.AddToChanges(src.GetContainerId().Value, src);
            datas.ChangeItems.AddToChanges(destItem.GetContainerId().Value, destItem);
        }

        /// <summary>
        /// 家园内部相关物品升级。
        /// </summary>
        /// <param name="datas"></param>
        [BlueprintMethod("384ed85c-82fd-4f08-86e7-eae5ad6eef2c")]
        private void UpgradeInHomeland(ApplyBlueprintDatas datas)
        {
            DateTime dt = DateTime.UtcNow;  //尽早确定开始时间
            Guid hlTid = ProjectConstant.HomelandSlotId; //家园Id
            Guid jianzhuBagTid = new Guid("{312612a5-30dd-4e0a-a71d-5074397428fb}");   //建筑背包tid
            if (!datas.Verify(datas.GameItems.Count == 1, "只能升级一个对象。"))
            {
                return;
            }

            GameItem hl = datas.GameChar.GetHomeland();   //家园对象
            GameItem worker = datas.Lookup(hl.Children, ProjectConstant.WorkerOfHomelandTId);
            if (worker is null)
            {
                return;
            }
            GameItem gi = hl.GetAllChildren().FirstOrDefault(c => c.Id == datas.GameItems[0].Id);   //要升级的物品
            var lut = gi.Properties.GetDecimalOrDefault("lut"); //冷却的秒数
            if (lut > 0 && !datas.Verify(worker.Count > 0, "所有建筑工人都在忙", worker.TemplateId))
            {
                return;
            }
            GameItemManager gim = World.ItemManager;
            GameItemTemplate template = gim.GetTemplateFromeId(gi.TemplateId); //物品的模板对象
            #region 等级校验
            if (template.TryGetPropertyValue("mbnlv", out object mbnlvObj) && OwConvert.TryToDecimal(mbnlvObj, out decimal mbnlv))    //若需要根据主控室等级限定升级
            {
                GameItem mb = hl.GetAllChildren().FirstOrDefault(c => c.TemplateId == ProjectConstant.HomelandSlotId);    //主控室
                decimal mbLv = mb.GetDecimalWithFcpOrDefault(GameThingTemplateBase.LevelPrefix, 0m); //当前主控室等级
                if (!datas.Verify(mbLv >= mbnlv, "主控室等级过低，不能升级指定物品。", gi.TemplateId))
                {
                    return;
                }
            }
            var charLv = datas.GameChar.Properties.GetDecimalOrDefault(World.PropertyManager.LevelPropertyName);    //角色等级
            var giLv = gi.Properties.GetDecimalOrDefault(World.PropertyManager.LevelPropertyName); //升级物品等级
            var innerCount = (charLv - giLv); //计算实际可以升级的次数
            if (charLv <= 0)   //若橘色等级不足
            {
                datas.HasError = true;
                datas.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                return;
            }
            #endregion 等级校验

            LevelUp(datas);
            if (datas.HasError)
                return;
            if (!datas.Verify(gi.TryGetPropertyWithFcp(ProjectConstant.LevelPropertyName, out decimal lvDec), "级别属性类型错误。"))
            {
                return;
            }

            int lv = (int)lvDec;    //原等级

            #region 修改属性

            if (gi.Name2FastChangingProperty.TryGetValue(ProjectConstant.UpgradeTimeName, out var fcp)) //若需要冷却
            {
                worker.Count--;
                datas.ChangeItems.AddToChanges(worker.GetContainerId().Value, worker);
            }
            else //立即完成
            {
                UpgradeCompletedInHomelang(ValueTuple.Create(datas.GameChar.Id, gi.Id));
            }
            #endregion 修改属性
            return;
        }

        /// <summary>
        /// 加速完成家园内的升级项目。
        /// </summary>
        /// <param name="datas"></param>
        private void HastenOnHomeland(ApplyBlueprintDatas datas)
        {
            if (!datas.Verify(datas.GameItems.Count == 1, "只能加速一个物品"))
            {
                return;
            }

            GameItem gameItem = datas.GameItems[0];  //加速的物品
            GameItem hl = datas.Lookup(datas.GameChar.GameItems, ProjectConstant.HomelandSlotId);
            if (hl is null) return;

            GameItem worker = datas.Lookup(hl.Children, ProjectConstant.WorkerOfHomelandTId);
            if (worker is null) return;

            if (!datas.Verify(gameItem.Name2FastChangingProperty.TryGetValue(ProjectConstant.UpgradeTimeName, out FastChangingProperty fcp), "物品未进行升级"))
            {
                datas.ErrorItemTIds.Add(gameItem.TemplateId);
                return;
            }
            DateTime dt = DateTime.UtcNow;
            fcp.GetCurrentValue(ref dt);
            if (fcp.LastValue >= fcp.MaxValue)  //若已经完成
            {
                gameItem.RemoveFastChangingProperty(ProjectConstant.UpgradeTimeName);
                datas.ChangeItems.AddToChanges(gameItem.GetContainerId().Value, gameItem);
                return;
            }
            else //若未完成
            {
                DateTime dtComplate = fcp.GetComplateDateTime();
                decimal tm = (decimal)(dtComplate - dt).TotalMinutes;

                decimal cost = tm switch //需要花费的钻石
                {
                    _ when tm <= 5m => 0,
                    _ => Math.Floor(tm) * 10,
                };
                if (cost > 0)   //若需要钻石
                {
                    GameItem dim = datas.Lookup(datas.GameChar.GetCurrencyBag().Children, ProjectConstant.ZuanshiId);    //钻石
                    if (dim is null) return;

                    if (!datas.Verify(dim.Count >= cost, $"需要{cost}钻石,但只有{dim.Count}钻石。"))
                    {
                        datas.ErrorCode = ErrorCodes.RPC_S_OUT_OF_RESOURCES;
                        datas.ErrorItemTIds.Add(dim.TemplateId);
                        return;
                    }
                    dim.Count -= cost;
                    datas.ChangeItems.AddToChanges(dim.GetContainerId().Value, dim);
                }
                var count = worker.Count;
                LevelUpCompleted((datas.GameChar.Id, gameItem.Id));
                //工人
                if (worker.Count != count)
                    datas.ChangeItems.AddToChanges(worker.GetContainerId().Value, worker);
                //追加新建物品
                datas.ChangeItems.AddRange(LastChangesItems); //追加新建物品
                LastChangesItems.Clear();
                gameItem.RemoveFastChangingProperty(ProjectConstant.UpgradeTimeName);
                datas.ChangeItems.AddToChanges(gameItem.GetContainerId().Value, gameItem);
            }

            return;
        }

        /// <summary>
        /// 购买pve次数。
        /// </summary>
        /// <param name="datas">钻石和塔防次数对象会在变化中返回。</param>
        private void BuyPveCount(ApplyBlueprintDatas datas)
        {
            //5，5，10，10，30,0
            //ltlv 记载最后一次升级的时间
            var gc = datas.GameChar;    //角色对象
            var td = datas.Lookup(gc.GetCurrencyBag().Children, ProjectConstant.PveTCounterTId);
            if (td is null) //若无塔防对象
                return;
            if (!datas.Verify(td.Name2FastChangingProperty.TryGetValue("Count", out _), "找不到自动恢复属性。"))
                return;
            var lv = td.GetDecimalWithFcpOrDefault(ProjectConstant.LevelPropertyName);
            DateTime dt = DateTime.UtcNow;  //当前时间
            if (td.TryGetProperty("ltlv", out var ltlvObj) && DateTime.TryParse(ltlvObj as string, out var ltlv))  //若找到上次升级时间属性
            {
                if (dt.Date <= ltlv.Date && !datas.Verify(td.GetTemplate().GetMaxLevel("lud") > lv + 1, "已经用尽全部购买次数。"))
                    return;
            }
            else
            {
                ltlv = dt;
                lv = 0;
            }
            var diam = datas.Lookup(gc.GetCurrencyBag().Children, ProjectConstant.ZuanshiId);//钻石
            if (diam is null)   //若没有钻石
                return;
            if (!datas.Verify(td.TryGetPropertyWithFcp("lud", out var lud), "没有找到升级所需钻石数量。"))
                return;
            if (!datas.Verify(lud <= diam.Count, "钻石不足")) return;
            //修改数据
            var gim = World.ItemManager;
            gim.SetPropertyValue(td, ProjectConstant.LevelPropertyName, lv + 1);    //变更购买价格
            td.SetPropertyValue("ltlv", ltlv.ToString());  //记录购时间
            datas.ChangeItems.AddToChanges(td.GetContainerId().Value, td);
            td.Name2FastChangingProperty["Count"].LastValue++;
            diam.Count -= lud;  //改钻石
            datas.ChangeItems.AddToChanges(diam);
            datas.ChangeItems.AddToChanges(td);
        }
        #endregion 家园相关

        [ContextStatic]
        private static List<ChangeItem> _LastChangesItems;

        /// <summary>
        /// 暂存自然cd得到的物品。
        /// </summary>
        public static List<ChangeItem> LastChangesItems => _LastChangesItems ??= new List<ChangeItem>();

        /// <summary>
        /// 使用指定数据升级或制造物品。
        /// </summary>
        /// <param name="datas"></param>
        public void ApplyBluprint(ApplyBlueprintDatas datas)
        {
            if (datas.Count <= 0)
            {
                datas.HasError = true;
                datas.DebugMessage = "Count必须大于0";
                return;
            }
            GameUser gu = datas.GameChar.GameUser;
            List<GameItem> tmpList;
            if (!World.CharManager.Lock(gu))
            {
                datas.HasError = true;
                datas.DebugMessage = $"无法锁定用户{gu.Id}";
                return;
            }
            try
            {
                _InitializeTask.Wait(); //等待初始化结束
                //获取有效的参与制造物品对象
                tmpList = datas.GameItems.Join(datas.GameChar.AllChildren, c => c.Id, c => c.Id, (l, r) => r).ToList();
                if (tmpList.Count < datas.GameItems.Count)
                {
                    return;
                }

                datas.GameItems.Clear();
                datas.GameItems.AddRange(tmpList);
                if (!Dispatch(datas))
                {
                    datas.HasError = true;
                    datas.DebugMessage = "未知蓝图。";
                    return;
                }
                ChangeItem.Reduce(datas.ChangeItems);    //压缩变化数据
                World.CharManager.NotifyChange(gu);
                if (!datas.HasError) //若无错
                    World.MissionManager.ScanAsync(datas.GameChar);
            }
            catch (Exception err)
            {
                datas.DebugMessage = err.Message + " @" + err.StackTrace;
                datas.HasError = true;
            }
            finally
            {
                World.CharManager.Unlock(gu, true);
            }
        }

        #region 孵化相关

        /// <summary>
        /// 取出孵化物品。
        /// </summary>
        /// <param name="datas"></param>
        public void GetFhResult(ApplyBlueprintDatas datas)
        {
            if (!datas.Verify(datas.GameItems.Count > 0, "参数过少。"))
            {
                return;
            }

            GameItem slotFh = datas.GameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.FuhuaSlotTId); //孵化槽
            if (null == slotFh)
            {
                datas.HasError = true;
                datas.DebugMessage = "找不到孵化槽。";
                return;
            }
            GameItem gameItem = datas.GameItems.Join(slotFh.Children, c => c.Id, c => c.Id, (l, r) => r).FirstOrDefault();    //要取出的物品
            if (null == gameItem)
            {
                datas.HasError = true;
                datas.DebugMessage = "找不到要取出的物品。";
                return;
            }
            if (!datas.Verify(gameItem.Name2FastChangingProperty.TryGetValue("fhcd", out FastChangingProperty fcp), $"孵化物品没有冷却属性。Number = {gameItem.Id}"))
            {
                return;
            }

            if (!datas.Verify(fcp.IsComplate, $"物品没有孵化完成。Number = {gameItem.Id}。")) //若未完成孵化
            {
                return;
            }

            GameItem slotZq = datas.GameChar.GameItems.First(c => c.TemplateId == ProjectConstant.ZuojiBagSlotId);   //坐骑背包
            GameItemManager gim = World.ItemManager;
            Guid headTid = gim.GetHead(gameItem).TemplateId;
            Guid bodyTid = gim.GetBody(gameItem).TemplateId;
            GameItem zq = slotZq.Children.FirstOrDefault(c =>    //找同头同身坐骑
            {
                return c.Children.Any(c2 => c2.TemplateId == headTid) && c.Children.Any(c2 => c2.TemplateId == bodyTid);
            });
            if (null != zq)    //若已经有同种坐骑
            {
                GameItem slotSl = datas.GameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.ShoulanSlotId);
                if (!datas.Verify(null != slotSl, "找不到兽栏。"))
                {
                    return;
                }

                gameItem.Properties["neatk"] = Math.Round(gameItem.GetDecimalWithFcpOrDefault("neatk"), MidpointRounding.AwayFromZero);
                gameItem.Properties["nemhp"] = Math.Round(gameItem.GetDecimalWithFcpOrDefault("nemhp"), MidpointRounding.AwayFromZero);
                gameItem.Properties["neqlt"] = Math.Round(gameItem.GetDecimalWithFcpOrDefault("neqlt"), MidpointRounding.AwayFromZero);
                var oldpid = gameItem.ParentId;
                if (!gim.MoveItem(gameItem, 1, slotSl, datas.ChangeItems))   //若无法放入
                {
                    //发邮件
                    var social = World.SocialManager;
                    var mail = new GameMail()
                    {
                    };
                    mail.Properties["MailTypeId"] = ProjectConstant.孵化补给动物.ToString();
                    social.SendMail(mail, new Guid[] { datas.GameChar.Id }, SocialConstant.FromSystemId,
                        new ValueTuple<GameItem, Guid>[] { (gameItem, ProjectConstant.ShoulanSlotId) });
                    gim.ForcedDelete(gameItem);
                    datas.ChangeItems.AddToRemoves(oldpid.Value, gameItem.Id);
                }

            }
            else //若尚无同种坐骑
            {
                gameItem.Properties["neatk"] = 10m;
                gameItem.Properties["nemhp"] = 10m;
                gameItem.Properties["neqlt"] = 10m;
                gim.MoveItem(gameItem, 1, slotZq, datas.ChangeItems);
            }
            //成就
            var mission = datas.GameChar.GetRenwuSlot().Children.FirstOrDefault(c => c.TemplateId == ProjectMissionConstant.孵化成就);
            if (null != mission)   //若找到成就对象
            {
                var oldVal = mission.Properties.GetDecimalOrDefault(ProjectMissionConstant.指标增量属性名);
                mission.Properties[ProjectMissionConstant.指标增量属性名] = oldVal + 1m; //设置该成就的指标值的增量，原则上都是正值
                World.MissionManager.ScanAsync(datas.GameChar);
            }
            World.CharManager.NotifyChange(datas.GameChar.GameUser);
        }

        /// <summary>
        /// 加速孵化。
        /// </summary>
        /// <param name="datas"></param>
        public void JiasuFuhua(ApplyBlueprintDatas datas)
        {
            GameItem fhSlot = datas.GameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.FuhuaSlotTId);    //孵化槽
            GameItem gameItem = fhSlot.Children.FirstOrDefault(c => c.Id == datas.GameItems[0].Id);  //要加速孵化的物品
            if (!gameItem.Name2FastChangingProperty.TryGetValue("fhcd", out FastChangingProperty fcp))
            {
                datas.HasError = true;
                datas.DebugMessage = "孵化物品没有冷却属性";
                return;
            }
            //计算所需钻石
            GameItem zuanshi = datas.GameChar.GetZuanshi();    //钻石
            DateTime dt = DateTime.UtcNow;
            decimal tm = (fcp.MaxValue - fcp.GetCurrentValue(ref dt)) / 60;  //每分钟10钻
            decimal cost;
            if (tm <= 5)   //若不收费
            {
                cost = 0;
            }
            else
            {
                cost = Math.Ceiling(tm) * 10;
            }

            if (!datas.Verify(cost <= zuanshi.Count, $"需要{cost}钻石,但目前仅有{zuanshi.Count}个钻石。"))
            {
                return;
            }
            //减少钻石
            zuanshi.Count -= cost;
            GameItemManager gim = World.ItemManager;
            datas.ChangeItems.AddToChanges(zuanshi.ParentId ?? zuanshi.OwnerId.Value, zuanshi);
            //修改冷却时间
            fcp.LastValue = fcp.MaxValue;
            fcp.LastDateTime = DateTime.UtcNow;
            datas.ChangeItems.AddToChanges(gameItem.ParentId ?? gameItem.OwnerId.Value, gameItem);
        }

        /// <summary>
        /// 孵化坐骑/动物。
        /// </summary>
        /// <param name="datas"></param>
        [BlueprintMethod("8b4ac76c-d8cc-4300-95ca-668350149821")]
        public void Fuhua(ApplyBlueprintDatas datas)
        {
            var propMng = World.PropertyManager;
            if (!datas.Verify(datas.GameItems.Count == 2, "必须指定双亲"))
                return;
            var jinyinTId = new Guid("{ac7d593c-ce82-4642-97a3-14025da633e4}");
            var jiyin = datas.GameChar.GetItemBag().Children.First(c => c.TemplateId == jinyinTId); //基因蛋
            if (!datas.Verify(null != jiyin && jiyin.Count > 0, "没有基因蛋", jinyinTId))
                return;
            var gim = World.ItemManager;
            var fuhuaSlot = datas.GameChar.GetFuhuaSlot();
            var renCout = propMng.GetRemainderCap(fuhuaSlot);
            if (!datas.Verify(renCout > 0, "孵化槽已经满", fuhuaSlot.TemplateId))
                return;
            var parent1 = datas.GameItems[0];
            var parent2 = datas.GameItems[1];
            var child = FuhuaCore(datas.GameChar, parent1, parent2);
            child.Name2FastChangingProperty.Add("fhcd", new FastChangingProperty(TimeSpan.FromSeconds(1), 1, 3600 * 8, 0, DateTime.UtcNow)
            {
            });
            gim.AddItem(child, fuhuaSlot, null, datas.ChangeItems); //放入孵化槽
            var qiwu = datas.GameChar.GetQiwuBag();
            if (jiyin.Count > 1)    //若尚有剩余基因蛋
            {
                jiyin.Count--;
                datas.ChangeItems.AddToChanges(jiyin);
            }
            else //若基因蛋用完
            {
                gim.MoveItem(jiyin, 1, qiwu, datas.ChangeItems);
            }

            if (parent1.TemplateId == ProjectConstant.HomelandPatCard) //若是卡片
                gim.MoveItem(parent1, 1, qiwu, datas.ChangeItems);
            if (parent2.TemplateId == ProjectConstant.HomelandPatCard) //若是卡片
                gim.MoveItem(parent2, 1, qiwu, datas.ChangeItems);
        }

        /// <summary>
        /// 孵化的核心算法。
        /// </summary>
        /// <param name="gameChar">角色对象。</param>
        /// <param name="parent1">双亲1。可以不是角色拥有的坐骑。</param>
        /// <param name="parent2">双亲2。可以不是角色拥有的坐骑。</param>
        /// <returns>孵化的结果。资质按双亲平均值计算。</returns>
        public GameItem FuhuaCore(GameChar gameChar, GameItem parent1, GameItem parent2)
        {
            var gim = World.ItemManager;
            var t1BodyT = gim.GetBodyTemplate(parent1);
            var t2BodyT = gim.GetBodyTemplate(parent2);
            var tids = gim.GetTujianResult(gameChar, t1BodyT, t2BodyT);
            GameItemTemplate headT, bodyT;  //输出结果的头和身体模板对象
            if (null != tids && VWorld.IsHit((double)tids.Value.Item3))  //若使用图鉴
            {
                headT = gim.GetTemplateFromeId(tids.Value.Item1);
                bodyT = gim.GetTemplateFromeId(tids.Value.Item2);
            }
            else //不用有图鉴
            {
                double probChun = 0.2;  //纯种坐骑的概率

                void action(GameItem c)
                {
                    if (c.TemplateId == ProjectConstant.HomelandPatCard) //若是卡片
                    {
                        var rank = parent1.Properties.GetDecimalOrDefault("nerank");  //等级
                        if (rank >= 3)   //若是高级坐骑
                        {
                            var bd = gim.GetBody(parent1);    //取身体对象
                            var tidString = bd.TemplateId.ToString(); //记录合成次数的键名
                            var suppusCount = gameChar.Properties.GetDecimalOrDefault(tidString); //已经用该卡合成的次数
                            if (suppusCount <= 3) //若尚未达成次数
                            {
                                probChun = 0; //不准出现纯种生物
                            }
                            gameChar.Properties[tidString] = ++suppusCount;
                        }
                    }
                }

                action(parent1); action(parent2);
                if (VWorld.IsHit(probChun))   //若出纯种生物
                {
                    if (VWorld.IsHit(0.5))   //若出a头
                    {
                        headT = gim.GetHeadTemplate(parent1);
                        bodyT = gim.GetBodyTemplate(parent1);
                    }
                    else //若出b头
                    {
                        headT = gim.GetHeadTemplate(parent2);
                        bodyT = gim.GetBodyTemplate(parent2);
                    }

                }
                else //出杂交生物
                {
                    if (VWorld.IsHit(0.5))   //若出a头
                    {
                        headT = gim.GetHeadTemplate(parent1);
                        bodyT = gim.GetBodyTemplate(parent2);
                    }
                    else //若出b头
                    {
                        headT = gim.GetHeadTemplate(parent2);
                        bodyT = gim.GetBodyTemplate(parent1);
                    }
                }
            }

            GameItem result = gim.CreateMounts(headT, bodyT);
            SetNe(result, parent1, parent2);    //设置天赋值
            return result;
        }

        /// <summary>
        /// 设置杂交后的天赋。
        /// </summary>
        /// <param name="child">要设置的对象。</param>
        /// <param name="parent1">双亲1。</param>
        /// <param name="parent1">双亲2。</param>
        private void SetNe(GameItem child, GameItem parent1, GameItem parent2)
        {
            var rank1 = parent1.Properties.GetDecimalOrDefault("nerank");
            var rank2 = parent2.Properties.GetDecimalOrDefault("nerank");

            var ne1 = parent1.Properties.GetDecimalOrDefault("neatk");
            var ne2 = parent2.Properties.GetDecimalOrDefault("neatk");
            var atk = Math.Clamp((ne1 * rank1 * 0.15m + ne2 * rank2 * 0.15m) / 2 + VWorld.WorldRandom.Next(-50, 51), 0, 100);

            ne1 = parent1.Properties.GetDecimalOrDefault("nemhp");
            ne2 = parent2.Properties.GetDecimalOrDefault("nemhp");
            var mhp = Math.Clamp((ne1 * rank1 * 0.15m + ne2 * rank2 * 0.15m) / 2 + VWorld.WorldRandom.Next(-50, 51), 0, 100);

            ne1 = parent1.Properties.GetDecimalOrDefault("neqlt");
            ne2 = parent2.Properties.GetDecimalOrDefault("neqlt");
            var qlt = Math.Clamp((ne1 * rank1 * 0.15m + ne2 * rank2 * 0.15m) / 2 + VWorld.WorldRandom.Next(-50, 51), 0, 100);
            child.Properties["atk"] = atk;
            child.Properties["mhp"] = mhp;
            child.Properties["qlt"] = qlt;
        }
        #endregion 孵化相关

        #region 合成相关

        /// <summary>
        /// 资质合成。
        /// </summary>
        /// <param name="datas"></param>
        public void Hecheng(ApplyBlueprintDatas datas)
        {
            if (!datas.Verify(datas.GameItems.Count == 2, $"物品数量错误。"))
            {
                return;
            }

            GameChar gc = datas.GameChar;
            GameItem lockAtk = datas.Lookup(gc.GameItems, ProjectConstant.LockAtkSlotId);
            if (lockAtk is null)
            {
                return;
            }

            GameItem lockMhp = datas.Lookup(gc.GameItems, ProjectConstant.LockMhpSlotId);
            if (lockMhp is null)
            {
                return;
            }

            GameItem lockQlt = datas.Lookup(gc.GameItems, ProjectConstant.LockQltSlotId);
            if (lockQlt is null)
            {
                return;
            }

            GameItem gameItem = datas.GameItems.FirstOrDefault(c => c.Parent.TemplateId == ProjectConstant.ZuojiBagSlotId);
            if (!datas.Verify(null != gameItem, "没有坐骑。"))
            {
                return;
            }

            GameItem gameItem2 = datas.GameItems.FirstOrDefault(c => c.Parent.TemplateId == ProjectConstant.ShoulanSlotId);
            if (!datas.Verify(null != gameItem2, "没有野兽。"))
            {
                return;
            }

            DbContext db = datas.GameChar.GameUser.DbContext;
            GameItemManager gim = World.ItemManager;
            //攻击资质
            if (lockAtk.Children.Count <= 0)
            {
                double rnd1 = VWorld.GetRandomNumber(0, 1);
                decimal rnd2 = (decimal)(rnd1 switch
                {
                    _ when rnd1 < 0.15 => VWorld.GetRandomNumber(0, 0.2),
                    _ when rnd1 >= 0.15 && rnd1 < 0.85 => VWorld.GetRandomNumber(0.25, 0.75),
                    _ => VWorld.GetRandomNumber(0.75, 1),
                });
                decimal ne = gameItem.GetDecimalWithFcpOrDefault("neatk") * (1 - rnd2) + gameItem2.GetDecimalWithFcpOrDefault("neatk") * rnd2;
                ne = Math.Round(ne, MidpointRounding.AwayFromZero);
                gameItem.SetPropertyValue("neatk", ne);
            }
            else
            {
                GameItem lockItem = lockAtk.Children.First();
                datas.ChangeItems.AddToRemoves(lockItem.GetContainerId().Value, lockItem.Id);
                gim.ForcedDelete(lockItem);
            }
            //血量资质
            if (lockMhp.Children.Count <= 0)
            {
                double rnd1 = VWorld.GetRandomNumber(0, 1);
                decimal rnd2 = (decimal)(rnd1 switch
                {
                    _ when rnd1 < 0.15 => VWorld.GetRandomNumber(0, 0.2),
                    _ when rnd1 >= 0.15 && rnd1 < 0.85 => VWorld.GetRandomNumber(0.25, 0.75),
                    _ => VWorld.GetRandomNumber(0.75, 1),
                });
                decimal ne = gameItem.GetDecimalWithFcpOrDefault("nemhp") * (1 - rnd2) + gameItem2.GetDecimalWithFcpOrDefault("nemhp") * rnd2;
                ne = Math.Round(ne, MidpointRounding.AwayFromZero);
                gameItem.SetPropertyValue("nemhp", ne);
            }
            else
            {
                GameItem lockItem = lockMhp.Children.First();
                datas.ChangeItems.AddToRemoves(lockItem.GetContainerId().Value, lockItem.Id);
                gim.ForcedDelete(lockItem);
            }
            //质量资质
            if (lockQlt.Children.Count <= 0)
            {
                double rnd1 = VWorld.GetRandomNumber(0, 1);
                decimal rnd2 = (decimal)(rnd1 switch
                {
                    _ when rnd1 < 0.15 => VWorld.GetRandomNumber(0, 0.2),
                    _ when rnd1 >= 0.15 && rnd1 < 0.85 => VWorld.GetRandomNumber(0.25, 0.75),
                    _ => VWorld.GetRandomNumber(0.75, 1),
                });
                decimal ne = gameItem.GetDecimalWithFcpOrDefault("neqlt") * (1 - rnd2) + gameItem2.GetDecimalWithFcpOrDefault("neqlt") * rnd2;
                ne = Math.Round(ne, MidpointRounding.AwayFromZero);
                gameItem.SetPropertyValue("neqlt", ne);
            }
            else
            {
                GameItem lockItem = lockQlt.Children.First();
                datas.ChangeItems.AddToRemoves(lockItem.GetContainerId().Value, lockItem.Id);
                gim.ForcedDelete(lockItem);
            }
            datas.ChangeItems.AddToRemoves(gameItem2.GetContainerId().Value, gameItem2.Id);
            gim.ForcedDelete(gameItem2);
            datas.ChangeItems.AddToChanges(gameItem.GetContainerId().Value, gameItem);
        }
        #endregion 合成相关

        #region 技能升级相关

        /// <summary>
        /// 主动技能升级。
        /// </summary>
        /// <param name="datas"></param>
        [BlueprintMethod("{B0680E76-809F-4C23-98A1-BE330816BF39}")] //主动技能升级
        public void ZhudongLU(ApplyBlueprintDatas datas)
        {
            const string prefix = "sk"; //前缀
            var giIds = new HashSet<Guid>(datas.GameItems.Select(c => c.Id));   //Id集合
            var gi = datas.GameChar.GetZuojiBag().Children.FirstOrDefault(c => giIds.Contains(c.Id));   //可能的坐骑
            if (gi is null)  //若没有找到坐骑
            {
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.DebugMessage = "没有找到坐骑";
                return;
            }
            //获取耗材
            var tt = World.ItemManager.GetBodyTemplate(gi); //获取模板
            var costTId = tt.Properties.GetGuidOrDefault($"{prefix}sh"); //获取耗材对象TId
            SkillLUCore(datas, prefix, costTId);
        }

        /// <summary>
        /// 设计技能的核心逻辑。
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="prefix"></param>
        /// <param name="costTId">耗材的模板id。</param>
        void SkillLUCore(ApplyBlueprintDatas datas, string prefix, Guid costTId)
        {
            var giIds = new HashSet<Guid>(datas.GameItems.Select(c => c.Id));   //Id集合
            var gi = datas.GameChar.GetZuojiBag().Children.FirstOrDefault(c => giIds.Contains(c.Id));   //可能的坐骑
            if (gi is null)  //若没有找到坐骑
            {
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.DebugMessage = "没有找到坐骑";
                return;
            }
            var seq = gi.GetTemplate().Properties.GetValueOrDefault($"{prefix}shuse") as decimal[];    //获取消耗资源序列
            //获取耗材
            var haocai = datas.GameChar.GetItemBag().Children.FirstOrDefault(c => c.TemplateId == costTId);
            if (haocai is null)
            {
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.DebugMessage = "没有升级所需道具。";
                return;
            }
            for (int i = datas.Count - 1; i >= 0; i--)  //逐次升级
            {
                var lv = (int)gi.Properties.GetDecimalOrDefault($"{prefix}lv");  //获取当前技能等级
                if (lv >= seq.Length)   //若已经到达最后级别
                {
                    if (i == datas.Count - 1)    //若没有升级成功过
                    {
                        datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                        datas.DebugMessage = "已达顶级";
                        return;
                    }
                    else //若已升级到顶
                    {
                        return; //不报错
                    }
                }
                var cost = Math.Abs(seq[lv]);   //耗材消耗数量
                //验证资源
                if (haocai.Count - cost < 0) //若耗材不够
                {
                    if (i == datas.Count - 1)    //若没有升级成功过
                    {
                        datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                        datas.DebugMessage = "耗材不足";
                        return;
                    }
                    else //若曾经成功过
                    {
                        return;
                    }
                }
                //修改数据
                haocai.Count -= cost;
                if (haocai.Count <= 0) //若耗材已经用完
                {
                    datas.ChangeItems.AddToRemoves(haocai.ParentId.Value, haocai.Id);
                    World.ItemManager.ForcedDelete(haocai);
                }
                else
                {
                    datas.ChangeItems.AddToChanges(haocai);
                }
                gi.Properties[$"{prefix}lv"] = (decimal)lv + 1;
                datas.ChangeItems.AddToChanges(gi);
                datas.SuccCount++;
            }
            ChangeItem.Reduce(datas.ChangeItems);
            if (datas.SuccCount > 0)
                World.CharManager.NotifyChange(datas.GameChar.GameUser);
        }

        /// <summary>
        /// 被动技能升级。
        /// </summary>
        /// <param name="datas"></param>
        [BlueprintMethod("{2CD84BF4-26F5-48DA-A463-289779C28DCA}")] //被动技能升级
        public void BeidongLU(ApplyBlueprintDatas datas)
        {
            const string prefix = "ps"; //前缀
            var giIds = new HashSet<Guid>(datas.GameItems.Select(c => c.Id));   //Id集合
            var gi = datas.GameChar.GetZuojiBag().Children.FirstOrDefault(c => giIds.Contains(c.Id));   //可能的坐骑
            if (gi is null)  //若没有找到坐骑
            {
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.DebugMessage = "没有找到坐骑";
                return;
            }
            //获取耗材
            var tt = World.ItemManager.GetHeadTemplate(gi); //获取模板
            var costTId = tt.Properties.GetGuidOrDefault($"{prefix}sh"); //获取耗材对象TId
            SkillLUCore(datas, prefix, costTId);
        }

        /// <summary>
        /// 跳跃技能升级。
        /// </summary>
        /// <param name="datas"></param>
        [BlueprintMethod("{DC5D14A9-CB50-440B-BB88-487A4CC663D9}")] //跳跃技能升级
        public void TiaoyueLU(ApplyBlueprintDatas datas)
        {
            const string prefix = "js"; //前缀
            var giIds = new HashSet<Guid>(datas.GameItems.Select(c => c.Id));   //Id集合
            var gi = datas.GameChar.GetZuojiBag().Children.FirstOrDefault(c => giIds.Contains(c.Id));   //可能的坐骑
            if (gi is null)  //若没有找到坐骑
            {
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.DebugMessage = "没有找到坐骑";
                return;
            }
            //获取耗材
            var tt = World.ItemManager.GetHeadTemplate(gi); //获取模板
            var costTId = tt.Properties.GetGuidOrDefault($"{prefix}sh"); //获取耗材对象TId
            SkillLUCore(datas, prefix, costTId);
        }

        #endregion 技能升级相关

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Verify(ApplyBlueprintDatas datas, IEnumerable<GameItem> gameItems, Guid containerTId, Guid itemTId)
        {
            GameItemTemplateManager gitm = World.ItemTemplateManager;
            GameItemTemplate cTemplate = gitm.GetTemplateFromeId(containerTId);
            if (datas.Verify(cTemplate != null, $"无法找到指定容器模板，Number = {containerTId}"))
            {
                return false;
            }

            GameItem container = gameItems.FirstOrDefault(c => c.TemplateId == containerTId);
            datas.Verify(container != null, $"无法找到指定模板Id的容器，模板Id = {containerTId}");
            return true;
        }

        /// <summary>
        /// 钻石买体力。
        /// </summary>
        /// <param name="datas"></param>
        [BlueprintMethod("7b1348b8-87de-4c98-98b8-4705340e1ed2")]
        public void AddTili(ApplyBlueprintDatas datas)
        {
            var tili = datas.GameChar.GetTili();
            var fcp = tili?.Name2FastChangingProperty.GetValueOrDefault("Count");
            if (!datas.Verify(fcp != null, "无法找到体力属性"))
            {
                return;
            }

            GameItem zuanshi = datas.GameChar.GetZuanshi();
            if (!datas.Verify(zuanshi != null, "无法找到钻石对象"))
            {
                return;
            }

            if (!datas.Verify(zuanshi.Count >= 200, $"需要200钻石，但目前仅有{zuanshi.Count}。"))
            {
                return;
            }

            fcp.GetCurrentValueWithUtc();
            fcp.LastValue += 20;    //无视限制增加体力
            tili.Count = fcp.LastValue; //
            zuanshi.Count -= 20 * 10;    //扣除钻石
            datas.ChangeItems.AddToChanges(datas.GameChar.Id, zuanshi);
            datas.ChangeItems.AddToChanges(tili);
        }

        #region 社交相关

        #endregion 社交相关

        #region 物品使用

        /// <summary>
        /// 使用动物商店礼包。
        /// </summary>
        /// <param name="datas"></param>
        [BlueprintMethod("{7c913496-da0f-443a-8f7a-d19e9f6c536e}")]
        public void UseItemc7c913496da0f443a8f7ad19e9f6c536e(ApplyBlueprintDatas datas)
        {
            using var dw = datas.LockUser();
            if (dw is null)
                return;
            for (int i = 0; i < datas.Count; i++)
            {
                var gi = datas.GameItems[0];
                var tt = gi.GetTemplate();
                var coll = tt.Properties.GetValuesWithoutPrefix("use");
                var htid = tt.Properties.GetGuidOrDefault("usehtid");
                var btid = tt.Properties.GetGuidOrDefault("usebtid");
                var tid = tt.Properties.GetGuidOrDefault("usetid");
                var mounts = World.ItemManager.CreateMounts(htid, btid, tid);
                if (World.ItemManager.IsExistsMounts(mounts, datas.GameChar)) //若存在此纯种坐骑
                {
                    mounts.Properties["neatk"] = 80 + VWorld.WorldRandom.Next(21);
                    mounts.Properties["nemhp"] = 80 + VWorld.WorldRandom.Next(21);
                    mounts.Properties["neqlt"] = 80 + VWorld.WorldRandom.Next(21);
                    World.ItemManager.AddItem(mounts, datas.GameChar.GetShoulanBag(), null, datas.ChangeItems);
                }
                else //若不存在该纯种坐骑
                {
                    World.ItemManager.AddItem(mounts, datas.GameChar.GetZuojiBag(), null, datas.ChangeItems);
                }
            }
        }

        /// <summary>
        /// 防护罩使用。
        /// </summary>
        /// <param name="datas"></param>
        [BlueprintMethod("{5b6dbed0-5323-49ea-aa55-9093dc9f1007}")]
        public void UseItem5b6dbed0532349eaaa559093dc9f1007(ApplyBlueprintDatas datas)
        {
            using var dw = datas.LockUser();
            if (dw is null)
                return;
            var gc = datas.GameChar;
            using var view = new PvpWarFreeCardsView(World, gc) { NowUtc = DateTime.UtcNow };
            for (int i = 0; i < datas.Count; i++)
            {
                if (view.ExpireUtc.HasValue && view.ExpireUtc > view.NowUtc && view.ExpireUtc - view.NowUtc > view.Uts) //若不可重叠
                {
                    datas.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                    break;
                }
                var gi = datas.GameItems[0];
                var kts = gi.GetDecimalWithFcpOrDefault("kts");
                view.ExpireUtc ??= view.NowUtc;
                view.ExpireUtc += TimeSpan.FromSeconds((double)kts);
            }
            view.Save();
        }

        /// <summary>
        /// 免战信息视图。
        /// </summary>
        public class PvpWarFreeCardsView : GameCharWorkDataBase
        {
            public PvpWarFreeCardsView([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
            {
            }

            public PvpWarFreeCardsView([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
            {
            }

            public PvpWarFreeCardsView([NotNull] VWorld world, [NotNull] string token) : base(world, token)
            {
            }

            /// <summary>
            /// 角色是否在免战状态。
            /// </summary>
            public bool IsWarFree
            {
                get
                {
                    return ExpireUtc >= NowUtc;
                }
            }

            /// <summary>
            /// 当前时间。
            /// </summary>
            public DateTime NowUtc { get; set; }

            /// <summary>
            /// 到期时间。
            /// </summary>
            public DateTime? ExpireUtc
            {
                get
                {
                    var tmp = GameChar.Properties.GetDateTimeOrDefault("PvpWarFreeCardsExpire", DateTime.MinValue);
                    if (tmp == DateTime.MinValue)
                        return null;
                    return tmp;
                }
                set
                {
                    if (value is null)
                        GameChar.Properties.Remove("PvpWarFreeCardsExpire");
                    else
                        GameChar.Properties["PvpWarFreeCardsExpire"] = value.Value.ToString("s");
                }
            }

            /// <summary>
            /// 允许的重叠使用时间。
            /// </summary>
            public TimeSpan Uts
            {
                get => TimeSpan.FromSeconds((double)GameChar.Properties.GetDecimalOrDefault("uts"));
                set => GameChar.Properties["uts"] = (decimal)value.TotalSeconds;
            }

            /// <summary>
            /// 保存数据。
            /// </summary>
            public override void Save()
            {
                base.Save();
            }
        }
        #endregion 物品使用
    }

    /// <summary>
    /// 
    /// </summary>
    public static class ApplyBlueprintDatasExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Verify(this ApplyBlueprintDatas obj, bool succ, string errorMessage, params Guid[] errorItemTIds)
        {
            if (!succ)
            {
                obj.ErrorItemTIds.AddRange(errorItemTIds);
                obj.DebugMessage = errorMessage;
                obj.HasError = true;
            }
            return succ;
        }

        /// <summary>
        /// 在指定的集合中寻找指定模板Id 或和 物品Id的物品。
        /// 无法找到时，自动填写<see cref="ApplyBlueprintDatas"/>中数据。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="parent"></param>
        /// <param name="templateId"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static GameItem Lookup(this ApplyBlueprintDatas obj, IEnumerable<GameItem> parent, Guid? templateId, Guid? id = null)
        {
            IEnumerable<GameItem> resultColl;
            if (templateId.HasValue)    //若需限定模板Id
            {
                Guid tid = templateId.Value;
                resultColl = parent.Where(c => c.TemplateId == tid);
            }
            else
            {
                resultColl = parent;
            }

            GameItem result;
            if (id.HasValue) //若要限定Id
            {
                Guid idMe = id.Value;
                result = resultColl.FirstOrDefault(c => c.Id == idMe);
            }
            else
            {
                result = resultColl.FirstOrDefault();
            }

            if (result is null)  //若没有找到
            {
                obj.DebugMessage = $"无法找到物品。TemplateId={templateId},Number={id}";
                obj.HasError = true;
                if (templateId.HasValue)
                {
                    obj.ErrorItemTIds.Add(templateId.Value);
                }
            }
            return result;
        }
    }

    /// <summary>
    /// 主基地升级送物配置类。
    /// </summary>
    public class MainbaseUpgradePrv
    {
        /// <summary>
        /// 送的等级，如1，就是升级到1级时送品
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// 如果有值，则是送品地块的位置号。
        /// </summary>
        public int? Genus { get; set; }

        /// <summary>
        /// 送品的模板Id。
        /// </summary>
        public Guid? PrvTId { get; set; }

        /// <summary>
        /// 送品的父容器模板Id。
        /// </summary>
        public Guid ParentTId { get; set; }

        /// <summary>
        /// 注释，服务器不使用。
        /// </summary>
        public string Remark { get; set; }

        private static List<MainbaseUpgradePrv> _Alls;
        public static List<MainbaseUpgradePrv> Alls
        {
            get
            {
                if (_Alls is null)
                {
                    lock (typeof(MainbaseUpgradePrv))
                    {
                        if (_Alls is null)
                        {
                            using var sr = File.OpenText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "主控室升级附送物品设置.txt"));
                            _Alls = new List<MainbaseUpgradePrv>();
                            for (string line = sr.ReadLine(); null != line; line = sr.ReadLine())
                            {
                                if (string.IsNullOrWhiteSpace(line))
                                {
                                    continue;
                                }

                                string[] ary = line.Split('\t', StringSplitOptions.None);
                                MainbaseUpgradePrv item = new MainbaseUpgradePrv()
                                {
                                    Level = int.Parse(ary[0]),
                                    Genus = int.TryParse(ary[2], out int gns) ? gns : null as int?,
                                    ParentTId = Guid.Parse(ary[3]),
                                    PrvTId = string.IsNullOrWhiteSpace(ary[1]) ? null as Guid? : Guid.Parse(ary[1]),
                                    Remark = ary[4],
                                };
                                _Alls.Add(item);
                            }
                        }
                    }
                }

                return _Alls;
            }
        }
    }
}
