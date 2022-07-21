using System;
using System.Collections.Generic;
using System.Text;

namespace OW.Game.Store
{

    public class VirtualThingBase : DbTreeNode
    {
        public VirtualThingBase()
        {
        }

        public VirtualThingBase(Guid id) : base(id)
        {
        }
    }

    public class VirtualThing : VirtualThingBase
    {
    }
}
