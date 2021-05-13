using System;
using System.Collections.Generic;
using System.Text;

namespace GY2021001DAL
{
    public class GameBlueprint : GameThingBase
    {
        public GameBlueprint()
        {

        }

        public virtual List<GameItem> BpInputs { get; } = new List<GameItem>();
    }


}
