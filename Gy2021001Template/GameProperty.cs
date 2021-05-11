using System;
using System.Collections.Generic;
using System.Text;

namespace Gy2021001Template
{
    public class GamePropertyTemplate : GameTemplateBase
    {
        public GamePropertyTemplate()
        {

        }

        public GamePropertyTemplate(Guid id) : base(id)
        {
        }

        /// <summary>
        /// 属性的名，这个字符串要唯一。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 序列属性的公式。如 lv*10+10
        /// </summary>
        public string SequenceFormula { get; set; }
    }
}
