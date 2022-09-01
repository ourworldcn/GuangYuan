using System;
using System.Collections.Generic;
using System.Text;

namespace OW.DDD
{
    public class EntityBase
    {
    }

    public interface IAggregateRoot
    {

    }

    public interface IRepository<out T> where T : IAggregateRoot
    {
    }
}
