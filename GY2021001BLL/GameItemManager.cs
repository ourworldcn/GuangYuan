using System;
using System.Collections.Generic;
using System.Text;

namespace GY2021001BLL
{
    /// <summary>
    /// 虚拟物品管理器。
    /// </summary>
    public class GameItemManager
    {
        private readonly IServiceProvider _ServiceProvider;

        public GameItemManager()
        {

        }

        public GameItemManager(IServiceProvider serviceProvider)
        {
            _ServiceProvider = serviceProvider;
        }
    }
}
