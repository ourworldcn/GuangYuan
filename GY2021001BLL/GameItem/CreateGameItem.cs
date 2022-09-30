using OW.Game;

namespace GuangYuan.GY001.BLL
{
    /// <summary>
    /// 创建一个物品的命令。
    /// </summary>
    public class CreateGameItemCommand : GameCommand<CreateGameItemCommand>
    {

    }

    public class CreateGameItemCommandResult : GameCommandResult<CreateGameItemCommandResult>
    {
    }

    public class CreateGameItemCommandHandler : GameCommandHandler<CreateGameItemCommand, CreateGameItemCommandResult>
    {
        public CreateGameItemCommandHandler()
        {
        }

        public CreateGameItemCommandHandler(OwEventBus eventBus)
        {
            EventBus = eventBus;
        }

        public OwEventBus EventBus { get; set; }

        public override CreateGameItemCommandResult Handle(CreateGameItemCommand command)
        {
            var result = new CreateGameItemCommandResult();
            EventBus.Add(new GameItemCreating());
            EventBus.Raise();

            EventBus.Add(new GameItemCreated());
            EventBus.Raise();
            return result;
        }
    }

    /// <summary>
    /// 正在试图创建一个物品的通告。
    /// </summary>
    public class GameItemCreating : GameNotificationBase
    {

    }

    /// <summary>
    /// 一个物品已经被创建的通告。
    /// </summary>
    public class GameItemCreated : GameNotificationBase
    {

    }
}