using System;
using System.Collections.Generic;
using System.Text;

namespace GuangYuan.GY001.UserDb
{
    public class GameBlueprint : GameThingBase
    {
        public GameBlueprint()
        {

        }

        public virtual List<GameItem> BpInputs { get; } = new List<GameItem>();
    }


}
