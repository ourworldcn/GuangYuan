/*
 * 一些游戏专有且和数据库相关的基础类。
 * 因玩家数据库依赖模板数据库，故当前基础类放于模板数据库中是正常设计，没必要额外添加工程。
 * */

using Microsoft.EntityFrameworkCore;
using OW.Game;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace Game.EntityFrameworkCore
{
    /// <summary>
    /// 提供一个基类，包含一个编码为字符串的压缩属性。且该字符串可以理解为一个字典的内容。
    /// </summary>
    public abstract class StringKeyDictionaryPropertyBase : GuidKeyBase, IBeforeSave
    {
        public StringKeyDictionaryPropertyBase()
        {
        }

        public StringKeyDictionaryPropertyBase(Guid id) : base(id)
        {
        }

        /// <summary>
        /// 内部使用的同步锁。
        /// </summary>
        public abstract object ThisLocker { get; }

        private string _PropertiesString;

        /// <summary>
        /// 属性字符串。格式如:atk=20.5,tid=933323D7-3A9B-4B0A-9072-E6AAD3FAC411,def=10|20|30,
        /// 数字，时间，Guid，字符串。
        /// </summary>
        public string PropertiesString { get => _PropertiesString; set => _PropertiesString = value; }

        private Dictionary<string, object> _Properties;

        /// <summary>
        /// 对属性字符串的解释。键是属性名，字符串类型。值有三种类型，decimal,string,decimal[]。
        /// 特别注意，如果需要频繁计算，则应把用于战斗的属性单独放在其他字典中。该字典因大量操作皆为读取，拆箱问题不大，且非核心战斗才会较多的使用该系统。
        /// </summary>
        [NotMapped]
        public Dictionary<string, object> Properties
        {
            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            get
            {
                if (_Properties is null)
                    lock (ThisLocker)
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
        public void WillSaving(DbContext db)
        {
            if (_Properties is null) //若未初始化字典
                return; //不变更属性
            PropertiesString = OwHelper.ToPropertiesString(Properties);
        }
    }

    /// <summary>
    /// POCO类在被保存前需要调用此接口将一些数据写入可存储的字段中。
    /// </summary>
    public interface IBeforeSave
    {
        /// <summary>
        /// 实体类在被保存前需要调用该成员。应该仅写入自身拥有的直接存储于数据库的简单字段。
        /// 相对地，不要引用的其他存储于数据库中的实体。
        /// </summary>
        /// <param name="db">该实体类将被保存到的数据库上下文。</param>
        void WillSaving(DbContext db);

    }
}