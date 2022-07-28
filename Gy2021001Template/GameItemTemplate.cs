using OW.Game;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;

namespace GuangYuan.GY001.TemplateDb
{
    /// <summary>
    /// 游戏内物品的模板对象。
    /// </summary>
    public class GameItemTemplate : GameThingTemplateBase
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public GameItemTemplate()
        {

        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="id"><inheritdoc/></param>
        public GameItemTemplate(Guid id) : base(id)
        {

        }

        /// <summary>
        /// 游戏内Id,代替了复杂的类属机制。
        /// </summary>
        public int? GId { get; set; }

        /// <summary>
        /// 所属的Id字符串，以逗号分隔。
        /// </summary>
        public string GenusIdString { get; set; }

        private List<Guid> _GenusIds;

        /// <summary>
        /// 获取所属的属Id集合。
        /// </summary>
        [NotMapped]
        public List<Guid> GenusIds
        {
            get
            {
                lock (this)
                    if (null == _GenusIds)
                    {
                        _GenusIds = GenusIdString.Split(OwHelper.CommaArrayWithCN, StringSplitOptions.RemoveEmptyEntries).Select(c => Guid.Parse(c)).ToList();
                    }
                return _GenusIds;
            }
        }

        /// <summary>
        /// 类型码。没有指定则返回0。
        /// </summary>
        [NotMapped]
        public int GenusCode
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Catalog3Number;
        }

        /// <summary>
        /// 序列号。
        /// </summary>
        [NotMapped]
        public int Sequence
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GId.GetValueOrDefault() % 1000;
        }

        /// <summary>
        /// 大类类号。
        /// </summary>
        [NotMapped]
        public int Catalog1Number
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GId.GetValueOrDefault() / 10000000;
        }

        /// <summary>
        /// 中类类号。
        /// </summary>
        [NotMapped]
        public int Catalog2Number
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GId.GetValueOrDefault() / 100000 % 100;
        }

        /// <summary>
        /// 小类类号。属号。
        /// </summary>
        [NotMapped]
        public int Catalog3Number
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GId.GetValueOrDefault() / 1000 % 100;
        }

        /// <summary>
        /// 类号。除了序列号以外的前6位(十进制)分类号。
        /// </summary>
        [NotMapped]
        public int CatalogNumber
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GId.GetValueOrDefault() / 1000;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="propertyName"><inheritdoc/></param>
        /// <param name="result"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public bool TryGetPropertyValue(string propertyName, out object result)
        {
            bool succ;
            switch (propertyName)
            {
                case "GId":
                    result = GId;
                    succ = true;
                    break;
                case "GenusCode":
                    result = GenusCode;
                    succ = true;
                    break;
                default:
                    succ = Properties.TryGetValue(propertyName, out result);
                    break;
            }
            return succ;
        }


    }
}
