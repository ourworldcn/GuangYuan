using OW.Game.PropertyChange;
using System.Collections.Generic;
using System;
using GuangYuan.GY001.UserDb;

namespace OW.Game
{
    public class PropertyChangesNotification : GameNotificationBase
    {
        public PropertyChangesNotification()
        {
        }

        public PropertyChangesNotification(List<GamePropertyChangeItem<object>> changes, GameChar gameChar)
        {
            _Changes = changes;
            GameChar = gameChar;
        }

        public void Flatten()
        {
            throw new NotImplementedException();
        }

        List<GamePropertyChangeItem<object>> _Changes;

        /// <summary>
        /// 
        /// </summary>
        public List<GamePropertyChangeItem<object>> Changes => _Changes ??= new List<GamePropertyChangeItem<object>>();

        /// <summary>
        /// 当前的角色。
        /// </summary>
        public GameChar GameChar { get; set; }
    }

}
