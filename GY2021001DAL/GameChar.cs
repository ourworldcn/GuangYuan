using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace GY2021001DAL
{
    [Table(nameof(GameChar))]
    public class GameChar : GameThingBase
    {
        public GameChar()
        {
            Id = Guid.NewGuid();
        }

        public GameChar(Guid id)
        {
            Id = id;
        }

        /// <summary>
        /// 一个角色初始创建时被调用。
        /// 通常这里预制一些道具，装备。
        /// </summary>
        public void InitialCreation()
        {
        }

        //[Key, ForeignKey(nameof(GameUser))]
        //public new Guid Id { get => base.Id; set => base.Id = value; }

        /// <summary>
        /// 直接拥有的事物。
        /// </summary>
        [NotMapped]
        public List<GameItem> GameItems { get; } = new List<GameItem>();


        /// <summary>
        /// 所属用户Id。
        /// </summary>
        [ForeignKey(nameof(GameUser))]
        public Guid GameUserId { get; set; }

        /// <summary>
        /// 所属用户的导航属性。
        /// </summary>
        public virtual GameUser GameUser { get; set; }

        /// <summary>
        /// 客户端要记录的一些属性，这个属性客户端可以随意更改，服务器不使用。
        /// </summary>
        public string ClientGutsString { get; set; }

        /// <summary>
        /// 角色显示用的名字。
        /// </summary>
        public string DisplayName { get; set; }


    }
}
