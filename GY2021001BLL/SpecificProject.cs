using GY2021001DAL;
using Gy2021001Template;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace GY2021001BLL
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
        public static readonly Guid ZuojiTou = new Guid("{A06B7496-F631-4D51-9872-A2CC84A56EAB}");

        /// <summary>
        /// 当前装备的坐骑身体容器模板Id。已废弃
        /// </summary>
        public static readonly Guid ZuojiShen = new Guid("{7D191539-11E1-49CD-8D0C-82E3E5B04D31}");
        /// <summary>
        /// 神纹背包槽Id。放在此槽中是未装备的神纹(碎片)。
        /// </summary>
        public static readonly Guid ShenWenBagSlotId = new Guid("{2BAA3FCD-2BE8-4096-916A-FF2D47E084EF}");

        #endregion 废弃模板Id

        #region 坐骑相关Id

        /// <summary>
        /// 坐骑头和身体需要一个容器组合起来。此类容器的模板Id就是这个。
        /// </summary>
        public static readonly Guid ZuojiZuheRongqi = new Guid("{6E179D54-5836-4E0B-B30D-756BD07FF196}");

        /// <summary>
        /// 坐骑组合中的头容器Id。
        /// </summary>
        public static readonly Guid ZuojiZuheTou = new Guid("{740FEBF3-7472-43CB-8A10-798F6C61335B}");

        /// <summary>
        /// 坐骑组合中的身体容器Id。
        /// </summary>
        public static readonly Guid ZuojiZuheShenti = new Guid("{F8B1987D-FDF3-4090-9E9B-EBAF1DB2DCCD}");
        #endregion 坐骑相关Id

        #region 角色直属槽及其相关

        /// <summary>
        /// 当前坐骑的容器Id。
        /// </summary>
        public static readonly Guid DangqianZuoqiSlotId = new Guid("{B19EE5AB-57E3-4513-8228-9F2A8364358E}");

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
        /// 坐骑背包Id。
        /// </summary>
        public static readonly Guid ZuojiBagSlotId = new Guid("{BA2AEE89-0BC3-4612-B6FF-5DDFEF85C9E5}");
        #endregion  角色直属槽及其相关
        /// <summary>
        /// 角色模板Id。当前只有一个模板。
        /// </summary>
        public static readonly Guid CharTemplateId = new Guid("{0CF39269-6301-470B-8527-07AF29C5EEEC}");

        #endregion 固定模板Id

        /// <summary>
        /// 神纹碎片的模板Id。
        /// </summary>
        public static readonly Guid RunesId = new Guid("{2B86FF50-0257-4913-8BEC-F5CF3C84B6D5}");

        /// <summary>
        /// 级别属性的名字。
        /// </summary>
        public const string LevelPropertyName = "lv";   //Runes

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

        #endregion 蓝图常量
    }

    /// <summary>
    /// 封装项目特定逻辑。
    /// </summary>
    public class SpecificProject
    {
        private readonly IServiceProvider _ServiceProvider;

        public SpecificProject()
        {
        }

        public SpecificProject(IServiceProvider serviceProvider)
        {
            _ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// 所有需要额外加入的模板对象。
        /// </summary>
        public static List<GameItemTemplate> StoreTemplates = new List<GameItemTemplate>()
        {
    #region 已废弃
            new GameItemTemplate(ProjectConstant.ZuojiTou)
            {
                DisplayName="当前坐骑头(已废弃)",
            },
            new GameItemTemplate(ProjectConstant.ZuojiShen)
            {
                DisplayName="当前坐骑身(已废弃)",

            },
	#endregion 已废弃

            new GameItemTemplate(ProjectConstant.ZuojiZuheRongqi)
            {
                DisplayName="坐骑组合",
                ChildrenTemplateIdString=$"{ProjectConstant.ZuojiZuheTou},{ProjectConstant.ZuojiZuheShenti}",
            },
            new GameItemTemplate(ProjectConstant.ZuojiZuheTou)
            {
                DisplayName="坐骑组合的头",
            },
            new GameItemTemplate(ProjectConstant.ZuojiZuheShenti)
            {
                DisplayName="坐骑组合的身体",
            },
            new GameItemTemplate(ProjectConstant.DangqianZuoqiSlotId)
            {
                DisplayName="当前坐骑槽"
            },
            new GameItemTemplate(ProjectConstant.CharTemplateId)
            {
                DisplayName="角色的模板",
                ChildrenTemplateIdString=$"{ProjectConstant.DangqianZuoqiSlotId},{ProjectConstant.ShenWenSlotId},{ProjectConstant.DaojuBagSlotId},{ProjectConstant.ShoulanSlotId}" +  //通过串联将长字符串文本拆分为较短的字符串，从而提高源代码的可读性。 编译时将这些部分连接到单个字符串中。 无论涉及到多少个字符串，均不产生运行时性能开销。
                    $",{ProjectConstant.JinbiId},{ProjectConstant.ShouyiSlotId},{ProjectConstant.ZuojiBagSlotId}",
                PropertiesString="mpp=20,dpp=1,ipp=1",    //最大体力，未测试临时更改 TO DO dpp=300
            },
            new GameItemTemplate(ProjectConstant.ShenWenSlotId)
            {
                DisplayName="神纹装备槽",
            },
            new GameItemTemplate(ProjectConstant.DaojuBagSlotId)
            {
                DisplayName="道具背包槽",
            },
            new GameItemTemplate(ProjectConstant.ShouyiSlotId)
            {
                DisplayName="收益槽",
            },
            new  GameItemTemplate(ProjectConstant.ShoulanSlotId)
            {
                DisplayName="兽栏槽",
            },
            new GameItemTemplate(ProjectConstant.JinbiId)
            {
                DisplayName="金币",
            },
            new GameItemTemplate(ProjectConstant.MucaiId)
            {
                DisplayName="木材",
            },
            new GameItemTemplate(ProjectConstant.ZuanshiId)
            {
                DisplayName="钻石",
            },
            new GameItemTemplate(ProjectConstant.ZuojiBagSlotId)
            {
                DisplayName="坐骑背包",
            },
        };

        /// <summary>
        /// 所有模板调赴后调用。此处可追加模板。
        /// </summary>
        /// <param name="itemTemplates"></param>
        /// <returns></returns>
        public static bool ItemTemplateLoaded(DbContext itemTemplates)
        {
            Comparer<GameItemTemplate> comparer = Comparer<GameItemTemplate>.Create((l, r) =>
            {
                if (l == r)
                    return 0;
                int result = Comparer<Guid>.Default.Compare(l.Id, r.Id);
                if (0 != result)
                    return result;
                return result;
            });
            bool dbDirty = false;
            var dbSet = itemTemplates.Set<GameItemTemplate>();
            foreach (var item in StoreTemplates)
            {
                var template = dbSet.Local.FirstOrDefault(c => c.Id == item.Id);
                if (null == template)
                {
                    dbSet.Add(item);
                    dbDirty = true;
                }
                else
                {
                    //TO DO 应判断是否脏
                    template.DisplayName = item.DisplayName;
                    template.ChildrenTemplateIdString = item.ChildrenTemplateIdString;
                    template.PropertiesString = item.PropertiesString;
                    dbDirty = true;
                }
            }
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
            var lst = world.ObjectPoolListGameItem.Get();
            var rem = world.ObjectPoolListGameItem.Get();
            var changes = world.ObjectPoolListGameItem.Get();
            try
            {
                //增加坐骑
                var mountsSlot = gameChar.GameItems.First(c => c.TemplateId == ProjectConstant.DangqianZuoqiSlotId);   //当前坐骑槽
                for (int i = 3001; i < 3004; i++)   //向出战槽添加坐骑
                {
                    var headTemplate = gitm.Id2Template.Values.FirstOrDefault(c => c.GId.GetValueOrDefault() == i);
                    var bodyTemplate = gitm.Id2Template.Values.FirstOrDefault(c => c.GId.GetValueOrDefault() == 1000 + i);
                    var mounts = CreateMounts(service, headTemplate, bodyTemplate);
                    lst.Add(mounts);
                }
                world.ItemManager.AddItems(lst, mountsSlot, rem, null);
                lst.Clear();
                changes.Clear();
                rem.Clear();
                var mountsBagSlot = gameChar.GameItems.First(c => c.TemplateId == ProjectConstant.ZuojiBagSlotId);   //坐骑背包槽
                for (int i = 3004; i < 3008; i++)
                {
                    var headTemplate = gitm.Id2Template.Values.FirstOrDefault(c => c.GId.GetValueOrDefault() == i);
                    var bodyTemplate = gitm.Id2Template.Values.FirstOrDefault(c => c.GId.GetValueOrDefault() == 1000 + i);
                    var mounts = CreateMounts(service, headTemplate, bodyTemplate);
                    world.ItemManager.ForcedAdd(mounts, mountsBagSlot);
                }
                //增加神纹
                var runseSlot = gameChar.GameItems.First(c => c.TemplateId == ProjectConstant.ShenWenSlotId);   //神纹装备槽
                var templates = world.ItemTemplateManager.Id2Template.Values.Where(c => c.GenusCode == 10);
                var shenwens = templates.Select(c => world.ItemManager.CreateGameItem(c));
                world.ItemManager.AddItems(shenwens, runseSlot, rem, null);
                //增加神纹道具
                var id = new Guid("08994941-A144-4A0B-9E24-516B021C4AC3");  //羊神纹道具tId
                var item = world.ItemManager.CreateGameItem(id);    //羊神纹道具
                item.Count = 5;
                var itemBag = gameChar.GameItems.First(c => c.TemplateId == ProjectConstant.DaojuBagSlotId);
                world.ItemManager.AddItem(item, itemBag);
                result = true;
                return result;

            }
            finally
            {
                world.ObjectPoolListGameItem.Return(lst);
                world.ObjectPoolListGameItem.Return(rem);
                world.ObjectPoolListGameItem.Return(changes);
            }
        }

        public static GameItem CreateMounts(IServiceProvider service, Guid headId, Guid bodyId)
        {
            var gitm = service.GetRequiredService<GameItemTemplateManager>();
            return CreateMounts(service, gitm.GetTemplateFromeId(headId), gitm.GetTemplateFromeId(bodyId));
        }


        /// <summary>
        /// 创建一个坐骑。
        /// </summary>
        /// <param name="service"></param>
        /// <param name="head">头的模板，若是null，则不创建。</param>
        /// <param name="body">身体的模板，若是null，则不创建。</param>
        /// <returns></returns>
        public static GameItem CreateMounts(IServiceProvider service, GameItemTemplate head, GameItemTemplate body)
        {
            var gim = service.GetService<GameItemManager>();
            var gitm = service.GetService<GameItemTemplateManager>();
            var result = gim.CreateGameItem(gitm.GetTemplateFromeId(ProjectConstant.ZuojiZuheRongqi));
            if (null != head)
            {
                var hGi = gim.CreateGameItem(head);
                result.Children.First(c => c.TemplateId == ProjectConstant.ZuojiZuheTou).Children.Add(hGi);
            }
            if (null != body)
            {
                var bGi = gim.CreateGameItem(body);
                result.Children.First(c => c.TemplateId == ProjectConstant.ZuojiZuheShenti).Children.Add(bGi);
            }
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
            if (parent == data.Template || cm.GetNext(parent) == data.Template)  //若是第一关
            {
                //扣除体力
                var pp = (decimal)parent.Properties.GetValueOrDefault("pp", 0m);
                if (gc.GradientProperties.TryGetValue("pp", out GradientProperty gp))
                {
                    if (gp.GetCurrentValueWithUtc() < pp)
                    {
                        data.HasError = true;
                        data.DebugMessage = $"体力只有{gp.LastValue},但是需要{pp}";
                        return false;
                    }
                    gp.LastValue -= pp;
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
            var mounts = data.GameItems.Where(c => c.TemplateId == ProjectConstant.ZuojiZuheRongqi).Select(c =>
            {
                var head = c.Children.First(c => c.TemplateId == ProjectConstant.ZuojiZuheTou).Children.First();
                var headTemplate = gitm.GetTemplateFromeId(head.TemplateId);
                var body = c.Children.First(c => c.TemplateId == ProjectConstant.ZuojiZuheShenti).Children.First();
                var bodyTemplate = gitm.GetTemplateFromeId(body.TemplateId);
                var result = CreateMounts(service, headTemplate, bodyTemplate);
                if (c.Properties.TryGetValue("neatk", out object valObj))
                    result.Properties["neatk"] = Convert.ToDecimal(valObj);
                if (c.Properties.TryGetValue("neqlt", out valObj))
                    result.Properties["neqlt"] = Convert.ToDecimal(valObj);
                if (c.Properties.TryGetValue("nemhp", out valObj))
                    result.Properties["nemhp"] = Convert.ToDecimal(valObj);
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
                var gim = world.ItemManager;
                var changes = new List<ChangesItem>();
                //移动收益槽数据到各自背包。
                //金币
                gim.MoveItems(shouyiSlot, c => c.TemplateId == ProjectConstant.JinbiId, gameChar, changes);
                //野生怪物
                var shoulan = gameChar.GameItems.First(c => c.TemplateId == ProjectConstant.ShoulanSlotId);
                gim.MoveItems(shouyiSlot, c => c.TemplateId == ProjectConstant.ZuojiZuheRongqi, shoulan, changes);
                //神纹碎片
                var shenwenBag = gameChar.GameItems.First(c => c.TemplateId == ProjectConstant.ShenWenBagSlotId);   //神纹背包
                gim.MoveItems(shouyiSlot, c =>
                {
                    var _ = gitm.GetTemplateFromeId(c.TemplateId)?.GenusCode;
                    return _ >= 15 && _ <= 17;
                }, shenwenBag, changes);
                //压缩变化数据
                ChangesItem.Reduce(changes);
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
}
