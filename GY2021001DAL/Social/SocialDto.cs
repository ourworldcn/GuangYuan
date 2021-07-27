using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Game.Social
{
    /// <summary>
    /// 社交相关对象的传输基类。
    /// </summary>
    [DataContract]
    public partial class GameSocialBaseDto
    {
        /// <summary>
        /// Id。
        /// </summary>
        [DataMember]
        public string Id { get; set; }

        /// <summary>
        /// 属性字典。
        /// </summary>
        [DataMember]
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 邮件地址的类型。
    /// </summary>
    public enum MailAddressKind
    {
        /// <summary>
        /// 该地址对象是一个发送人的地址。
        /// </summary>
        From = 1,
        /// <summary>
        /// 该地址对象是一个收件人的地址。
        /// </summary>
        To = 2,

        /// <summary>
        /// 保留未用。
        /// </summary>
        CC = 4,

        /// <summary>
        /// 保留未用。
        /// </summary>
        SC = 8,
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
    /// </summary>
    [DataContract]
    public partial class GameMailAttachmentDto : GameSocialBaseDto
    {


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

        public const string xxxPNmae = "";
    }
}