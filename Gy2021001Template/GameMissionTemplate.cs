using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace GuangYuan.GY001.TemplateDb
{
    /// <summary>
    /// reward
    /// </summary>
    public class GameMissionTemplate : GameTemplateBase
    {
        /// <summary>
        /// 前置任务Id集合。逗号分割。
        /// </summary>
        public string PreMissionIdString { get; set; }


        List<Guid> _PreMissionIds;
        /// <summary>
        /// 前置任务Id集合。
        /// </summary>
        [NotMapped]
        public List<Guid> PreMissionIds
        {
            get
            {
                if (_PreMissionIds is null)
                {
                    _PreMissionIds = PreMissionIdString.Split(OwHelper.CommaArrayWithCN, StringSplitOptions.RemoveEmptyEntries).Select(c => OwConvert.ToGuid(c)).ToList();
                }
                return _PreMissionIds;
            }
        }

        /// <summary>
        /// 获取或设置分组标识。
        /// </summary>
        public string GroupNumber { get; set; }
    }
}
