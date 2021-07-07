using Microsoft.EntityFrameworkCore;
using OwGame;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace GY2021001DAL
{
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

        List<GameItem> _GameItems;

        /// <summary>
        /// 直接拥有的事物。
        /// 通常是一些容器，但也有个别不是。
        /// </summary>
        [NotMapped]
        public List<GameItem> GameItems
        {
            get
            {
                if (null == _GameItems)
                {
                    _GameItems = GameUser.DbContext.Set<GameItem>().Where(c => c.OwnerId == Id).Include(c => c.Children).ThenInclude(c => c.Children).ToList();
                }
                return _GameItems;
            }
            internal set
            {
                _GameItems = value;
            }
        }

        /// <summary>
        /// 获取该物品直接或间接下属对象的枚举数。深度优先。
        /// </summary>
        /// <returns>枚举数。不包含自己。枚举过程中不能更改树节点的关系。</returns>
        [NotMapped]
        public IEnumerable<GameItem> AllChildren
        {
            get
            {
                foreach (var item in GameItems)
                {
                    yield return item;
                    foreach (var item2 in item.AllChildren)
                        yield return item2;
                }
            }
        }

        /// <summary>
        /// 获取该物品直接或间接下属对象的枚举数。广度优先。
        /// </summary>
        /// <returns>枚举数。不包含自己。枚举过程中不能更改树节点的关系。</returns>
        [NotMapped]
        public IEnumerable<GameItem> AllChildrenWithBfs
        {
            get => OwHelper.GetAllSubItemsOfTreeWithBfs(c => c.Children, GameItems.ToArray());
        }

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
                            new FastChangingProperty(TimeSpan.FromSeconds(Convert.ToDouble( Properties.GetValueOrDefault("dpp",300m))),(decimal)Properties.GetValueOrDefault("ipp",1m),
                                (decimal)Properties.GetValueOrDefault("mpp",20m),(decimal)Properties.GetValueOrDefault("pp",20m),DateTime.Parse( Properties.GetValueOrDefault("cpp",DateTime.UtcNow.ToString()) as string))
                            { Name="pp"}
                        },
                    };
                }
                return _GradientProperties;
            }
        }


        private Dictionary<string, GameClientExtendProperty> _ClientExtendProperties = new Dictionary<string, GameClientExtendProperty>();

        /// <summary>
        /// 客户端使用的扩展属性集合，服务器不使用该属性，仅帮助保存和传回。
        /// 键最长64字符，值最长8000字符。（一个中文算一个字符）
        /// </summary>
        [NotMapped]
        public IDictionary<string, GameClientExtendProperty> ClientExtendProperties { get => _ClientExtendProperties; }

        /// <summary>
        /// 在基础数据加载到内存后调用。
        /// </summary>
        public void InvokeLoaded()
        {
            var db = GameUser.DbContext;
            //加载所属物品对象
            _GameItems ??= db.Set<GameItem>().Where(c => c.OwnerId == Id).Include(c => c.Children).ThenInclude(c => c.Children).ThenInclude(c => c.Children).ToList();
            foreach (var item in _GameItems)
            {

            }
            //加载客户端属性
            var coll = db.Set<GameClientExtendProperty>().Where(c => c.ParentId == Id);
            foreach (var item in coll)
            {
                ClientExtendProperties[item.Name] = item;
            }
            foreach (var item in AllChildren)
            {

            }
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
                    Properties["cpp"] = p.LastDateTime.ToString();
                }
            }
            PropertiesString = OwHelper.ToPropertiesString(Properties);
            foreach (var item in OwHelper.GetAllSubItemsOfTree(GameItems, c => c.Children).ToArray())
                item.InvokeSaving(EventArgs.Empty);

        }
    }

}
