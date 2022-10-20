using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Game.Logging
{
    public class PayOrder : IJsonDynamicProperty, IEntityWithSingleKey<string>, IBeforeSave
    {
        public PayOrder()
        {

        }

        public PayOrder(string id)
        {
            Id = id;
        }

        [Key, MaxLength(64)]
        public string Id { get; set; }

        /// <summary>
        /// 付款方Id，就是账号id。
        /// </summary>
        [MaxLength(64)]
        public string PayerId { get; set; }

        /// <summary>
        /// 订单创建时间。
        /// </summary>
        public DateTime CreateUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 计价币种的标识。
        /// </summary>
        [MaxLength(8)]
        public string CurrencyId { get; set; }

        /// <summary>
        /// 总金额。
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// 冲红账单Id，如果不是冲红则为null。
        /// </summary>
        [MaxLength(64)]
        public string OffsetId { get; set; }

        /// <summary>
        /// 付费渠道。目前仅有78 和 89。
        /// </summary>
        public int Bank { get; set; }

        /// <summary>
        /// 稽核。false未稽核，通常是存在疑问，true已经稽核即对账无疑问。
        /// </summary>
        public bool Audit { get; set; }

        /// <summary>
        /// 发生时间。
        /// </summary>
        #region JsonObject相关

        private string _JsonObjectString;
        /// <summary>
        /// 属性字符串。格式数Json字符串。
        /// </summary>
        public string JsonObjectString
        {
            get => _JsonObjectString;
            set
            {
                if (!ReferenceEquals(_JsonObjectString, value))
                {
                    _JsonObjectString = value;
                    _JsonObject = null;
                    JsonObjectType = null;
                }
            }
        }

        /// <summary>
        /// 获取或初始化<see cref="JsonObject"/>属性并返回。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual T GetJsonObject<T>() where T : new()
        {
            if (typeof(T) != JsonObjectType || JsonObject is null)  //若需要初始化
            {
                if (string.IsNullOrWhiteSpace(JsonObjectString))
                {
                    JsonObject = new T();
                }
                else
                {
                    JsonObject = JsonSerializer.Deserialize(JsonObjectString, typeof(T));
                }
                JsonObjectType = typeof(T);
            }
            return (T)JsonObject;
        }

        private object _JsonObject;
        /// <summary>
        /// 用<see cref="GetJsonObject{T}"/>获取。
        /// </summary>
        [JsonIgnore, NotMapped]
        public object JsonObject
        {
            get => _JsonObject;
            set
            {
                _JsonObject = value;
                JsonObjectType = value?.GetType();
            }
        }

        [JsonIgnore, NotMapped]
        public Type JsonObjectType { get; set; }

        #endregion JsonObject相关

        public virtual void PrepareSaving(DbContext db)
        {
            if (JsonObject != null)
                JsonObjectString = JsonSerializer.Serialize(JsonObject, JsonObjectType ?? JsonObject.GetType());
            //base.PrepareSaving(db);
        }


    }
}
