using Game.Social;
using OwGame;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace GY2021001DAL
{
    public class GameMail : GameSocialBase, ICloneable
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

        /// <summary>
        /// 所有已经标记为删除的Id集合。
        /// 如果用户标记删除邮件，则其Id会加入这里。
        /// 当前版本保留未用，适用于大量长期存在的管理员群发邮件的情况，当前需求不存在这种情况。
        /// </summary>
        public byte[] IdsOfMarkDeleteStream { get; set; }

        List<Guid> _IdsOfMarkDelete;
        /// <summary>
        /// 保留未用，<seealso cref="IdsOfMarkDeleteStream"/>
        /// </summary>
        public List<Guid> IdsOfMarkDelete
        {
            get
            {
                lock (ThisLocker)
                    if (null == _IdsOfMarkDelete)
                    {
                        _IdsOfMarkDelete = new List<Guid>();
                        for (int i = 0; i < IdsOfMarkDeleteStream.Length; i += 8)
                        {
                            if (i + 8 >= IdsOfMarkDeleteStream.Length)
                                break;  //容错
                            _IdsOfMarkDelete.Add(new Guid(IdsOfMarkDeleteStream[i..(i + 8)]));
                        }
                    }
                return _IdsOfMarkDelete;
            }
        }
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
                lock (ThisLocker)
                    return _Attachmentes ??= new List<GameMailAttachment>();
            }
        }

        public void InvokeSaving(EventArgs e)
        {
            OnSaving(e);
        }

        protected virtual void OnSaving(EventArgs e)
        {

        }

        /// <summary>
        /// 复制一个深表副本。
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            var result = new GameMail()
            {
                Id = Guid.NewGuid(),
                Subject = Subject,
                Body = Body,
                CreateUtc = DateTime.UtcNow,
                _Attachmentes = new List<GameMailAttachment>(Attachmentes.Select(c => (GameMailAttachment)c.Clone())),
                _Addresses = new List<GameMailAddress>(Addresses),
            };
            foreach (var item in Properties)    //复制属性值
            {
                result.Properties[item.Key] = item.Value;
            }
            return result;
        }
    }

    /// <summary>
    /// 表示邮件发件人或收件人的地址。
    /// </summary>

    public class GameMailAddress : GuidKeyBase
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
    /// </summary>
    public class GameMailAttachment : GameSocialBase, ICloneable
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
        /// 复制一个深表副本，但没有设置导航相关的属性。
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            var result = new GameMailAttachment()
            {
                Id = Guid.NewGuid(),

            };
            foreach (var item in Properties)    //复制属性值
            {
                result.Properties[item.Key] = item.Value;
            }
            return result;
        }
    }
}
