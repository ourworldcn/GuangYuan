/*
 * 特定与家园相关的代码
 */

using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace GuangYuan.GY001.BLL.Homeland
{
    /// <summary>
    /// 风格。
    /// </summary>
    public class HomelandFengge
    {
        public HomelandFengge()
        {

        }

        /// <summary>
        /// 风格号。
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// 下属方案对象集合。
        /// </summary>
        public List<HomelandFangan> Fangans { get; set; } = new List<HomelandFangan>();

        /// <summary>
        /// 客户端记录一些额外信息。服务器不使用。
        /// 记录在风格对象的 ClientString 上。
        /// </summary>
        public string ClientString { get; set; }
    }

    /// <summary>
    /// 方案对象。
    /// </summary>
    public class HomelandFangan
    {
        public HomelandFangan()
        {

        }

        /// <summary>
        /// 唯一Id，暂时无用，但一旦生成则保持不变。
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 下属具体加载物品及其位置信息
        /// </summary>
        public List<HomelandFanganItem> FanganItems { get; set; } = new List<HomelandFanganItem>();

        /// <summary>
        /// 该方案是否被激活。
        /// </summary>
        public bool IsActived { get; set; }

        /// <summary>
        /// 客户端记录一些额外信息。服务器不使用。
        /// </summary>
        public string ClientString { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<(Guid, Guid)> ToWorkItems()
        {
            foreach (var item in FanganItems)
            {
                foreach (var id in item.ItemIds)
                {
                    yield return (item.ContainerId, id);
                }
            }
        }
    }

    /// <summary>
    /// 方案中的子项。
    /// </summary>
    public class HomelandFanganItem
    {
        public HomelandFanganItem()
        {

        }

        /// <summary>
        /// 要加入 ContainerId 指出容器的子对象Id。
        /// </summary>
        public List<Guid> ItemIds { get; set; } = new List<Guid>();

        /// <summary>
        /// 容器的Id。
        /// </summary>
        public Guid ContainerId { get; set; }

        /// <summary>
        /// 要替换的新的模板Id值。空表示不替换。
        /// </summary>
        public Guid? NewTemplateId { get; set; }

        /// <summary>
        /// 客户端记录一些额外信息。服务器不使用。
        /// </summary>
        public string ClientString { get; set; }

    }

    /// <summary>
    /// 关于家园的扩展方法封装类。
    /// </summary>
    public static class HomelandExtensions
    {
        #region 基础功能

        /// <summary>
        /// 获取指定用户的主基地对象。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns>没有找到则返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public GameItem GetMainbase(this GameChar gameChar) =>
            gameChar.GetHomeland()?.Children.FirstOrDefault(c => c.GetDikuaiIndex() == 0);

        /// <summary>
        /// 获取家园对象。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns>如果没有找到，可能返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameItem GetHomeland(this GameChar gameChar) => gameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.HomelandSlotId);

        /// <summary>
        /// 获取风格背包。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns>如果没有找到，可能返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameItem GetFenggeBag(this GameChar gameChar) => gameChar.GetHomeland()?.Children.FirstOrDefault(c => c.TemplateId == ProjectConstant.HomelandPlanBagTId);

        /// <summary>
        /// 获取所有主基地模板。
        /// </summary>
        /// <param name="templateManager"></param>
        /// <returns>钻石计价的价格，如果没有价格则返回null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public IEnumerable<GameItemTemplate> GetMainBaseTemplates(this GameItemTemplateManager templateManager) => templateManager.GetTemplates(c => c.CatalogNumber == 0100);

        /// <summary>
        /// 同步锁。
        /// </summary>
        static private readonly object ThisLocker = new object();

        /// <summary>
        /// 缓存所有地块信息。
        /// </summary>
        private static ConcurrentDictionary<ValueTuple<int, int>, GameItemTemplate> _AllDikuai;

        /// <summary>
        /// 获取所有地块模板的字典。
        /// </summary>
        /// <param name="manager"></param>
        /// <returns>键二元值类型元组(风格号,地块索引号)，值是地块的模板对象。</returns>
        static public ConcurrentDictionary<ValueTuple<int, int>, GameItemTemplate> GetAllDikuai(this GameItemTemplateManager manager)
        {
            if (_AllDikuai is null)
                lock (ThisLocker)
                    if (_AllDikuai is null)
                    {
                        var coll = manager.GetTemplates(c => c.CatalogNumber / 100 == 1).Select(c => KeyValuePair.Create((c.GetFenggeNumber(), c.GetDikuaiIndex()), c));
                        _AllDikuai = new ConcurrentDictionary<(int, int), GameItemTemplate>(coll);
                        Debug.Assert(_AllDikuai.Keys.All(c => c.Item1 != 0), "风格号不应为0");
                    }
            return _AllDikuai;
        }

        /// <summary>
        /// 获取指定风格号的所有模板对象。
        /// </summary>
        /// <param name="fenggeNumbers"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public IEnumerable<GameItemTemplate> GetTemplatesByFenggeNumber(this GameItemTemplateManager manager, int fenggeNumber) =>
           manager.GetAllDikuai().Values.Where(c => c.GetFenggeNumber() == fenggeNumber);

        /// <summary>
        /// 获取指定地块号的模板。
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="index">地块号，0是主基地，第一个地块是1，以此类推。</param>
        /// <returns>指定地块号的所有风格的模板。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public IEnumerable<GameItemTemplate> GetTemplatesByDikuaiIndex(this GameItemTemplateManager manager, int index) =>
            manager.GetAllDikuai().Values.Where(c => c.GetDikuaiIndex() == index);

        /// <summary>
        /// 按地块索引和风格号返回模板。如果没有找到则返回null。
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="fenggeNumber"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public GameItemTemplate GetTemplateByNumberAndIndex(this GameItemTemplateManager manager, int fenggeNumber, int index) =>
            manager.GetAllDikuai().GetValueOrDefault((index, fenggeNumber), null);

        /// <summary>
        /// 获取模板的风格号，如果没有风格号则返回-1.
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public int GetFenggeNumber(this GameItemTemplate template) => template.CatalogNumber / 100 == 1 ? template.Sequence : -1;

        /// <summary>
        /// 获取模板的地块索引号，如果不是地块则返回-1.
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public int GetDikuaiIndex(this GameItemTemplate template) => template.CatalogNumber / 100 == 1 ? template.Catalog3Number : -1;

        /// <summary>
        /// 返回用户当前有多少地块，包含主基地。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public int GetDikuaiCount(this GameChar gameChar) =>
            gameChar.GetHomeland().Children.Count(c => ((c.Template as GameItemTemplate)?.GetDikuaiIndex() ?? 0) >= 0);

        /// <summary>
        /// 获得地块信息。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public IEnumerable<(int, GameItem)> GetDikuais(this GameChar gameChar) =>
            gameChar.GetHomeland().Children.Where(c => c.IsDikuai()).
                Select(c => (c.GetDikuaiIndex(), c));

        /// <summary>
        /// 获取指定对象是否是地块。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <param name="dikuaiIndex">当是地块时，返回地块的索引号。</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsDikuai(this GameItem gameItem) =>
            gameItem.GetDikuaiIndex() >= 0;

        /// <summary>
        /// 获取该物品的地块号。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <returns>地块号，如果不是地块则返回-1。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public int GetDikuaiIndex(this GameItem gameItem) => (gameItem.Template as GameItemTemplate)?.GetDikuaiIndex() ?? -1;

        /// <summary>
        /// 获取模板的风格号，如果不是风格则返回-1。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public int GetFenggeNumber(this GameItem gameItem) => (gameItem.Template as GameItemTemplate)?.GetFenggeNumber() ?? -1;

        /// <summary>
        /// 获取角色当前的风格号。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public int GetCurrentFenggeNumber(this GameChar gameChar) =>
            gameChar.GetHomeland().Children.FirstOrDefault(c => c.GetDikuaiIndex() == 0)?.GetFenggeNumber() ?? -1;

        /// <summary>
        /// 合并风格对象。
        /// </summary>
        /// <param name="gameChar">角色对象。</param>
        /// <param name="fenggeItems">自动添加数据，和免费风格。</param>
        /// <param name="manager">模板管理器。</param>
        static public void MergeFangans(this GameChar gameChar, List<HomelandFengge> fenggeItems, GameItemTemplateManager manager)
        {
            var hl = gameChar.GetHomeland();

            var numbers = manager.GetFenggeNumbersWithFree().Union(fenggeItems.Select(c => c.Number));    //当前有的风格号及免费送的风格号
            foreach (var number in numbers.ToArray()) //遍历每个风格号
            {
                var fengge = fenggeItems.FirstOrDefault(c => c.Number == number);   //获取风格对象
                if (fengge is null)  //若没有风格对象
                {
                    fengge = new HomelandFengge()
                    {
                        Number = number,
                    };
                    fenggeItems.Add(fengge);
                }
                while (fengge.Fangans.Count < 2)   //若方案不足
                {
                    var fangan = new HomelandFangan();
                    fengge.Fangans.Add(fangan);
                }
                foreach (var fangan in fengge.Fangans)  //遍历方案
                {
                    fangan.FanganItems.MergeContainer(hl, number, manager); //合并方案项
                }
            }
        }

        /// <summary>
        /// 追加或合并一个物品的信息到一组方案项中。
        /// </summary>
        /// <param name="obj">要合并的集合。</param>
        /// <param name="gameItem">顶层物品。</param>
        /// <param name="fenggeNumber">指定风格号。</param>
        /// <param name="manager">模板管理器。</param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        static public void MergeContainer(this ICollection<HomelandFanganItem> obj, GameItem gameItem, int fenggeNumber, GameItemTemplateManager manager)
        {
            var dikuaiNumber = gameItem.GetDikuaiIndex();   //指定物品的地块索引
            var item = obj.FirstOrDefault(c => c.ContainerId == gameItem.Id);   //获得此项在方案项中的对象
            if (item is null)    //若需要加入
            {
                item = new HomelandFanganItem()
                {
                    ContainerId = gameItem.Id,
                };
                obj.Add(item);
            }
            if (dikuaiNumber >= 0)   //若是地块
                item.NewTemplateId = manager.GetAllDikuai()[(fenggeNumber, dikuaiNumber)].Id;
            else if (gameItem.GetCatalogNumber() == 42)  //若是旗帜
            {
                item.NewTemplateId ??= gameItem.TemplateId;
            }
            foreach (var child in gameItem.Children)    //遍历加入子项
            {
                if (!item.ItemIds.Contains(child.Id))    //若无此子项Id
                    item.ItemIds.Add(child.Id);
                obj.MergeContainer(child, fenggeNumber, manager);   //递归加入
            }
        }

        #endregion 基础功能

        /// <summary>
        /// 设置家园的建设方案。
        /// </summary>
        /// <param name="plans">家园建设方案的集合。</param>
        static public void SetHomelandPlans(this GameCharManager manager, IEnumerable<HomelandFengge> plans, GameChar gameChar)
        {
            var world = manager.World;
            var gu = gameChar.GameUser;
            if (!world.CharManager.Lock(gu))
                return;
            try
            {
                //var hpb = gameChar.AllChildren.First(c => c.TemplateId == ProjectConstant.HomelandPlanBagTId); //家园方案背包
                //var coll = from nPlan in plans
                //           join oPlan in hpb.Children on nPlan.Number equals oPlan.Number
                //           select (NewPlan: nPlan, OldPlan: oPlan);
                //foreach (var item in coll)
                //{
                //    var exProp = item.OldPlan.GetOrAddExtendProperty(ProjectConstant.HomelandPlanPropertyName, c =>
                //         new GameExtendProperty() { Name = c, });
                //    var jsonStr = JsonSerializer.Serialize(item.NewPlan);
                //    exProp.Text = jsonStr;
                //}
                world.CharManager.NotifyChange(gu);
            }
            finally
            {
                world.CharManager.Unlock(gu, true);
            }
        }

        /// <summary>
        /// 获取指定角色的家园建设风格及方案。
        /// 此函数不重置下线计时器。
        /// </summary>
        /// <param name="gc">角色对象。</param>
        /// <returns>方案集合，对应每个家园风格对象都会生成一个风格，如无内容则仅有Id,ClientString有效。</returns>
        /// <exception cref="InvalidOperationException">内部数据结构损坏</exception>
        static public IEnumerable<HomelandFengge> GetHomelandPlans(this GameCharManager manager, GameChar gc)
        {
            var result = new List<HomelandFengge>();
            try
            {
                //var hpb = gc.AllChildren.First(c => c.TemplateId == ProjectConstant.HomelandPlanBagTId); //家园方案背包
                //foreach (var item in hpb.Children)
                //{
                //    var hpo = item.ExtendProperties.FirstOrDefault(c => c.Name == ProjectConstant.HomelandPlanPropertyName);  //方案数据对象
                //    HomelandFengge tmp;
                //    if (hpo is null || string.IsNullOrWhiteSpace(hpo.Text)) //若未初始化
                //    {
                //        tmp = new HomelandFengge() { Number = item.Number, ClientString = item.ClientGutsString };
                //    }
                //    else
                //    {
                //        tmp = JsonSerializer.Deserialize(hpo.Text, typeof(HomelandFengge)) as HomelandFengge;
                //    }
                //    result.Add(tmp);
                //}
            }
            catch (Exception err)
            {

                throw new InvalidOperationException("", err);
            }
            return result;
        }

        /// <summary>
        /// 获取数据库中所有免费的风格号。
        /// TO DO 未来可能加缓存机制。
        /// </summary>
        /// <param name="manager"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<int> GetFenggeNumbersWithFree(this GameItemTemplateManager manager) =>
           manager.GetAllDikuai().Where(c => c.Value.CatalogNumber == 100 && c.Value.IsFree()) //免费的
                .Select(c => c.Value.GetFenggeNumber()).Distinct();

        #region 方案的缓存与存储

        /// <summary>
        /// 获取家园风格对象。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns>初始状态可能是空集合。需要自行初始化。</returns>
        public static List<HomelandFengge> GetFengges(this GameChar gameChar)
        {
            var bag = gameChar.GetHomeland().Children.First(c => c.TemplateId == ProjectConstant.HomelandPlanBagTId);   //方案背包
            var descriptor = bag.ExtendPropertyDictionary.GetOrAdd(ProjectConstant.HomelandPlanPropertyName, c =>
                 new ExtendPropertyDescriptor()
                 {
                     Data = new List<HomelandFengge>(),
                     IsPersistence = true,
                     Name = c,
                     Type = typeof(List<HomelandFengge>),
                 });
            var result = descriptor.Data as List<HomelandFengge>;

            return result;
        }

        /// <summary>
        /// 获取当前激活的风格号，若没有激活风格则返回-1。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        public static int GetActiveStyleNumber(this GameChar gameChar)
        {
            foreach (var fengge in gameChar.GetFengges())
            {
                if (fengge.Fangans.Any(fangan => fangan.IsActived))
                    return fengge.Number;
            }
            return -1;
        }

        #endregion 方案的缓存与存储

    }
}