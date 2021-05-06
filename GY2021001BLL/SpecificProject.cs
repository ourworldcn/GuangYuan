using GY2021001DAL;
using Gy2021001Template;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        /// <summary>
        /// 当前坐骑的容器Id。
        /// </summary>
        public static readonly Guid DangqianZuoqiSlotId = new Guid("{B19EE5AB-57E3-4513-8228-9F2A8364358E}");

        /// <summary>
        /// 角色模板Id。当前只有一个模板。
        /// </summary>
        public static readonly Guid CharTemplateId = new Guid("{0CF39269-6301-470B-8527-07AF29C5EEEC}");

        /// <summary>
        /// 神纹槽Id。放在此槽中是装备的神纹。当前每种身体对应一种神纹。
        /// </summary>
        public static readonly Guid ShenWenSlotId = new Guid("{88A4EED6-0AEB-4A70-8FDE-67F75E5E2C0A}");

        /// <summary>
        /// 神纹背包槽Id。放在此槽中是未装备的神纹(碎片)。
        /// </summary>
        public static readonly Guid ShenWenBagSlotId = new Guid("{2BAA3FCD-2BE8-4096-916A-FF2D47E084EF}");

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
        /// 神纹碎片的模板Id。
        /// </summary>
        public static readonly Guid RunesId = new Guid("{2B86FF50-0257-4913-8BEC-F5CF3C84B6D5}");

        #endregion 固定模板Id

        /// <summary>
        /// 级别属性的名字。
        /// </summary>
        public const string LevelPropertyName = "lv";   //Runes

        /// <summary>
        /// 堆叠上限属性的名字。
        /// </summary>
        public const string StackUpperLimit = "stc";

        /// <summary>
        /// 容器容量上限属性。
        /// </summary>
        public const string ContainerCapacity = "cap";

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
                ChildrenTemplateIdString=$"{ProjectConstant.DangqianZuoqiSlotId},{ProjectConstant.ShenWenSlotId},{ProjectConstant.ShenWenBagSlotId},{ProjectConstant.ShoulanSlotId}" +  //通过串联将长字符串文本拆分为较短的字符串，从而提高源代码的可读性。 编译时将这些部分连接到单个字符串中。 无论涉及到多少个字符串，均不产生运行时性能开销。
                    $",{ProjectConstant.JinbiId},{ProjectConstant.ShouyiSlotId}",
                PropertiesString="mpp=20,dpp=300,ipp=1",    //最大体力
            },
            new GameItemTemplate(ProjectConstant.ShenWenSlotId)
            {
                DisplayName="神纹装备槽",
            },
            new GameItemTemplate(ProjectConstant.ShenWenBagSlotId)
            {
                DisplayName="神纹背包槽",
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
                DisplayName="金币Id",
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
            var result = false;

            var mountsSlot = gameChar.GameItems.First(c => c.TemplateId == ProjectConstant.DangqianZuoqiSlotId);   //当前坐骑槽

            var headTemplate = gitm.Id2Template.Values.FirstOrDefault(c => c.GId.GetValueOrDefault() == 4001);
            var bodyTemplate = gitm.Id2Template.Values.FirstOrDefault(c => c.GId.GetValueOrDefault() == 3001);
            var mounts = CreateMounts(service, headTemplate, bodyTemplate);
            mountsSlot.Children.Add(mounts);
            result = true;
            return result;
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

        public static bool CombatStart(IServiceProvider service, StartCombatData data)
        {
            return true;
        }

        public static bool CombatEnd(IServiceProvider service, EndCombatData data)
        {
            GameChar gameChar = data.GameChar;
            IEnumerable<GameItem> gameItems = data.GameItems;
            if (null == gameChar.CurrentDungeonId || !gameChar.CurrentDungeonId.HasValue)
                return false;
            var world = service.GetRequiredService<VWorld>();
            var gitm = world.ItemTemplateManager;
            var cmbm = world.CombatManager;
            var tm = gitm.GetTemplateFromeId(gameChar.CurrentDungeonId.Value);    //关卡模板
            //校验时间
            DateTime dt = gameChar.CombatStartUtc.GetValueOrDefault(DateTime.UtcNow);
            var dtNow = DateTime.UtcNow;
            if (dtNow - dt < TimeSpan.FromSeconds((double)tm.Properties.GetValueOrDefault("tl", decimal.Zero))) //若时间过短
            {
                return false;
            }
            if (!Verify(service, tm, gameItems, out string msg))
                return false;
            var totalItems = gameChar.GameItems.First(c => c.TemplateId == ProjectConstant.ShouyiSlotId).Children.Concat(gameItems);    //总计收益
            var sec = (int)tm.Properties["sec"];    //关卡号
            if (-1 != sec) //若不是大关
            {
                var coll = from tmp in gitm.Id2Template.Values
                           where tmp.TypeCode == 7 && (int)tmp.Properties.GetValueOrDefault("typ", decimal.Zero) == (int)tm.Properties.GetValueOrDefault("typ") &&
                           (int)tmp.Properties.GetValueOrDefault("mis", decimal.Zero) == (int)tm.Properties.GetValueOrDefault("mis")
                           select tmp;
                tm = coll.First();
            }
            if (!Verify(service, tm, totalItems, out msg))
                return false;
            //记录收益
            var shouyiSlot = gameChar.GameItems.First(c => c.TemplateId == ProjectConstant.ShouyiSlotId);
            shouyiSlot.Children.AddRange(gameItems);
            //下一关数据
            data.Template = cmbm.GetNext(data.Template);
            if (null == data.Template) //若已经结束
            {
                //移动数据
                //金币
                var gold = shouyiSlot.Children.Where(c => c.TemplateId == ProjectConstant.JinbiId);
                var goldCount = gold.Sum(c => c.Count);
                foreach (var item in gold)
                    shouyiSlot.Children.Remove(item);
                gameChar.GameItems.First(c => c.TemplateId == ProjectConstant.JinbiId).Count += goldCount;
                //野生怪物
                var mounts = shouyiSlot.Children.Where(c => c.TemplateId == ProjectConstant.ZuojiZuheRongqi);
                var shoulan = gameChar.GameItems.First(c => c.TemplateId == ProjectConstant.ShoulanSlotId);
                foreach (var item in mounts)
                {
                    shouyiSlot.Children.Remove(item);
                }
                shoulan.Children.AddRange(mounts);
                //神纹
                var shenwens = (from tmp in shouyiSlot.Children
                                let typeCode = gitm.GetTemplateFromeId(tmp.TemplateId).TypeCode
                                where typeCode == ProjectConstant.ShenwenAtkTCode || typeCode == ProjectConstant.ShenwenHPTCode || typeCode == ProjectConstant.ShenwenQltTCode
                                select tmp).ToArray();  //得到所有神纹
                var ary = (from tmp in shenwens
                           group tmp by tmp.TemplateId into g
                           select new { g.Key, count = g.Count() }).ToArray();  //分类处理
                foreach (var item in shenwens)  //清理收益槽
                    shouyiSlot.Children.Remove(item);
                var shenwenBag = gameChar.GameItems.First(c => c.TemplateId == ProjectConstant.ShenWenBagSlotId);   //神纹背包
                var modifies = ary.Join(shenwenBag.Children, c => c.Key, c => c.TemplateId, (l, r) => (l, r));   //修改数量的集合
                foreach (var (l, r) in modifies)
                    r.Count += l.count;
            }

            return true;
        }

        /// <summary>
        /// 校验收益是否超过上限。
        /// </summary>
        /// <param name="service"></param>
        /// <param name="itemTemplate"></param>
        /// <param name="gameItems"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        private static bool Verify(IServiceProvider service, GameItemTemplate itemTemplate, IEnumerable<GameItem> gameItems, out string msg)
        {
            var gitm = service.GetService<GameItemTemplateManager>();
            //typ关卡类别=1普通管卡 mis大关数 sec=小关gold=数金币掉落上限，aml=获得资质野怪的数量，mne=资质合上限，mt=神纹数量上限，
            if (itemTemplate.Properties.TryGetValue("gold", out object gold)) //若要限制金币数量
            {
                if (gameItems.Where(c => c.TemplateId == ProjectConstant.JinbiId).Select(c => c.Count).Sum() > (decimal)gold)   //若金币超过上限
                {
                    msg = "金币超过上限";
                    return false;
                }
            }
            if (itemTemplate.Properties.TryGetValue("aml", out object monsterCount)) //若要限制怪数量
            {
                if (gameItems.Where(c => c.TemplateId == ProjectConstant.ZuojiZuheRongqi).Count() > (decimal)monsterCount)   //若怪数量超过上限
                {
                    msg = "怪数量超过上限";
                    return false;
                }
            }
            if (itemTemplate.Properties.TryGetValue("mne", out object mne)) //若要限制单个怪资质总和
            {
                var coll = from tmp in gameItems
                           let mneatk = (float)tmp.Properties.GetValueOrDefault("mneatk", decimal.Zero)
                           let mneqlt = (float)tmp.Properties.GetValueOrDefault("mneqlt", decimal.Zero)
                           let mnemhp = (float)tmp.Properties.GetValueOrDefault("mnemhp", decimal.Zero)
                           let mneTotal = mneatk + mneqlt + mnemhp
                           where mneTotal > (float)mne
                           select tmp;
                var errItem = coll.FirstOrDefault();
                if (null != errItem)   //若单个怪资质总和超过上限
                {
                    msg = $"单个怪资质总和超过上限,TemplateId={gitm.GetTemplateFromeId(errItem.TemplateId)?.GId.GetValueOrDefault()}";
                    return false;
                }
            }
            if (itemTemplate.Properties.TryGetValue("mt", out object mt)) //若要限制神纹数量
            {
                var coll = gitm.Id2Template.Values.Where(c => c.TypeCode == ProjectConstant.ShenwenTCode); //获取所有神纹模板
                var shenwen = gameItems.Join(coll, c => c.TemplateId, c => c.Id, (l, r) => l);    //获取神纹的集合
                if (shenwen.Count() > (int)mt) //若神纹数量超过上限
                {
                    msg = "神纹数量超过上限";
                    return false;
                }
            }
            msg = null;
            return true;
        }
    }
}
