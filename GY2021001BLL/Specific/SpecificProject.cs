using Game.Social;
using GuangYuan.GY001.BLL.Homeland;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OW.Game;
using OW.Game.Item;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace GuangYuan.GY001.BLL
{
    public static class ProjectMissionConstant
    {
        public static readonly Guid 玩家等级成就 = new Guid("{25FFBEE1-F617-49BD-B0DE-32B3E3E975CB}");
        public static readonly Guid 关卡成就 = new Guid("{96a36fbe-f79a-4579-932e-588772436da5}");
        public static readonly Guid 坐骑最高等级成就 = new Guid("{2f48528e-fd7f-4269-92c9-dbd6f14ffef0}");
        public static readonly Guid LV20坐骑数量 = new Guid("{7d5ad309-2614-434e-b8d3-afe4db93d8b3}");
        public static readonly Guid 关卡模式总战力成就 = new Guid("{8bba8a00-e767-4a6a-aa6b-22ef03a3f527}");
        public static readonly Guid 纯种坐骑数量成就 = new Guid("{49ee3541-3a6e-4d05-85b0-566c6bfecde2}");
        public static readonly Guid 孵化成就 = new Guid("{814e47cd-8bdf-4efc-bd26-61af57b7fcf8}");
        public static readonly Guid 最高资质成就 = new Guid("{0c29f28b-d3ac-4f44-8c41-8d279fd319b5}");
        public static readonly Guid 最高神纹等级成就 = new Guid("{6ffc1f03-1c8e-4f7c-bc88-717e42eae59b}");
        public static readonly Guid 神纹突破次数成就 = new Guid("{4b708b18-e0a3-4388-866f-56d0c6a6da0d}");
        public static readonly Guid 累计访问好友天次成就 = new Guid("{42d3236c-ea7c-4444-898e-469aac1fda07}");
        public static readonly Guid 累计塔防模式次数成就 = new Guid("{5c3d9daf-fe89-43a4-93f8-7abdc85418e5}");
        public static readonly Guid PVP进攻成就 = new Guid("{6f8f5d48-e4b4-4e37-a48f-f8b6badc6f44}");
        public static readonly Guid PVP防御成就 = new Guid("{c20cc819-dc76-482f-a3c4-cfd32b8b83c7}");
        public static readonly Guid PVP助战成就 = new Guid("{6817d0d6-ad3d-4dd1-a8f5-4368ac5a568d}");
        public static readonly Guid 方舟成就 = new Guid("{530efb1e-fc5d-4638-a728-e069431b197a}");
        public static readonly Guid 炮塔成就 = new Guid("{26c63192-867a-43f4-919b-10a614ee2865}");
        public static readonly Guid 陷阱成就 = new Guid("{03d80847-f273-413b-a2a2-81545ab03a89}");
        public static readonly Guid 旗帜成就 = new Guid("{5af7a4f2-9ba9-44e0-b368-1aa1bd9aed6d}");
        public const string 指标增量属性名 = "diffmetrics";
    }

    /// <summary>
    /// 该项目使用的特定常量。
    /// </summary>
    public static class ProjectConstant
    {
        #region 固定模板Id

        #region 废弃模板Id

        /// <summary>
        /// 当前装备的坐骑头容器模板Id。已废弃。
        /// </summary>
        [Obsolete("下个版本可能会被删除。")]
        public static readonly Guid ZuojiTou = new Guid("{A06B7496-F631-4D51-9872-A2CC84A56EAB}");

        /// <summary>
        /// 当前装备的坐骑身体容器模板Id。已废弃
        /// </summary>
        [Obsolete("下个版本可能会被删除。")]
        public static readonly Guid ZuojiShen = new Guid("{7D191539-11E1-49CD-8D0C-82E3E5B04D31}");

        /// <summary>
        /// 神纹装备槽。
        /// </summary>
        public static readonly Guid ShenWenBagSlotId = new Guid("88A4EED6-0AEB-4A70-8FDE-67F75E5E2C0A");

        /// <summary>
        /// 当前坐骑的容器Id。出战坐骑。
        /// </summary>
        [Obsolete("下个版本可能会被删除。")]
        public static readonly Guid DangqianZuoqiSlotId = new Guid("{B19EE5AB-57E3-4513-8228-9F2A8364358E}");

        /// <summary>
        /// 坐骑组合中的身体容器Id。
        /// </summary>
        [Obsolete("下个版本可能会被删除。")]
        public static readonly Guid ZuojiZuheShenti = new Guid("{F8B1987D-FDF3-4090-9E9B-EBAF1DB2DCCD}");

        /// <summary>
        /// 坐骑组合中的头容器Id。
        /// </summary>
        [Obsolete("下个版本可能会被删除。")]
        public static readonly Guid ZuojiZuheTou = new Guid("{740FEBF3-7472-43CB-8A10-798F6C61335B}");

        #endregion 废弃模板Id

        #region 坐骑相关Id

        /// <summary>
        /// 坐骑头和身体需要一个容器组合起来。此类容器的模板Id就是这个。
        /// </summary>
        public static readonly Guid ZuojiZuheRongqi = new Guid("{6E179D54-5836-4E0B-B30D-756BD07FF196}");

        #endregion 坐骑相关Id

        #region 角色直属槽及其相关

        /// <summary>
        /// 神纹槽Id。放在此槽中是装备的神纹。当前每种类型的野兽身体对应一种神纹。
        /// </summary>
        public static readonly Guid ShenWenSlotId = new Guid("{88A4EED6-0AEB-4A70-8FDE-67F75E5E2C0A}");

        /// <summary>
        /// 道具背包槽Id。这个就是初期规划的神纹碎片背包。
        /// </summary>
        public static readonly Guid DaojuBagSlotId = new Guid("{2BAA3FCD-2BE8-4096-916A-FF2D47E084EF}");

        /// <summary>
        /// 战斗收益槽。如果处于战斗中，此槽内表示大关的的总收益，用于计算收益限制。若不在战斗中，此槽为空（其中物品移动到各种背包中）。
        /// </summary>
        public static readonly Guid ShouyiSlotId = new Guid("{FEA0B277-8CC6-462F-B0ED-85409ABE9C79}");

        /// <summary>
        /// 兽栏槽Id，抓捕的野兽存于此槽内。
        /// </summary>
        public static readonly Guid ShoulanSlotId = new Guid("{1630A0A1-3540-479A-B2C5-10B63E7A5774}");

        /// <summary>
        /// 坐骑背包Id。
        /// </summary>
        public static readonly Guid ZuojiBagSlotId = new Guid("{BA2AEE89-0BC3-4612-B6FF-5DDFEF85C9E5}");

        /// <summary>
        /// 货币袋模板Id。
        /// </summary>
        public static readonly Guid CurrencyBagTId = new Guid("{7066A96D-F514-42C7-A30E-5E7567900AD4}");

        /// <summary>
        /// 弃物槽模板Id。
        /// </summary>
        public static readonly Guid QiwuBagTId = new Guid("{346A2F55-9CE8-47DE-B0E0-525FFB765A93}");

        /// <summary>
        /// 图鉴背包模板Id。
        /// </summary>
        public static readonly Guid TujianBagTId = new Guid("{6437ce7b-8a03-4e67-9f89-8c9ab7141263}");

        /// <summary>
        /// 任务/成就槽模板Id。
        /// </summary>
        public static readonly Guid RenwuSlotTId = new Guid("{46005860-9DD8-4932-9DE1-D89E4161044E}");

        #endregion  角色直属槽及其相关

        #region 货币类模板Id
        /// <summary>
        /// 金币Id，这个不是槽，它的Count属性直接记录了金币数，目前其子代为空。这个省事，但未来在金币袋上开脑洞，不能保证不变。
        /// </summary>
        public static readonly Guid JinbiId = new Guid("{2B83C942-1E9C-4B45-9816-AD2CBF0E473F}");

        /// <summary>
        /// 木材Id，这个不是槽，它的Count属性直接记录了数量，目前其子代为空。
        /// </summary>
        public static readonly Guid MucaiId = new Guid("{01959584-E2C9-4E54-BBB7-FCC58A9484EC}");

        /// <summary>
        /// 钻石Id，这个不是槽，它的Count属性直接记录了数量，目前其子代为空。
        /// </summary>
        public static readonly Guid ZuanshiId = new Guid("{3E365BEC-F83D-467D-A58C-9EBA43458682}");

        /// <summary>
        /// 体力Id，这个不是槽，它的Count属性直接记录了数量，目前其子代为空。
        /// </summary>
        public static readonly Guid TiliId = new Guid("{99B4CD0D-CBFA-4851-9F6A-0035F4685E77}");

        /// <summary>
        /// 塔防PVE次数的记录对象模板Id。
        /// </summary>
        public static readonly Guid PveTCounterTId = new Guid("{D56E11C8-48AA-4787-822B-CE4EBBFA684D}");

        /// <summary>
        /// 友情商店货币。
        /// </summary>
        public static readonly Guid FriendCurrencyTId = new Guid("{8DBBFD26-6B4B-4C00-B0B8-BD7A79B21CBA}");

        /// <summary>
        /// PVP数据记录对象的模板Id。
        /// 这也是一种货币。
        /// </summary>
        public static readonly Guid PvpObjectTId = new Guid("{D1A2750B-9300-4C57-A407-941EC1024B1C}");

        /// <summary>
        /// PVP大关卡模板Id。
        /// </summary>
        public static readonly Guid PvpTId = new Guid("{4805434E-605E-4479-B426-9A27C083D7D4}");

        #endregion  货币类模板Id

        #region 邮件类型Id
        public static readonly Guid 友情孵化补给动物 = new Guid("b4c30a07-2179-435e-b053-fd4b0c36251b");

        public static readonly Guid 孵化补给动物 = new Guid("{366ce206-8d17-47ea-a039-9280ddd81bbc}");

        public static readonly Guid PVP系统奖励 = new Guid("{e7b07795-2051-4e3f-a5bc-b668bbebeb36}");

        public static readonly Guid PVP反击邮件 = new Guid("{83c52c66-991f-489b-8b49-6c6c8af25fa5}");

        public static readonly Guid PVP反击邮件_自己_胜利 = new Guid("{62e2da38-3212-43db-b314-5e545d8a1cb4}");

        public static readonly Guid PVP反击邮件_自己_失败 = new Guid("{d885b6cb-5b19-4cac-b2cd-3c550d9f9e28}");

        public static readonly Guid PVP反击邮件_被求助者_求助 = new Guid("{398d16bc-8574-4206-81a7-6729314f25f1}");

        public static readonly Guid PVP反击邮件_求助_胜利_求助者 = new Guid("{12860818-95cb-4030-a048-06c5a1929675}");

        public static readonly Guid PVP反击邮件_求助_失败_求助者 = new Guid("{90e8b5a2-d6b9-45f2-a7f6-26d2ab532886}");

        public static readonly Guid PVP反击_自己_两项全失败 = new Guid("{391c4e23-bc0a-4b75-bab8-60deea1d0a1d}");

        #endregion 邮件类型Id
        /// <summary>
        /// 角色模板Id。当前只有一个模板。
        /// </summary>
        public static readonly Guid CharTemplateId = new Guid("{0CF39269-6301-470B-8527-07AF29C5EEEC}");

        public static readonly Guid LockAtkSlotId = new Guid("{82b18ec6-9190-4804-81b5-33ffa0351ade}");
        public static readonly Guid LockMhpSlotId = new Guid("{b0a92419-6daa-41c8-9074-957175fd9c3b}");
        public static readonly Guid LockQltSlotId = new Guid("{b10c4510-0c8e-40ad-87bb-6f5828273e29}");

        /// <summary>
        /// 神纹碎片的模板Id。
        /// </summary>
        public static readonly Guid RunesId = new Guid("{2B86FF50-0257-4913-8BEC-F5CF3C84B6D5}");

        #region 家园及相关

        /// <summary>
        /// 家园坐骑互动结果卡。
        /// </summary>
        public static readonly Guid HomelandPatCard = new Guid("{8BA64889-63D4-4CCE-A7CC-9CF29ECE73ED}");

        /// <summary>
        /// 家园模板Id。
        /// </summary>
        public static readonly Guid HomelandSlotId = new Guid("{3a855606-a5ee-459b-b1ed-76e9b5847d7d}");

        /// <summary>
        /// 主基地模板Id。
        /// </summary>
        public static readonly Guid MainBaseSlotId = new Guid("{234f8c55-4c3c-4406-ad38-081d29564f20}");

        /// <summary>
        /// 主控室模板Id。
        /// </summary>
        public static readonly Guid MainControlRoomSlotId = new Guid("{fb9ccb27-05c2-4df1-ae21-345eeaace08f}");

        /// <summary>
        /// 家园工人模板Id。
        /// </summary>
        public static readonly Guid WorkerOfHomelandTId = new Guid("3b70d798-4969-443a-b081-b05a966002e5");

        /// <summary>
        /// 玉米田（玉米）模板Id。
        /// </summary>
        public static readonly Guid YumitianTId = new Guid("{7a00740c-035e-4846-a619-2d0855f60b55}");

        /// <summary>
        /// 木材树（木材）模板Id。
        /// </summary>
        public static readonly Guid MucaishuTId = new Guid("{9c5edb6d-b5bd-4be9-a3a6-cbf794e6bf13}");

        /// <summary>
        /// 木材仓库模板Id。
        /// </summary>
        public static readonly Guid MucaiStoreTId = new Guid("{8caea73b-e210-47bf-a121-06cc12973baf}");

        /// <summary>
        /// 家园方案背包模板Id。
        /// </summary>
        public static readonly Guid HomelandPlanBagTId = new Guid("{366468d3-00d7-42ec-811d-8822fb0def42}");

        /// <summary>
        /// 家园建筑背包模板Id。
        /// </summary>
        public static readonly Guid HomelandBuilderBagTId = new Guid("{312612a5-30dd-4e0a-a71d-5074397428fb}");

        /// <summary>
        /// 家园方案对象的模板Id。
        /// </summary>
        public static readonly Guid HomelandPlanTId = new Guid("{5d374961-a072-4222-ab46-94d72dc394f7}");

        /// <summary>
        /// 家园数据存储于家园方案背包对象中 <see cref="GameThingBase.ExtendProperties"/> 属性的名字。
        /// </summary>
        public const string HomelandPlanPropertyName = "d681df0c-73ed-434a-9eb7-5c6c158ea1af";

        #endregion 家园及相关

        public const string ZhangLiName = "推关战力";

        #endregion 固定模板Id

        /// <summary>
        /// 快速变化属性的属性名前缀。
        /// </summary>
        public const string FastChangingPropertyName = "fcp";

        /// <summary>
        /// 级别属性的名字。
        /// </summary>
        public const string LevelPropertyName = "lv";   //Runes

        /// <summary>
        /// 升级计时快速变化属性名。
        /// </summary>
        public const string UpgradeTimeName = "upgradecd";

        /// <summary>
        /// 堆叠上限属性的名字。没有该属性的不可堆叠，无上限限制用-1表示。
        /// </summary>
        public const string StackUpperLimit = "stc";

        /// <summary>
        /// 容器容量上限属性。
        /// </summary>
        public const string ContainerCapacity = "cap";

        /// <summary>
        /// 裝備的神纹已经突破攻击的次数的属性名。
        /// </summary>
        public const string ShenwenTupoAtkCountPropertyName = "sscatk";

        /// <summary>
        /// 裝備的神纹已经突破最大血量的次数的属性名。
        /// </summary>
        public const string ShenwenTupoMHpCountPropertyName = "sscmhp";

        /// <summary>
        /// 裝備的神纹已经突破质量的次数的属性名。
        /// </summary>
        public const string ShenwenTupoQltCountPropertyName = "sscqlt";

        /// <summary>
        /// 阵容属性前缀。
        /// </summary>
        public const string ZhenrongPropertyName = "for";

        /// <summary>
        /// 唯一性标识属性名。该属性不存在或为0，表示不需要唯一性验证，否则需要相应模板的物品，在容器内唯一。
        /// </summary>
        public const string IsUniquePName = "uni";

        /// <summary>
        /// PVP积分的扩展属性名。
        /// </summary>
        public const string PvpScoreName = "PvpScore;public";
        #region 类别号
        /// <summary>
        /// 血量神纹碎片的类别号。
        /// </summary>
        public const int ShenwenHPTCode = 15;

        /// <summary>
        /// 攻击神纹碎片的类别号。
        /// </summary>
        public const int ShenwenAtkTCode = 16;

        /// <summary>
        /// 质量神纹碎片的类别号。
        /// </summary>
        public const int ShenwenQltTCode = 17;

        /// <summary>
        /// 装备的神纹的类别号。
        /// </summary>
        public const int ShenwenTCode = 10;

        #endregion 类别号

        #region 战斗常量
        /// <summary>
        /// 主动pvp大关卡模板Id。
        /// </summary>
        public static readonly Guid PvpDungeonTId = new Guid("{4805434E-605E-4479-B426-9A27C083D7D4}");

        /// <summary>
        /// 反击pvp大关卡模板Id。
        /// </summary>
        public static readonly Guid PvpForRetaliationDungeonTId = new Guid("{B4CDDF06-AD35-4E80-BFE2-975A5AF429CA}");

        /// <summary>
        /// 协助pvp大关卡模板Id。
        /// </summary>
        public static readonly Guid PvpForHelpDungeonTId = new Guid("{7A313D1C-7A53-4810-9586-6B52147D64C3}");

        #endregion 战斗常量

        #region 蓝图常量
        /// <summary>
        /// 突破蓝图Id。
        /// </summary>
        public static readonly Guid ShenWenTupoBlueprint = new Guid("{92f63905-a39f-4e1a-ad17-ea648a99be7a}");

        /// <summary>
        /// 神纹升级蓝图Id。
        /// </summary>
        public static readonly Guid ShenwenLvUpBlueprint = new Guid("{31E0945A-94E4-43D5-835F-6546D68349F1}");

        /// <summary>
        /// 孵化槽的模板Id。
        /// </summary>
        public static readonly Guid FuhuaSlotTId = new Guid("{b84072af-bb91-46eb-af5c-b462e3361c6c}");

        #endregion 蓝图常量

    }

    /// <summary>
    /// 项目特定的初始化。
    /// </summary>
    public class Gy001Initializer : GameManagerBase<Gy001InitializerOptions>, IGameObjectInitializer
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public Gy001Initializer()
        {

        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="service"><inheritdoc/></param>
        public Gy001Initializer(IServiceProvider service) : base(service)
        {
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="service"><inheritdoc/></param>
        /// <param name="options"><inheritdoc/></param>
        public Gy001Initializer(IServiceProvider service, Gy001InitializerOptions options) : base(service, options)
        {
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="parameters"></param>
        public bool Created(object obj, IReadOnlyDictionary<string, object> parameters)
        {
            if (obj is GameItem gi)
            {

            }
            else if (obj is GameChar gc)
            {
                return InitializerChar(gc, parameters);
            }
            else if (obj is GameUser gu)
            {
                InitializerUser(gu, parameters);
            }
            else
                return false;
            return true;
        }

        public bool Loaded(object obj, DbContext context)
        {
            if (obj is GameItem gi)
            {

            }
            else if (obj is GameChar gc)
            {
                //清除锁定属性槽内物品，放回道具背包中
                var gim = World.ItemManager;
                var daojuBag = gc.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.DaojuBagSlotId); //道具背包
                var slot = gc.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.LockAtkSlotId); //锁定槽
                gim.MoveItems(slot, c => true, daojuBag);
                slot = gc.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.LockMhpSlotId); //锁定槽
                gim.MoveItems(slot, c => true, daojuBag);
                slot = gc.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.LockQltSlotId); //锁定槽
                gim.MoveItems(slot, c => true, daojuBag);
                //挂接升级回调
                var hl = gc.GetHomeland();
                foreach (var item in hl.AllChildren)
                {
                    if (!item.Name2FastChangingProperty.TryGetValue(ProjectConstant.UpgradeTimeName, out var fcp))
                        continue;

                    var dt = fcp.GetComplateDateTime();
                    var now = DateTime.UtcNow;
                    TimeSpan ts;
                    if (now >= dt)   //若已经超时
                        ts = TimeSpan.Zero;
                    else
                        ts = dt - now;
                    var tm = new Timer(World.BlueprintManager.LevelUpCompleted, ValueTuple.Create(gc.Id, item.Id), ts, Timeout.InfiniteTimeSpan);
                }

            }
            else if (obj is GameUser gu)
            {
                gu.CurrentChar = gu.GameChars[0];   //项目特定:一个用户有且仅有一个角色
                gu.CurrentChar.Loaded(gu.Services, gu.DbContext);
            }
            else
                return false;
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public bool InitializerUser(GameUser user, IReadOnlyDictionary<string, object> parameters)
        {
            var gc = new GameChar();
            user.GameChars.Add(gc);
            user.CurrentChar = gc;
            gc.Initialize(Service, new Dictionary<string, object>()
                {
                    { "tid",ProjectConstant.CharTemplateId},
                    { "user",user},
                    {"DisplayName",parameters.GetValueOrDefault("charDisplayName") },
                });
            //生成缓存数据
            var sep = new CharSpecificExpandProperty
            {
                CharLevel = (int)gc.Properties.GetDecimalOrDefault(ProjectConstant.LevelPropertyName),
                LastPvpScore = 1000,
                PvpScore = 1000,
                Id = gc.Id,
                GameChar = gc,
                FrinedCount = 0,
                FrinedMaxCount = 10,
                LastLogoutUtc = DateTime.UtcNow,
            };
            gc.SpecificExpandProperties = sep;
            return true;
        }
        /// <summary>
        /// 角色创建后被调用。
        /// </summary>
        /// <param name="gameChar">已经创建的对象。</param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public bool InitializerChar(GameChar gameChar, IReadOnlyDictionary<string, object> parameters)
        {
            var world = World;
            var gitm = world.ItemTemplateManager;
            var result = false;
            //增加坐骑
            var mountsBagSlot = gameChar.GameItems.First(c => c.TemplateId == ProjectConstant.ZuojiBagSlotId);   //坐骑背包槽
            for (int i = 3001; i < 3002; i++)   //仅增加羊坐骑
            {
                var headTemplate = gitm.Id2Template.Values.FirstOrDefault(c => c.GId.GetValueOrDefault() == i);
                var bodyTemplate = gitm.Id2Template.Values.FirstOrDefault(c => c.GId.GetValueOrDefault() == 1000 + i);
                var mounts = world.ItemManager.CreateMounts(headTemplate, bodyTemplate);
                world.ItemManager.ForcedAdd(mounts, mountsBagSlot);
            }
            //增加神纹
            var runseSlot = gameChar.GameItems.First(c => c.TemplateId == ProjectConstant.ShenWenSlotId);   //神纹装备槽
            var templates = world.ItemTemplateManager.Id2Template.Values.Where(c => c.GenusCode == 10);
            var shenwens = templates.Select(c =>
            {
                var r = new GameItem();
                r.Initialize(Service, c.Id);
                return r;
            });
            foreach (var item in shenwens)
            {
                world.ItemManager.ForcedAdd(item, runseSlot);
            }
            var db = gameChar.GameUser.DbContext;
            if (string.IsNullOrWhiteSpace(gameChar.DisplayName))    //若没有指定昵称
            {
                string displayName;
                for (displayName = CnNames.GetName(VWorld.IsHit(0.5)); db.Set<GameChar>().Any(c => c.DisplayName == displayName); displayName = CnNames.GetName(VWorld.IsHit(0.5)))
                    ;
                gameChar.DisplayName = displayName;
            }
            result = true;
            //修正木材存贮最大量
            //var mucai = gameChar.GameItems.First(c => c.TemplateId == ProjectConstant.MucaiId);
            //var stcMucai = mucai.GetStc();
            //if (stcMucai < decimal.MaxValue)
            //{
            //    var mucaiStore = gameChar.GetHomeland().Children.Where(c => c.TemplateId == ProjectConstant.MucaiStoreTId);
            //    var stcs = mucaiStore.Select(c => c.GetStc());
            //    if (stcs.Any(c => c == decimal.MaxValue))   //若有任何仓库是最大堆叠
            //        mucai.SetPropertyValue(ProjectConstant.StackUpperLimit, -1);
            //    else
            //        mucai.SetPropertyValue(ProjectConstant.StackUpperLimit, stcs.Sum() + stcMucai);
            //}
            //将坐骑入展示宠物
            var sheepBodyTId = new Guid("BBC9FE07-29BD-486D-8AD6-B99DB0BD07D6");
            var gim = world.ItemManager;
            var showMount = gameChar.GetZuojiBag().Children.FirstOrDefault();
            var dic = showMount?.Properties;
            if (dic != null)
                dic["for10"] = 0;
            GameSocialRelationship gsr = new GameSocialRelationship()
            {
                Id = gameChar.Id,
                Id2 = gim.GetBody(showMount).TemplateId,
                KeyType = SocialConstant.HomelandShowKeyType,
            };
            db.Add(gsr);
            return result;
        }
    }

    public class Gy001InitializerOptions
    {
        public Gy001InitializerOptions()
        {

        }
    }

    /// <summary>
    /// 封装项目特定逻辑。
    /// </summary>
    public class SpecificProject
    {
        private readonly IServiceProvider _ServiceProvider;

        /// <summary>
        /// 获取使用的服务容器。
        /// </summary>
        public IServiceProvider Services => _ServiceProvider;

        public SpecificProject()
        {
        }

        public SpecificProject(IServiceProvider serviceProvider)
        {
            _ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// 所有模板调赴后调用。此处可追加模板。
        /// </summary>
        /// <param name="itemTemplates"></param>
        /// <returns></returns>
        public static bool ItemTemplateLoaded(DbContext itemTemplates)
        {
            bool dbDirty = false;
            //Comparer<GameItemTemplate> comparer = Comparer<GameItemTemplate>.Create((l, r) =>
            //{
            //    if (l == r)
            //        return 0;
            //    int result = Comparer<Guid>.Default.Compare(l.Id, r.Id);
            //    if (0 != result)
            //        return result;
            //    return result;
            //});
            //var dbSet = itemTemplates.Set<GameItemTemplate>();
            //foreach (var item in StoreTemplates)
            //{
            //    var template = dbSet.Local.FirstOrDefault(c => c.Id == item.Id);
            //    if (null == template)
            //    {
            //        dbSet.Add(item);
            //        dbDirty = true;
            //    }
            //    else
            //    {
            //        //TO DO 应判断是否脏
            //        template.DisplayName = item.DisplayName;
            //        template.ChildrenTemplateIdString = item.ChildrenTemplateIdString;
            //        template.PropertiesString = item.PropertiesString;
            //        dbDirty = true;
            //    }
            //}
            return dbDirty;
        }


        /// <summary>
        /// 当一个虚拟事物创建后调用。
        /// </summary>
        /// <param name="service"></param>
        /// <param name="gameItem"></param>
        /// <returns></returns>
        public static bool GameItemCreated(IServiceProvider service, GameItem gameItem)
        {
            var result = false;
            return result;
        }

        /// <summary>
        /// 开始战斗的项目特定回调。
        /// </summary>
        /// <param name="service"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool CombatStart(IServiceProvider service, StartCombatData data)
        {
            var world = service.GetService<VWorld>();
            var gc = data.GameChar;
            var cm = world.CombatManager;
            var parent = cm.GetParent(data.Template);   //取大关
            if (parent.TryGetPropertyValue("minCE", out var minCEObj) && OwHelper.TryGetDecimal(minCEObj, out _))  //若需要校验战力
            {
                //TO DO
            }
            if (parent == data.Template || cm.GetNext(parent) == data.Template)  //若是第一关
            {
                //扣除体力
                var tili = gc.GetTili();
                var fcp = tili.Name2FastChangingProperty.GetValueOrDefault("Count");
                var pp = (decimal)parent.Properties.GetValueOrDefault("pp", 0m);
                if (fcp.GetCurrentValueWithUtc() < pp)
                {
                    data.HasError = true;
                    data.DebugMessage = $"体力只有{fcp.LastValue},但是需要{pp}";
                    return false;
                }
                fcp.LastValue -= pp;
                //扣除次数
                var tdt = parent.Properties.GetDecimalOrDefault("tdt", 0m);
                if (tdt > 0)
                {
                    var pveT = gc.GetPveT();
                    fcp = pveT.Name2FastChangingProperty.GetValueOrDefault("Count");
                    var count = fcp?.GetCurrentValueWithUtc() ?? pveT.Count.Value;
                    if (count < tdt)
                    {
                        data.HasError = true;
                        data.DebugMessage = $"允许的进攻次数只有{count},但是需要{tdt}。";
                        return false;
                    }
                    fcp.LastValue -= tdt;
                }
            }
            return true;
        }

        /// <summary>
        /// 结束战斗的项目特定回调。
        /// </summary>
        /// <param name="service"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool CombatEnd(IServiceProvider service, EndCombatData data)
        {
            GameChar gameChar = data.GameChar;
            IEnumerable<GameItem> gameItems = data.GameItems;
            var world = service.GetRequiredService<VWorld>();
            var gitm = world.ItemTemplateManager;
            var cmbm = world.CombatManager;
            var gim = world.ItemManager;
            var tm = gitm.GetTemplateFromeId(gameChar.CurrentDungeonId.Value);    //关卡模板
                                                                                  //校验时间
            DateTime dt = gameChar.CombatStartUtc.GetValueOrDefault(DateTime.UtcNow);
            var dtNow = DateTime.UtcNow;
            var lt = TimeSpan.FromSeconds(Convert.ToDouble(tm.Properties.GetValueOrDefault("tl", decimal.Zero)));   //最短时间
            lt = TimeSpan.FromSeconds(1);   //TO DO为测试临时更改
            if (dtNow - dt < lt) //若时间过短
            {
                data.HasError = true;
                data.DebugMessage = "时间过短";
                return false;
            }
            if (!Verify(service, tm, gameItems, out string msg))
            {
                data.DebugMessage = msg;
                data.HasError = true;
                return false;
            }
            var shouyiSlot = gameChar.GameItems.First(c => c.TemplateId == ProjectConstant.ShouyiSlotId);
            var totalItems = shouyiSlot.Children.Concat(gameItems);    //总计收益
            if (!Verify(service, cmbm.GetParent(tm), totalItems, out msg))  //若总计收益超过限制
            {
                data.DebugMessage = $"总计收益超过限制。{msg}";
                data.HasError = true;
                return false;
            }
            //记录收益——改写收益槽数据
            //坐骑
            var mounts = data.GameItems.Where(c => c.TemplateId == ProjectConstant.ZuojiZuheRongqi).Select(c => //规范化坐骑数据
            {
                var head = gim.GetHead(c);
                var headTemplate = gitm.GetTemplateFromeId(head.TemplateId);
                var body = gim.GetBody(c);
                var bodyTemplate = gitm.GetTemplateFromeId(body.TemplateId);
                var result = gim.CreateMounts(headTemplate, bodyTemplate);

                if (c.Properties.TryGetValue("neatk", out object valObj) && OwHelper.TryGetDecimal(valObj, out var dec))
                    result.Properties["neatk"] = dec;
                if (c.Properties.TryGetValue("neqlt", out valObj) && OwHelper.TryGetDecimal(valObj, out dec))
                    result.Properties["neqlt"] = dec;
                if (c.Properties.TryGetValue("nemhp", out valObj) && OwHelper.TryGetDecimal(valObj, out dec))
                    result.Properties["nemhp"] = dec;
                return result;
            });
            shouyiSlot.Children.AddRange(mounts);   //加入坐骑
                                                    //神纹
            var shenwen = from tmp in data.GameItems
                          let template = gitm.GetTemplateFromeId(tmp.TemplateId)
                          where template.GenusCode >= 15 && template.GenusCode <= 17  //神纹碎片
                          select (template, tmp.Count ?? 1);
            shouyiSlot.Children.AddRange(shenwen.Select(c =>
            {
                var sw = new GameItem();
                sw.Initialize(world.Service, c.template.Id);
                sw.Count = c.Item2;
                return sw;
            }));
            //金币,暂时不用创建新的金币对象。
            var coll = data.GameItems.Where(c => c.TemplateId == ProjectConstant.JinbiId).ToList();
            coll.ForEach(c => c.Count ??= 1);
            shouyiSlot.Children.AddRange(coll); //收益槽
                                                //下一关数据
            data.NextTemplate = cmbm.GetNext(data.Template);
            if (null == data.NextTemplate || data.EndRequested) //若大关卡已经结束
            {
                var changes = new List<ChangeItem>();
                //移动收益槽数据到各自背包。
                //金币
                gim.MoveItems(shouyiSlot, c => c.TemplateId == ProjectConstant.JinbiId, gameChar, changes);
                //野生怪物
                var shoulan = gameChar.GameItems.First(c => c.TemplateId == ProjectConstant.ShoulanSlotId);
                gim.MoveItems(shouyiSlot, c => c.TemplateId == ProjectConstant.ZuojiZuheRongqi, shoulan, changes);
                //神纹碎片
                var shenwenBag = gameChar.GameItems.First(c => c.TemplateId == ProjectConstant.DaojuBagSlotId);   //神纹背包
                gim.MoveItems(shouyiSlot, c =>
                {
                    var _ = gitm.GetTemplateFromeId(c.TemplateId)?.GenusCode;
                    return _ >= 15 && _ <= 17;
                }, shenwenBag, changes);
                //压缩变化数据
                ChangeItem.Reduce(changes);
                data.ChangesItems.AddRange(changes);
            }

            return true;
        }

        /// <summary>
        /// 校验收益是否超过上限。
        /// </summary>
        /// <param name="service"></param>
        /// <param name="itemTemplate">限定数据的模板</param>
        /// <param name="gameItems"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        private static bool Verify(IServiceProvider service, GameItemTemplate itemTemplate, IEnumerable<GameItem> gameItems, out string msg)
        {
            var gitm = service.GetService<GameItemTemplateManager>();
            //typ关卡类别=1普通管卡 mis大关数 sec=小关gold=数金币掉落上限，aml=获得资质野怪的数量，mne=资质合上限，mt=神纹数量上限，tl=最短通关时间
            if (itemTemplate.Properties.TryGetValue("gold", out object goldObj)) //若要限制金币数量
            {
                if (goldObj is decimal gold)
                {
                    var _ = gameItems.Where(c => c.TemplateId == ProjectConstant.JinbiId).Select(c => c.Count).Sum();
                    if (_ > gold)   //若金币超过上限
                    {
                        msg = "金币超过上限";
                        return false;
                    }
                }
            }
            if (itemTemplate.Properties.TryGetValue("aml", out object monsterCountObj)) //若要限制怪数量
            {
                if (monsterCountObj is decimal monsterCount)
                {
                    var _ = gameItems.Where(c => c.TemplateId == ProjectConstant.ZuojiZuheRongqi).Count();
                    if (_ > monsterCount)   //若怪数量超过上限
                    {
                        msg = "怪数量超过上限";
                        return false;
                    }
                }
            }
            if (itemTemplate.Properties.TryGetValue("mne", out object mneObj) && mneObj is decimal mne) //若要限制单个怪资质总和
            {
                var coll = from tmp in gameItems
                           let mneatk = Convert.ToDecimal(tmp.Properties.GetValueOrDefault("neatk", decimal.Zero))
                           let mneqlt = Convert.ToDecimal(tmp.Properties.GetValueOrDefault("neqlt", decimal.Zero))
                           let mnemhp = Convert.ToDecimal(tmp.Properties.GetValueOrDefault("nemhp", decimal.Zero))
                           let mneTotal = mneatk + mneqlt + mnemhp
                           where mneTotal > mne
                           select tmp;
                var errItem = coll.FirstOrDefault();
                if (null != errItem)   //若单个怪资质总和超过上限
                {
                    msg = $"单个怪资质总和超过上限,TemplateId={gitm.GetTemplateFromeId(errItem.TemplateId)?.GId.GetValueOrDefault()}";
                    return false;
                }
            }
            if (itemTemplate.Properties.TryGetValue("mt", out object mtObj) && mtObj is decimal mt) //若要限制神纹数量
            {
                var coll = gitm.Id2Template.Values.Where(c => c.GenusCode <= 17 && c.GenusCode >= 15); //获取所有神纹模板
                var shenwen = gameItems.Join(coll, c => c.TemplateId, c => c.Id, (l, r) => l);    //获取神纹的集合
                if (shenwen.Sum(c => c.Count) > (int)mt) //若神纹数量超过上限
                {
                    msg = "神纹数量超过上限";
                    return false;
                }
            }
            msg = null;
            return true;
        }

        public static bool ApplyBlueprint(IServiceProvider service, ApplyBlueprintDatas datas)
        {
            throw new NotImplementedException();
        }
    }

    public static class GameItemExtensions
    {
        /// <summary>
        /// 类号。除了序列号以外的前6位(十进制)分类号。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <returns>如果不能得到正确的模板对象则返回-1。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCatalogNumber(this GameItem gameItem) => gameItem.ItemTemplate?.CatalogNumber ?? -1;
    }

    /// <summary>
    /// 项目特定扩展函数。
    /// </summary>
    public static class ProjectExtensions
    {
        /// <summary>
        /// 是否包含不可分割的孩子。
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsIncludeChildren(this GameItem item) =>
            ProjectConstant.HomelandPatCard == item.TemplateId || ProjectConstant.ZuojiZuheRongqi == item.TemplateId;

        /// <summary>
        /// 获取弃物槽对象。
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameItem GetQiwuBag(this GameChar gameChar) =>
            gameChar.GameItems.FirstOrDefault(c => ProjectConstant.QiwuBagTId == c.TemplateId);

        /// <summary>
        /// 获取货币袋。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameItem GetCurrencyBag(this GameChar gameChar) =>
            gameChar.GameItems.FirstOrDefault(c => ProjectConstant.CurrencyBagTId == c.TemplateId);

        /// <summary>
        /// 获取金币对象。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameItem GetJinbi(this GameChar gameChar) =>
            gameChar.GetCurrencyBag().Children.FirstOrDefault(c => c.TemplateId == ProjectConstant.JinbiId);

        /// <summary>
        /// 获取木材对象。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameItem GetMucai(this GameChar gameChar) =>
            gameChar.GetCurrencyBag().Children.FirstOrDefault(c => c.TemplateId == ProjectConstant.MucaiId);

        /// <summary>
        /// 获取钻石对象。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameItem GetZuanshi(this GameChar gameChar) =>
            gameChar.GetCurrencyBag().Children.FirstOrDefault(c => c.TemplateId == ProjectConstant.ZuanshiId);

        /// <summary>
        /// 获取体力对象。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameItem GetTili(this GameChar gameChar) =>
            gameChar.GetCurrencyBag().Children.FirstOrDefault(c => c.TemplateId == ProjectConstant.TiliId);

        /// <summary>
        /// 获取Pve次数对象。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameItem GetPveT(this GameChar gameChar) =>
            gameChar.GetCurrencyBag().Children.FirstOrDefault(c => c.TemplateId == ProjectConstant.PveTCounterTId);

        /// <summary>
        /// 获取友情商店货币。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameItem GetFriendCurrency(this GameChar gameChar) =>
            gameChar.GetCurrencyBag().Children.FirstOrDefault(c => c.TemplateId == ProjectConstant.FriendCurrencyTId);

        /// <summary>
        /// 获取PVP数据记录对象。
        /// 这也是一种货币对象。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameItem GetPvpObject(this GameChar gameChar) =>
            gameChar.GetCurrencyBag().Children.FirstOrDefault(c => c.TemplateId == ProjectConstant.PvpObjectTId);
    }
}
