using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OW.DDD;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace OW.Game
{

    public class EventBusManagerOptions : IOptions<EventBusManagerOptions>
    {
        public EventBusManagerOptions Value => this;
    }

    public class EventBusManager
    {
        public EventBusManager(IServiceProvider service)
        {
            _Service = service;
        }

        IServiceProvider _Service;

        ConcurrentQueue<INotification> _Datas = new ConcurrentQueue<INotification>();

        public void AddData(INotification eventData)
        {
            _Datas.Enqueue(eventData);
        }

        public void RaiseEvent()
        {
            while (_Datas.TryDequeue(out var item))
            {
                var type = typeof(INotificationHandler<>).MakeGenericType(item.GetType());
                var svc = _Service.GetServices(type);
                //svc.ForEach(c=>c)
            }
        }
    }
}
