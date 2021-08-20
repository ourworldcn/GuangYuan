using GuangYuan.GY001.UserDb;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Game.Social
{
    /// <summary>
    /// 社交相关对象的传输基类。
    /// MailTypeId={邮件类型Id},wood=木材数量，gold=金币数量，iswin=是否胜利
    /// </summary>
    [DataContract]
    public partial class GameSocialBaseDto
    {
        public GameSocialBaseDto()
        {

        }

        /// <summary>
        /// Id。这个属性指代主体的Id。角色A的Id放在这里，表示这个实体是角色A的。
        /// </summary>
        [DataMember]
        public string Id { get; set; }

        private Dictionary<string, object> _Properties;
        /// <summary>
        /// 属性字典。
        /// </summary>
        [DataMember]
        public Dictionary<string, object> Properties { get => _Properties ?? (_Properties = new Dictionary<string, object>()); set => _Properties = value; }

    }

    /// <summary>
    /// 邮件对象。
    /// </summary>
    [DataContract]
    public partial class GameMailDto : GameSocialBaseDto
    {

        /// <summary>
        /// 获取或设置此邮件的主题行。
        /// </summary>
        [DataMember]
        public string Subject { get; set; }

        /// <summary>
        /// 获取或设置邮件正文。
        /// </summary>
        [DataMember]
        public string Body { get; set; }

        /// <summary>
        /// 获取或设置创建该邮件的时间。UTC时间。
        /// </summary>
        [DataMember]
        public DateTime CreateUtc { get; set; } = DateTime.UtcNow;

        #region 地址相关

        /// <summary>
        /// 发件人地址。
        /// </summary>
        [DataMember]
        public GameMailAddressDto From { get; set; } = new GameMailAddressDto();

        /// <summary>
        /// 收件人地址列表。
        /// </summary>
        [DataMember]
        public List<GameMailAddressDto> To { get; set; } = new List<GameMailAddressDto>();

        #endregion 地址相关

        [DataMember]
        /// <summary>
        /// 附件的集合。
        /// </summary>
        public List<GameMailAttachmentDto> Attachmentes { get; set; } = new List<GameMailAttachmentDto>();

    }

    /// <summary>
    /// 邮件地址的传输对象。
    /// </summary>
    [DataContract]
    public partial class GameMailAddressDto
    {
        [DataMember]
        public string Id { get; set; }

        /// <summary>
        /// 获取由创建此实例时指定的显示名和地址信息构成的显示名。
        /// </summary>
        [DataMember]
        public string DisplayName { get; set; }

        /// <summary>
        /// 这个地址代表的事物的Id。可能是玩家Id、团体Id等。
        /// </summary>
        [DataMember]
        public string ThingId { get; set; }

        /// <summary>
        /// 这个地址的类型。
        /// </summary>
        [DataMember]
        public MailAddressKind Kind { get; set; }
    }

    /// <summary>
    /// 邮件附件的传输对象。
    /// 针对本项目，Properties 里的键值说明，
    /// TId={物品模板Id},HTId={头模板Id},BTId={身体模板Id},Count=物品数量，PTId=物品所属容器的模板Id,neatk=攻击资质,nemhp=血量资质,neqlt=质量资质。
    /// </summary>
    [DataContract]
    public partial class GameMailAttachmentDto : GameSocialBaseDto
    {
        public GameMailAttachmentDto() : base()
        {
        }

        /// <summary>
        /// 是否已经被删除。true该附件已经被删除。false该附件有效。
        /// </summary>
        [DataMember]
        public bool IdDeleted { get; set; }
    }

    /// <summary>
    /// 社交对象用到的常量封装类。
    /// </summary>
    static public class SocialConstant
    {
        /// <summary>
        /// 这是一个例子。
        /// </summary>
        public static readonly Guid xxxId = new Guid("{FC7D6CC7-D5B8-4495-A790-5E1F7708ADAA}");

        /// <summary>
        /// 指代群发的Id，<see cref="GameMailAddressDto.ThingId"/>是这个id则指代所有玩家。
        /// </summary>
        public static readonly Guid ToAllId = new Guid("{DA5B83F6-BB73-4961-A431-96177DE82BFF}");

        /// <summary>
        /// <see cref="GameMailAddressDto.ThingId"/>是这个id指代系统作为发送者,未来可能细分不同子系统，不过目前没需求。
        /// </summary>
        public static readonly Guid FromSystemId = new Guid("{21B9A80F-9F48-410A-806E-1709AD102520}");

        /// <summary>
        /// 好友槽模板Id。
        /// </summary>
        public static readonly Guid FriendSlotTId = new Guid("{7396db31-1d02-43d3-af05-c14f4ca2a5fc}");

        /// <summary>
        /// 邮件动态属性键名，该名的值不为0则说明是系统发送邮件。
        /// </summary>
        public const string FromSystemPNmae = "FromSystem";

        /// <summary>
        /// 附件物品的模板Id的键名。
        /// </summary>
        public const string SentTIdPName = "tid";

        /// <summary>
        /// 附件物品的数量的键名。
        /// </summary>
        public const string SentCountPName = "count";

        /// <summary>
        /// 附件物品的放入容器的模板Id的键名。
        /// </summary>
        public const string SentDestPTIdPName = "ptid";

        /// <summary>
        /// 双方是否已经确认该关系。附属在社交关系对象上。
        /// 0申请中，1已经确认。
        /// </summary>
        public const string ConfirmedFriendPName = "Confirmed";

    }

    /// <summary>
    /// 请求添加好友的返回值。
    /// </summary>
    [DataContract]
    public enum RequestFriendResult
    {
        /// <summary>
        /// 一般性未知错误。
        /// </summary>
        [DataMember]
        UnknowError = -1,

        /// <summary>
        /// 成功添加。
        /// </summary>
        [DataMember]
        Success = 0,

        /// <summary>
        /// 已经是好友。
        /// </summary>
        [DataMember]
        Already,

        /// <summary>
        /// 已经在请求中。
        /// </summary>
        [DataMember]
        Doing,

        /// <summary>
        /// 指定玩家不存在。
        /// </summary>
        [DataMember]
        NotFoundThisChar,

        /// <summary>
        /// 要添加为好友的用户不存在。
        /// </summary>
        [DataMember]
        NotFoundObjectChar,

        /// <summary>
        /// 对方已经将你拖入黑名单。
        /// </summary>
        [DataMember]
        BlackList,

        /// <summary>
        /// 自己将对方已经拖入黑名单。
        /// </summary>
        [DataMember]
        AlreadyBlack,
    }

}