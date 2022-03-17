using OW.Game.PropertyChange;
using System;
using System.Collections.Generic;
using System.Text;

namespace GuangYuan.GY001.BLL.Specific
{
    public class Gy001GamePropertyChangeManager : GamePropertyChangeManager
    {
        public Gy001GamePropertyChangeManager()
        {
        }

        public Gy001GamePropertyChangeManager(IServiceProvider service) : base(service)
        {
        }

        public Gy001GamePropertyChangeManager(IServiceProvider service, GamePropertyChangeManagerOptions options) : base(service, options)
        {
        }


    }
}
