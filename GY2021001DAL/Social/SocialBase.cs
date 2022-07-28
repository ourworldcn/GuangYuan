using OW.Game;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace GuangYuan.GY001.UserDb
{
    public abstract class GameSocialBase : GameEntityRelationshipBase
    {
        
        public GameSocialBase()
        {

        }

        public GameSocialBase(Guid id) : base(id)
        {
        }

        protected GameSocialBase(Guid id, Guid id2, int keyType, int flag) : base(id, id2, keyType, flag)
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
