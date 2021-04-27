using System;
using System.Collections.Generic;
using System.Text;

namespace GY2021001BLL
{
    /// <summary>
    /// 战斗管理器。
    /// </summary>
    public class CombatManager
    {
        private readonly IServiceProvider _ServiceProvider;

        public CombatManager()
        {

        }

        public CombatManager(IServiceProvider serviceProvider)
        {
            _ServiceProvider = serviceProvider;
        }
    }
}
