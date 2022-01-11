using GuangYuan.GY001.BLL.Homeland;
using GuangYuan.GY001.UserDb;
using GuangYuan.GY001.UserDb.Combat;
using OW.Game;
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
            var gc = world.CharManager.GetCharFromId(booty.CharId);
            var hl = gc.GetHomeland();
            GameItem gi = new GameItem();
            world.EventsManager.GameItemCreated(gi, booty.TemplateId, null, null, null);
            gi.Count = booty.Count;
            var gim = world.ItemManager;
            GameItem parent;
            if (booty.TemplateId == ProjectConstant.JinbiId)    //若是战利品
            {
                //{
                //    var yumiTian = hl.AllChildren.First(c => c.TemplateId == ProjectConstant.YumitianTId);
                //    var fcpCount = yumiTian.Name2FastChangingProperty["Count"];
                //    fcpCount.GetCurrentValueWithUtc();
                //    fcpCount.LastValue += booty.Count;
                //}
                parent = gc.GetJinbi().Parent;
            }
            else if (booty.TemplateId == ProjectConstant.MucaiId)
            {
                //{
                //    var mucaiShu = hl.AllChildren.First(c => c.TemplateId == ProjectConstant.MucaishuTId);
                //    var fcpCount = mucaiShu.Name2FastChangingProperty["Count"];
                //    fcpCount.GetCurrentValueWithUtc();
                //    fcpCount.LastValue += booty.Count;
                //}
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

        /// <summary>
        /// 将信息从字典中取出。
        /// </summary>
        /// <param name="booty">追加到该集合中。</param>
        /// <param name="world"></param>
        /// <param name="dic">存储信息的字典。</param>
        public static void FillToBooty(this ICollection<GameBooty> booty, VWorld world, IReadOnlyDictionary<string, object> dic)
        {
            const string prefix = "gTId"; int index = prefix.Length;
            var coll = dic.Where(c => c.Key.StartsWith(prefix));
            foreach (var item in coll)
            {
                var bt = new GameBooty()
                {
                    TemplateId = new Guid(item.Key[index..]),
                    Count = dic.GetDecimalOrDefault(item.Key),
                };
                booty.Add(bt);
            }
        }
    }
}
