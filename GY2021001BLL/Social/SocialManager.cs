using Game.Social;
using GuangYuan.GY001.BLL.Homeland;
using GuangYuan.GY001.BLL.Social;
using GuangYuan.GY001.UserDb;
using Microsoft.EntityFrameworkCore;
using OW.Game;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace GuangYuan.GY001.BLL
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
        }

        #endregion 构造函数及相关

        #region 邮件及相关

        /// <summary>
        /// 获取指定角色的所有有效邮件。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        private IEnumerable<GameMail> GetMails(GameChar gameChar, DbContext db)
        {
            var result = new List<GameMail>();
            var gcId = gameChar.Id;

            var coll = db.Set<GameMailAddress>().Where(c => c.ThingId == gcId && !c.IsDeleted && c.Kind == MailAddressKind.To).Select(c => c.Mail).Distinct();

            return coll;
        }

        /// <summary>
        /// 获取指定用户的所有邮件。
        /// </summary>
        /// <returns></returns>
        public List<GameMail> GetMails(GameChar gameChar)
        {
            var db = World.CreateNewUserDbContext();
            try
            {
                var coll = GetMails(gameChar, db);
                var result = coll.ToList();
                FillAttachmentes(gameChar.Id, result.SelectMany(c => c.Attachmentes), db);
                return result;
            }
            finally
            {
                Task.Delay(4000).ContinueWith((c, dbObj) => (dbObj as DbContext)?.DisposeAsync(), db, TaskContinuationOptions.ExecuteSynchronously);
            }
        }

        /// <summary>
        /// 修正附件的一些属性。
        /// </summary>
        /// <param name="gcId">角色Id。</param>
        /// <param name="attachments"></param>
        /// <param name="db"></param>
        private void FillAttachmentes(Guid gcId, IEnumerable<GameMailAttachment> attachments, DbContext db)
        {
            foreach (var item in attachments)
            {
                item.SetDeleted(gcId);
            }
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
                var coll = db.Set<GameMailAddress>().Where(c => c.ThingId == gcId && c.Kind == MailAddressKind.To).Where(c => mailIds.Contains(c.MailId));
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
                mail.Properties[SocialConstant.FromSystemPNmae] = decimal.One;
            }
            else
            {
                var tmpChar = db.GameChars.Find(sendId);
                if (tmpChar is null)
                    throw new ArgumentException("找不到指定Id的角色。", nameof(sendId));
                sender = new GameMailAddress()
                {
                    ThingId = sendId,
                    DisplayName = tmpChar.DisplayName,
                    Kind = MailAddressKind.From,
                    IsDeleted = false,
                    Mail = mail,
                    MailId = mail.Id,
                };
                mail.Properties[SocialConstant.FromSystemPNmae] = decimal.Zero;
            }
            //追加相关人
            mail.Addresses.AddRange(tos);
            mail.Addresses.Add(sender);
            db.Mails.Add(mail);
            db.SaveChangesAsync().ContinueWith((task, dbPara) => (dbPara as DbContext)?.DisposeAsync(), db, TaskContinuationOptions.ExecuteSynchronously);  //清理
        }

        /// <summary>
        /// 用指定的物品作为附件发送邮件。
        /// </summary>
        /// <param name="mail">要发送的邮件。</param>
        /// <param name="tos">收件人列表。</param>
        /// <param name="senderId">发件人。</param>
        /// <param name="gameItems">(要发送的物品，父容器模板Id) 要发送的附件，会追加到附加集合中。不会自动销毁这些物品，调用者要自行处理。</param>
        public void SendMail(GameMail mail, IEnumerable<Guid> tos, Guid senderId, IEnumerable<(GameItem, Guid)> gameItems)
        {
            var gim = World.ItemManager;
            foreach (var item in gameItems)
            {
                // TId={物品模板Id},HTId={头模板Id},BTId={身体模板Id},Count=物品数量，PTId=物品所属容器的模板Id,neatk=攻击资质,nemhp=血量资质,neqlt=质量资质。
                var att = new GameMailAttachment();
                var gi = item.Item1;
                if (gim.IsMounts(gi))  //若是动物
                {
                    att.Properties["HTId"] = gim.GetHeadTemplate(gi).Id.ToString();
                    att.Properties["BTId"] = gim.GetBodyTemplate(gi).Id.ToString();
                    att.Properties["neatk"] = gi.Properties.GetValueOrDefault("neatk");
                    att.Properties["nemhp"] = gi.Properties.GetValueOrDefault("nemhp");
                    att.Properties["neqlt"] = gi.Properties.GetValueOrDefault("neqlt");
                }
                else
                {
                    att.Properties["TId"] = gi.TemplateId.ToString();
                }
                att.Properties["Count"] = gi.Count ?? 1;
                att.Properties["PTId"] = item.Item2.ToString();
                mail.Attachmentes.Add(att);
            }
            SendMail(mail, tos, senderId);
        }

        /// <summary>
        /// 领取附件。
        /// </summary>
        /// <param name="attachmentesIds">附件Id集合。</param>
        /// <param name="gameChar"></param>
        /// <param name="changes"></param>
        public bool GetAttachmentes(IEnumerable<Guid> attachmentesIds, GameChar gameChar, IList<ChangeItem> changes = null)
        {
            var db = World.CreateNewUserDbContext();
            ValueTuple<Guid, decimal, Guid>[] templates;
            try
            {
                //附件Id是IdMark.Id,角色Id是IdMark.Id
                var gcId = gameChar.Id;
                var mails = GetMails(gameChar, db);  //角色的所有邮件
                var coll = mails.SelectMany(c => c.Attachmentes).ToList();
                var atts = (from id in attachmentesIds
                            join att in coll
                            on id equals att.Id
                            select att    //所有属于该玩家且被指定的附件
                ).ToList();
                if (atts.Count < attachmentesIds.Count())
                {
                    VWorld.SetLastErrorMessage("至少有一个附件不属于指定角色。");
                    return false;
                };
                atts.ForEach(c => c.SetDeleted(gcId));

                if (atts.Any(c => c.IdDeleted))
                {
                    VWorld.SetLastErrorMessage("至少有一个附件已经被领取。");
                    return false;
                }
                atts.ForEach(c => c.RemovedIds.Add(gcId));  //标记已经删除
                templates = atts.Select(c =>
                {
                    return ValueTuple.Create(c.Properties.GetGuidOrDefault(SocialConstant.SentTIdPName, Guid.Empty),
                     c.Properties.GetDecimalOrDefault(SocialConstant.SentCountPName, decimal.Zero),
                     c.Properties.GetGuidOrDefault(SocialConstant.SentDestPTIdPName, Guid.Empty));
                }).ToArray();
            }
            finally
            {
                Task.Delay(1000).ContinueWith((c, dbObj) => (dbObj as DbContext)?.DisposeAsync(), db, TaskContinuationOptions.ExecuteSynchronously);
            }
            //修正玩家数据
            if (!World.CharManager.Lock(gameChar.GameUser))
            {
                VWorld.SetLastErrorMessage("无法锁定玩家。");
                return false;
            }
            try
            {
                var gim = World.ItemManager;
                foreach (var item in templates) //遍历需要追加的物品
                {
                    var gameItem = gim.CreateGameItem(item.Item1);  //物品管理器
                    gameItem.Count = item.Item2;
                    GameObjectBase parent = gameChar.AllChildren.FirstOrDefault(c => c.TemplateId == item.Item3);
                    parent ??= gameChar;
                    gim.AddItem(gameItem, parent, null, changes);
                }
                World.CharManager.NotifyChange(gameChar.GameUser);
            }
            finally
            {
                World.CharManager.Unlock(gameChar.GameUser, true);
            }
            return true;
        }
        #endregion  邮件及相关

        #region 黑白名单相关

        /// <summary>
        /// 获取指定角色与其他角色之间关系的对象集合的延迟查询。
        /// </summary>
        /// <param name="charId"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public IQueryable<GameSocialRelationship> GetCharRelationship(Guid charId, DbContext db) =>
            db.Set<GameSocialRelationship>().Where(c => c.Flag >= SocialConstant.MinFriendliness && c.Flag <= SocialConstant.MaxFriendliness && c.Id == charId);

        /// <summary>
        /// 获取与指定id为目标客体的所有角色关系集合。
        /// </summary>
        /// <param name="charId"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public IQueryable<GameSocialRelationship> GetCharRelationshipToMe(Guid charId, DbContext db) =>
           db.Set<GameSocialRelationship>().Where(c => c.Id2 == charId && c.Flag >= SocialConstant.MinFriendliness && c.Flag <= SocialConstant.MaxFriendliness);

        /// <summary>
        /// 获取正在申请成为好友的关系对象集合的延迟查询。
        /// </summary>
        /// <param name="charId"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public IQueryable<GameSocialRelationship> GetRequestingToMe(Guid charId, DbContext db) =>
            GetCharRelationshipToMe(charId, db).Where(c => c.Flag > SocialConstant.MiddleFriendliness + 5 &&
            c.PropertiesString.Contains($"{SocialConstant.ConfirmedFriendPName}=0"));

        /// <summary>
        /// 获取一组Id集合，正在申请好友的角色Id集合。这些用户不能申请好友。因为他们是以下情况之一：
        /// 1.正在申请好友;2.已经是好友;3.将当前用户拉黑。
        /// </summary>
        /// <param name="charId">当前角色Id。</param>
        /// <param name="db">使用的数据库上线文。</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IQueryable<Guid> GetFriendsOrRequestingOrBlackIds(Guid charId, DbContext db) =>
             GetCharRelationshipToMe(charId, db).Where(c => c.Flag > SocialConstant.MiddleFriendliness + 5 && c.Flag < SocialConstant.MiddleFriendliness - 5).Select(c => c.Id);

        /// <summary>
        /// 获取活跃用户的延迟查询。
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public IQueryable<GameChar> GetActiveChars(DbContext db) =>
             db.Set<GameChar>().Join(db.Set<GameActionRecord>(), c => c.Id, c => c.ParentId, (l, r) => new { l, r }).OrderByDescending(c => c.r.DateTimeUtc).Select(c => c.l);

        /// <summary>
        /// 获取一组角色的摘要数据。
        /// </summary>
        /// <param name="ids">要获得摘要信息的角色Id集合。</param>
        /// <param name="db">使用的用户数据库上下文。</param>
        /// <returns>指定的角色摘要信息。</returns>
        /// <exception cref="ArgumentException">至少一个指定的Id不是有效角色Id。</exception>
        public IEnumerable<CharSummary> GetCharSummary(IEnumerable<Guid> ids, DbContext db)
        {
            var gameChars = db.Set<GameChar>().Where(c => ids.Contains(c.Id)).ToArray();
            if (gameChars.Length != ids.Count())
                throw new ArgumentException("至少一个指定的Id不是有效角色Id。",nameof(ids));
            var result = gameChars.Select(c =>
            {
                var cs = new CharSummary();
                FillCharSummary(c, cs, db);
                return cs;
            });
            return result.ToList();
        }

        /// <summary>
        /// 获取一组可以申请好友的角色信息。
        /// </summary>
        /// <param name="datas"></param>
        /// <returns>目前最多返回5条。</returns>
        public void GetCharIdsForRequestFriend(GetCharIdsForRequestFriendDatas datas)
        {
            datas.DbContext ??= World.CreateNewUserDbContext();
            var db = datas.DbContext;
            var data = new FriendDatas(World, datas.GameChar, DateTime.UtcNow);
            IEnumerable<Guid> result;
            if (!string.IsNullOrWhiteSpace(datas.DisplayName))   //若需要按角色昵稱过滤
            {
                result = db.Set<GameChar>().Where(c => c.DisplayName == datas.DisplayName).Select(c => c.Id).Take(5).ToList();
            }
            else //若需要按身体模板Id过滤或不限制
            {
                if (data.HasData && datas.DonotRefresh)  //若不需要刷新数据
                    result = data.LastListIds;
                else
                    result = data.RefreshLastList(datas.BodyTIds).Take(5);
                data.TodayIds.AddRange(result);
            }
            data.Save();
            datas.CharIds.AddRange(result);
        }

        public void FillCharSummary(GameChar gameChar, CharSummary summary, DbContext db)
        {
            summary.Id = gameChar.Id;
            summary.DisplayName = gameChar.DisplayName;
            summary.Level = (int)gameChar.Properties.GetDecimalOrDefault("lv", decimal.Zero);
            summary.CombatCap = 4000;
            summary.LastLogoutDatetime = gameChar.SpecificExpandProperties.LastLogoutUtc == new DateTime(9999, 1, 1) ? new DateTime?() : gameChar.SpecificExpandProperties.LastLogoutUtc;
            var collMountIds = db.Set<GameSocialRelationship>().Where(c => c.Id == gameChar.Id && c.Flag == SocialConstant.HomelandShowFlag).Select(c => c.Id2);
            var coll = db.Set<GameItem>().Where(c => collMountIds.Contains(c.Id));
            summary.HomelandShows.AddRange(coll);
        }

        /// <summary>
        /// 请求加好友。
        /// 如果对方已经请求添加自己为好友，则这个函数会自动确认。
        /// </summary>
        /// <param name="gameChar">请求添加好友的角色对象。</param>
        /// <param name="friendId">请求加好友的Id。</param>
        /// <returns>0成功发送请求。-1无法锁定账号。
        /// -2对方Id不存在。</returns>
        public RequestFriendResult RequestFriend(GameChar gameChar, Guid friendId)
        {
            if (!World.CharManager.Lock(gameChar.GameUser))
            {
                VWorld.SetLastErrorMessage($"无法锁定指定玩家，Id={gameChar.Id}。");
                return RequestFriendResult.NotFoundThisChar;
            }
            GY001UserContext db = null;
            try
            {
                db = World.CreateNewUserDbContext();
                var objChar = db.GameChars.Find(friendId);  //要请求的角色对象。
                if (objChar is null)
                {
                    VWorld.SetLastErrorMessage($"找不到指定角色的角色，Id={friendId}。");
                    return RequestFriendResult.NotFoundObjectChar;
                }
                var sr = db.SocialRelationships.Find(gameChar.Id, objChar.Id);  //关系对象
                var nsr = db.SocialRelationships.Find(objChar.Id, gameChar.Id); //对方的关系对象
                if (nsr != null && nsr.IsBlack()) //若对方已经把当前用户加入黑名单
                    return RequestFriendResult.BlackList;
                if (sr is null) //若尚无该关系对象
                {
                    sr = new GameSocialRelationship()
                    {
                        Id = gameChar.Id,
                        Id2 = friendId,
                    };
                    sr.SetFriend();
                    sr.Properties[SocialConstant.ConfirmedFriendPName] = decimal.Zero;
                    db.SocialRelationships.Add(sr);
                }
                else //若是已经存在的对象
                {
                    if (sr.IsBlack())  //黑名单
                        return RequestFriendResult.AlreadyBlack;
                    else
                    {
                        var alreay = sr.Properties.GetDecimalOrDefault(SocialConstant.ConfirmedFriendPName, decimal.Zero);
                        if (alreay == 0m)   //正在申请
                            return RequestFriendResult.Doing;
                        else if (sr.IsFriendOrRequesting()) //已经是好友
                            return RequestFriendResult.Already;
                    }
                    //其他状况
                    sr.Properties[SocialConstant.ConfirmedFriendPName] = decimal.Zero;
                    sr.SetFriend();    //设置好友关系
                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception)
                {
                    return RequestFriendResult.UnknowError;
                }
                World.CharManager.Nope(gameChar.GameUser);  //重置下线计时器
            }
            finally
            {
                db?.DisposeAsync();
                World.CharManager.Unlock(gameChar.GameUser);
            }
            return RequestFriendResult.Success;
        }

        /// <summary>
        /// 获取社交关系列表。😀 👌
        /// Confirmed。
        /// </summary>
        public IEnumerable<GameSocialRelationship> GetSocialRelationships(GameChar gameChar)
        {
            if (!World.CharManager.Lock(gameChar.GameUser))
            {
                VWorld.SetLastErrorMessage($"无法锁定指定玩家，Id={gameChar.Id}。");
                return null;
            }
            GY001UserContext db = null;
            try
            {
                db = World.CreateNewUserDbContext();
                var gcId = gameChar.Id;
                var coll1 = db.SocialRelationships.Where(c => c.Id == gcId);
                var coll2 = GetRequestingToMe(gcId, db);    //请求添加
                var result = coll1.Union(coll2).ToArray();
                World.CharManager.Nope(gameChar.GameUser);  //重置下线计时器
                return result;
            }
            finally
            {
                db?.DisposeAsync();
                World.CharManager.Unlock(gameChar.GameUser, true);
            }
        }

        /// <summary>
        /// 确认或拒绝好友申请。
        /// </summary>
        /// <param name="gameChar">当前角色。</param>
        /// <param name="friendId">申请人Id。</param>
        /// <param name="rejected">true拒绝好友申请。</param>
        /// <returns></returns>
        public bool ConfirmFriend(GameChar gameChar, Guid friendId, bool rejected = false)
        {
            if (!World.CharManager.Lock(gameChar.GameUser))
            {
                VWorld.SetLastErrorMessage($"无法锁定指定玩家，Id={gameChar.Id}。");
                return false;
            }
            GY001UserContext db = null;
            try
            {
                db = World.CreateNewUserDbContext();
                var gcId = gameChar.Id;
                var sr = db.SocialRelationships.Find(gcId, friendId);
                var nsr = db.SocialRelationships.Find(friendId, gcId);
                if (nsr is null || nsr.IsFriendOrRequesting())   //若未申请好友
                    return false;
                if (sr is null)  //若未建立关系
                {
                    sr = new GameSocialRelationship()
                    {
                        Id = gcId,
                        Id2 = friendId,
                    };
                    sr.SetFriend();
                    db.SocialRelationships.Add(sr);
                }
                if (rejected)   //若拒绝
                {
                    db.SocialRelationships.Remove(sr);
                    db.SocialRelationships.Remove(nsr);
                }
                else
                {
                    var slot = gameChar.GameItems.First(c => c.TemplateId == SocialConstant.FriendSlotTId);
                    if (slot.GetNumberOfStackRemainder() <= 0)
                    {
                        VWorld.SetLastErrorMessage("好友位已满。");
                        return false;
                    }
                    sr.Properties[SocialConstant.ConfirmedFriendPName] = decimal.One;
                    nsr.Properties[SocialConstant.ConfirmedFriendPName] = decimal.One;
                    slot.Count++;
                }
                db.SaveChanges();
            }
            finally
            {
                db?.DisposeAsync();
                World.CharManager.Unlock(gameChar.GameUser, true);
            }
            return true;
        }

        /// <summary>
        /// 移除好友。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="friendId"></param>
        /// <returns>true成功移除了好友关系。</returns>
        public bool RemoveFriend(GameChar gameChar, Guid friendId)
        {
            if (!World.CharManager.Lock(gameChar.GameUser))
            {
                VWorld.SetLastErrorMessage($"无法锁定指定玩家，Id={gameChar.Id}。");
                return false;
            }
            GY001UserContext db = null;
            try
            {
                db = World.CreateNewUserDbContext();
                var gcId = gameChar.Id;
                var sr = db.SocialRelationships.Find(gcId, friendId);
                var nsr = db.SocialRelationships.Find(friendId, gcId);
                if (null != sr)
                {
                    if (sr.IsFriend())    //确定是好友关系
                    {
                        var slot = gameChar.GameItems.First(c => c.TemplateId == SocialConstant.FriendSlotTId);
                        slot.Count--;
                    }
                    sr.SetNeutrally();
                    sr.Properties[SocialConstant.ConfirmedFriendPName] = decimal.One;
                }

                if (null != nsr)
                {
                    nsr.SetNeutrally();
                    nsr.Properties[SocialConstant.ConfirmedFriendPName] = decimal.One;
                }
                db.SaveChanges();
            }
            finally
            {
                db?.DisposeAsync();
                World.CharManager.Unlock(gameChar.GameUser, true);
            }
            return true;
        }

        public bool SetFrindless(GameChar gameChar, Guid objId)
        {
            if (!World.CharManager.Lock(gameChar.GameUser))
            {
                VWorld.SetLastErrorMessage($"无法锁定指定玩家，Id={gameChar.Id}。");
                return false;
            }
            GY001UserContext db = null;
            try
            {
                db = World.CreateNewUserDbContext();
                var gcId = gameChar.Id;
                var sr = db.SocialRelationships.Find(gcId, objId);
                var nsr = db.SocialRelationships.Find(objId, gcId);
                if (null != sr)
                {
                    sr.SetBlack();
                    sr.Properties[SocialConstant.ConfirmedFriendPName] = decimal.One;
                }
                if (null != nsr)
                {
                    nsr.SetNeutrally();
                    nsr.Properties[SocialConstant.ConfirmedFriendPName] = decimal.One;
                }
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                VWorld.SetLastErrorMessage("并发冲突，请重试一次。");
                return false;
            }
            finally
            {
                db?.DisposeAsync();
                World.CharManager.Unlock(gameChar.GameUser, true);
            }
            return true;

        }

        #endregion 黑白名单相关

        #region 项目特定功能

        /// <summary>
        /// 去好友家互动以获得体力。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="objectId"></param>
        public PatForTiliResult PatForTili(GameChar gameChar, Guid objectId)
        {
            if (!World.CharManager.Lock(gameChar.GameUser))
            {
                VWorld.SetLastErrorMessage($"无法锁定指定玩家，Id={gameChar.Id}。");
                return PatForTiliResult.Unknow; //一般不可能。
            }
            GY001UserContext db = null;
            try
            {
                db = World.CreateNewUserDbContext();
                var objChar = db.GameChars.Find(objectId);
                if (gameChar.Id == objectId || objChar is null)
                {
                    VWorld.SetLastErrorMessage("试图访问自己或指定的角色不存在");
                    return PatForTiliResult.Unknow;
                }
                DateTime dtNow = DateTime.UtcNow;   //当前时间
                var datas = new PatForTiliWrapper(gameChar, dtNow);
                var dt = dtNow;
                if (datas.Fcp.GetCurrentValue(ref dt) <= 0) //若已经用完访问次数
                    return PatForTiliResult.TimesOver;
                if (datas.Visitors.Any(c => c.Item1 == objectId))    //若访问过了
                    return PatForTiliResult.Already;
                //成功抚摸
                var sr = db.SocialRelationships.Find(objectId, objectId);
                if (sr is null)
                {
                    sr = new GameSocialRelationship()
                    {
                        Id = objectId,
                        Id2 = objectId,
                    };
                    db.SocialRelationships.Add(sr);
                }
                var count = sr.Properties.GetDecimalOrDefault("FriendCurrency", decimal.Zero);
                count += 5;
                sr.Properties["FriendCurrency"] = count;
                db.SaveChanges();   //TO DO
                //成功抚摸
                datas.Fcp.LastValue--;
                datas.Visitors.Add((objectId, dtNow));
                //db.SocialRelationships.dea.Local.Clear();
                World.CharManager.NotifyChange(gameChar.GameUser);  //通知状态发生了更改
            }
            finally
            {
                db?.DisposeAsync();
                World.CharManager.Unlock(gameChar.GameUser, true);
            }
            return PatForTiliResult.Success;
        }

        public class PatForTiliWrapper
        {
            private const string key = "patcountVisitors";

            public PatForTiliWrapper()
            {

            }

            /// <summary>
            /// 构造函数。
            /// </summary>
            /// <param name="gameChar"></param>
            /// <param name="now">按该时间点计算。</param>
            public PatForTiliWrapper(GameChar gameChar, DateTime now)
            {
                _GameChar = gameChar;
                _Now = now;
            }

            private readonly GameChar _GameChar;
            private readonly DateTime _Now;

            public GameItem Tili { get => _GameChar.GetTili(); }
            public FastChangingProperty Fcp { get => Tili.Name2FastChangingProperty["patcount"]; }


            public List<(Guid, DateTime)> Visitors
            {
                get
                {
                    var result = Tili.ExtendPropertyDictionary.GetOrAdd(key, c => new ExtendPropertyDescriptor()
                    {
                        Data = DateTime.UtcNow,
                        Name = c,
                        IsPersistence = true,
                        Type = typeof(List<(Guid, DateTime)>),
                    }).Data as List<(Guid, DateTime)>;
                    result.RemoveAll(c => c.Item2.Date != _Now.Date);   //去掉不是今天访问的角色Id
                    return result;
                }
            }

            private void Visitors_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            {

            }

            public void Save()
            {

            }
        }

        /// <summary>
        /// 与好友坐骑互动。
        /// </summary>
        /// <param name="datas">参数及返回值的封装类。</param>
        public void PatWithMounts(PatWithMountsDatas datas)
        {
            var currentMounts = datas.GameChar.GetMounetsFromId(datas.CurrentMountsId);
            if (currentMounts is null)
            {
                datas.HasError = true;
                datas.DebugMessage = $"找不到当前坐骑，Id={datas.CurrentMountsId}";
                return;
            }
            var view = new PatWithMountsWrapper(datas.GameChar);

            using var db = World.CreateNewUserDbContext();
            var gameItem = db.GameItems.Find(datas.MountsId);   //要互动的坐骑Id
            if (gameItem is null)    //若没有找到
            {
                datas.HasError = true;
                datas.DebugMessage = $"找不到要互动的坐骑";
                return;
            }
            if (!World.ItemManager.IsMounts(gameItem))  //若不是坐骑
            {
                datas.HasError = true;
                datas.DebugMessage = $"指定Id不是坐骑";
                return;
            }
            //找到对方Char对象
            GameItem tmp;
            for (tmp = gameItem; tmp.Parent != null; tmp = tmp.Parent) ;
            if (!tmp.OwnerId.HasValue)
            {
                datas.HasError = true;
                datas.DebugMessage = $"数据错误。";
                return;
            }
            var objChar = db.GameChars.Find(tmp.OwnerId.Value);
            if (objChar is null)
            {
                datas.HasError = true;
                datas.DebugMessage = $"找不到对方所属角色。";
                return;
            }
            DateTime now = DateTime.UtcNow;
            if (!view.TryVisit(objChar.Id, datas.MountsId, now))    //若失败
            {
                datas.HasError = true;
                datas.DebugMessage = VWorld.GetLastErrorMessage();
                return;
            }
            var vItem = view.Visitors.First(c => c.ItemId == datas.MountsId);
            if (vItem.Count >= 10)  //若需要合成
            {
                var outGameItem = World.BlueprintManager.FuhuaCore(datas.GameChar, currentMounts, gameItem);
                var shoulanBag = datas.GameChar.GetShoulanBag();
                var remainder = new List<GameItem>();
                World.ItemManager.AddItem(outGameItem, shoulanBag, remainder, datas.Changes);
                if (remainder.Count > 0)
                {
                    var t = World.ItemTemplateManager.GetTemplateFromeId(ProjectConstant.友情孵化补给动物);
                    var mail = new GameMail() { };
                    mail.Properties["MailTypeId"] = ProjectConstant.友情孵化补给动物.ToString();
                    SendMail(mail, new Guid[] { datas.GameChar.Id }, SocialConstant.FromSystemId,
                        new ValueTuple<GameItem, Guid>[] { (outGameItem, ProjectConstant.ShoulanSlotId) });
                    //{b4c30a07-2179-435e-b053-fd4b0c36251b} 友情孵化补给动物
                    datas.MailItems.AddToAdds(mail.Id, outGameItem);
                }
                vItem.Count = 0;
            }
            return;
        }

        /// <summary>
        /// 
        /// </summary>
        public class PatWithMountsDatas
        {
            /// <summary>
            /// 主动方角色。
            /// </summary>
            public GameChar GameChar { get; set; }

            /// <summary>
            /// 要互动的坐骑Id。
            /// </summary>
            public Guid MountsId { get; set; }

            /// <summary>
            /// 如果友好度已满，使用此坐骑与对方坐骑杂交。
            /// </summary>
            public Guid CurrentMountsId { get; set; }

            /// <summary>
            /// 是否有错误。
            /// </summary>
            public bool HasError { get; set; }

            /// <summary>
            /// 错误的提示信息。
            /// </summary>
            public string DebugMessage { get; set; }

            /// <summary>
            /// 通过邮件发送了物品集合。
            /// Changes属性都可能有数据（部分放入）。
            /// 其中<see cref="ChangesItem.ContainerId"/>是邮件的Id。
            /// </summary>
            public List<ChangeItem> MailItems { get; set; }

            /// <summary>
            /// 互动产生的野兽。如果条件不具备则是空集合。
            /// </summary>
            public List<ChangeItem> Changes { get; set; } = new List<ChangeItem>();

        }

        /// <summary>
        /// 好友坐骑互动的数据视图。
        /// </summary>
        public class PatWithMountsWrapper
        {
            /// <summary>
            /// 计数器的名字。挂在家园对象下。
            /// </summary>
            public const string FcpName = "PatWithMountsCounter";

            /// <summary>
            /// 扩展属性的名字。
            /// </summary>
            public const string ExPropName = "PatWithMountsExProp";

            /// <summary>
            /// 构造函数。
            /// </summary>
            /// <param name="gameChar"></param>
            public PatWithMountsWrapper(GameChar gameChar)
            {
                _Counter = gameChar.GetHomeland().Name2FastChangingProperty[FcpName];
                var epd = gameChar.ExtendPropertyDictionary.GetOrAdd(ExPropName, c => new ExtendPropertyDescriptor(new List<PatWithMountsItem>(), c, true));
                _Visitors = (List<PatWithMountsItem>)epd.Data;
            }

            private readonly FastChangingProperty _Counter;
            /// <summary>
            /// 剩余次数。
            /// </summary>
            public FastChangingProperty Counter => _Counter;

            private readonly List<PatWithMountsItem> _Visitors;
            /// <summary>
            /// 详细信息。
            /// </summary>
            public List<PatWithMountsItem> Visitors => _Visitors;

            /// <summary>
            /// 试图与指定玩家的指定座机互动。
            /// </summary>
            /// <param name="charId">要互动的坐骑所属的角色Id。</param>
            /// <param name="itemId">要互动坐骑的Id。</param>
            /// <param name="now">使用的时间点。</param>
            /// <returns>true成功互动，false因种种原因没有成功。仅会修改数据，不会有后续连锁动作。</returns>
            public bool TryVisit(Guid charId, Guid itemId, DateTime now)
            {
                var dt = now;
                if (Counter.GetCurrentValue(ref dt) < 1)
                {
                    VWorld.SetLastErrorMessage("访问次数用尽。");
                    return false;
                }
                var dtToday = dt.Date;
                if (Visitors.Any(c => c.CharId == charId && dtToday == c.LastDateTime.Date))  //若今日互动过的坐骑
                {
                    VWorld.SetLastErrorMessage("已经和该角色的宠物互动过。");
                    return false;
                }
                var item = Visitors.FirstOrDefault(c => c.CharId == charId && c.ItemId == itemId);  //访问过的数据项
                if (item is null)    //若没有访问过该坐骑
                {
                    item = new PatWithMountsItem()
                    {
                        CharId = charId,
                        ItemId = itemId,
                        Count = 1,
                        LastDateTime = now,
                    };
                    Visitors.Add(item);
                }
                else
                {
                    item.Count++;
                    item.LastDateTime = now;
                }
                //修正数据
                Counter.LastValue--;
                return true;
            }
        }

        /// <summary>
        /// 互动坐骑的记录数据项。
        /// </summary>
        [Serializable]
        public class PatWithMountsItem
        {
            /// <summary>
            /// 构造函数。
            /// </summary>
            public PatWithMountsItem()
            {

            }

            /// <summary>
            /// 对方角色Id。
            /// </summary>
            public Guid CharId { get; set; }

            /// <summary>
            /// 对方坐骑Id。
            /// </summary>
            public Guid ItemId { get; set; }

            /// <summary>
            /// 最后一次访问的时间。
            /// </summary>
            public DateTime LastDateTime { get; set; }

            /// <summary>
            /// 已经访问的次数。
            /// </summary>
            public int Count { get; set; }
        }

        /// <summary>
        /// 获取其他玩家的家园数据。
        /// </summary>
        public void GetHomelandData(GetHomelandDataDatas datas)
        {
            using var disposer = datas.LockUser(World.CharManager);
            if (disposer is null)
                return;
            var gc = datas.GameChar;
            var gim = World.ItemManager;
            using var db = World.CreateNewUserDbContext();
            try
            {
                var objChar = db.GameChars.Find(datas.OtherCharId);
                if (objChar is null)
                {
                    datas.HasError = true;
                    datas.DebugMessage = $"找不到指定Id的角色，Id{datas.OtherCharId}";
                    datas.ResultCode = (int)HttpStatusCode.BadRequest;
                    return;
                }
                //构造对象
                var objUser = db.GameUsers.Find(objChar.GameUserId);
                objUser.DbContext = db;
                objUser.CurrentChar = objChar;
                objChar.GameUser = objUser;
                var mountsBag = objChar.GetZuojiBag();
                //获取风格
                var gitm = World.ItemTemplateManager;
                var fengges = gc.GetFengges();
                if (fengges.Count == 0) //若未初始化
                    gc.MergeFangans(fengges, gitm);
                var fengge = fengges.FirstOrDefault(c => c.Fangans.Any(c1 => c1.IsActived));
                if (fengge is null) //若没有指定激活风格
                    fengge = fengges.First();
                datas.CurrentFengge = fengge;
                //获取家园地块数据
                var hl = objChar.GetHomeland();
                var dikuais = hl.AllChildren.Where(c => gitm.GetTemplateFromeId(c.TemplateId).CatalogNumber / 100 == 1);  //获取地块
                datas.Lands.AddRange(dikuais);
                //获取阵容数据
                var collMounts = gim.GetLineup(objChar, 10).Union(gim.GetLineup(objChar, 20))
                     .Union(gim.GetLineup(objChar, 21)).Union(gim.GetLineup(objChar, 22)).Union(gim.GetLineup(objChar, 23)).Union(gim.GetLineup(objChar, 24));
                datas.Mounts.AddRange(collMounts);
            }
            finally
            {
                Task.Delay(1000).ContinueWith((task, state) => (state as DbContext)?.DisposeAsync(), db, TaskContinuationOptions.ExecuteSynchronously);
            }
        }

        /// <summary>
        /// 获取该用户的指定指定日期的可pvp对象。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="now">utc时间，取其指定日期部分。</param>
        /// <returns>返回当前的pvp对象Id列表。如果没有则自动生成。</returns>
        public IEnumerable<GameActionRecord> GetPvpChars(GameChar gameChar, DateTime now)
        {
            const string pvpChar = "PvpChar";
            using var db = World.CreateNewUserDbContext();
            var dt = now.Date;
            var dt1 = dt + TimeSpan.FromDays(1);
            var coll = from tmp in db.ActionRecords
                       where tmp.DateTimeUtc >= dt.Date && tmp.DateTimeUtc < dt1 && tmp.ParentId == gameChar.Id && tmp.ActionId == pvpChar
                       select tmp;  //获取今日已经出现在列表中的数据。
            var lst = coll.ToList();
            List<GameActionRecord> result = new List<GameActionRecord>();
            var rankings = db.Set<CharSpecificExpandProperty>();
            if (lst.Count <= 0)    //若该日没有生成pvp对象数据
            {
                var count = 3;  //一次刷的数量
                var ranking = rankings.Find(gameChar.Id);
                var lv = gameChar.Properties.GetDecimalOrDefault(ProjectConstant.LevelPropertyName);
                var newColl = (from tmp in rankings
                               where tmp.PvpScore > ranking.PvpScore && tmp.CharLevel > lv && tmp.Id != gameChar.Id  //取上手
                               orderby tmp.PvpScore
                               select tmp).Take(count).Union(
                    (from tmp in rankings
                     where tmp.PvpScore >= ranking.PvpScore && tmp.Id != gameChar.Id
                     orderby tmp.PvpScore
                     select tmp).Take(count)).Union(
                    (from tmp in rankings
                     where tmp.PvpScore <= ranking.PvpScore && tmp.Id != gameChar.Id
                     orderby tmp.PvpScore descending
                     select tmp).Take(count)).Union(
                    (from tmp in rankings
                     where tmp.PvpScore < ranking.PvpScore && tmp.CharLevel < lv && tmp.Id != gameChar.Id    //取下手
                     orderby tmp.PvpScore descending
                     select tmp).Take(count));
                var lstResult = newColl.OrderBy(c => c.PveCScore).ThenBy(c => c.CharLevel).ToList(); //排行榜集合

                //获取下手
                if (lstResult.Count == 0)   //若没有角色
                    return result;
                var addItem = lstResult.FirstOrDefault(c => c.PveCScore < ranking.PveCScore && c.CharLevel < lv) ?? lstResult.First();
                result.Add(new GameActionRecord()
                {
                    ActionId = pvpChar,
                    DateTimeUtc = now,
                    ParentId = gameChar.Id,
                    PropertiesString = $"Id2={addItem.Id}"
                });
                lstResult.Remove(addItem);
                //获取平手
                if (lstResult.Count == 0)   //若已无角色
                    goto save;
                addItem = (lstResult.FirstOrDefault(c => c.PveCScore == ranking.PveCScore && c.CharLevel == lv) ?? lstResult.FirstOrDefault(c => c.PveCScore == ranking.PveCScore))
                    ?? lstResult.First();   //获取一个尽量相等的
                result.Add(new GameActionRecord()
                {
                    ActionId = pvpChar,
                    DateTimeUtc = now,
                    ParentId = gameChar.Id,
                    PropertiesString = $"Id2={addItem.Id}"
                });
                lstResult.Remove(addItem);
                //获取上手
                if (lstResult.Count == 0)   //若已无角色
                    goto save;
                addItem = (lstResult.LastOrDefault(c => c.PveCScore > ranking.PveCScore && c.CharLevel > lv) ?? lstResult.LastOrDefault(c => c.PveCScore > ranking.PveCScore))
                    ?? lstResult.Last();   //获取一个尽量相等的
                result.Add(new GameActionRecord()
                {
                    ActionId = pvpChar,
                    DateTimeUtc = now,
                    ParentId = gameChar.Id,
                    PropertiesString = $"Id2={addItem.Id}"
                });
            //lstResult.Remove(addItem);
            //追加新生成的数据
            save:
                db.ActionRecords.AddRange(result);
                db.SaveChanges();
            }
            else //若已经生成
            {
                result.AddRange(lst);
            }
            return result;
        }

        #endregion  项目特定功能
    }

    /// <summary>
    /// <see cref="GameSocialManager.GetHomelandData(GetHomelandDataDatas)"/>使用的工作数据封装类。
    /// </summary>
    public class GetHomelandDataDatas : ComplexWorkDatsBase
    {
        public GetHomelandDataDatas()
        {
        }

        /// <summary>
        /// 要获取的角色的Id。
        /// </summary>
        public Guid OtherCharId { get => Parameters.GetGuidOrDefault(nameof(OtherCharId)); set => Parameters[nameof(OtherCharId)] = value; }

        private HomelandFengge _CurrentFengge;

        /// <summary>
        /// 当前风格数据，下面仅含激活的方案数据。
        /// </summary>
        public HomelandFengge CurrentFengge { get => GetOrAdd(nameof(CurrentFengge), ref _CurrentFengge); set => Result[nameof(CurrentFengge)] = value; }

        private List<GameItem> _Mounts;

        /// <summary>
        /// 相关坐骑的数据。
        /// </summary>
        public List<GameItem> Mounts => GetOrAdd(nameof(Mounts), ref _Mounts);

        private List<GameItem> _Lands;
        /// <summary>
        /// 地块信息。
        /// </summary>
        public List<GameItem> Lands => GetOrAdd(nameof(Lands), ref _Lands);
    }

    public enum PatForTiliResult
    {
        /// <summary>
        /// 未知错误。
        /// </summary>
        Unknow = -1,

        /// <summary>
        /// 成功。
        /// </summary>
        Success = 0,

        /// <summary>
        /// 访问次数超限。
        /// </summary>
        TimesOver,

        /// <summary>
        /// 已经访问过该角色。
        /// </summary>
        Already,
    }

    public class GetCharIdsForRequestFriendDatas : ComplexWorkDatsBase
    {

        string _DisplayName;

        /// <summary>
        /// 指定搜索角色的昵称。为空表示不限定角色名。
        /// </summary>
        public string DisplayName
        {
            get => _DisplayName;
            set => _DisplayName = value;
        }

        /// <summary>
        /// 指定家园展示坐骑的身体模板Id。空集合表示限定。
        /// </summary>
        public List<Guid> BodyTIds { get; } = new List<Guid>();

        /// <summary>
        /// 返回的角色Id集合。
        /// </summary>
        public List<Guid> CharIds { get; } = new List<Guid>();

        /// <summary>
        /// 所使用的数据库上下文。
        /// </summary>
        public DbContext DbContext { get; set; }

        /// <summary>
        /// 仅当按身体模板过滤时，此属性才有效。
        /// false若今天曾经刷新过数据则返回该数据，true强制刷新数据返回。
        /// </summary>
        public bool DonotRefresh { get; set; }

        bool disposedValue;

        public GetCharIdsForRequestFriendDatas()
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    DbContext?.Dispose();
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                DbContext = null;

                disposedValue = true;
                base.Dispose(disposing);
            }
        }
    }

}
