using GuangYuan.GY001.TemplateDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using OW.Game.PropertyChange;
using OW.Game.Store;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace OW.Game.Managers
{
    public class VirtualThingManager : GameManagerBase<VirtualThingManagerOptions>
    {
        #region 构造函数

        /// <summary>
        /// 构造函数。
        /// </summary>
        public VirtualThingManager()
        {
            Initializer();
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="service"></param>
        public VirtualThingManager(IServiceProvider service) : base(service)
        {
            Initializer();
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="service"></param>
        /// <param name="options"></param>
        public VirtualThingManager(IServiceProvider service, VirtualThingManagerOptions options) : base(service, options)
        {
            Initializer();
        }

        void Initializer()
        {

        }

        #endregion 构造函数

        #region 析构及处置对象相关

        private volatile bool _IsDisposed;
        /// <summary>
        /// 对象是否已经被处置。
        /// </summary>
        public bool IsDisposed
        {
            get => _IsDisposed;
            protected set => _IsDisposed = value;
        }

        /// <summary>
        /// 实际处置当前对象的方法。
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_IsDisposed)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _IsDisposed = true;
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~SimpleDynamicPropertyBase()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        /// <summary>
        /// 处置对象。
        /// </summary>
        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion 析构及处置对象相关

        /// <summary>
        /// 用属性字典初始化一个对象。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="thing"></param>
        /// <param name="propertyBag"></param>
        public void ThingCreated<T>(IVirtualThing<T> thing, IReadOnlyDictionary<string, object> propertyBag) where T : GuidKeyObjectBase
        {
            var obj = (T)thing;
            GameItemTemplate tt = null;
            if (propertyBag.TryGetValue("tt", out var ttObj) && ttObj is GameItemTemplate)
            {
                tt = (GameItemTemplate)ttObj;
            }
            else if (propertyBag.TryGetGuid("tid", out var tid))
            {
                tt = World.ItemTemplateManager.GetTemplateFromeId(tid);
            }

            var coll = TypeDescriptor.GetProperties(obj).OfType<PropertyDescriptor>();
            var tmpDic = DictionaryPool<string, object>.Shared.Get();
            using var dw = DisposeHelper.Create(c => DictionaryPool<string, object>.Shared.Return(c), tmpDic);

            if (null != tt)
                OwHelper.Copy(tt.Properties, tmpDic);
            OwHelper.Copy(propertyBag, tmpDic);
            var lv = (int)tmpDic.GetDecimalOrDefault(World.PropertyManager.LevelPropertyName, 0m);  //取等级
            foreach (var item in coll)
            {
                if (item.IsReadOnly)
                    continue;
                if (!tmpDic.TryGetValue(item.Name, out var val))
                    continue;
                try
                {
                    if (val is IList list)  //若是一个序列
                        item.SetValue(obj, list[lv]);
                    else
                        item.SetValue(obj, val);
                    tmpDic.Remove(item.Name);
                }
                catch (Exception)
                {
                    Debug.WriteLine($"无法写入属性:要求类型{item.PropertyType.Name},实际类型{val.GetType().Name}");
                }
            }
            var coll1 = from tmp in tmpDic
                        where tmp.Value is IList
                        let list = tmp.Value as IList
                        select (tmp.Key, list[lv]);
            var dic1 = coll1.ToDictionary(c => c.Key, c => c.Item2);
            OwHelper.Copy(dic1, tmpDic);
            thing.JsonObjectString = JsonSerializer.Serialize(tmpDic);
        }

        public void ThingSetProperty<T>(IVirtualThing<T> thing, IEnumerable<(string, object)> propertyBag) where T : GuidKeyObjectBase
        {
            var obj = (T)thing;
            var coll = TypeDescriptor.GetProperties(obj).OfType<PropertyDescriptor>().Join(propertyBag, c => c.Name, c => c.Item1, (l, r) => (Descriptor: l, Value: r.Item2));
            coll.ForEach(c => c.Descriptor.SetValue(obj, c.Value));
        }

        #region 关系操作

        /// <summary>
        /// 
        /// </summary>
        /// <param name="thing"></param>
        /// <param name="parent"></param>
        /// <param name="changes"></param>
        public void Add<T>(IVirtualThing<T> thing, IVirtualThing<T> parent, ICollection<GamePropertyChangeItem<object>> changes = null) where T : GuidKeyObjectBase
        {
            parent.Children.Add((T)thing);
            thing.ParentId = parent.Id;
            thing.Parent = (T)parent;

            changes?.Add(new GamePropertyChangeItem<object>()
            {
                Object = parent,
                HasOldValue = false,
                HasNewValue = true,
                NewValue = thing,
                PropertyName = nameof(parent.Children),
            });
        }

        public void AddLeaf(VirtualThing node, VirtualThing parent, ICollection<GamePropertyChangeItem<object>> changes = null)
        {

        }

        public void RemoveLeaf(VirtualThing node, ICollection<GamePropertyChangeItem<object>> changes = null)
        {

        }

        public void MoveLeafNode(VirtualThing node, ICollection<GamePropertyChangeItem<object>> changes = null)
        {

        }

        public void RemoveLeafNode(VirtualThing node, GamePropertyChangeItem<object> changes = null)
        {

        }
        #endregion 关系操作
    }

    public class VirtualThingManagerOptions
    {
    }
}
