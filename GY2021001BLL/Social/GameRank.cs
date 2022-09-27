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
    public class GetTotalPowerTopRankCommand : GameCommandBase<GetTotalPowerTopRankCommand>
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
    public class GetTotalPowerTopRankCommandResult : GameCommandResultBase<GetTotalPowerTopRankCommandResult>
    {
        public GetTotalPowerTopRankCommandResult()
        {

        }

        /// <summary>
        /// 返回值的集合，元素结构(角色Id,战力,显示名)
        /// </summary>
        public List<(Guid, decimal, string)> ResultCollction { get; } = new List<(Guid, decimal, string)>();
    }

    /// <summary>
    /// 获取全服推关战力排名前n位成员命令处理程序。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(ICommandHandler<GetTotalPowerTopRankCommand, GetTotalPowerTopRankCommandResult>))]
    public class GetTotalPowerTopRankCommandHandler : GameCommandHandlerBase<GetTotalPowerTopRankCommand, GetTotalPowerTopRankCommandResult>
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
            var coll = from slot in db.Set<GameItem>()
                       where slot.ExtraGuid == ProjectConstant.TuiGuanTId
                       join parent in db.Set<GameItem>()
                       on slot.ParentId equals parent.Id
                       join gc in db.Set<GameChar>().Where(gc => !gc.CharType.HasFlag(CharType.SuperAdmin) && !gc.CharType.HasFlag(CharType.Admin) && !gc.CharType.HasFlag(CharType.Npc) && !gc.CharType.HasFlag(CharType.Robot))
                       on parent.OwnerId equals gc.Id
                       orderby slot.ExtraDecimal.Value descending, gc.DisplayName
                       select new { gc.Id, gc.DisplayName, slot.ExtraDecimal.Value };
            var collResult = coll.Take(datas.Top).ToList();
            result.ResultCollction.AddRange(collResult.Select(c => (c.Id, c.Value, c.DisplayName)));
            return result;
        }
    }
}
