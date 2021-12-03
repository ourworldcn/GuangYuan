using System;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using System.Collections.Generic;

namespace GuangYuan.GY001.BLL.Script
{
    /// <summary>
    /// 
    /// </summary>
    public class ItemTemplateScriptBase
    {
        private GameItemTemplate _Template;

        public ItemTemplateScriptBase(GameItemTemplate template)
        {
            _Template = template;
        }

        public GameItemTemplate Template { get => _Template; set => _Template = value; }

        public virtual void UseItem(IEnumerable<GameItem> outs)
        {

        }
    }
}
