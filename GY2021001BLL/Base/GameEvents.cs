using GuangYuan.GY001.BLL;
using GuangYuan.GY001.UserDb;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace OW.Game
{
    public class GameEventsManagerOptions
    {

    }

    public class SimplePropertyChangedItem<T>
    {
        public SimplePropertyChangedItem()
        {

        }

        public SimplePropertyChangedItem(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 构造函数。
        /// 无论<paramref name="newValue"/>和<paramref name="oldValue"/>给定任何值，<see cref="HasOldValue"/>和<see cref="HasNewValue"/>都设置为true。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        public SimplePropertyChangedItem(string name, T oldValue, T newValue)
        {
            Name = name;
            OldValue = oldValue;
            NewValue = newValue;
            HasOldValue = HasNewValue = true;
        }

        /// <summary>
        /// 属性的名字。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 指示<see cref="OldValue"/>中的值是否有意义。
        /// </summary>
        public bool HasOldValue { get; set; }

        /// <summary>
        /// 就值。
        /// </summary>
        public T OldValue { get; set; }

        /// <summary>
        /// 指示<see cref="NewValue"/>中的值是否有意义。
        /// </summary>
        public bool HasNewValue { get; set; }

        /// <summary>
        /// 新值。
        /// </summary>
        public T NewValue { get; set; }
    }

    public class SimplePropertyChangedCollection : ICollection<SimplePropertyChangedItem<object>>
    {
        public SimplePropertyChangedCollection()
        {

        }

        public GameThingBase Thing { get; set; }

        public List<SimplePropertyChangedItem<object>> Items { get; } = new List<SimplePropertyChangedItem<object>>();

        public object Tag { get; set; }

        #region ICollection<SimplePropertyChangedItem<object>>接口

        public int Count => ((ICollection<SimplePropertyChangedItem<object>>)Items).Count;

        public bool IsReadOnly => ((ICollection<SimplePropertyChangedItem<object>>)Items).IsReadOnly;

        public void Add(SimplePropertyChangedItem<object> item)
        {
            ((ICollection<SimplePropertyChangedItem<object>>)Items).Add(item);
        }

        public void Clear()
        {
            ((ICollection<SimplePropertyChangedItem<object>>)Items).Clear();
        }

        public bool Contains(SimplePropertyChangedItem<object> item)
        {
            return ((ICollection<SimplePropertyChangedItem<object>>)Items).Contains(item);
        }

        public void CopyTo(SimplePropertyChangedItem<object>[] array, int arrayIndex)
        {
            ((ICollection<SimplePropertyChangedItem<object>>)Items).CopyTo(array, arrayIndex);
        }

        public IEnumerator<SimplePropertyChangedItem<object>> GetEnumerator()
        {
            return ((IEnumerable<SimplePropertyChangedItem<object>>)Items).GetEnumerator();
        }

        public bool Remove(SimplePropertyChangedItem<object> item)
        {
            return ((ICollection<SimplePropertyChangedItem<object>>)Items).Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Items).GetEnumerator();
        }
        #endregion ICollection<SimplePropertyChangedItem<object>>接口

    }

    public class DynamicPropertyChangedCollection : ICollection<SimplePropertyChangedCollection>
    {
        public DynamicPropertyChangedCollection()
        {

        }

        public List<SimplePropertyChangedCollection> Items { get; } = new List<SimplePropertyChangedCollection>();

        #region ICollection<DynamicPropertyChangedItem>接口

        public int Count => ((ICollection<SimplePropertyChangedCollection>)Items).Count;

        public bool IsReadOnly => ((ICollection<SimplePropertyChangedCollection>)Items).IsReadOnly;

        public void Add(SimplePropertyChangedCollection item)
        {
            var changedItem = GetOrAddItem(item.Thing);
            changedItem.Items.AddRange(item.Items);
        }

        public void Clear()
        {
            ((ICollection<SimplePropertyChangedCollection>)Items).Clear();
        }

        public bool Contains(SimplePropertyChangedCollection item)
        {
            return ((ICollection<SimplePropertyChangedCollection>)Items).Contains(item);
        }

        public void CopyTo(SimplePropertyChangedCollection[] array, int arrayIndex)
        {
            ((ICollection<SimplePropertyChangedCollection>)Items).CopyTo(array, arrayIndex);
        }

        public IEnumerator<SimplePropertyChangedCollection> GetEnumerator()
        {
            return ((IEnumerable<SimplePropertyChangedCollection>)Items).GetEnumerator();
        }

        public bool Remove(SimplePropertyChangedCollection item)
        {
            return ((ICollection<SimplePropertyChangedCollection>)Items).Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Items).GetEnumerator();
        }
        #endregion ICollection<DynamicPropertyChangedItem>接口

        /// <summary>
        /// 获取或添加变化项。
        /// </summary>
        /// <param name="thing"></param>
        /// <returns></returns>
        public SimplePropertyChangedCollection GetOrAddItem(GameThingBase thing) =>
             Items.FirstOrDefault(c => c.Thing.Id == thing.Id) ?? new SimplePropertyChangedCollection() { Thing = thing };

        /// <summary>
        /// 记录指定的动态属性的旧值。
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="keyName"></param>
        public void Mark(GameThingBase thing, string keyName)
        {
            var item = GetOrAddItem(thing);
            var hasOld = thing.Properties.TryGetValue(keyName, out var oldVal);
            var result = new SimplePropertyChangedItem<object>(keyName, oldVal, default) { HasOldValue = hasOld };
            item.Add(result);
        }

        /// <summary>
        /// 记录属性当前值，并设置为新值，记录这一更改。
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="keyName"></param>
        /// <param name="newValue">无论设置任何值，总会将<see cref="SimplePropertyChangedItem{T}.HasNewValue"/>设置为true。</param>
        public void MarkAndSet(GameThingBase thing, string keyName, object newValue)
        {
            var item = GetOrAddItem(thing);
            var hasOld = thing.Properties.TryGetValue(keyName, out var oldVal);
            var result = new SimplePropertyChangedItem<object>(keyName, oldVal, newValue) { HasOldValue = hasOld, HasNewValue = true, };
            item.Add(result);
            thing.Properties[keyName] = newValue;
        }

        /// <summary>
        /// 记录原始值后删除属性。
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="keyName"></param>
        /// <returns></returns>
        public bool MarkAndRemove(GameThingBase thing, string keyName)
        {
            var item = GetOrAddItem(thing);
            var hasOld = thing.Properties.Remove(keyName, out var oldVal);
            var result = new SimplePropertyChangedItem<object>(keyName) { OldValue = hasOld, HasOldValue = hasOld, HasNewValue = false, };
            item.Add(result);
            return hasOld;
        }
    }

    /// <summary>
    /// 事件服务。
    /// </summary>
    public class GameEventsManager : GameManagerBase<GameEventsManagerOptions>
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameEventsManager()
        {
            Initialize();
        }

        public GameEventsManager(IServiceProvider service) : base(service)
        {
            Initialize();
        }

        public GameEventsManager(IServiceProvider service, GameEventsManagerOptions options) : base(service, options)
        {
            Initialize();
        }

        private void Initialize()
        {
        }

        public virtual void OnDynamicPropertyChanged(DynamicPropertyChangedCollection args)
        {
            var lvName = World.PropertyManager.LevelPropertyName;
            foreach (var item in args)
            {
                foreach (var subItem in item)
                {
                    if (subItem.Name == lvName)
                    {
                        OnLvChanged(item.Thing, (int)Convert.ToDecimal(subItem.NewValue));
                        break;
                    }
                }
            }
        }

        public virtual bool OnLvChanged(GameThingBase gameThing, int oldLv)
        {
            gameThing.RemoveFastChangingProperty(ProjectConstant.UpgradeTimeName);
            //扫描成就
            if (!(gameThing is GameChar gc))
                gc = (gameThing as GameItem)?.GameChar;
            if (null != gc)
                World.MissionManager.ScanAsync(gc);
            return false;
        }

        /// <summary>
        /// 物品/道具添加到容器后被调用。
        /// </summary>
        /// <param name="gameItems">添加的对象，元素中的关系属性已经被正常设置。</param>
        /// <param name="parameters">参数。</param>
        public virtual void OnGameItemAdd(IEnumerable<GameItem> gameItems, Dictionary<string, object> parameters)
        {

        }
    }

    public static class GameEventsManagerExtensions
    {
    }

    public class Gy001GameEventsManagerOptions : GameEventsManagerOptions
    {

    }

    /// <summary>
    /// 全局事件管理器。
    /// </summary>
    public class Gy001GameEventsManager : GameEventsManager
    {

        public Gy001GameEventsManager()
        {
        }

        public Gy001GameEventsManager(IServiceProvider service) : base(service)
        {
        }

        public Gy001GameEventsManager(IServiceProvider service, GameEventsManagerOptions options) : base(service, options)
        {
        }

        public override void OnDynamicPropertyChanged(DynamicPropertyChangedCollection args)
        {
            base.OnDynamicPropertyChanged(args);
            var mrs = args.Where(c => c.Thing.TemplateId == ProjectConstant.MainControlRoomSlotId && c.Thing is GameItem); //主控室
            foreach (var item in mrs)
            {
                foreach (var sunItem in item.Items.Where(c => c.Name == ProjectConstant.LevelPropertyName)) //若主控室升级了
                {
                    var gi = item.Thing as GameItem;
                    var newLv = gi.Properties.GetDecimalOrDefault(ProjectConstant.LevelPropertyName);
                    var oldLv = Convert.ToDecimal(sunItem.OldValue);
                    if (newLv == oldLv + 1)
                    {

                    }
                    break;
                }
            }
        }

        public override void OnGameItemAdd(IEnumerable<GameItem> gameItems, Dictionary<string, object> parameters)
        {
            base.OnGameItemAdd(gameItems, parameters);
        }
    }

}
