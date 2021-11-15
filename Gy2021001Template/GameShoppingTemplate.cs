using OW.Game.Store;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GuangYuan.GY001.TemplateDb
{
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
        public Guid ItemTemplateId { get; set; }

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
        public char ValidPeriodUnit => string.IsNullOrWhiteSpace(ValidPeriod) ? 'n' : char.ToLower(SellPeriod[^1]);

        [NotMapped]
        public decimal ValidPeriodValue => !string.IsNullOrWhiteSpace(ValidPeriod) && decimal.TryParse(ValidPeriod[0..^1], out var val) ? val : -1;
    }
}
