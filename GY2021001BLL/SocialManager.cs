using Game.Social;
using GY2021001DAL;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GY2021001BLL
{
    public class SocialManagerOptions
    {
        public SocialManagerOptions()
        {

        }
    }

    /// <summary>
    /// 社交类功能管理器。
    /// </summary>
    public class GameSocialManager : GameManagerBase<SocialManagerOptions>
    {
        #region 构造函数及相关

        public GameSocialManager()
        {
            Initialize();
        }

        public GameSocialManager(IServiceProvider service) : base(service)
        {
            Initialize();
        }

        public GameSocialManager(IServiceProvider service, SocialManagerOptions options) : base(service, options)
        {
            Initialize();
        }

        /// <summary>
        /// 初始化函数。
        /// </summary>
        private void Initialize()
        {
            _DbContext = World.CreateNewUserDbContext();
        }

        /// <summary>
        /// 管理邮件的。
        /// </summary>
        private GY2021001DbContext _DbContext;

        #endregion 构造函数及相关

        /// <summary>
        /// 获取指定用户的所有邮件。
        /// </summary>
        /// <returns></returns>
        public List<GameMail> GetMails(GameChar gameChar)
        {
            using var db = World.CreateNewUserDbContext();
            var result = new List<GameMail>();
            var gcId = gameChar.Id;

            var coll = db.MailAddress.Where(c => c.ThingId == gcId && !c.IsDeleted && c.Kind == MailAddressKind.To).Select(c => c.Mail).Distinct();
            return coll.Include(c => c.Addresses).Include(c => c.Attachmentes).ToList();
        }

        /// <summary>
        /// 标记删除邮件。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="mailIds">要删除的邮件Id集合。</param>
        /// <returns>true指定Id的邮件已经全部删除，false由于至少一个Id无效或无法锁定角色而没有删除任何邮件。</returns>
        public bool RemoveMails(GameChar gameChar, IEnumerable<Guid> mailIds)
        {
            if (!World.CharManager.Lock(gameChar.GameUser))
                return false;
            Guid gcId;
            try
            {
                gcId = gameChar.Id;
                var db = World.CreateNewUserDbContext();
                var coll = db.MailAddress.Where(c => c.ThingId == gcId && c.Kind == MailAddressKind.To).Where(c => mailIds.Contains(c.MailId));
                foreach (var addr in coll)
                    addr.IsDeleted = true;
                db.SaveChangesAsync().ContinueWith((c, dbObj) => (dbObj as DbContext)?.DisposeAsync(), db, TaskContinuationOptions.ExecuteSynchronously);
            }
            finally
            {
                World.CharManager.Unlock(gameChar.GameUser, true);
            }
            return true;
        }

        /// <summary>
        /// 发送邮件。
        /// </summary>
        /// <param name="mail">除了相关人列表之外的内容要填写齐备。</param>
        /// <param name="to">收件角色Id集合。</param>
        /// <param name="sendId">发件人Id.</param>
        /// <param name="cc">抄送人列表，当前未用，保留未null或省略。</param>
        /// <param name="sc">密件抄送列表，当前未用，保留未null或省略。</param>
        public void SendMail(GameMail mail, IEnumerable<Guid> to, Guid sendId, IEnumerable<Guid> cc = null, IEnumerable<Guid> sc = null)
        {
            Trace.Assert(cc is null && sc is null);
            var db = World.CreateNewUserDbContext();
            mail.GenerateIdIfEmpty();
            IEnumerable<GameMailAddress> tos;
            if (to.Contains(SocialConstant.ToAllId)) //若是所有人群发所有人
            {
                tos = from gc in db.GameChars
                      select new GameMailAddress()
                      {
                          ThingId = gc.Id,
                          DisplayName = gc.DisplayName,
                          Kind = MailAddressKind.To,
                          IsDeleted = false,
                          Mail = mail,
                          MailId = mail.Id,
                      };
            }
            else //若是逐个群发
            {
                tos = from gc in db.GameChars
                      where to.Contains(gc.Id)
                      select new GameMailAddress()
                      {
                          ThingId = gc.Id,
                          DisplayName = gc.DisplayName,
                          Kind = MailAddressKind.To,
                          IsDeleted = false,
                          Mail = mail,
                          MailId = mail.Id,
                      };
            }
            GameMailAddress sender;
            if (SocialConstant.FromSystemId == sendId)  //若是系统发送
            {
                sender = new GameMailAddress()
                {
                    ThingId = SocialConstant.FromSystemId,
                    DisplayName = "System",
                    Kind = MailAddressKind.From,
                    IsDeleted = false,
                    Mail = mail,
                    MailId = mail.Id,
                };
                mail.Properties[SocialConstant.FromSystemPNmae] = 1m;
            }
            else
            {
                var tmpChar = db.GameChars.Find(sendId);
                if (tmpChar is null)
                    throw new ArgumentException("找不到指定Id的角色。", nameof(sendId));
                sender = new GameMailAddress()
                {
                    ThingId = SocialConstant.FromSystemId,
                    DisplayName = tmpChar.DisplayName,
                    Kind = MailAddressKind.From,
                    IsDeleted = false,
                    Mail = mail,
                    MailId = mail.Id,
                };
                mail.Properties[SocialConstant.FromSystemPNmae] = decimal.Zero;
            }
            //追加相关人
            mail.Addresses.AddRange(tos.Prepend(sender));
            db.Mails.Add(mail);
            db.SaveChangesAsync().ContinueWith((task, dbPara) => (dbPara as DbContext)?.DisposeAsync(), db, TaskContinuationOptions.ExecuteSynchronously);  //清理
        }

    }
}
