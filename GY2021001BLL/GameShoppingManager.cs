using Game.Social;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using Microsoft.EntityFrameworkCore;
using OW.Game;
using OW.Game.Item;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace GuangYuan.GY001.BLL
{
    public class GameShoppingManagerOptions
    {

    }

    /// <summary>
    /// 商城相关功能的服务类。
    /// </summary>
    public class GameShoppingManager : GameManagerBase<GameShoppingManagerOptions>
    {
        private Lazy<Dictionary<string, int[]>> _Genus2GroupNumbers;
        /// <summary>
        /// 键是刷新商品的属，值刷新商品的组号。
        /// </summary>
        public IReadOnlyDictionary<string, int[]> Genus2GroupNumbers => _Genus2GroupNumbers.Value;

        public GameShoppingManager()
        {
            Initializer();
        }

        public GameShoppingManager(IServiceProvider service) : base(service)
        {
            Initializer();
        }

        public GameShoppingManager(IServiceProvider service, GameShoppingManagerOptions options) : base(service, options)
        {
            Initializer();
        }

        /// <summary>
        /// 构造函数调用的的初始化函数。
        /// </summary>
        private void Initializer()
        {
            _Genus2GroupNumbers = new Lazy<Dictionary<string, int[]>>(() =>
            {
                var coll = from tmp in World.ItemTemplateManager.Id2Shopping.Values
                           where tmp.GroupNumber.HasValue
                           group tmp by tmp.Genus into g
                           select (g.Key, g.Select(c => c.GroupNumber.Value).ToArray());
                return coll.ToDictionary(c => c.Key, c => c.Item2);

            }, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        /// 获取当前用户可见的商城表，不在有效时间范围内的不会返回。
        /// </summary>
        /// <param name="datas"></param>
        public void GetList(GetListDatas datas)
        {
            using var dwUser = datas.LockUser();
            if (dwUser is null)
                return;
            IEnumerable<GameShoppingTemplate> coll; //非随机刷新商品
            if (string.IsNullOrWhiteSpace(datas.Genus))
                coll = World.ItemTemplateManager.Id2Shopping.Values.Where(c => IsValid(c, datas.Now) && c.GroupNumber is null);
            else
                coll = World.ItemTemplateManager.Id2Shopping.Values.Where(c => c.Genus == datas.Genus && IsValid(c, datas.Now) && c.GroupNumber is null);
            var view = new ShoppingSlotView(World, datas.GameChar, datas.Now);
            foreach (var item in Genus2GroupNumbers.Keys)
                RefreshIfDateChanged(view, item, datas.Now);    //刷新所有日期变化后的随机商品
            var rg = from tmp in World.ItemTemplateManager.Id2Shopping.Values   //随机刷新的商品
                     where tmp.GroupNumber.HasValue && view.RandomGods[tmp.Genus] == tmp.GroupNumber && (string.IsNullOrWhiteSpace(datas.Genus) || tmp.Genus == datas.Genus)
                     select tmp;
            datas.ShoppingTemplates.AddRange(coll);
            datas.ShoppingTemplates.AddRange(rg);
            view.Save();
        }

        /// <summary>
        /// 购买指定商品。
        /// </summary>
        /// <param name="datas">
        /// <paramref name="datas"/>
        /// </param>
        public void Buy(BuyDatas datas)
        {
            using var dwUser = datas.LockUser();
            if (dwUser is null)
                return;
            if (!World.ItemTemplateManager.Id2Shopping.TryGetValue(datas.ShoppingId, out var template))
            {
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.ErrorMessage = "找不到商品Id";
                return;
            }
            DateTime now = DateTime.UtcNow;   //当前时间
            var view = new ShoppingSlotView(World, datas.GameChar, datas.Now);
            var oldCount = view.GetCount(template, now);
            if (-1 != template.MaxCount && oldCount + datas.Count > template.MaxCount)
            {
                datas.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                datas.ErrorMessage = "指定商品剩余数量不足";
                return;
            }
            //校验可购买性
            if (template.GroupNumber.HasValue)   //若购买商品可刷新
            {
                if (view.RandomGods.TryGetValue(template.Genus, out var gpn) && template.GroupNumber != gpn)
                {
                    datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                    datas.ErrorMessage = "指定商品不可购买。";
                    return;
                }
            }
            //修改数据
            //生成物品
            var gim = World.ItemManager;
            var gi = new GameItem();
            World.EventsManager.GameItemCreated(gi, template.ItemTemplateId);
            var container = gim.GetDefaultContainer(datas.GameChar, gi);
            List<GameItem> list = new List<GameItem>();
            World.ItemManager.AddItem(gi, container, list, datas.ChangeItems);
            if (list.Count > 0)    //若需要发送邮件
            {
                var mail = new GameMail();
                World.SocialManager.SendMail(mail, new Guid[] { datas.GameChar.Id }, SocialConstant.FromSystemId, list.Select(c => (c, gim.GetDefaultContainer(datas.GameChar, c).TemplateId)));
            }
            //改写购买记录数据
            view.AddItem(template, datas.Count, now);
            World.CharManager.NotifyChange(datas.GameChar.GameUser);
        }

        public void Refresh(RefreshDatas datas)
        {
            using var dwUser = datas.LockUser();
            if (dwUser is null)
                return;
            var slot = datas.GameChar.GetShoppingSlot();
            var view = new ShoppingSlotView(World, datas.GameChar, datas.Now);
            var dic = view.RandomGods;
        }

        /// <summary>
        /// 若最后刷新日期已经变化，则刷新随机刷新商品。
        /// </summary>
        /// <param name="view"></param>
        /// <param name="genus"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        private bool RefreshIfDateChanged(ShoppingSlotView view, string genus, DateTime now)
        {
            if (view.RandomGodsRefreshDatetime.GetValueOrDefault(genus).Date != now.Date)
                return Refresh(view, genus, now);
            return false;
        }

        /// <summary>
        /// 强制复位可刷新商品。
        /// </summary>
        /// <param name="view"></param>
        /// <param name="genus">商品的属。</param>
        /// <param name="now">当前时间。</param>
        private bool Refresh(ShoppingSlotView view, string genus, DateTime now)
        {
            if (!Genus2GroupNumbers.TryGetValue(genus, out var ary))    //若不是包含随机商品的属
                return false;
            view.RandomGodsRefreshDatetime[genus] = now;    //设置刷新时间
            view.RandomGods[genus] = VWorld.WorldRandom.Next(ary.Length);
            return true;
        }

        /// <summary>
        /// 指定商品在指定时间是否可以销售。
        /// </summary>
        /// <param name="template"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        public bool IsValid(GameShoppingTemplate template, DateTime now)
        {
            var start = template.GetStart(now);
            var end = template.GetEnd(now);
            return now >= start && now <= end;
        }
    }

    public class RefreshDatas : ChangeItemsWorkDatasBase
    {
        public RefreshDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public RefreshDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public RefreshDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 要刷新的页签(属)名。
        /// </summary>
        public string Genus { get; set; }
        public DateTime Now { get; internal set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 购买商品的接口工作数据类。
    /// </summary>
    public class BuyDatas : ChangeItemsAndMailWorkDatsBase
    {
        public BuyDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public BuyDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public BuyDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 购买商品的模板Id。
        /// </summary>
        public Guid ShoppingId { get; set; }

        /// <summary>
        /// 购买的数量。
        /// </summary>
        public decimal Count { get; set; }

        public DateTime Now { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 获取商城数据的工作接口数据。
    /// </summary>
    public class GetListDatas : ComplexWorkDatasBase
    {
        public GetListDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public GetListDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public GetListDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 指定时间点。
        /// </summary>
        public DateTime Now { get; set; }

        /// <summary>
        /// 页签的名字，前端与数据协商好即可。如果设置了则仅返回指定页签(属)的商品。null会返回所有商品。
        /// </summary>
        public string Genus { get; set; }

        public List<GameShoppingTemplate> ShoppingTemplates { get; } = new List<GameShoppingTemplate>();
    }

    public static class GameShoppingManagerExtensions
    {
        public static readonly Guid ShoppingSlotTId = new Guid("{9FB9BA0E-244B-4CC3-A151-461AD18E2699}");

        /// <summary>
        /// 获取商城槽数据。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        public static GameItem GetShoppingSlot(this GameChar gameChar) =>
            gameChar.GameItems.FirstOrDefault(c => c.TemplateId == ShoppingSlotTId);
    }

    public class ShoppingSlotView : GameCharWorkDataBase
    {
        public ShoppingSlotView([NotNull] VWorld world, [NotNull] GameChar gameChar, DateTime now) : base(world, gameChar)
        {
            _Now = now;
        }

        public GameItem ShoppingSlot => GameChar.GetShoppingSlot();

        private DateTime _Now;
        public DateTime Now { get => _Now; set => _Now = value; }

        /// <summary>
        /// 增加购买记录。
        /// </summary>
        /// <param name="template"></param>
        /// <param name="count"></param>
        /// <param name="now"></param>
        /// <returns>false 原有购买记录，true 新增了购买记录</returns>
        public bool AddItem(GameShoppingTemplate template, decimal count, DateTime now)
        {
            var gar = new GameActionRecord()
            {
                DateTimeUtc = now,
                ParentId = GameChar.Id,
                ActionId = BuyRecordActionId,
            };
            gar.Properties["ShoppingId"] = template.IdString;
            gar.Properties["BuyCount"] = count;
            UserContext.Add(gar);
            return true;
        }

        /// <summary>
        /// 指定周期内指定物品已购买数量。
        /// </summary>
        /// <param name="template"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        public decimal GetCount(GameShoppingTemplate template, DateTime now)
        {
            var start = template.GetStart(now); //此周期开始时间
            var end = template.GetStart(now);   //此周期结束时间
            var coll = UserContext.Set<GameActionRecord>().Where(c => c.DateTimeUtc >= start && c.DateTimeUtc < end && c.ParentId == GameChar.Id && c.ActionId == BuyRecordActionId); //此周期内购买的购买记录
            var count = coll.AsEnumerable().Where(c => c.Properties.GetGuidOrDefault("ShoppingId") == template.Id).Sum(c => c.Properties.GetDecimalOrDefault("BuyCount"));    //已经够买的数量
            return count;
        }

        private Dictionary<string, int> _RandomGods;
        /// <summary>
        /// 获取当前随机商品信息。键是页签值是当前使用的组号。
        /// 若没有给页签指定组号则自动指定一个随机组号。这相当于初始化了数据。
        /// </summary>
        public Dictionary<string, int> RandomGods
        {
            get
            {
                if (_RandomGods is null)
                {
                    _RandomGods = new Dictionary<string, int>();
                    foreach (var item in World.ShoppingManager.Genus2GroupNumbers)
                    {
                        _RandomGods[item.Key] = item.Value[VWorld.WorldRandom.Next(item.Value.Length)];
                    }
                    var tmp = OwConvert.FromUriString<Dictionary<string, int>>(ShoppingSlot.Properties.GetStringOrDefault(nameof(RandomGods)));
                    OwConvert.Fill(tmp, _RandomGods);
                }
                return _RandomGods;
            }
        }

        private Dictionary<string, DateTime> _RandomGodsRefreshDatetime;
        /// <summary>
        /// 刷新商品的时间。没有刷新自动补全为调用时刻刷新。键是随机商品的属，值是随机商品的最后刷新时间。
        /// </summary>
        public Dictionary<string, DateTime> RandomGodsRefreshDatetime
        {
            get
            {
                if (_RandomGodsRefreshDatetime is null)
                {
                    _RandomGodsRefreshDatetime = new Dictionary<string, DateTime>(RandomGods.Select(c => new KeyValuePair<string, DateTime>(c.Key, Now)));
                    var tmp = OwConvert.FromUriString<Dictionary<string, DateTime>>(ShoppingSlot.Properties.GetStringOrDefault(nameof(RandomGodsRefreshDatetime)));
                    OwConvert.Fill(tmp, _RandomGodsRefreshDatetime);
                }
                return _RandomGodsRefreshDatetime;
            }
        }

        private const string BuyRecordActionId = "商城购买";
        private ObservableCollection<GameActionRecord> _BuyRecords;

        /// <summary>
        /// 今日购买记录。
        /// </summary>
        public ObservableCollection<GameActionRecord> BuyRecords
        {
            get
            {
                if (_BuyRecords is null)
                {
                    var coll = UserContext.Set<GameActionRecord>().AsNoTracking().Where(c => c.DateTimeUtc > Now.Date && c.ParentId == GameChar.Id && c.ActionId == BuyRecordActionId);  //今日购买记录
                    _BuyRecords = new ObservableCollection<GameActionRecord>(coll);
                    _BuyRecords.CollectionChanged += BuyRecordsCollectionChanged;
                }
                return _BuyRecords;
            }
        }

        private void BuyRecordsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    UserContext.AddRange(e.NewItems.OfType<GameActionRecord>());
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                default:
                    throw new InvalidOperationException();
            }
        }

        public Dictionary<GameShoppingTemplate, decimal> GetBuyRecordes()
        {
            var coll = from tmp in BuyRecords
                       group tmp.Properties.GetDecimalOrDefault("BuyCount") by tmp.Properties.GetGuidOrDefault("ShoppingId") into g
                       select (g.Key, g.Sum());
            return coll.Select(c => (World.ItemTemplateManager.Id2Shopping[c.Key], c.Item2)).ToDictionary(c => c.Item1, c => c.Item2);
        }

        public void Save()
        {
            if (null != _RandomGods)    //若内容可能有变化
            {
                ShoppingSlot.Properties[nameof(RandomGods)] = OwConvert.ToUriString(_RandomGods);
            }
            if (null != _RandomGodsRefreshDatetime)    //若内容可能有变化
            {
                ShoppingSlot.Properties[nameof(RandomGodsRefreshDatetime)] = OwConvert.ToUriString(_RandomGodsRefreshDatetime);
            }
            UserContext?.SaveChanges();
        }
    }

    public class StringsFromDictionary : IDisposable
    {
        private readonly string _Prefix;
        private readonly char _Separator;
        private Dictionary<string, object> _Dictionary;

        public const char Separator = '`';

        public StringsFromDictionary(Dictionary<string, object> dictionary, string prefix, char separator = Separator)
        {
            _Prefix = prefix;
            _Separator = separator;
            _Dictionary = dictionary;
        }

        public Guid Key { get; set; }

        public ObservableCollection<string> _Datas;
        private bool disposedValue;

        public ObservableCollection<string> Datas
        {
            get
            {
                if (_Datas is null)
                {
                    var str = _Dictionary.GetStringOrDefault($"{_Prefix}{Key}");
                    var ary = string.IsNullOrWhiteSpace(str) ? Array.Empty<string>() : str.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
                    _Datas = new ObservableCollection<string>(ary);
                }
                return _Datas;
            }
        }

        public void Save()
        {
            if (_Datas is null) //若没有改变内容
                return;
            _Dictionary[$"{_Prefix}{Key}"] = string.Join(Separator, Datas);

        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                _Datas = null;
                _Dictionary = null;
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~StringsFromDictionary()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
