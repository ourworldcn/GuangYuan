
using GuangYuan.GY001.UserDb;
using Microsoft.Extensions.DependencyInjection;
using OW.Game;
using OW.Game.Mission;
using OW.Game.Store;
using System;
using System.Linq;

namespace GuangYuan.GY001.BLL
{
    /// <summary>
    /// 已经和好友互动的通知。
    /// </summary>
    public class HomelandInteractionedCommand : GameCommandBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public HomelandInteractionedCommand()
        {

        }
    }

    public class HomelandInteractionedCommandHandler : GameCommandHandlerBase<HomelandInteractionedCommand>
    {
        public HomelandInteractionedCommandHandler(IServiceProvider service)
        {
            _Service = service;
        }

        IServiceProvider _Service;

        public override void Handle(HomelandInteractionedCommand command)
        {
            var context = _Service.GetService<GameCommandContext>();
            using var dw = context.LockUser();
            if (dw is null)
            {
                return;
            }
            var gc = context.GameChar;
            var dt = gc.GetSdpDateTimeOrDefault("HomelandInteractioned");
            if (dt.Date != context.UtcNow.Date)  //若今天尚未处理
            {
                //设置成就
                var gmm = _Service.GetService<GameMissionManager>();

                var mission = context.GameChar.GetRenwuSlot().Children.FirstOrDefault(c => c.ExtraGuid == ProjectMissionConstant.累计访问好友天次成就);
                var oldVal = mission.GetSdpDecimalOrDefault(ProjectMissionConstant.指标增量属性名);
                mission.SetSdp(ProjectMissionConstant.指标增量属性名, oldVal + 1m); //设置该成就的指标值的增量，原则上都是正值
                gmm.ScanAsync(context.GameChar);
                gc.SetSdp("HomelandInteractioned", context.UtcNow);
            }
        }
    }
}