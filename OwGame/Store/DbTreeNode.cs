using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json.Serialization;

namespace OW.Game.Store
{
    /// <summary>
    /// 
    /// </summary>
    public class DbTreeNodeBase : JsonDynamicPropertyBase, IDisposable
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
                ExtraString = null;
                base.Dispose(disposing);
            }
        }
        #endregion 析构及处置对象相关

        #region 数据库属性

        /// <summary>
        ///记录一些额外的信息，通常这些信息用于排序，加速查找符合特定要求的对象。
        ///常用于记录模板Id或与其它节点的特殊绑定关系，如果没有则是<see cref="Guid.Empty"/>。
        /// </summary>
        /// <remarks><see cref="ExtraGuid"/><see cref="ExtraString"/><see cref="ExtraDecimal"/>三个字段按顺序形成多字段索引以加快搜索速度。
        /// 也创建如下顺序创建索引<see cref="ExtraGuid"/><see cref="ExtraDecimal"/><see cref="ExtraString"/></remarks>
        public Guid ExtraGuid { get; set; }

        /// <summary>
        /// 记录一些额外的信息，通常这些信息用于排序，加速查找符合特定要求的对象。
        /// </summary>
        [MaxLength(64)]
        public string ExtraString { get; set; }

        /// <summary>
        /// 记录一些额外的信息，用于排序搜索使用的字段。
        /// </summary>
        public decimal? ExtraDecimal { get; set; }

        /// <summary>
        /// 时间戳。
        /// </summary>
        [Timestamp]
        [JsonIgnore]
        public byte[] Timestamp { get; set; }

        #endregion 数据库属性
    }

    /// <summary>
    /// 存储于数据库的树状节点。
    /// </summary>
    [Table("TreeNodes")]
    public class DbTreeNode : DbTreeNodeBase, IDisposable
    {
        #region 构造函数

        /// <summary>
        /// 构造函数。
        /// </summary>
        public DbTreeNode()
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="id"></param>
        public DbTreeNode(Guid id) : base(id)
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
                Parent = null;
                _Children = null;
                base.Dispose(disposing);
            }
        }

        #endregion 析构及处置对象相关

        #region 导航属性

        /// <summary>
        /// 所属槽导航属性。
        /// </summary>
        [JsonIgnore]
        public virtual DbTreeNode Parent { get; set; }

        /// <summary>
        /// 所属槽Id。
        /// </summary>
        [ForeignKey(nameof(Parent))]
        public Guid? ParentId { get; set; }

        List<DbTreeNode> _Children;
        /// <summary>
        /// 拥有的子物品或槽。
        /// </summary>
        public virtual List<DbTreeNode> Children { get => _Children ??= new List<DbTreeNode>(); set => _Children = value; }

        #endregion 导航属性
    }

    public interface IDbTreeNode
    {
        /// <summary>
        /// 所属槽导航属性。
        /// </summary>
        [JsonIgnore]
        public DbTreeNode Parent { get; set; }

        /// <summary>
        /// 所属槽Id。
        /// </summary>
        [ForeignKey(nameof(Parent))]
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 拥有的子物品或槽。
        /// </summary>
        public List<DbTreeNode> Children { get; set; }
    }
}
