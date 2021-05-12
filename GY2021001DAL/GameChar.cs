using OwGame;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace GY2021001DAL
{
    [Table(nameof(GameChar))]
    public class GameChar : GameThingBase
    {

        public GameChar()
        {
            Id = Guid.NewGuid();
        }

        public GameChar(Guid id)
        {
            Id = id;
        }

        /// <summary>
        /// 构造函数。用于延迟加载。
        /// </summary>
        /// <param name="lazyLoader">延迟加载器。</param>
        //private GameChar(Action<object, string> lazyLoader)
        //{
        //    LazyLoader = lazyLoader;
        //}

        //public Action<object,string> LazyLoader { get; set; }

        /// <summary>
        /// 一个角色初始创建时被调用。
        /// 通常这里预制一些道具，装备。
        /// </summary>
        public void InitialCreation()
        {

        }

        //[Key, ForeignKey(nameof(GameUser))]
        //public new Guid Id { get => base.Id; set => base.Id = value; }

        /// <summary>
        /// 直接拥有的事物。
        /// </summary>
        [NotMapped]
        public List<GameItem> GameItems { get; } = new List<GameItem>();


        /// <summary>
        /// 所属用户Id。
        /// </summary>
        [ForeignKey(nameof(GameUser))]
        public Guid GameUserId { get; set; }

        /// <summary>
        /// 所属用户的导航属性。
        /// </summary>
        public virtual GameUser GameUser { get; set; }

        /// <summary>
        /// 角色显示用的名字。
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 用户所处地图区域的Id,这也可能是战斗关卡的Id。如果没有在战斗场景中，则可能是空。
        /// </summary>
        public Guid? CurrentDungeonId { get; set; }

        /// <summary>
        /// 进入战斗场景的时间。注意是Utc时间。如果没有在战斗场景中，则可能是空。
        /// </summary>
        public DateTime? CombatStartUtc { get; set; }

        Dictionary<string, GradientProperty> _GradientProperties;
        /// <summary>
        /// 渐变属性字典。
        /// </summary>
        [NotMapped]
        public IReadOnlyDictionary<string, GradientProperty> GradientProperties
        {
            get
            {
                if (null == _GradientProperties)
                {
                    _GradientProperties = new Dictionary<string, GradientProperty>()
                    {
                        {
                            "pp",
                            new GradientProperty((decimal)Properties.GetValueOrDefault("pp",20m),DateTime.Parse( Properties.GetValueOrDefault("cpp",DateTime.UtcNow.ToString()) as string),
                                TimeSpan.FromSeconds(Convert.ToDouble( Properties.GetValueOrDefault("dpp",300m))),(decimal)Properties.GetValueOrDefault("ipp",1m),(decimal)Properties.GetValueOrDefault("mpp",20m))
                            { Tag="pp"}
                        },
                    };
                }
                return _GradientProperties;
            }
        }


        private Dictionary<string, GameExtendProperty> _ClientExtendProperties = new Dictionary<string, GameExtendProperty>();

        /// <summary>
        /// 客户端使用的扩展属性集合，服务器不使用该属性，仅帮助保存和传回。
        /// 键最长64字符，值最长8000字符。（一个中文算一个字符）
        /// </summary>
        [NotMapped]
        public IDictionary<string, GameExtendProperty> ClientExtendProperties { get => _ClientExtendProperties; }

        /// <summary>
        /// 在基础数据加载到内存后调用。
        /// </summary>
        public void InvokeLoaded()
        {

        }

        /// <summary>
        /// 在保存数据前被调用。
        /// </summary>
        public void InvokeSaving()
        {
            if (null != _GradientProperties)   //若已经生成了渐变属性
            {
                DateTime dtNow = DateTime.UtcNow;
                if (_GradientProperties.TryGetValue("pp", out GradientProperty p))
                {
                    Properties["pp"] = p.GetCurrentValue(ref dtNow);
                    Properties["cpp"] = p.LastComputerDateTime.ToString();
                }
            }
            PropertiesString = OwHelper.ToPropertiesString(Properties);
        }
    }

    /// <summary>
    /// 渐变属性封装类。
    /// </summary>
    [NotMapped]
    public class GradientProperty
    {
        /// <summary>
        /// 构造函数、
        /// </summary>
        /// <param name="oriVal">当前值。</param>
        /// <param name="now">时间。建议一律采用Utc时间。</param>
        /// <param name="delay">计算间隔。</param>
        /// <param name="increment">增量。</param>
        /// <param name="maxVal">最大值。不会超过此值。</param>
        public GradientProperty(decimal oriVal, DateTime now, TimeSpan delay, decimal increment, decimal maxVal)
        {
            LastValue = oriVal;
            LastComputerDateTime = now;
            Delay = delay;
            Increment = increment;
            MaxValue = maxVal;
        }

        public decimal MaxValue { get; set; }

        /// <summary>
        /// 获取或设置最后计算的时间。建议一律采用Utc时间。
        /// </summary>
        public DateTime LastComputerDateTime { get; set; }

        /// <summary>
        /// 获取或设置最后计算的结果。
        /// </summary>
        public decimal LastValue { get; set; }

        /// <summary>
        /// 多久计算一次。
        /// </summary>
        public TimeSpan Delay { get; set; }

        /// <summary>
        /// 增量。
        /// </summary>
        public decimal Increment { get; set; }

        /// <summary>
        /// 获取当前值。自动修改LastComputerDateTime和LastValue属性。
        /// </summary>
        /// <param name="now">当前时间。返回时可能更改，如果没有正好到跳变时间，则会略微提前到上一次跳变的时间点。</param>
        /// <returns>当前值。</returns>
        public decimal GetCurrentValue(ref DateTime now)
        {
            var count = Math.DivRem((now - LastComputerDateTime).Ticks, Delay.Ticks, out long remainder);  //跳变次数 和 余数
            var val = Math.Min(count * Increment + LastValue, MaxValue);
            LastValue = val; //计算得到最后值
            now = now - TimeSpan.FromTicks(remainder);
            LastComputerDateTime = now;
            return LastValue;
        }

        /// <summary>
        /// 使用当前Utc时间获取当前值。
        /// </summary>
        /// <returns></returns>
        public decimal GetCurrentValueWithUtc()
        {
            DateTime now = DateTime.UtcNow;
            return GetCurrentValue(ref now);
        }

        /// <summary>
        /// 一个记录额外信息的属性。本类成员不使用该属性。
        /// </summary>
        public object Tag { get; set; }
    }
}
