using OW.DDD;
using System;
using System.Collections.Generic;
using System.Text;

namespace OW.Game
{
    public abstract class GameCommandBase<T> : CommandBase<T>
    {
    }

    public abstract class GameCommandResultBase<T> : CommandResultBase<T>
    {
    }

    public abstract class GameCommandHandlerBase<TRequest, TResponse> : CommandHandlerBase<TRequest, TResponse> where TRequest : ICommand<TRequest>
    {

    }
}
