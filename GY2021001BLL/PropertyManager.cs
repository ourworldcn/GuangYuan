using System;

namespace OW.Game
{
    public class PropertyManagerOptions
    {
        public PropertyManagerOptions()
        {

        }
    }

    /// <summary>
    /// 属性管理器。
    /// </summary>
    public class PropertyManager : GameManagerBase<PropertyManagerOptions>
    {
        public PropertyManager()
        {
        }

        public PropertyManager(IServiceProvider service) : base(service)
        {
        }

        public PropertyManager(IServiceProvider service, PropertyManagerOptions options) : base(service, options)
        {
        }

    }

}
