using Microsoft.EntityFrameworkCore;
using OW.Game.Store;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GuangYuan.GY001.UserDb
{
    public abstract class GameThingBaseV2 : GameObjectBaseV2, IBeforeSave, IDisposable
    {
        protected GameThingBaseV2()
        {
        }

        protected GameThingBaseV2(Guid id) : base(id)
        {
        }

        /// <summary>
        /// 模板Id。
        /// </summary>
        public Guid TemplateId { get; set; }

        /// <summary>
        /// 记录一些额外的信息，通常这些信息用于排序，加速查找符合特定要求的对象。
        /// </summary>
        [MaxLength(64)]
        public string ExtraString { get; set; }

        /// <summary>
        /// 记录一些额外的信息，用于排序搜索使用的字段。
        /// </summary>
        public decimal? ExtraDecimal { get; set; }

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
                base.Dispose(disposing);
            }
        }

        public override void PrepareSaving(DbContext db)
        {
            base.PrepareSaving(db);
        }
    }
}
