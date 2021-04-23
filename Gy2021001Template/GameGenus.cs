using OwGame;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Gy2021001Template
{
    /// <summary>
    /// "属"对象。
    /// </summary>
    public class GameGenus
    {
        public GameGenus()
        {

        }

        /// <summary>
        /// 子属Name的字符串，以逗号分割。
        /// </summary>
        public string ChildrenIdString { get; set; }

        /// <summary>
        /// 父属Name的字符串，以逗号分割。
        /// </summary>
        public string ParentsIdString { get; set; }

        /// <summary>
        /// 名字。这个是键值，必须唯一。最大64字符(中文算1个字符)
        /// </summary>
        [Key, StringLength(64)]
        public string Name { get; set; }

        /// <summary>
        /// 助记名，服务器不使用。
        /// </summary>
        public string ShortName { get; set; }

        /// <summary>
        /// 显示名。
        /// </summary>
        public string DisplayName { get; set; }
    }
}
