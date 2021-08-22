﻿
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace OW.Game.Store
{
    public abstract class OrmObjectBase<TKey>
    {
        private TKey _Id;

        /// <summary>
        /// 构造函数。
        /// 不会给<see cref="Id"/>属性赋值。
        /// </summary>
        public OrmObjectBase()
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="id">初始化<see cref="Id"/>属性的值。</param>
        public OrmObjectBase(TKey id)
        {
            _Id = id;
        }

        /// <summary>
        /// Id属性。
        /// </summary>
        [Key, Column(Order = 0)]
        public TKey Id
        {
            get { return _Id; }
            set { _Id = value; }
        }

    }

    public abstract class GuidKeyObjectBase
    {
        /// <summary>
        /// 构造函数。
        /// 会自动用<see cref="Guid.NewGuid"/>生成<see cref="Id"/>属性值。
        /// </summary>
        public GuidKeyObjectBase()
        {
            Id = Guid.NewGuid();
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="id">指定该实体对象的<see cref="Id"/>属性。</param>
        public GuidKeyObjectBase(Guid id)
        {
            Id = id;
        }

        /// <summary>
        /// 主键。
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None), Column(Order = 0)]
        public Guid Id { get; set; }

        /// <summary>
        /// 如果Id是Guid.Empty则生成新Id,否则立即返回false。
        /// </summary>
        /// <returns>true生成了新Id，false已经有了非空Id。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GenerateIdIfEmpty()
        {
            if (Guid.Empty != Id)
                return false;
            Id = Guid.NewGuid();
            return true;
        }

    }

    /// <summary>
    /// 提供一个基类，包含一个编码为字符串的压缩属性。且该字符串可以理解为一个字典的内容。
    /// </summary>
    public abstract class SimpleExtendPropertyBase : GuidKeyObjectBase, IBeforeSave
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public SimpleExtendPropertyBase()
        {
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="id"><inheritdoc/></param>
        public SimpleExtendPropertyBase(Guid id) : base(id)
        {
        }

        private string _PropertiesString;

        /// <summary>
        /// 属性字符串。格式如:atk=20.5,tid=933323D7-3A9B-4B0A-9072-E6AAD3FAC411,def=10|20|30,
        /// 数字，时间，Guid，字符串。
        /// </summary>
        public string PropertiesString { get => _PropertiesString; set => _PropertiesString = value; }

        private Dictionary<string, object> _Properties;

        /// <summary>
        /// 对属性字符串的解释。键是属性名，字符串类型。值有三种类型，decimal,string,decimal[]。
        /// 特别注意，如果需要频繁计算，则应把用于战斗的属性单独放在其他字典中。该字典因大量操作皆为读取，拆箱问题不大，且非核心战斗才会较多的使用该属性。
        /// </summary>
        [NotMapped]
        public Dictionary<string, object> Properties
        {
            get
            {
                if (_Properties is null)
                {
                    _Properties = new Dictionary<string, object>();
                    OwHelper.AnalysePropertiesString(PropertiesString, _Properties);
                }
                return _Properties;
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="db"><inheritdoc/></param>
        public virtual void PrepareSaving(DbContext db)
        {
            if (_Properties is null) //若未初始化字典
                return; //不变更属性
            PropertiesString = OwHelper.ToPropertiesString(Properties);
        }
    }

    public class SimpleExtendPropertyBaseExtensions
    {

    }

    /// <summary>
    /// 描述虚拟世界内对象关系的通用基类。
    /// </summary>
    /// <remarks>
    /// 以下建议仅针对，联合主键是前面的更容易引发查找的情况：
    /// 通常应使用Id属性指代最长查找的实体——即"我"这一方，Id2可以记录关系对象Id。
    /// </remarks>
    public class GameEntityRelationshipBase : SimpleExtendPropertyBase
    {
        public GameEntityRelationshipBase()
        {
        }

        public GameEntityRelationshipBase(Guid id) : base(id)
        {
        }

        public GameEntityRelationshipBase(Guid id, Guid id2, long flag) : base(id)
        {
            Id2 = id2;
            Flag = flag;
        }

        /// <summary>
        /// 客体实体Id。
        /// </summary>
        public Guid Id2 { get; set; }

        public long Flag { get; set; }

        [MaxLength(64)]
        public string PropertyString { get; set; }

    }

    public static class PocoLoadingExtensions
    {
        public static TRelated Load<TRelated>(
            this Action<object, string> loader,
            object entity,
            ref TRelated navigationField,
            [CallerMemberName] string navigationName = null)
            where TRelated : class
        {
            loader?.Invoke(entity, navigationName);

            return navigationField;
        }

    }

    /// <summary>
    /// POCO类在被保存前需要调用此接口将一些数据写入可存储的字段中。
    /// </summary>
    public interface IBeforeSave
    {
        /// <summary>
        /// 实体类在被保存前需要调用该成员。应该仅写入自身拥有的直接存储于数据库的简单字段。
        /// 不要引用其他存储于数据库中的实体。否则，需要考虑重载其他实体的该接口方法，保证不会反复提交，或者是有序的保存。
        /// </summary>
        /// <param name="db">该实体类将被保存到的数据库上下文。</param>
        void PrepareSaving(DbContext db);
    }

    /// <summary>
    /// 创建读取或写入游戏数据相关的服务。
    /// </summary>
    public interface IGameStore
    {
        public GameUserContext CreateNewUserDbContext();

        public GameTemplateContext CreateNewTemplateContext();
    }

    public abstract class GameStoreBase : IGameStore
    {
        public virtual GameTemplateContext CreateNewTemplateContext()
        {
            return new GameTemplateContext(null);
        }

        public virtual GameUserContext CreateNewUserDbContext()
        {
            return new GameUserContext(null);
        }
    }
}