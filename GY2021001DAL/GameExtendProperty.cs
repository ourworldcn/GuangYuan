
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;

namespace GuangYuan.GY001.UserDb
{
    /// <summary>
    /// 服务器内部使用的通用扩展属性。
    /// <see cref="Id"/> 和 <see cref="Name"/> 组成联合主键。
    /// </summary>
    public class GameExtendProperty : GameObjectBase
    {
        /// <summary>
        /// 客户端属性使用的键名。
        /// <see cref="Name"/>是该值的，表示由客户端使用，服务器不会使用该对象。
        /// </summary>
        public const string ClientPropertyName = "d51b3d58-2dec-4d24-b85d-a57aafe10dd7";

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public GameExtendProperty()
        {

        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="name"></param>
        public GameExtendProperty(string name)
        {
            Name = name;
        }

        private string _Name;
        /// <summary>
        /// 属性的名称。
        /// </summary>
        [MaxLength(64)]
        public string Name
        {
            get
            {
                return _Name;
            }

            set
            {
                if (value.Length > 64)
                    throw new ArgumentException("最长仅能支持64个字符。", nameof(value));
                _Name = value;
            }
        }

        private string _StringValue;

        /// <summary>
        /// 短文本属性，可以索引加速查找。
        /// </summary>
        [MaxLength(256)]
        public string StringValue
        {
            get => _StringValue;
            set
            {
                if (value.Length > 256)
                    throw new ArgumentException("最长仅能支持256个字符。", nameof(value));
                _StringValue = value;
            }
        }

        public int IntValue { get; set; }

        public decimal DecimalValue { get; set; }

        public double DoubleValue { get; set; }

        /// <summary>
        /// 长文本属性，无法索引。
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// 日期属性。
        /// </summary>
        /// <value>默认值是创建此对象是的UTC时间。</value>
        public DateTime DateTimeValue { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 一个<see cref="Guid"/>值。
        /// </summary>
        public Guid? GuidValue { get; set; }
    }

    /// <summary>
    /// 服务器代码使用的通用扩展属性类。
    /// </summary>
    public class ExtendPropertyDescriptor
    {
        /// <summary>
        /// 持久化标志。
        /// <see cref="GameExtendProperty.StringValue"/>是该字符串开头，
        /// 且<see cref="GameExtendProperty.IntValue"/>指定了<see cref="GameExtendProperty.Text"/>，开头多少个字符是类型全名，且后跟一个分号.然后是Json序列化的内容。
        /// 则该<see cref="GameExtendProperty"/>对象会被认为是一个需要持久化的属性。
        /// </summary>
        public const string MarkIdString = "a88c6717-4fdc-4cb0-b127-e1799ebf3b35";

        /// <summary>
        /// 试图从<see cref="GameExtendProperty"/>中转化得到<see cref="ExtendPropertyDescriptor"/>对象。
        /// 特别地，本成员使用了反射，因此程序集改名导致原有数据无法读回。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="result"></param>
        /// <returns>true成功得到对象，false转化错误。</returns>
        static public bool TryParse(GameExtendProperty obj, out ExtendPropertyDescriptor result)
        {
            if (MarkIdString != obj.StringValue)    //若不是特定标记开头
            {
                result = null;
                return false;
            }
            if (obj.IntValue <= 0 || obj.Text.Length <= obj.IntValue + 1 || obj.Text[obj.IntValue] != ';')   //若格式不正确
            {
                result = null;
                return false;
            }
            var fullName = obj.Text[..obj.IntValue];
            var type = Type.GetType(fullName);
            if (type is null)   //若找不到指定类
            {
                result = null;
                return false;
            }
            var guts = obj.Text[(obj.IntValue + 1)..];
            result = new ExtendPropertyDescriptor()
            {
                Data = string.IsNullOrWhiteSpace(guts) ? default : JsonSerializer.Deserialize(guts, type),
                IsPersistence = true,
                Name = obj.Name,
                Type = type,
            };
            return true;
        }

        /// <summary>
        /// 将当前对象内容填写到指定的<see cref="GameExtendProperty"/>对象中。
        /// </summary>
        /// <param name="obj"></param>
        public void FillTo(GameExtendProperty obj)
        {
            var fullName = Type.AssemblyQualifiedName;
            obj.IntValue = fullName.Length;
            obj.Text = $"{fullName};{(Data is null ? null : JsonSerializer.Serialize(Data, Type))}";
            obj.StringValue = MarkIdString;
            obj.Name = Name;
        }

        /// <summary>
        /// 更新或追加对象。
        /// </summary>
        /// <param name="srcs"></param>
        /// <param name="dests"></param>
        static public void Fill(IEnumerable<ExtendPropertyDescriptor> srcs, ICollection<GameExtendProperty> dests)
        {
            var coll = (from src in srcs
                        where src.IsPersistence
                        join dest in dests
                        on src.Name equals dest.Name into g
                        from tmp in g.DefaultIfEmpty()
                        select (src, dest: tmp)).ToArray();
            foreach (var (src, dest) in coll)  //更新已有对象
            {
                if (dest is null)
                {
                    var tmp = new GameExtendProperty();
                    src.FillTo(tmp);
                    dests.Add(tmp);
                }
                else
                    src.FillTo(dest);
            }
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        public ExtendPropertyDescriptor()
        {

        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="data"></param>
        /// <param name="name"></param>
        /// <param name="isPersistence"></param>
        /// <param name="type"></param>
        public ExtendPropertyDescriptor(object data, string name, bool isPersistence = false, Type type = null)
        {
            Data = data;
            Name = name;
            IsPersistence = isPersistence;
            Type = type ?? data.GetType();
        }

        /// <summary>
        /// 名称，对应<see cref="GameExtendProperty.Name"/>
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// <see cref="Data"/>的实际类型，<see cref="Type.FullName"/>会存储在<see cref="GameExtendProperty.StringValue"/>中。前提是该数据需要持久化。
        /// 鉴于二进制序列化过于复杂危险，当前实现使用<see cref="JsonSerializer"/>来完成序列化工作。
        /// </summary>
        public Type Type { get; set; }

        public object Data { get; set; }

        public bool IsPersistence { get; set; }
    }

}