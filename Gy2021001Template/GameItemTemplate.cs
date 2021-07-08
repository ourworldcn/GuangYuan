using OwGame;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace Gy2021001Template
{
    public class GameItemTemplate : GameThingTemplateBase
    {
        public GameItemTemplate()
        {

        }

        public GameItemTemplate(Guid id) : base(id)
        {

        }

        /// <summary>
        /// 游戏内Id服务器不使用。
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
        public int GenusCode { get => GId.GetValueOrDefault() / 1000; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="propertyName"><inheritdoc/></param>
        /// <param name="result"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public override bool TryGetPropertyValue(string propertyName, out object result)
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
                    succ = base.TryGetPropertyValue(propertyName, out result);
                    break;
            }
            return succ;
        }
    }
}
