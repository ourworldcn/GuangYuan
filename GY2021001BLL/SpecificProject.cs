using GuangYuan.GY001.UserDb;
using GuangYuan.GY001.TemplateDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OW.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Game.Social;

namespace GuangYuan.GY001.BLL
{
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
        /// 神纹碎片背包槽Id。放在此槽中是未装备的神纹(碎片)。
        /// </summary>
        [Obsolete("下个版本可能会被删除。")]
        public static readonly Guid ShenWenBagSlotId = new Guid("{2BAA3FCD-2BE8-4096-916A-FF2D47E084EF}");

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

        #endregion  货币类模板Id

        #region 邮件类型Id
        public static readonly Guid 友情孵化补给动物 = new Guid("b4c30a07-2179-435e-b053-fd4b0c36251b");


        public static readonly Guid 孵化补给动物 = new Guid("{366ce206-8d17-47ea-a039-9280ddd81bbc}");

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
        /// 角色创建后被调用。
        /// </summary>
        /// <param name="service">服务提供者。</param>
        /// <param name="gameChar">已经创建的对象。</param>
        /// <returns></returns>
        public static bool CharCreated(IServiceProvider service, GameChar gameChar)
        {
            var gitm = service.GetService<GameItemTemplateManager>();
            var world = service.GetService<VWorld>();
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
            var shenwens = templates.Select(c => world.ItemManager.CreateGameItem(c));
            foreach (var item in shenwens)
            {
                world.ItemManager.ForcedAdd(item, runseSlot);
            }
            var db = gameChar.GameUser.DbContext;
            string displayName;
            for (displayName = CnNames.GetName(VWorld.IsHit(0.5)); db.Set<GameChar>().Any(c => c.DisplayName == displayName); displayName = CnNames.GetName(VWorld.IsHit(0.5)))
                ;
            gameChar.DisplayName ??= displayName;
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
            //将坐骑羊放入展示宠物
            var sheepBodyTId = new Guid("BBC9FE07-29BD-486D-8AD6-B99DB0BD07D6");
            var gim = service.GetRequiredService<GameItemManager>();
            var showMount = gameChar.GetZuojiBag().Children.FirstOrDefault();
            var dic = showMount?.Properties;
            if (dic != null)
                dic["for10"] = 0;
            GameSocialRelationship gsr = new GameSocialRelationship()
            {
                Id = gameChar.Id,
                Id2 = gim.GetBody(showMount).TemplateId,
                Flag = SocialConstant.HomelandShowFlag,
            };
            db.Add(gsr);
            //发送测试邮件
            Task.Delay(5000).ContinueWith(c =>
            {
                //创建欢迎邮件
                var mail = new GameMail()
                {
                    Subject = "欢迎您加入XXX世界",
                    Body = "此邮件是测试目的，正式版将删除。",
                };
                mail.Attachmentes.Add(new GameMailAttachment()
                {
                    PropertiesString = "TName=这是一个测试的附件对象,tid={89A586A8-CD8D-40FF-BDA2-41E68B6EC505},ptid={3D87D1FA-F270-42AB-9241-E30498246947},count=1,desc=tid是送的物品模板id;count是数量;ptid是放入容器的模板Id。",
                });
                world.SocialManager.SendMail(mail, new Guid[] { gameChar.Id }, SocialConstant.FromSystemId);
                for (int i = VWorld.WorldRandom.Next(2) + 1; i >= 0; i--)
                {
                    var mail2 = new GameMail()
                    {
                        Subject = "测试邮件" + i,
                        Body = "此邮件是测试目的，正式版将删除。",
                    };
                    world.SocialManager.SendMail(mail2, new Guid[] { gameChar.Id }, SocialConstant.FromSystemId);
                }
            });

            return result;
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
                var sw = world.ItemManager.CreateGameItem(c.template);
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
        static public int GetCatalogNumber(this GameItem gameItem) => gameItem.ItemTemplate?.CatalogNumber ?? -1;
    }

    /// <summary>
    /// 项目特定扩展函数。
    /// </summary>
    public static class ProjectExtensions
    {
        /// <summary>
        /// 获取弃物槽对象。
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public GameItem GetQiwuBag(this GameChar gameChar) =>
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
