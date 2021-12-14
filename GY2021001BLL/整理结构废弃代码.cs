using System;
using System.Collections.Generic;
using System.Text;

namespace GuangYuan.GY001.BLL
{        /// <summary>
         /// 获取一个延迟查询，它返回所有坐骑的身体对象。
         /// </summary>
         /// <param name="db"></param>
         /// <param name="bodyTids">指定身体模板Id的集合，是空或者空集合，则不限定。</param>
         /// <returns>键是角色Id，值是家园展示坐骑的集合。</returns>
    //public IQueryable<Guid> GetBodiesQuery(DbContext db, IEnumerable<Guid> bodyTids = null)
    //{
    //    IQueryable<GameItem> bodys;  //身体对象集合
    //    if (null != bodyTids && bodyTids.Any())  //若需要限定身体模板Id
    //    {
    //        bodys = from body in db.Set<GameItem>()
    //                where bodyTids.Contains(body.TemplateId) //限定身体模板
    //                orderby body.TemplateId
    //                select body;
    //    }
    //    else
    //        bodys = from body in db.Set<GameItem>()
    //                orderby body.TemplateId
    //                select body;  //身体对象集合
    //    var mounts = from mount in db.Set<GameItem>()
    //                 where mount.TemplateId == ProjectConstant.ZuojiZuheRongqi //限定坐骑容器
    //                 && mount.PropertiesString.Contains("for10=")   //限定是家园展示坐骑
    //                 select mount;
    //    var bags = from bag in db.Set<GameItem>()
    //               where bag.TemplateId == ProjectConstant.ZuojiBagSlotId && bag.OwnerId.HasValue    //坐骑背包
    //               select bag;
    //    var coll = from body in bodys //身体
    //               join mount in mounts //坐骑容器
    //               on body.ParentId equals mount.Id
    //               join bag in bags
    //               on mount.ParentId equals bag.Id //实际坐骑
    //               join tmp in db.Set<CharSpecificExpandProperty>()
    //               on bag.OwnerId.Value equals tmp.Id
    //               group mount by new { bag.OwnerId.Value, tmp.LastLogoutUtc };   //获取实际坐骑
    //    var result = from tmp in coll
    //                 orderby tmp.Count() descending, tmp.Key.LastLogoutUtc descending
    //                 select tmp.Key.Value;
    //    return result;
    //}

}
