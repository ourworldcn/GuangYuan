using OW.Game;
using System;

namespace GuangYuan.GY001.BLL
{
    /// <summary>
    /// 任务成就管理器的配置数据类。
    /// </summary>
    public class GameMissionManagerOptions
    {
        public GameMissionManagerOptions()
        {

        }
    }

    /// <summary>
    /// 任务/成就管理器。
    /// </summary>
    public class GameMissionManager : GameManagerBase<GameMissionManagerOptions>
    {
        public GameMissionManager()
        {
        }

        public GameMissionManager(IServiceProvider service) : base(service)
        {
        }

        public GameMissionManager(IServiceProvider service, GameMissionManagerOptions options) : base(service, options)
        {
        }
    }

}
