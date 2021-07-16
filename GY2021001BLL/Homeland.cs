/*
 * 特定与家园相关的代码
 */

using GY2021001DAL;
using Gy2021001Template;
using Microsoft.Extensions.DependencyInjection;
using OwGame;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace GY2021001BLL.Homeland
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
        public int Id { get; set; }

        /// <summary>
        /// 下属方案对象集合。
        /// </summary>
        public List<HomelandFangan> Fangans { get; } = new List<HomelandFangan>();

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
        public Guid Id { get; set; }

        /// <summary>
        /// 下属具体加载物品及其位置信息
        /// </summary>
        public List<HomelandFanganItem> FanganItems { get; } = new List<HomelandFanganItem>();

        /// <summary>
        /// 该方案是否被激活。
        /// </summary>
        public bool IsActived { get; set; }

        /// <summary>
        /// 客户端记录一些额外信息。服务器不使用。
        /// </summary>
        public string ClientString { get; set; }

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
        public List<Guid> ItemIds { get; } = new List<Guid>();

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
        static ConcurrentDictionary<ValueTuple<int, int>, GameItemTemplate> _AllDikuai;

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
        /// 获取模板的风格号，如果没有风格号则返回-1。
        /// </summary>
        /// <param name="gameItem"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public int GetFenggeNumber(this GameItem gameItem) => (gameItem.Template as GameItemTemplate)?.GetFenggeNumber() ?? -1;

        static public void GetCurrentFengge(this GameChar gameChar, List<HomelandFanganItem> fanganItems)
        {
            var hl = gameChar.GetHomeland();

            var tmp = new HomelandFanganItem()
            {
                ContainerId = hl.Id,
            };
            var mb = gameChar.GetMainbase();
            var mbTemplate = mb.Template as GameItemTemplate;
            var result = new HomelandFengge() { Id = mbTemplate.GetFenggeNumber() };
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
                //           join oPlan in hpb.Children on nPlan.Id equals oPlan.Id
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
                //        tmp = new HomelandFengge() { Id = item.Id, ClientString = item.ClientGutsString };
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

        /// <summary>
        /// 获取指定风格号的风格对象。
        /// </summary>
        /// <param name="gameChar">根据当前解锁的地块生成方案对象。</param>
        /// <param name="fenggeNumber">方案号。</param>
        /// <param name="services">用到的服务容器。</param>
        /// <returns></returns>
        public static List<HomelandFengge> GetFenggeObject(this GameChar gameChar, int fenggeNumber, List<HomelandFengge> fengges, IServiceProvider services)
        {
            if (gameChar is null)
                throw new ArgumentNullException(nameof(gameChar));

            if (services is null)
                throw new ArgumentNullException(nameof(services));

            var gitm = services.GetService<GameItemTemplateManager>();
            var gim = services.GetService<GameItemManager>();

            var result = gameChar.GetFengges();
            if (0 == result.Count) //若需要初始化
            {
                var numbers = gitm.GetFenggeNumbersWithFree();  //所有免费风格号
                foreach (var number in numbers)
                {
                    var mbTemplate = gitm.GetTemplateByNumberAndIndex(number, 0); //唯一的主基地模板
                    var fengge = new HomelandFengge() { Id = number };
                    for (int i = 0; i < 3; i++) //三个方案
                    {
                        var fangan = new HomelandFangan()
                        {
                            Id = Guid.NewGuid(),
                        };
                        var fanganItme = new HomelandFanganItem()
                        {
                            ContainerId = gameChar.GetHomeland().Id,
                        };

                        fengge.Fangans.Add(fangan);
                    }

                }

            }
            var coll = gitm.GetTemplatesByFenggeNumber(fenggeNumber).ToArray(); //该风格的所有相关模板
            for (int i = 0; i < 2; i++) //方案
            {
                HomelandFangan fangan = new HomelandFangan()    //方案
                {
                    Id = Guid.NewGuid(),
                };
                foreach (var item in coll)    //方案项
                {

                }
                //result.Fangans.Add(fangan);
            }
            return result;
        }


        static public void AddFengge(this GameChar gameChar, HomelandFengge fengge, int dikuaiIndex, IServiceProvider services)
        {
            foreach (var fangan in fengge.Fangans)
            {

            }
        }
        #region 方案的缓存与存储

        /// <summary>
        /// 暂存用户的家园风格-方案对象。
        /// 键是角色Id,值是所有风格对象集合。
        /// </summary>
        static readonly ConcurrentDictionary<Guid, List<HomelandFengge>> _HomelandFengges = new ConcurrentDictionary<Guid, List<HomelandFengge>>();

        /// <summary>
        /// 获取家园风格对象。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns>初始状态可能是空集合。需要自行初始化。</returns>
        public static List<HomelandFengge> GetFengges(this GameChar gameChar)
        {
            var result = _HomelandFengges.GetOrAdd(gameChar.Id, c =>
            {
                var bag = gameChar.GetFenggeBag();
                var obj = bag.GetOrAddExtendProperty(ProjectConstant.HomelandPlanPropertyName, c => new GameExtendProperty() { Name = c });

                var str = obj.Text;
                var result = string.IsNullOrWhiteSpace(str) ? new List<HomelandFengge>() : (List<HomelandFengge>)JsonSerializer.Deserialize(str, typeof(List<HomelandFengge>));
                gameChar.Saving += Bag_Saving;
                return result;
            });
            return result;
        }

        private static void Bag_Saving(object sender, EventArgs e)
        {
            if (sender is GameChar gc && _HomelandFengges.TryGetValue(gc.Id, out var list))
            {
                var bag = gc.GetFenggeBag();
                var obj = bag.GetOrAddExtendProperty(ProjectConstant.HomelandPlanPropertyName, c => new GameExtendProperty() { Name = c });
                var str = JsonSerializer.Serialize(list);
                obj.Text = str;
            }
        }

        static public void InitializeFengge(this GameChar gameChar, IServiceProvider service)
        {
            var list = gameChar.GetFengges();
        }
        #endregion 方案的缓存与存储

    }
}