using OW.Game;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace GuangYuan.GY001.UserDb
{
    /// <summary>
    /// 社交关系对象。
    /// 一些附属信息放在<see cref="GameSocialBase.Properties"/>中。
    /// 本对象的Id是主体Id。
    /// 主体和客体对象是联合主键。<see cref="GameEntityRelationshipBase.KeyType"/> SocialKeyTypes 枚举类型。
    /// 当前以如下方法判断具体状态:
    /// </summary>
    public class GameSocialRelationship : GameSocialBase
    {
        #region 扩展属性名称定义
        #endregion 扩展属性名称定义

        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameSocialRelationship()
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="id">指定Id。</param>
        public GameSocialRelationship(Guid id) : base(id)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="id"></param>
        /// <param name="id2"></param>
        /// <param name="flag"></param>
        /// <param name="keyType"></param>
        public GameSocialRelationship(Guid id, Guid id2, int keyType, int flag) : base(id, id2, keyType, flag)
        {
        }

    }

}
