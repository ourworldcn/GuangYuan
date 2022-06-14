using GuangYuan.GY001.BLL.Homeland;
using GuangYuan.GY001.UserDb;
using GuangYuan.GY001.UserDb.Combat;
using OW.Game;
using OW.Game.PropertyChange;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GuangYuan.GY001.BLL
{
    public static class GameBootyExtensions
    {
        /// <summary>
        /// 根据战利品数据设置实际的物品数量。
        /// </summary>
        /// <param name="booty"></param>
        /// <param name="world"></param>
        /// <param name="changes"></param>
        public static void SetGameItems(this GameBooty booty, VWorld world, ICollection<ChangeItem> changes = null)
        {
            var changes1 = new List<GamePropertyChangeItem<object>>();
            var gc = world.CharManager.GetCharFromId(booty.CharId);
            var hl = gc.GetHomeland();
            GameItem gi = new GameItem();
            world.EventsManager.GameItemCreated(gi, booty.Properties);
            var gim = world.ItemManager;
            GameItem parent;
            var tid = gi.TemplateId;
            if (tid == ProjectConstant.JinbiId)    //若是战利品
            {
                //{
                //    var yumiTian = hl.AllChildren.First(c => c.TemplateId == ProjectConstant.YumitianTId);
                //    var fcpCount = yumiTian.Name2FastChangingProperty["Count"];
                //    fcpCount.GetCurrentValueWithUtc();
                //    fcpCount.LastValue += booty.Count;
                //}
                parent = gc.GetJinbi().Parent;
            }
            else if (tid == ProjectConstant.MucaiId)
            {
                //{
                //    var mucaiShu = hl.AllChildren.First(c => c.TemplateId == ProjectConstant.MucaishuTId);
                //    var fcpCount = mucaiShu.Name2FastChangingProperty["Count"];
                //    fcpCount.GetCurrentValueWithUtc();
                //    fcpCount.LastValue += booty.Count;
                //}
                parent = gc.GetMucai().Parent;
            }
            else if (tid == ProjectConstant.MucaishuTId)
            {
                parent = gc.GetMainbase().Children.FirstOrDefault(c => c.TemplateId == ProjectConstant.MucaishuTId).Parent;
            }
            else if (tid == ProjectConstant.YumitianTId)
            {
                parent = gc.GetMainbase().Children.FirstOrDefault(c => c.TemplateId == ProjectConstant.YumitianTId).Parent;
            }
            else
                throw new InvalidOperationException();
            gim.MoveItem(gi, gi.Count ?? 1, parent, null, changes1);
            if (null != changes)
                changes1.CopyTo(changes);
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
            dic[$"{prefix}{booty.Properties.GetGuidOrDefault("tid")}"] = booty.Properties.GetDecimalOrDefault("count");
        }

    }
}
