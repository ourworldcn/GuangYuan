using OW.DDD;
using System;
using System.Collections.Generic;
using System.Text;

namespace OW.Game
{
    public class GameCommandBase : CommandBase
    {
    }

    public abstract class GameCommandHandlerBase<TRequest, TResponse> : CommandHandlerBase<TRequest, TResponse> where TRequest : ICommand<TRequest>
    {

    }
}
