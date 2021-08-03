using OW.Game;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace GuangYuan.GY001.UserDb
{
    public abstract class GameSocialBase : GameObjectBase
    {
        public GameSocialBase()
        {

        }

        public GameSocialBase(Guid id) : base(id)
        {
        }

        private readonly object _ThisLocker = new object();

        /// <summary>
        /// 同步锁。
        /// </summary>
        [NotMapped]
        public object ThisLocker => _ThisLocker;

    }

}
