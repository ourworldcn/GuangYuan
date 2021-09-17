using GuangYuan.GY001.BLL.Homeland;
using GuangYuan.GY001.UserDb;
using GuangYuan.GY001.UserDb.Combat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GuangYuan.GY001.BLL
{
    public static class GameBootyExtensions
    {
        /// <summary>
        /// 设置实际的物品增减。
        /// </summary>
        /// <param name="booty"></param>
        /// <param name="world"></param>
        /// <param name="changes"></param>
        public static void SetGameItems(this GameBooty booty, VWorld world, ICollection<ChangeItem> changes = null)
        {
            var gc = world.CharManager.GetCharFromId(booty.CharId);
            GameItem gi = new GameItem();
            gi.Initialize(world.Service, booty.TemplateId); gi.Count = booty.Count;
            var gim = world.ItemManager;
            GameItem parent;
            if (booty.TemplateId == ProjectConstant.JinbiId)
            {
                parent = gc.GetJinbi().Parent;
            }
            else if (booty.TemplateId == ProjectConstant.MucaiId)
            {
                parent = gc.GetMucai().Parent;
            }
            else if (booty.TemplateId == ProjectConstant.MucaishuTId)
            {
                parent = gc.GetMainbase().Children.FirstOrDefault(c => c.TemplateId == ProjectConstant.MucaishuTId).Parent;
            }
            else
                throw new InvalidOperationException();
            gim.AddItem(gi, parent, null, changes);
        }

        /// <summary>
        /// 将信息填充到字典中。
        /// </summary>
        /// <param name="booty"></param>
        /// <param name="world"></param>
        /// <param name="dic"></param>
        public static void FillToDictionary(this GameBooty booty, VWorld world, IDictionary<string, object> dic)
        {
            const string prefix = "gTId";
            dic[$"{prefix}{booty.TemplateId}"] = booty.Count;
        }
    }
}
