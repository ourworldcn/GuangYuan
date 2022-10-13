using GuangYuan.GY001.UserDb;
using OW.Game;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OW.DDD;

namespace GuangYuan.GY001.BLL.Social
{
    /// <summary>
    /// 获取全服推关战力排名前n位成员命令。
    /// </summary>
    public class GetTotalPowerTopRankCommand : GameCommand<GetTotalPowerTopRankCommand>
    {
        public GetTotalPowerTopRankCommand()
        {

        }

        /// <summary>
        /// 前多少位的排名。过大的值将导致缓慢，设计时考虑100左右。
        /// </summary>
        public int Top { get; set; }
    }

    /// <summary>
    /// 获取全服推关战力排名前n位成员命令的返回值。
    /// </summary>
    public class GetTotalPowerTopRankCommandResult : GameCommandResult<GetTotalPowerTopRankCommandResult>
    {
        public GetTotalPowerTopRankCommandResult()
        {

        }

        /// <summary>
        /// 返回值的集合，元素结构(角色Id,战力,显示名,头像索引)
        /// </summary>
        public List<(Guid, decimal, string, int)> ResultCollction { get; } = new List<(Guid, decimal, string, int)>();
    }

    /// <summary>
    /// 获取全服推关战力排名前n位成员命令处理程序。
    /// </summary>
    public class GetTotalPowerTopRankCommandHandler : GameCommandHandler<GetTotalPowerTopRankCommand, GetTotalPowerTopRankCommandResult>
    {
        public GetTotalPowerTopRankCommandHandler()
        {

        }

        public GetTotalPowerTopRankCommandHandler(VWorld world)
        {
            _World = world;
        }

        VWorld _World;

        public override GetTotalPowerTopRankCommandResult Handle(GetTotalPowerTopRankCommand datas)
        {
            var result = new GetTotalPowerTopRankCommandResult();
            using var db = _World.CreateNewUserDbContext();
            var allowGcs = from gc in db.Set<GameChar>()    //允许参与排名的角色集合
                           where !gc.CharType.HasFlag(CharType.SuperAdmin) && !gc.CharType.HasFlag(CharType.Admin) && !gc.CharType.HasFlag(CharType.Npc) && !gc.CharType.HasFlag(CharType.Robot)
                           select gc;

            var coll = from slot in db.Set<GameItem>()
                       where slot.ExtraGuid == ProjectConstant.TuiGuanTId
                       join parent in db.Set<GameItem>()
                       on slot.ParentId equals parent.Id
                       join gc in allowGcs
                       on parent.OwnerId equals gc.Id
                       orderby slot.ExtraDecimal.Value descending, gc.DisplayName
                       select new { gc.Id, gc.DisplayName, slot.ExtraDecimal.Value, gc.PropertiesString };
            var collResult = coll.Take(datas.Top).ToList();
            result.ResultCollction.AddRange(collResult.Select(c =>
            {
                var tmp = new Dictionary<string, object>();
                OwConvert.Copy(c.PropertiesString, tmp);
                var icon = (int)tmp.GetDecimalOrDefault("charIcon", 0);
                return (c.Id, c.Value, c.DisplayName, icon);
            }));

            return result;
        }
    }


}
