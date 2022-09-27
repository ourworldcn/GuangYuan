using OW.DDD;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace OW.Game
{
    public class GameNotificationBase : NotificationBase
    {

    }

    public abstract class GameNotificationHandlerBase<T> : NotificationHandlerBase<T> where T : INotification
    {
    }
}
