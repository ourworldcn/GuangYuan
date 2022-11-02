using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;

namespace OW.Game.Store
{
    /// <summary>
    /// 为可快速查找的数据库类提供基础接口。
    /// </summary>
    public interface IDbQuickFind
    {
        /// <summary>
        ///记录一些额外的信息，通常这些信息用于排序，加速查找符合特定要求的对象。
        ///常用于记录模板Id或与其它节点的特殊绑定关系，如果没有则是<see cref="Guid.Empty"/>。
        /// </summary>
        /// <remarks><see cref="ExtraGuid"/><see cref="ExtraString"/><see cref="ExtraDecimal"/>三个字段按顺序形成多字段索引以加快搜索速度。
        /// 也创建如下顺序创建索引<see cref="ExtraGuid"/><see cref="ExtraDecimal"/><see cref="ExtraString"/></remarks>
        Guid ExtraGuid { get; set; }

        [MaxLength(64)]
        string ExtraString { get; set; }

        decimal? ExtraDecimal { get; set; }

    }

    /// <summary>
    /// 存储在数据库中树状节点的基础接口。
    /// </summary>
    /// <typeparam name="TNode">节点类型。</typeparam>
    public interface IDbTreeNode<TNode> where TNode : IEntityWithSingleKey<Guid>
    {
        /// <summary>
        /// 所属槽导航属性。
        /// </summary>
        [JsonIgnore]
        [MaybeNull]
        public abstract TNode Parent { get; set; }

        /// <summary>
        /// 所属槽Id。
        /// </summary>
        [ForeignKey(nameof(Parent))]
        public abstract Guid? ParentId { get; set; }

        /// <summary>
        /// 拥有的子物品或槽。
        /// </summary>
        public abstract List<TNode> Children { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class DbTreeNodeBase<T> : JsonDynamicPropertyBase, IDisposable, IDbQuickFind, IDbTreeNode<T> where T : IEntityWithSingleKey<Guid>
    {
        #region 构造函数

        /// <summary>
        /// 构造函数。
        /// </summary>
        public DbTreeNodeBase()
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="id"></param>
        public DbTreeNodeBase(Guid id) : base(id)
        {
        }

        #endregion 构造函数

        #region 析构及处置对象相关

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _ExtraString = null;
                _Parent = default;
                _Children = null;
                base.Dispose(disposing);
            }
        }
        #endregion 析构及处置对象相关

        #region 数据库属性

        /// <summary>
        ///<inheritdoc/>
        /// </summary>
        public Guid ExtraGuid { get; set; }

        private string _ExtraString;
        /// <summary>
        /// 记录一些额外的信息，通常这些信息用于排序，加速查找符合特定要求的对象。
        /// </summary>
        [MaxLength(64)]
        public string ExtraString { get => _ExtraString; set => _ExtraString = value; }

        /// <summary>
        /// 记录一些额外的信息，用于排序搜索使用的字段。
        /// </summary>
        public decimal? ExtraDecimal { get; set; }

        #endregion 数据库属性

        #region 导航属性

        private T _Parent;
        /// <summary>
        /// 所属槽导航属性。
        /// </summary>
        [JsonIgnore]
        public virtual T Parent { get => _Parent; set => _Parent = value; }

        /// <summary>
        /// 所属槽Id。
        /// </summary>
        [ForeignKey(nameof(Parent))]
        public Guid? ParentId { get; set; }


        List<T> _Children;
        /// <summary>
        /// 拥有的子物品或槽。
        /// </summary>
        public virtual List<T> Children { get => _Children ??= new List<T>(); set => _Children = value; }

        #endregion 导航属性
    }
}
