using GuangYuan.GY001.TemplateDb;
using Microsoft.EntityFrameworkCore;
using OW.Game.PropertyChange;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace GuangYuan.GY001.UserDb
{
    /// <summary>
    /// 游戏中虚拟事物的类，不随用户数据一起加载到内存中的数据放在这个类中。如邮件、工会、战报等。
    /// </summary>
    [Table("SeparateThings")]
    public class SeparateThing : GameThingBaseV2, IDisposable
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public SeparateThing()
        {

        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="id">指定Id。</param>
        public SeparateThing(Guid id) : base(id)
        {

        }

        /// <summary>
        /// 乐观锁的并发令牌。
        /// </summary>
        [Timestamp]
        public byte[] Timestamp { get; set; }

        #region 导航属性

        /// <summary>
        /// 所属槽导航属性。
        /// </summary>
        [JsonIgnore]    //Json序列化时由父到子
        public virtual SeparateThing Parent { get; set; }

        /// <summary>
        /// 所属槽Id。
        /// </summary>
        [ForeignKey(nameof(Parent))]
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 拥有的子物品或槽。
        /// </summary>
        public virtual List<SeparateThing> Children { get; set; } = new List<SeparateThing>();

        /// <summary>
        /// 所属事物的Id或其他关联对象的Id。
        /// </summary>
        public Guid? OwnerId { get; set; }

        #endregion 导航属性

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        //public override string ToString()
        //{
        //    var result = this.GetTemplate()?.DisplayName ?? this.GetTemplate()?.Remark;
        //    if (string.IsNullOrWhiteSpace(result))
        //    {
        //        return base.ToString();
        //    }

        //    return $"{{{result},{}}}";
        //}

        #region IDisposable接口相关

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                    Children.ForEach(c => c.Dispose());
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                Parent = null;
                base.Dispose(disposing);
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~GameItemBase()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        #endregion IDisposable接口相关

    }

    /// <summary>
    /// 封装一些与<see cref="GameItem"/>相关的扩展方法。
    /// </summary>
    public static class SeparateThingExtensions
    {
        ///// <summary>
        ///// 获取模板对象。
        ///// </summary>
        ///// <param name="gItem"></param>
        ///// <returns></returns>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static GameItemTemplate GetTemplate(this SeparateThing gItem) =>
        //    ((GameThingBase)gItem).GetTemplate() as GameItemTemplate;

        ///// <summary>
        ///// 设置模板对象。
        ///// </summary>
        ///// <param name="thing"></param>
        ///// <param name="template"></param>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static void SetTemplate(this SeparateThing gItem, GameItemTemplate template) =>
        //    ((GameThingBase)gItem).SetTemplate(template);

        public static IEnumerable<SeparateThing> GetAllChildren(this SeparateThing gameItem)
        {
            foreach (var item in gameItem.Children)
            {
                yield return item;
                foreach (var item2 in item.GetAllChildren())
                {
                    yield return item2;
                }
            }
        }

    }

}
