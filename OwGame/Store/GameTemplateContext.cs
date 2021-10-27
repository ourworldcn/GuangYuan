using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace OW.Game.Store
{
    /// <summary>
    /// 游戏模板存储上下文。
    /// </summary>
    public class GameTemplateContext : DbContext
    {
        public GameTemplateContext([NotNull] DbContextOptions options) : base(options)
        {
        }

        protected GameTemplateContext()
        {
        }
    }

    /// <summary>
    /// 所有游戏模板类的基类。
    /// </summary>
    public abstract class GameTemplateBase : SimpleDynamicPropertyBase
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public GameTemplateBase()
        {

        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="id"><inheritdoc/></param>
        public GameTemplateBase(Guid id) : base(id)
        {

        }

        string _IdString;

        /// <summary>
        /// Id的字符串形式"00000000-0000-0000-0000-000000000000"从 a 到 f 的十六进制数字是小写的。
        /// 大量频繁使用模板对象时，使用该属性避免分配新字符串。
        /// </summary>
        [NotMapped]
        public string IdString => _IdString ??= Id.ToString("D");

        /// <summary>
        /// 服务器不是用该属性。仅用于人读备注。
        /// </summary>
        [Column("备注", Order = 90)]
        public string Remark { get; set; }

        private IServiceProvider _Service;

        [NotMapped]
        public IServiceProvider Service => _Service;

        public void SetService(IServiceProvider service)
        {
            _Service = service;
        }

    }

}
