using Game.Social;
using GY2021001DAL;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        GY2021001DbContext _DbContext;

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

            var coll = db.MailAddress.Where(c => c.ThingId == gcId && c.Kind == MailAddressKind.To).Select(c => c.Mail).Distinct();
            return coll.Include(c => c.MailAddresses).Include(c => c.Attachmentes).ToList();
        }

        public void Remove(GameChar gameChar, IEnumerable<Guid> mailIds)
        {

        }

        /// <summary>
        /// 发送邮件。
        /// </summary>
        /// <param name="mail"></param>
        /// <param name="result"></param>
        public void AddMails(GameMail mail, GameChar result = null)
        {
            using var db = World.CreateNewUserDbContext();
            db.Mails.Add(mail);
            db.SaveChanges();
        }
    }
}
