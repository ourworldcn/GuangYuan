using OW.Game.Store;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GuangYuan.GY001.TemplateDb
{
    /// <summary>
    /// 卡池数据模板。
    /// </summary>
    public class GameCardPoolTemplate : GameTemplateBase
    {
        /// <summary>
        /// 卡池标识。
        /// </summary>
        public string CardPoolGroupString { get; set; }

        /// <summary>
        /// 奖池标识。
        /// </summary>
        public string SubCardPoolString { get; set; }

        /// <summary>
        /// 是否自动使用获得的物品。
        /// </summary>
        public bool AutoUse { get; set; }

        /// <summary>
        /// 起始日期，此日期及之后此物品才会出现在卡池内。
        /// </summary>
        public DateTime StartDateTime { get; set; }

        /// <summary>
        /// 终止日期，此日期及之前此物品才会出现在卡池内
        /// </summary>
        public DateTime EndDateTime { get; set; }

        /// <summary>
        /// 周期。s秒,d天,w周,m月,y年。不填写则表示无周期(唯一周期)。
        /// </summary>
        [MaxLength(64)]
        public string SellPeriod { get; set; }

        /// <summary>
        /// 周期开始后持续有效时间,s秒,d天,w周,m月,y年。仅在有效期内才出售，不填则是永久有效（在起止期间和周期的约束下）
        /// </summary>
        [MaxLength(64)]
        public string ValidPeriod { get; set; }

        /// <summary>
        /// 销售周期的单位的标量数值。
        /// </summary>
        [NotMapped]
        public decimal SellPeriodValue => !string.IsNullOrWhiteSpace(SellPeriod) && decimal.TryParse(SellPeriod[0..^1], out var val) ? val : -1;

        /// <summary>
        /// 销售周期的单位字符(小写)。n表示无限。
        /// </summary>
        [NotMapped]
        public char SellPeriodUnit => string.IsNullOrWhiteSpace(SellPeriod) ? 'n' : char.ToLower(SellPeriod[^1]);

        [NotMapped]
        public char ValidPeriodUnit => string.IsNullOrWhiteSpace(ValidPeriod) ? 'n' : char.ToLower(ValidPeriod[^1]);

        [NotMapped]
        public decimal ValidPeriodValue => !string.IsNullOrWhiteSpace(ValidPeriod) && decimal.TryParse(ValidPeriod[0..^1], out var val) ? val : -1;

    }

    public static class GameCardPoolTemplateExtensions
    {
        /// <summary>
        /// 获取最近开始周期起始时间。
        /// </summary>
        /// <param name="template"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        public static DateTime GetStart(this GameCardPoolTemplate template, DateTime now)
        {
            DateTime start; //最近一个周期的开始时间
            DateTime templateStart = template.StartDateTime;
            var val = template.SellPeriodValue;
            switch (template.SellPeriodUnit)
            {
                case 'n':   //无限
                    start = templateStart;
                    break;
                case 's':
                    var times = (now - templateStart).Ticks / TimeSpan.FromSeconds((double)val).Ticks;  //相隔秒数
                    start = templateStart.AddTicks(times * TimeSpan.FromSeconds((double)val).Ticks);
                    break;
                case 'd':   //日周期
                    times = (now - templateStart).Ticks / TimeSpan.FromDays((double)val).Ticks;  //相隔日数
                    start = templateStart.AddTicks(times * TimeSpan.FromDays((double)val).Ticks);
                    break;
                case 'w':   //周周期
                    times = (now - templateStart).Ticks / TimeSpan.FromDays(7 * (double)val).Ticks;  //相隔周数
                    start = templateStart.AddTicks(TimeSpan.FromDays(7 * (double)val).Ticks * times);
                    break;
                case 'm':   //月周期
                    DateTime tmp;
                    for (tmp = templateStart; tmp <= now; tmp = tmp.AddMonths(((int)val)))
                    {
                    }
                    start = tmp.AddMonths(-((int)val));
                    break;
                case 'y':   //年周期
                    for (tmp = templateStart; tmp <= now; tmp = tmp.AddYears(((int)val)))
                    {
                    }
                    start = tmp.AddYears(-(int)val);
                    break;
                default:
                    throw new InvalidOperationException("无效的周期表示符。");
            }
            return start;

        }

        /// <summary>
        /// 获取指定时间点所处周期的结束时间点。
        /// </summary>
        /// <param name="template"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        public static DateTime GetEnd(this GameCardPoolTemplate template, DateTime now)
        {
            DateTime result; //最近一个周期的开始时间
            var val = template.ValidPeriodValue;
            var start = GetStart(template, now);    //周期开始时间
            switch (template.ValidPeriodUnit)
            {
                case 'n':   //无限
                    result = DateTime.MaxValue;
                    break;
                case 's':
                    result = start + TimeSpan.FromSeconds((double)val);
                    break;
                case 'd':   //日周期
                    result = start + TimeSpan.FromDays((double)val);
                    break;
                case 'w':   //周周期
                    result = start + TimeSpan.FromDays((double)val * 7);
                    break;
                case 'm':   //月周期
                    result = start.AddMonths((int)val);
                    break;
                case 'y':   //年周期
                    result = start.AddYears((int)val);
                    break;
                default:
                    throw new InvalidOperationException("无效的周期表示符。");
            }
            return result;
        }
    }

    /// <summary>
    /// 商品表数据。
    /// </summary>
    public class GameShoppingTemplate : GameTemplateBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameShoppingTemplate()
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="id"></param>
        public GameShoppingTemplate(Guid id) : base(id)
        {
        }

        /// <summary>
        /// 最长64个字符的字符串，用于标志一组商品，服务器不理解其具体意义。
        /// </summary>
        [MaxLength(64)]
        public string Genus { get; set; }

        /// <summary>
        /// 同页签同组号的物品一同出现/消失。用于随机商店.刷新逻辑用代码实现。非随机刷商品可以不填写。
        /// </summary>
        public int? GroupNumber { get; set; }

        /// <summary>
        /// 物品模板Id。
        /// </summary>
        public Guid? ItemTemplateId { get; set; }

        /// <summary>
        /// 是否自动使用。仅对可使用物品有效。
        /// </summary>
        public bool AutoUse { get; set; }

        /// <summary>
        /// 首次销售日期
        /// </summary>
        public DateTime StartDateTime { get; set; }

        /// <summary>
        /// 多长周期销售一次。d天,w周,m月,y年。不填写则表示无周期(唯一周期)。
        /// </summary>
        [MaxLength(64)]
        public string SellPeriod { get; set; }

        /// <summary>
        /// 销售周期的单位字符(小写)。n表示无限。
        /// </summary>
        [NotMapped]
        public char SellPeriodUnit => string.IsNullOrWhiteSpace(SellPeriod) ? 'n' : char.ToLower(SellPeriod[^1]);

        /// <summary>
        /// 销售周期的单位的标量数值。
        /// </summary>
        [NotMapped]
        public decimal SellPeriodValue => !string.IsNullOrWhiteSpace(SellPeriod) && decimal.TryParse(SellPeriod[0..^1], out var val) ? val : -1;

        /// <summary>
        /// 销售的最大数量。-1表示不限制。
        /// </summary>
        public decimal MaxCount { get; set; }

        /// <summary>
        /// 销售一次持续时间,d天,w周,m月,y年。仅在有效期内才出售，不填则是永久有效
        /// </summary>
        [MaxLength(64)]
        public string ValidPeriod { get; set; }

        [NotMapped]
        public char ValidPeriodUnit => string.IsNullOrWhiteSpace(ValidPeriod) ? 'n' : char.ToLower(ValidPeriod[^1]);

        [NotMapped]
        public decimal ValidPeriodValue => !string.IsNullOrWhiteSpace(ValidPeriod) && decimal.TryParse(ValidPeriod[0..^1], out var val) ? val : -1;
    }

    public static class GameShoppingTemplateExtensions
    {
        /// <summary>
        /// 获取最近开始周期第一天。
        /// </summary>
        /// <param name="template"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        public static DateTime GetStart(this GameShoppingTemplate template, DateTime now)
        {
            DateTime start; //最近一个周期的开始时间
            DateTime templateStart = template.StartDateTime;
            var val = template.SellPeriodValue;
            switch (template.SellPeriodUnit)
            {
                case 'n':   //无限
                    start = templateStart;
                    break;
                case 's':
                    var times = (now - templateStart).Ticks / TimeSpan.FromSeconds((double)val).Ticks;  //相隔秒数
                    start = templateStart.AddTicks(times * TimeSpan.FromSeconds((double)val).Ticks);
                    break;
                case 'd':   //日周期
                    times = (now - templateStart).Ticks / TimeSpan.FromDays((double)val).Ticks;  //相隔日数
                    start = templateStart.AddTicks(times * TimeSpan.FromDays((double)val).Ticks);
                    break;
                case 'w':   //周周期
                    times = (now - templateStart).Ticks / TimeSpan.FromDays(7 * (double)val).Ticks;  //相隔周数
                    start = templateStart.AddTicks(TimeSpan.FromDays(7 * (double)val).Ticks * times);
                    break;
                case 'm':   //月周期
                    DateTime tmp;
                    for (tmp = templateStart; tmp <= now; tmp = tmp.AddMonths(((int)val)))
                    {
                    }
                    start = tmp.AddMonths(-((int)val));
                    break;
                case 'y':   //年周期
                    for (tmp = templateStart; tmp <= now; tmp = tmp.AddYears(((int)val)))
                    {
                    }
                    start = tmp.AddYears(-(int)val);
                    break;
                default:
                    throw new InvalidOperationException("无效的周期表示符。");
            }
            return start;

        }

        /// <summary>
        /// 获取指定时间点所处周期的结束时间点。
        /// </summary>
        /// <param name="template"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        public static DateTime GetEnd(this GameShoppingTemplate template, DateTime now)
        {
            DateTime result; //最近一个周期的开始时间
            var val = template.ValidPeriodValue;
            var start = GetStart(template, now);    //周期开始时间
            switch (template.ValidPeriodUnit)
            {
                case 'n':   //无限
                    result = DateTime.MaxValue;
                    break;
                case 's':
                    result = start + TimeSpan.FromSeconds((double)val);
                    break;
                case 'd':   //日周期
                    result = start + TimeSpan.FromDays((double)val);
                    break;
                case 'w':   //周周期
                    result = start + TimeSpan.FromDays((double)val * 7);
                    break;
                case 'm':   //月周期
                    result = start.AddMonths((int)val);
                    break;
                case 'y':   //年周期
                    result = start.AddYears((int)val);
                    break;
                default:
                    throw new InvalidOperationException("无效的周期表示符。");
            }
            return result;
        }
    }
}
