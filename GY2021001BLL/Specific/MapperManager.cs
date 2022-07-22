using OW.Game;
using System;
using System.Collections.Generic;
using System.Text;

namespace GuangYuan.GY001.BLL.Specific
{
    /// <summary>
    /// 转换服务。
    /// </summary>
    public class MapperManager : GameManagerBase<MapperManagerOptions>
    {
        public MapperManager()
        {
            Initialize();
        }

        public MapperManager(IServiceProvider service) : base(service)
        {
            Initialize();
        }

        public MapperManager(IServiceProvider service, MapperManagerOptions options) : base(service, options)
        {
            Initialize();
        }

        void Initialize()
        {

        }
    }

    public class MapperManagerOptions
    {
    }
}
