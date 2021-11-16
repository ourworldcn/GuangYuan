using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using OW.Game;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

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
        public GameShoppingManager()
        {
        }

        public GameShoppingManager(IServiceProvider service) : base(service)
        {
        }

        public GameShoppingManager(IServiceProvider service, GameShoppingManagerOptions options) : base(service, options)
        {
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
            IEnumerable<GameShoppingTemplate> coll;
            if (string.IsNullOrWhiteSpace(datas.Genus))
                coll = World.ItemTemplateManager.Id2Shopping.Values.Where(c => IsValid(c, datas.Now));
            else
                coll = World.ItemTemplateManager.Id2Shopping.Values.Where(c => c.Genus == datas.Genus && IsValid(c, datas.Now));
            datas.ShoppingTemplates.AddRange(coll);
        }

        public void Buy(BuyDatas datas)
        {
            
        }

        public void Refresh()
        {

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

    /// <summary>
    /// 购买商品的接口工作数据类。
    /// </summary>
    public class BuyDatas : ComplexWorkDatasBase
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

        /// <summary>
        /// 购买后导致物品变化的数据。
        /// </summary>
        public List<ChangeItem> ChangeItems { get; } = new List<ChangeItem>();
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

    public class ShoppingSlotView
    {
        /// <summary>
        /// 记录曾经买过商品的属性名前缀。"{BuyedPrefix}{shoppingSlot}={日期}"
        /// </summary>
        public const string BuyedPrefix = "buyed";
        public const char Separator = '`';

        private readonly GameItem _ShoppingSlot;
        private readonly VWorld _World;

        public ShoppingSlotView(GameItem shoppingSlot, VWorld world)
        {
            _ShoppingSlot = shoppingSlot;
            _World = world;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="template"></param>
        /// <param name="count"></param>
        /// <param name="now"></param>
        /// <returns>false 原有购买记录，true 新增了购买记录</returns>
        public bool AddItem(GameShoppingTemplate template, decimal count, DateTime now)
        {
            var key = $"{BuyedPrefix}{template.IdString}";
            bool succ = false;
            if (_ShoppingSlot.Properties.ContainsKey(key))
            {
                var ary = _ShoppingSlot.Properties.GetStringOrDefault(key).Split(Separator, StringSplitOptions.RemoveEmptyEntries);
                if (ary.Length == 2 && OwHelper.TryGetDecimal(ary[0], out var oldCount) && OwConvert.TryGetDateTime(ary[1], out var dt))
                {
                    var oldStart = template.GetStart(dt);
                    var start = template.GetStart(now);
                    if (Math.Abs((start - oldStart).Ticks) < 10)  //若两次购买在同一个周期内，计算误差允许范围内
                    {
                        _ShoppingSlot.Properties[key] = $"{count + oldCount}{Separator}{now:s}";
                        succ = true;
                    }
                    //若在新周期内购买
                }
            }
            if (!succ)  //若在新周期内购买
            {
                _ShoppingSlot.Properties[key] = $"{count}{Separator}{now:s}";
            }
            return !succ;
        }

        /// <summary>
        /// 指定周期内指定物品购买数量。
        /// </summary>
        /// <param name="template"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        public decimal GetCount(GameShoppingTemplate template, DateTime now)
        {
            var key = $"{BuyedPrefix}{template.IdString}";
            if (_ShoppingSlot.Properties.ContainsKey(key))
            {
                var ary = _ShoppingSlot.Properties.GetStringOrDefault(key).Split(Separator, StringSplitOptions.RemoveEmptyEntries);
                if (ary.Length == 2 && OwHelper.TryGetDecimal(ary[0], out var oldCount) && OwConvert.TryGetDateTime(ary[1], out var dt))    //若找到数据项
                {
                    var oldStart = template.GetStart(dt);
                    var start = template.GetStart(now);
                    if (Math.Abs((start - oldStart).Ticks) < 10)  //若两次购买在同一个周期内，计算误差允许范围内
                    {
                        return oldCount;
                    }
                    //若在新周期内购买
                }
            }
            return 0;
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
