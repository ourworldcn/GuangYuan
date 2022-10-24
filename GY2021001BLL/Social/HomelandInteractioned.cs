
using OW.Game;

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
        public override void Handle(HomelandInteractionedCommand command)
        {
        }
    }
}