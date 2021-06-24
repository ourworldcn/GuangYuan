using OwGame;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
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

        Dictionary<string, FastChangingProperty> _GradientProperties;
        /// <summary>
        /// 渐变属性字典。
        /// </summary>
        [NotMapped]
        public IReadOnlyDictionary<string, FastChangingProperty> GradientProperties
        {
            get
            {
                if (null == _GradientProperties)
                {
                    _GradientProperties = new Dictionary<string, FastChangingProperty>()
                    {
                        {
                            "pp",
                            new FastChangingProperty((decimal)Properties.GetValueOrDefault("pp",20m),DateTime.Parse( Properties.GetValueOrDefault("cpp",DateTime.UtcNow.ToString()) as string),
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
                if (_GradientProperties.TryGetValue("pp", out FastChangingProperty p))
                {
                    Properties["pp"] = p.GetCurrentValue(ref dtNow);
                    Properties["cpp"] = p.LastComputerDateTime.ToString();
                }
            }
            PropertiesString = OwHelper.ToPropertiesString(Properties);
            foreach (var item in OwHelper.GetAllSubItemsOfTree(GameItems, c => c.Children).ToArray())
                item.InvokeSaving(EventArgs.Empty);
        }
    }

}
