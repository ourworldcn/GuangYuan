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
            var headId = Guid.Parse("{A06B7496-F631-4D51-9872-A2CC84A56EAB}");
            var bodyId = Guid.Parse("{7D191539-11E1-49CD-8D0C-82E3E5B04D31}");
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

    }
}
