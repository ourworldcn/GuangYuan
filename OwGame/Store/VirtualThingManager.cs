﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OW.Game.PropertyChange;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Text;

namespace OW.Game.Managers
{
    public class VirtualThingManager : IDisposable
    {
        #region 构造函数

        /// <summary>
        /// 构造函数。
        /// </summary>
        public VirtualThingManager()
        {
        }

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

        #endregion 构造函数

        #region 关系操作

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="parent"></param>
        /// <param name="changes"></param>
        public void Add(VirtualThing node, VirtualThing parent, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            parent.Children.Add(node);
            node.ParentId = parent.Id;
            node.Parent = parent;

            changes?.Add(new GamePropertyChangeItem<object>()
            {
                Object = parent,
                HasOldValue = false,
                HasNewValue=true,
                NewValue=node,
                PropertyName=nameof(parent.Children),
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
}
