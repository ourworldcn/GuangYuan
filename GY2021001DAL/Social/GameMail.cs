using Microsoft.EntityFrameworkCore;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace GuangYuan.GY001.UserDb
{
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
    /// 
    /// </summary>
    public class GameMail : GameSocialBase
    {
        public GameMail()
        {
        }

        public GameMail(Guid id) : base(id)
        {
        }

        /// <summary>
        /// 获取或设置此邮件的主题行。
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// 获取或设置邮件正文。
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// 获取或设置创建该邮件的时间。UTC时间。
        /// </summary>
        public DateTime CreateUtc { get; set; } = DateTime.UtcNow;

        #region 地址相关

        private List<GameMailAddress> _Addresses;

        /// <summary>
        /// 获取或设置此邮件相关人的地址集合。
        /// </summary>
        virtual public List<GameMailAddress> Addresses
        {
            get
            {
                lock (ThisLocker)
                    return _Addresses ??= new List<GameMailAddress>();
            }
        }

        private GameMailAddress _From;


        [NotMapped]
        public GameMailAddress From
        {
            get
            {
                if (_From is null)
                    lock (ThisLocker)
                        if (_From is null)
                            _From = Addresses.First(c => c.Kind == MailAddressKind.From);
                return _From;
            }
        }

        private List<GameMailAddress> _To;

        /// <summary>
        /// 获取收件人列表，可能不包含已经标记删除的人员。
        /// </summary>
        [NotMapped]
        public List<GameMailAddress> To
        {
            get
            {
                if (_To is null)
                    lock (ThisLocker)
                        if (_To is null)
                        {
                            _To = Addresses.Where(c => c.Kind == MailAddressKind.To && !c.IsDeleted).ToList();
                        }
                return _To;
            }
        }


        #endregion 地址相关

        private List<GameMailAttachment> _Attachmentes;

        /// <summary>
        /// 附件的集合。
        /// </summary>
        virtual public List<GameMailAttachment> Attachmentes
        {
            get
            {
                if (_Attachmentes is null)
                    lock (ThisLocker)
                        _Attachmentes ??= new List<GameMailAttachment>();
                return _Attachmentes;
            }
        }

        public void InvokeSaving(EventArgs e)
        {
            OnSaving(e);
        }

        protected virtual void OnSaving(EventArgs e)
        {

        }

    }

    /// <summary>
    /// 表示邮件发件人或收件人的地址。
    /// </summary>
    [Table("MailAddresses")]
    public class GameMailAddress : GuidKeyObjectBase
    {
        public GameMailAddress()
        {

        }

        /// <summary>
        /// 获取由创建此实例时指定的显示名和地址信息构成的显示名。
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 这个地址代表的事物的Id。可能是玩家Id、团体Id等。
        /// </summary>
        public Guid ThingId { get; set; }

        /// <summary>
        /// 获取或设置此对象所属邮件的Id。
        /// </summary>
        [ForeignKey(nameof(Mail))]
        public Guid MailId { get; set; }

        /// <summary>
        /// 导航属性，此地址所属邮件。
        /// </summary>
        virtual public GameMail Mail { get; set; }

        /// <summary>
        /// 这个地址的类型。
        /// </summary>
        public MailAddressKind Kind { get; set; }

        /// <summary>
        /// 标记这个对象代表相关人已经标记删除该邮件。
        /// 没有特别要求则此邮件不会出现在该相关人的列表中。
        /// </summary>
        public bool IsDeleted { get; set; } = false;
    }

    /// <summary>
    /// 邮件附件类。
    /// 针对本项目，Properties 里的键值说明，
    /// TId={物品模板Id},HTId={头模板Id},BTId={身体模板Id},Count=物品数量，PTId=物品所属容器的模板Id,neatk=攻击资质,nemhp=血量资质,neqlt=质量资质。
    /// </summary>
    [Table("MailAttachmentes")]
    public class GameMailAttachment : GameSocialBase, IBeforeSave
    {
        /// <summary>
        /// 获取或设置此对象所属邮件的Id。
        /// </summary>
        [ForeignKey(nameof(Mail))]
        public Guid MailId { get; set; }

        /// <summary>
        /// 导航属性，此地址所属邮件。
        /// </summary>
        virtual public GameMail Mail { get; set; }

        /// <summary>
        /// 已经领取该附件的角色Id的集合。
        /// </summary>
        public byte[] ReceivedCharIds { get; set; } = Array.Empty<byte>();

        private List<Guid> _RemovedIds;
        [NotMapped]
        public List<Guid> RemovedIds
        {
            get
            {
                if (_RemovedIds is null)
                {
                    _RemovedIds = new List<Guid>();
                    var lengthe = 16;
                    for (int i = 0; i < ReceivedCharIds.Length; i += lengthe)
                    {
                        var tmp = ReceivedCharIds[i..(i + lengthe)];
                        _RemovedIds.Add(new Guid(tmp));
                    }
                }
                return _RemovedIds;
            }
        }

        public void SetDeleted(Guid charId) =>
            _IdDeleted = RemovedIds.Contains(charId);

        private bool _IdDeleted;

        /// <summary>
        /// 是否已经删除，这个属性仅在针对特定玩家时才有效。
        /// </summary>
        [NotMapped]
        public bool IdDeleted => _IdDeleted;

        public override void PrepareSaving(DbContext db)
        {
            if (null != _RemovedIds)
            {
                ReceivedCharIds = _RemovedIds.SelectMany(c => c.ToByteArray()).ToArray();
            }
            base.PrepareSaving(db);
        }
    }

}
