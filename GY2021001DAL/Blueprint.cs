using System;
using System.Collections.Generic;
using System.Text;

namespace GuangYuan.GY001.UserDb
{
    public class GameBlueprint : OW.Game.Store.GameObjectBase
    {
        public GameBlueprint()
        {

        }

        public virtual List<GameItem> BpInputs { get; } = new List<GameItem>();
    }


}
