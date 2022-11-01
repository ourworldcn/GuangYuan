using Game.Social;
using GuangYuan.GY001.BLL.Homeland;
using GuangYuan.GY001.BLL.Social;
using GuangYuan.GY001.UserDb;
using GuangYuan.GY001.UserDb.Combat;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OW.Extensions.Game.Store;
using OW.Game;
using OW.Game.Caching;
using OW.Game.Item;
using OW.Game.Log;
using OW.Game.PropertyChange;
using OW.Game.Store;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GuangYuan.GY001.BLL
{
    public class SocialManagerOptions
    {
        public SocialManagerOptions()
        {

        }
    }

    public class GetGeneralCharSummaryDatas : RelationshipGameContext
    {
        public GetGeneralCharSummaryDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public GetGeneralCharSummaryDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public GetGeneralCharSummaryDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        private List<GameChar> _GameChars;

        /// <summary>
        /// 角色信息。
        /// </summary>
        public List<GameChar> GameChars => _GameChars ??= new List<GameChar>();

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                _GameChars = null;
                base.Dispose(disposing);
            }
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

        private Timer _Timer;

        protected Timer Timer => _Timer;

        private Timer _TimerOfDay;  //每日0点清理任务计时器

        /// <summary>
        /// 初始化函数。
        /// </summary>
        private void Initialize()
        {
            //清理邮件
            _Timer = new Timer(c =>
            {
                if (Environment.HasShutdownStarted)
                    return;
                var sql = "DELETE FROM [dbo].[Mails] WHERE DATEDIFF(day, [CreateUtc], GETUTCDATE())> 7";
                ((VWorld)c).AddToUserContext(sql);
            }, World, TimeSpan.Zero, TimeSpan.FromDays(1));
            DateTime utcNow = DateTime.UtcNow;
            _TimerOfDay = new Timer(c =>
            {
                foreach (var gc in World.CharManager.Id2GameChar.Values)
                {
                    using var dwUser = World.CharManager.LockAndReturnDisposer(gc.GameUser);
                    if (dwUser is null)
                        continue;
                    var list = World.CharManager.GetChangeData(gc);
                    var np = new ChangeData()
                    {
                        ActionId = 2,
                        NewValue = 0,
                        ObjectId = gc.Id,
                        OldValue = 0,
                        PropertyName = ProjectConstant.AllChanged.ToString(),
                        TemplateId = ProjectConstant.MailSlotTId,
                    };
                    np.Properties.Add("charId", gc.IdString);
                    list.Add(np);
                    World.CharManager.NotifyChange(gc.GameUser);
                }
            }, null, utcNow.Date + TimeSpan.FromDays(1) - utcNow, TimeSpan.FromDays(1));
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
        public void GetMails(GetMailsDatas datas)
        {
            using var dwUser = datas.LockUser();
            if (dwUser is null)
                return;
            var coll = GetMails(datas.GameChar, datas.UserDbContext);
            datas.Mails.AddRange(coll.ToList());
            FillAttachmentes(datas.GameChar.Id, datas.Mails.SelectMany(c => c.Attachmentes));
        }

        public class GetMailsDatas : ComplexWorkGameContext
        {
            public GetMailsDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
            {
            }

            public GetMailsDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
            {
            }

            public GetMailsDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
            {
            }

            private List<GameMail> _Mails;

            /// <summary>
            /// 返回的邮件。
            /// </summary>
            public List<GameMail> Mails => _Mails ??= new List<GameMail>();

        }

        /// <summary>
        /// 修正附件的一些属性。
        /// </summary>
        /// <param name="gcId">角色Id。</param>
        /// <param name="attachments"></param>
        /// 
        private void FillAttachmentes(Guid gcId, IEnumerable<GameMailAttachment> attachments)
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
        public virtual void SendMail([NotNull] GameMail mail, [NotNull] IEnumerable<Guid> to, Guid sendId, IEnumerable<Guid> cc = null, IEnumerable<Guid> sc = null)
        {
            try
            {
                mail.GenerateIdIfEmpty();
                IEnumerable<GameMailAddress> tos;
                using var db = World.CreateNewUserDbContext();
                if (to.Contains(SocialConstant.ToAllId)) //若是所有人群发所有人
                {
                    //tos = from gc in db.GameChars
                    //      select new GameMailAddress()
                    //      {
                    //          ThingId = gc.Id,
                    //          DisplayName = gc.DisplayName,
                    //          Kind = MailAddressKind.To,
                    //          IsDeleted = false,
                    //          Mail = mail,
                    //          MailId = mail.Id,
                    //      };
                    tos = from gc in db.GameChars.AsNoTracking()
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
                    //tos = from gc in db.GameChars
                    //      where to.Contains(gc.Id)
                    //      select new GameMailAddress()
                    //      {
                    //          ThingId = gc.Id,
                    //          DisplayName = gc.DisplayName,
                    //          Kind = MailAddressKind.To,
                    //          IsDeleted = false,
                    //          Mail = mail,
                    //          MailId = mail.Id,
                    //      };
                    tos = from tmp in to
                          select new GameMailAddress()
                          {
                              ThingId = tmp,
                              //DisplayName = gc.DisplayName,
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
                    mail.SetSdp(SocialConstant.FromSystemPNmae, decimal.One);
                }
                else
                {
                    //var tmpChar = db.GameChars.Find(sendId);
                    //if (tmpChar is null)
                    //    throw new ArgumentException("找不到指定Id的角色。", nameof(sendId));
                    var dn = db.Set<GameChar>().AsNoTracking().FirstOrDefault(c => c.Id == sendId)?.DisplayName;
                    sender = new GameMailAddress()
                    {
                        ThingId = sendId,
                        DisplayName = dn,
                        Kind = MailAddressKind.From,
                        IsDeleted = false,
                        Mail = mail,
                        MailId = mail.Id,
                    };
                    mail.SetSdp(SocialConstant.FromSystemPNmae, decimal.Zero);
                }
                //追加相关人
                mail.Addresses.AddRange(tos);
                mail.Addresses.Add(sender);
                World.AddToUserContext(new object[] { mail });
                //发送邮件到达通知
                var ids = tos.Select(c => c.ThingId).ToArray();
                Task.Run(() => NotifyMail(ids));
                OwHelper.SetLastError(ErrorCodes.NO_ERROR);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 发送邮件到达标志。
        /// </summary>
        /// <param name="charIds">角色id，无法识别通用角色Id。需要展开。</param>
        private void NotifyMail(Guid[] charIds)
        {
            var list = new List<Guid>(charIds);
            Array.ForEach(charIds, c => //优先通知已经在内存的角色
            {
                var gc = World.CharManager.GetCharFromId(c);
                if (gc is null) //若不在内存中
                    return;
                var gu = gc.GameUser;
                using var dwUser = World.CharManager.LockAndReturnDisposer(gu, Timeout.InfiniteTimeSpan);
                //生成通知数据。
                var lst = World.CharManager.GetChangeData(gc);
                if (lst != null)
                {
                    var np = new ChangeData()
                    {
                        ActionId = 2,
                        NewValue = 0,
                        ObjectId = gc.Id,
                        OldValue = 0,
                        PropertyName = "Count",
                        TemplateId = ProjectConstant.MailSlotTId,
                    };
                    np.Properties.Add("charId", gc.IdString);
                    lst.Add(np);
                    World.CharManager.NotifyChange(gu);
                    return;
                }
                else
                    return;
            });

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
            var eveMng = World.EventsManager;
            foreach (var item in gameItems)
            {
                // tid={物品模板Id},htid={头模板Id},btid={身体模板Id},count=物品数量，ptid=物品所属容器的模板Id,neatk=攻击资质,nemhp=血量资质,neqlt=质量资质。
                var att = new GameMailAttachment();
                var gi = item.Item1;
                eveMng.Copy(gi, att.Properties);
                att.SetSdp("ptid", item.Item2.ToString());
                att.SetSdp("count", gi.Count ?? 1);
                mail.Attachmentes.Add(att);
            }
            SendMail(mail, tos, senderId);
        }

        /// <summary>
        /// 领取附件。
        /// </summary>
        /// <param name="attachmentesIds">附件Id集合。</param>
        /// <param name="gameChar"></param>
        /// <param name="db">访问公共数据使用的数据库上下文对象。</param>
        /// <param name="changes"></param>
        /// <param name="results">每一项的获取结果。</param>
        public bool GetAttachmentes(IEnumerable<Guid> attachmentesIds, GameChar gameChar, DbContext db, IList<ChangeItem> changeItems = null,
            ICollection<(Guid, GetAttachmenteItemResult)> results = null)
        {
            using var dwChar = World.CharManager.LockAndReturnDisposer(gameChar.GameUser);
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
                OwHelper.SetLastErrorMessage("至少有一个附件不属于指定角色。");
                return false;
            }
            var changes = new List<GamePropertyChangeItem<object>>();
            var gim = World.ItemManager;
            bool dirty = false;
            List<GameItem> remainder = new List<GameItem>();
            foreach (var item in atts)  //遍历附件
            {
                try
                {
                    if (item.RemovedIds.Contains(gcId))  //若已经被领取
                    {
                        results?.Add((item.Id, GetAttachmenteItemResult.Done));
                        continue;
                    }
                    var ptid = item.GetSdpGuidOrDefault(SocialConstant.SentDestPTIdPName, Guid.Empty);
                    var gameItem = new GameItem();  //物品
                    World.EventsManager.GameItemCreated(gameItem, item.Properties);
                    GameThingBase parent = gameChar.AllChildren.FirstOrDefault(c => c.ExtraGuid == ptid);
                    if (parent is null)
                    {
                        if (ptid == ProjectConstant.CharTemplateId)
                            parent = gameChar;
                        else
                        {
                            results?.Add((item.Id, GetAttachmenteItemResult.Unknow));
                            continue;
                        }
                    }
                    remainder.Clear();
                    gim.MoveItem(gameItem, gameItem.Count ?? 1, parent, remainder, changes);
                    if (remainder.Count > 0)
                    {
                        results?.Add((item.Id, GetAttachmenteItemResult.Full));
                        continue;
                    }
                    results?.Add((item.Id, GetAttachmenteItemResult.Success));
                    item.RemovedIds.Add(gcId);

                    dirty = true;
                }
                catch (Exception)
                {
                    results.Add((item.Id, GetAttachmenteItemResult.Unknow));
                }
            }
            if (dirty)  //若数据发生了变化
            {
                try
                {
                    db.SaveChanges();    //修正玩家数据
                    World.CharManager.NotifyChange(gameChar.GameUser);
                }
                catch (Exception err)
                {
                    OwHelper.SetLastErrorMessage($"发生未知错误——{err.Message}");
                    return false;
                }
            }
            if (null != changeItems)
                changes.CopyTo(changeItems);
            return true;
        }

        /// <summary>
        /// 试图请求好友协助自己的攻击。
        /// </summary>
        /// <param name="datas"></param>
        public void RequestAssistance(RequestAssistanceDatas datas)
        {
            using var dwCombat = World.CombatManager.GetAndLockCombat(datas.RootCombatId, out var combat);
            if (dwCombat.IsEmpty)
            {
                datas.FillErrorFromWorld();
                return;
            }
            var oldCombat = combat;
            if (oldCombat.Assistancing || oldCombat.Assistanced)   //若已经请求了协助
            {
                datas.HasError = true;
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.DebugMessage = $"已经请求了协助者或已经协助结束。";
                return;
            }
            //更改数据
            oldCombat.Assistancing = true;
            oldCombat.AssistanceId = datas.OtherCharId; //设置请求协助的对象。
            //发送邮件
            var mail = new GameMail()
            {
            };
            mail.SetSdp("MailTypeId", ProjectConstant.PVP反击邮件_被求助者_求助.ToString());
            mail.SetSdp("CombatId", oldCombat.Thing.IdString);
            World.SocialManager.SendMail(mail, new Guid[] { datas.OtherCharId }, datas.GameChar.Id); //被攻击邮件
            //关系数据
            datas.SocialRelationship.Flag++;
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
            db.Set<GameSocialRelationship>().Where(c => c.Id == charId && c.KeyType == SocialConstant.FriendKeyType);

        /// <summary>
        /// 获取与指定id为目标客体的所有角色关系集合。
        /// </summary>
        /// <param name="charId"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public IQueryable<GameSocialRelationship> GetCharRelationshipToMe(Guid charId, DbContext db) =>
           GetCharRelationship(charId, db).Where(c => c.Id2 == charId && c.Flag >= SocialConstant.MinFriendliness && c.Flag <= SocialConstant.MaxFriendliness);

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
        /// 获取一组角色的摘要数据。从数据库直接获取，可能有延迟。
        /// </summary>
        /// <param name="ids">要获得摘要信息的角色Id集合。</param>
        /// <param name="db">使用的用户数据库上下文。</param>
        /// <returns>指定的角色摘要信息。</returns>
        /// <exception cref="ArgumentException">至少一个指定的Id不是有效角色Id。</exception>
        public IEnumerable<CharSummary> GetCharSummary(IEnumerable<Guid> ids, [NotNull] DbContext db)
        {
            Debug.WriteLineIf(ids.Count() > 1000, "GetCharSummary查询用户摘要信息过多。");
            var innerIds = ids.Distinct().ToArray();

            var collBase = (from gc in db.Set<GameChar>()
                            where innerIds.Contains(gc.Id)
                            join bag in db.Set<GameItem>().Where(c => c.ExtraGuid == ProjectConstant.CurrencyBagTId)    //货币袋
                            on gc.Id equals bag.OwnerId

                            join gold in db.Set<GameItem>().Where(c => c.ExtraGuid == ProjectConstant.JinbiId) //金币对象
                            on bag.Id equals gold.ParentId into l2
                            from goldR in l2.DefaultIfEmpty()

                            join wood in db.Set<GameItem>().Where(c => c.ExtraGuid == ProjectConstant.MucaiId) //木材
                            on bag.Id equals wood.ParentId

                            join tuiguan in db.Set<GameItem>().Where(c => c.ExtraGuid == ProjectConstant.TuiGuanTId)   //推关战力对象
                            on gc.Id equals tuiguan.Parent.OwnerId into l1
                            from tuiguanR in l1.DefaultIfEmpty()

                            join goldOfStore in db.Set<GameItem>().Where(c => c.ExtraGuid == ProjectConstant.YumitianTId) //玉米田
                            on gc.Id equals goldOfStore.Parent.Parent.OwnerId into l3
                            from goldOfStoreR in l3.DefaultIfEmpty()

                            join woodOfStore in db.Set<GameItem>().Where(c => c.ExtraGuid == ProjectConstant.MucaishuTId) //树林
                            on gc.Id equals woodOfStore.Parent.Parent.OwnerId into l4
                            from woodOfStoreR in l4.DefaultIfEmpty()

                            join mainRoom in db.Set<GameItem>().Where(c => c.ExtraGuid == ProjectConstant.MainControlRoomSlotId) //主控室
                            on gc.Id equals mainRoom.Parent.Parent.OwnerId into l5
                            from mainRoomR in l5.DefaultIfEmpty()

                            join pvpObject in db.Set<GameItem>().Where(c => c.ExtraGuid == ProjectConstant.PvpObjectTId) //pvp积分
                            on gc.Id equals pvpObject.Parent.OwnerId into l6
                            from pvpObjectR in l6.DefaultIfEmpty()

                            select new { gc, tuiguanR, goldR, goldOfStoreR, wood, woodOfStoreR, mainRoomR, pvpObjectR }).AsNoTracking();   //基准集合
                                                                                                                                           //where pvpObjectR.ExtraGuid == 

            if (collBase.Count() != innerIds.Length)
            {
                var errColl = innerIds.Except(collBase.Select(c => c.gc.Id).ToArray());
                throw new ArgumentException($"至少一个指定的Id不是有效角色Id。如:{errColl.First()}", nameof(ids));
            }
            var mounts = (from gc in db.Set<GameChar>()
                          where innerIds.Contains(gc.Id)
                          join gi in db.Set<GameItem>()
                          on gc.Id equals gi.Parent.OwnerId
                          where gi.ExtraGuid == ProjectConstant.ZuojiZuheRongqi && gi.PropertiesString.Contains("for10=")
                          select new { gc.Id, gi }).ToList();    //展示坐骑
            var logoutTime = (from tmp in db.Set<GameActionRecord>()
                              where innerIds.Contains(tmp.ParentId) && tmp.ActionId == "Logout"
                              group tmp by tmp.ParentId into g
                              select new { g.Key, LastLogoutDatetime = g.Max(c => c.DateTimeUtc) }).AsEnumerable().ToDictionary(c => c.Key, c => c.LastLogoutDatetime); //下线时间
            var result = new List<CharSummary>();
            foreach (var item in collBase.AsEnumerable())  //逐一生成结果
            {
                var tmp = new CharSummary
                {
                    CombatCap = (int)item.tuiguanR?.ExtraDecimal.GetValueOrDefault(),
                    DisplayName = item.gc.DisplayName,
                    Id = item.gc.Id,
                    Level = (int)item.gc.GetSdpDecimalOrDefault("lv", decimal.Zero),
                    IconIndex = (int)item.gc.GetSdpDecimalOrDefault("charIcon", decimal.Zero),
                    Gold = item.goldR.Count.GetValueOrDefault(),
                    GoldOfStore = item.goldOfStoreR.Count.GetValueOrDefault(),
                    Wood = item.wood.Count ?? 0,
                    WoodOfStore = item.woodOfStoreR?.Count.GetValueOrDefault() ?? default,
                    MainBaseLevel = (int)(item.mainRoomR?.GetPropertyWithFcpOrDefalut(World.PropertyManager.LevelPropertyName) ?? 0),
                    PvpScores = item.pvpObjectR?.ExtraDecimal.GetValueOrDefault() ?? 0,
                };
                var mount = mounts.Where(c => c.Id == item.gc.Id).Select(c => c.gi);
                if (null != mount)
                    tmp.HomelandShows.AddRange(mount);
                if (World.CharManager.IsOnline(item.gc.Id))
                    tmp.LastLogoutDatetime = new DateTime?();
                else
                    tmp.LastLogoutDatetime = logoutTime.TryGetValue(item.gc.Id, out var dt) ? dt : new DateTime?();
                result.Add(tmp);
            }
            return result.ToList();
        }

        /// <summary>
        /// 获取一组可以申请好友的角色信息。
        /// </summary>
        /// <param name="datas"></param>
        /// <returns>目前最多返回5条。</returns>
        public void GetCharIdsForRequestFriend(GetCharIdsForRequestFriendDatas datas)
        {
            using var view = new FriendDataView(World, datas.GameChar, DateTime.UtcNow);
            //var friend = datas.GameChar.GameItems.FirstOrDefault(c => c.ExtraGuid == ProjectConstant.SocialRelationshipSlotTId);

            IEnumerable<Guid> result;
            if (!string.IsNullOrWhiteSpace(datas.DisplayName))   //若需要按角色昵稱过滤
            {
                var coll = view.RefreshLastList(datas.DisplayName);
                var count = coll.Count();
                if (count > 0)
                {
                    result = coll.Skip(VWorld.WorldRandom.Next(count)).Take(1);
                    view.TodayIds.AddRange(result);
                    view.HasData = true;
                }
                else
                    result = Array.Empty<Guid>();
            }
            else //若需要按身体模板Id过滤或不限制
            {
                if (view.HasData && datas.DonotRefresh)  //若不需要刷新数据
                    result = view.LastListIds;
                else
                    result = view.RefreshLastList(datas.BodyTIds).Where(c => c != datas.GameChar.Id).Take(5).ToArray();
                //记录已刷新用户Id
                if (result.Any())
                {
                    view.LastListIds.Clear();
                    view.LastListIds.AddRange(result);
                    view.HasData = true;
                }
            }
            datas.CharIds.AddRange(result);
            view.Save();
        }

        /// <summary>
        /// 请求加好友。
        /// 如果对方已经请求添加自己为好友，则这个函数会自动确认。
        /// </summary>
        /// <param name="gameChar">请求添加好友的角色对象。</param>
        /// <param name="friendId">请求加好友的Id。</param>
        /// <returns>0成功发送请求。-1无法锁定账号。
        /// -2对方Id不存在。</returns>
        public RequestFriendResult RequestFriend(RequestFriendData data)
        {
            using var dwUsers = data.LockAll();
            if (dwUsers is null)
            {
                OwHelper.SetLastErrorMessage($"无法锁定指定角色。");
                return RequestFriendResult.NotFoundThisChar;
            }
            GameUserContext db = data.UserDbContext;
            var objChar = data.OtherChar;  //要请求的角色对象。
            //TO DO限制当日添加好友次数。
            var sr = GetSrOrAdd(db, data.GameChar.Id, objChar.Id);  //关系对象
            var nsr = GetNSrOrAdd(db, data.GameChar.Id, objChar.Id); //对方的关系对象
            if (nsr.IsBlack()) //若对方已经把当前用户加入黑名单
                return RequestFriendResult.BlackList;
            if (sr.IsBlack())  //黑名单
                return RequestFriendResult.AlreadyBlack;
            else
            {
                if (sr.TryGetSdp(SocialConstant.ConfirmedFriendPName, out var obj) && obj is decimal deci && deci == 0) //若正在申请
                    return RequestFriendResult.Doing;
                else if (sr.IsFriendOrRequesting()) //已经是好友
                    return RequestFriendResult.Already;
            }
            //其他状况
            sr.SetSdp(SocialConstant.ConfirmedFriendPName, decimal.Zero);
            sr.SetFriend();    //设置好友关系
            try
            {
                db.SaveChanges();
            }
            catch (Exception)
            {
                return RequestFriendResult.UnknowError;
            }
            //生成通知数据。
            var gi = objChar.GetFriendSlot();
            var lst = World.CharManager.GetChangeData(objChar);
            if (lst != null && gi != null)
            {
                var np = new ChangeData()
                {
                    ActionId = 2,
                    NewValue = 0,
                    ObjectId = gi.Id,
                    OldValue = 0,
                    PropertyName = World.PropertyManager.LevelPropertyName,
                    TemplateId = gi.ExtraGuid,
                };
                np.Properties.Add("charId", objChar.Id.ToString());
                lst.Add(np);
                World.CharManager.NotifyChange(objChar.GameUser);
            }
            else
            {
                //TO DO
            }

            World.CharManager.Nope(data.GameChar.GameUser);  //重置下线计时器
            return RequestFriendResult.Success;
        }

        /// <summary>
        /// 获取社交关系列表。😀 👌
        /// Confirmed。
        /// </summary>
        /// <param name="gameChar">己方角色对象。</param>
        /// <param name="db"></param>
        /// <returns></returns>
        public IEnumerable<GameSocialRelationship> GetSocialRelationships(GameChar gameChar, DbContext db)
        {
            using var dwChar = World.CharManager.LockAndReturnDisposer(gameChar.GameUser);
            if (dwChar is null)
                return null;
            try
            {
                var gcId = gameChar.Id;
                var coll1 = GetSocialRelationshipQuery(db).Where(c => c.Id == gcId || c.Id2 == gcId);   //自己添加 和 对方请求添加的
                var result = coll1.ToArray();
                World.CharManager.Nope(gameChar.GameUser);  //重置下线计时器
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 或取关系对象的延迟查询。
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected IQueryable<GameSocialRelationship> GetSocialRelationshipQuery(DbContext context) =>
            context.Set<GameSocialRelationship>().Where(c => c.KeyType == SocialConstant.FriendKeyType);

        /// <summary>
        /// 获取自己与对方的关系对象。
        /// </summary>
        /// <param name="context"></param>
        /// <param name="charId"></param>
        /// <param name="friendId"></param>
        /// <returns></returns>
        protected GameSocialRelationship GetSrOrDefault(DbContext context, Guid charId, Guid friendId) =>
            GetSocialRelationshipQuery(context).Where(c => c.Id == charId && c.Id2 == friendId).FirstOrDefault();

        /// <summary>
        /// 获取自己和对方关系的对象。如果没有则添加。
        /// </summary>
        /// <param name="context"></param>
        /// <param name="charId"></param>
        /// <param name="otherId"></param>
        /// <returns>新加的关系对象其Flag是<see cref="SocialConstant.MiddleFriendliness"/></returns>
        protected GameSocialRelationship GetSrOrAdd(DbContext context, Guid charId, Guid otherId)
        {
            var result = GetSrOrDefault(context, charId, otherId);
            if (result is null)
            {
                result = new GameSocialRelationship() { Id = charId, Id2 = otherId, KeyType = SocialConstant.FriendKeyType, Flag = SocialConstant.MiddleFriendliness, };
                context.Add(result);
            }
            return result;
        }

        /// <summary>
        /// 获取对方与自己的关系对象。
        /// </summary>
        /// <param name="context"></param>
        /// <param name="charId"></param>
        /// <param name="friendId"></param>
        /// <returns>没找到则返回null。</returns>
        protected GameSocialRelationship GetNSrOrDefault(DbContext context, Guid charId, Guid friendId) =>
            GetSocialRelationshipQuery(context).Where(c => c.Id == friendId && c.Id2 == charId).FirstOrDefault();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="charId"></param>
        /// <param name="otherId"></param>
        /// <returns>新加的关系对象其Flag是<see cref="SocialConstant.MiddleFriendliness"/></returns>
        protected GameSocialRelationship GetNSrOrAdd(DbContext context, Guid charId, Guid otherId)
        {
            var result = GetNSrOrDefault(context, charId, otherId);
            if (result is null)
            {
                result = new GameSocialRelationship() { Id = otherId, Id2 = charId, KeyType = SocialConstant.FriendKeyType, Flag = SocialConstant.MiddleFriendliness };
                context.Add(result);
            }
            return result;
        }
        /// <summary>
        /// 确认或拒绝好友申请。
        /// </summary>
        /// <param name="gameChar">当前角色。</param>
        /// <param name="friendId">申请人Id。</param>
        /// <param name="rejected">true拒绝好友申请。</param>
        /// <returns></returns>
        public ConfirmFriendResult ConfirmFriend(GameChar gameChar, Guid friendId, bool rejected = false)
        {
            if (!World.CharManager.Lock(gameChar.GameUser))
            {
                OwHelper.SetLastErrorMessage($"无法锁定指定玩家，Id={gameChar.Id}。");
                return ConfirmFriendResult.Unknown;
            }
            using var dwChar = DisposerWrapper.Create(() => World.CharManager.Unlock(gameChar.GameUser));
            using var db = World.CreateNewUserDbContext();
            var gcId = gameChar.Id;
            var sr = GetSrOrAdd(db, gcId, friendId);
            var nsr = GetNSrOrAdd(db, gcId, friendId);
            if (!nsr.IsRequesting())   //若未申请好友
                return ConfirmFriendResult.Unknown;
            if (rejected)   //若拒绝
            {
                sr.SetNeutrally(); sr.SetConfirmed();
                nsr.SetNeutrally(); nsr.SetConfirmed();
            }
            else //接受
            {
                var slot = gameChar.AllChildren.First(c => c.ExtraGuid == SocialConstant.FriendSlotTId);
                if (World.PropertyManager.GetRemainderStc(slot) <= 0)
                {
                    OwHelper.SetLastErrorMessage("好友位已满。");
                    return ConfirmFriendResult.CharFriendFull;
                }
                sr.SetFriend();
                sr.SetConfirmed();
                nsr.SetFriend();
                nsr.SetConfirmed();
                slot.Count++;
                var otherGc = World.CharManager.GetCharFromId(sr.Id2);  //对方角色
                if (otherGc != null && World.CharManager.Lock(otherGc?.GameUser, 500))  //TODO:乱序锁
                {
                    using var dw = DisposeHelper.Create(c => World.CharManager.Unlock(c), otherGc.GameUser);
                    var list = World.CharManager.GetChangeData(otherGc);
                    var otherSlot = otherGc.AllChildren.First(c => c.ExtraGuid == SocialConstant.FriendSlotTId);
                    var np = new ChangeData()
                    {
                        ActionId = 2,
                        NewValue = otherSlot.Count,
                        ObjectId = otherSlot.Id,
                        OldValue = otherSlot.Count - 1,
                        PropertyName = "Count",
                        TemplateId = ProjectConstant.FriendSlotTId,
                    };
                    list.Add(np);
                    World.CharManager.NotifyChange(otherGc.GameUser);
                }
            }
            db.SaveChanges();
            return ConfirmFriendResult.Success;
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
                OwHelper.SetLastErrorMessage($"无法锁定指定玩家，Id={gameChar.Id}。");
                return false;
            }
            using var dwChar = DisposerWrapper.Create(() => World.CharManager.Unlock(gameChar.GameUser, true));
            using var db = World.CreateNewUserDbContext();
            var gcId = gameChar.Id;
            var sr = GetSrOrAdd(db, gcId, friendId);
            var nsr = GetNSrOrAdd(db, gcId, friendId);
            if (sr.IsFriend())    //确定是好友关系
            {
                var slot = gameChar.GameItems.First(c => c.ExtraGuid == SocialConstant.FriendSlotTId);
                slot.Count--;
                sr.SetNeutrally(); sr.SetConfirmed();
            }

            nsr.SetNeutrally(); nsr.SetConfirmed();
            db.SaveChanges();
            return true;
        }

        /// <summary>
        /// 设置黑名关系。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="objId"></param>
        /// <returns></returns>
        public bool SetFrindless(GameChar gameChar, Guid objId)
        {
            if (!World.CharManager.Lock(gameChar.GameUser))
            {
                OwHelper.SetLastErrorMessage($"无法锁定指定玩家，Id={gameChar.Id}。");
                return false;
            }
            using var dwChar = DisposerWrapper.Create(() => World.CharManager.Unlock(gameChar.GameUser, true));
            using var db = World.CreateNewUserDbContext();
            try
            {
                var gcId = gameChar.Id;
                var sr = GetSrOrAdd(db, gcId, objId);
                var nsr = GetNSrOrAdd(db, gcId, objId);
                sr.SetBlack();
                sr.SetSdp(SocialConstant.ConfirmedFriendPName, decimal.One);
                nsr.SetNeutrally();
                nsr.SetSdp(SocialConstant.ConfirmedFriendPName, decimal.One);
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                OwHelper.SetLastErrorMessage("并发冲突，请重试一次。");
                return false;
            }
            return true;

        }

        /// <summary>
        /// 移除黑名单。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="objId"></param>
        /// <returns>true成功移除，false指定角色Id不是黑名单角色。</returns>
        public bool RemoveBlack(GameChar gameChar, Guid objId)
        {
            if (!World.CharManager.Lock(gameChar.GameUser))
            {
                OwHelper.SetLastErrorMessage($"无法锁定指定玩家，Id={gameChar.Id}。");
                return false;
            }
            using var dwChar = DisposerWrapper.Create(() => World.CharManager.Unlock(gameChar.GameUser, true));
            using var db = World.CreateNewUserDbContext();
            try
            {
                var gcId = gameChar.Id;
                var sr = GetSrOrAdd(db, gcId, objId);
                var nsr = GetNSrOrAdd(db, gcId, objId);
                if (!sr.IsBlack() || !nsr.IsBlack())
                    return false;
                sr.SetNeutrally();
                nsr.SetNeutrally();
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                OwHelper.SetLastErrorMessage("并发冲突，请重试一次。");
                return false;
            }
            return true;
        }
        #endregion 黑白名单相关

        #region 项目特定功能

        /// <summary>
        /// 获取指定角色家园及社交信息。
        /// </summary>
        /// <param name="datas"></param>
        public void GetCharInfo(GetCharInfoDatas datas)
        {
            using var dw = datas.LockAll();
            if (dw is null)
                return;
            datas.Homeland.Add(datas.OtherChar.GetHomeland());
            datas.Mounts.AddRange(datas.OtherChar.GetZuojiBag().Children);
        }

        /// <summary>
        /// 去好友家互动以获得体力。 返回值参见 PatForTiliResult。
        /// </summary>
        /// <param name="datas">工作数据。</param>
        /// <returns><seealso cref="PatForTiliResult"/></returns>
        public PatForTiliResult PatForTili(PatForTiliWorkData datas)
        {
            using var dwUsers = datas.LockAll();
            if (dwUsers is null)
                return PatForTiliResult.TimesOver;
            DateTime dtNow = DateTime.UtcNow;   //当前时间
            var dt = dtNow;
            if (datas.Fcp.GetCurrentValue(ref dt) <= 0) //若已经用完访问次数
                return PatForTiliResult.TimesOver;
            var objectId = datas.OtherChar.Id;
            if (datas.Visitors.Any(c => c.Id2 == objectId && DateTime.TryParse(c.PropertyString, out var dt) && dt.Date == datas.Now.Date))    //若访问过了
                return PatForTiliResult.Already;
            //成功交互
            var sr = datas.Visitors.FirstOrDefault(c => c.Id2 == objectId);
            if (sr is null)  //若没有条目
            {
                sr = new GameSocialRelationship(datas.GameChar.Id, objectId, (int)SocialKeyTypes.PatTili, 0)
                {
                    PropertyString = datas.Now.ToString("s"),
                };
                datas.Visitors.Add(sr);
            }
            datas.FriendCurrency += 5;  //增加友情货币。
                                        //扣除次数
            datas.Fcp.LastValue--;
            datas.Tili.Name2FastChangingProperty["Count"].LastValue += 5; //增加体力
            datas.Save();
            datas.ChangeItems.AddToChanges(datas.Tili);
            World.CharManager.NotifyChange(datas.GameChar.GameUser);  //通知状态发生了更改
            var command = new HomelandInteractionedCommand();
            datas.CommandContext.GetCommandManager().Handle(command);
            return PatForTiliResult.Success;
        }

        public class PatForTiliWorkData : BinaryRelationshipGameContext
        {
            //private const string key = "patcountVisitors";

            public PatForTiliWorkData([NotNull] IServiceProvider service, [NotNull] GameChar gameChar, Guid otherGCharId, DateTime now) : base(service, gameChar, otherGCharId)
            {
                _Now = now;
            }

            public PatForTiliWorkData([NotNull] VWorld world, [NotNull] GameChar gameChar, Guid otherGCharId, DateTime now) : base(world, gameChar, otherGCharId)
            {
                _Now = now;
            }

            public PatForTiliWorkData([NotNull] VWorld world, [NotNull] string token, Guid otherGCharId, DateTime now) : base(world, token, otherGCharId)
            {
                _Now = now;
            }

            private readonly DateTime _Now;

            public DateTime Now => _Now;

            public GameItem Tili { get => GameChar.GetTili(); }

            /// <summary>
            /// 与主控室互动的剩余次数。
            /// </summary>
            public FastChangingProperty Fcp { get => Tili.Name2FastChangingProperty["patcount"]; }

            private ObservableCollection<GameSocialRelationship> _Visitors;

            /// <summary>
            /// 用户和其他玩家的主控室互动获得体力的数据条目。
            /// </summary>
            public ObservableCollection<GameSocialRelationship> Visitors
            {
                get
                {
                    if (_Visitors is null)
                    {
                        _Visitors = new ObservableCollection<GameSocialRelationship>(UserDbContext.Set<GameSocialRelationship>().
                            Where(c => c.Id == GameChar.Id && c.KeyType == (int)SocialKeyTypes.PatTili));
                        _Visitors.CollectionChanged += VisitorsCollectionChanged;
                    }
                    return _Visitors;
                }
            }

            private void VisitorsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        UserDbContext.AddRange(e.NewItems.OfType<GameSocialRelationship>());
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        UserDbContext.RemoveRange(e.OldItems.OfType<GameSocialRelationship>());
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        UserDbContext.RemoveRange(e.OldItems.OfType<GameSocialRelationship>());
                        UserDbContext.AddRange(e.NewItems.OfType<GameSocialRelationship>());
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        throw new NotSupportedException();
                    case NotifyCollectionChangedAction.Move:
                    default:
                        break;
                }
            }

            private GameItem _FriendCurrencyItem;

            /// <summary>
            /// 友情货币数。
            /// </summary>
            public decimal FriendCurrency
            {
                get
                {
                    if (_FriendCurrencyItem is null)
                    {
                        _FriendCurrencyItem = GameChar.GetFriendCurrency();
                    }
                    return _FriendCurrencyItem.Count ?? 0;
                }
                set
                {
                    if (_FriendCurrencyItem is null)
                    {
                        _FriendCurrencyItem = GameChar.GetFriendCurrency();
                    }
                    _FriendCurrencyItem.Count = value;
                }
            }

            public GameCommandContext CommandContext { get; set; }

            public override void Save()
            {
                base.Save();
            }
        }

        /// <summary>
        /// 与好友坐骑互动。返回码：160=今日已经与该坐骑互动过了；1721=今日互动次数已经用完。
        /// </summary>
        /// <param name="datas">参数及返回值的封装类。</param>
        public void PatWithMounts(PatWithMountsDatas datas)
        {
            using var dwChar = datas.LockAll();
            var now = datas.Today;
            if (dwChar is null)
            {
                datas.HasError = true;
                datas.ErrorCode = OwHelper.GetLastError();
                datas.DebugMessage = OwHelper.GetLastErrorMessage();
                return;
            }
            var db = datas.UserDbContext;
            var tmpNow = now;
            var sr = datas.GetOrAddSr();  //与该坐骑互动的数据条目
            if (datas.IsRemove)  //若是解约
            {
                //修改数据
                sr.Flag = 0;
                datas.SetDateTime(sr, datas.Today);
                datas.SetOtherCharId(sr, datas.OtherCharId);
                datas.UserDbContext.Remove(sr);
            }
            else //签约或增加互动
            {
                if (datas.Hudong.TodayValues.Contains(datas.MountsId))
                {
                    datas.HasError = true;
                    datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                    datas.DebugMessage = "今日已经与该坐骑互动过了。";
                    return;
                }
                var dt = datas.Now;
                if (datas.Counter.GetCurrentValue(ref dt) <= 0)
                {
                    datas.HasError = true;
                    datas.ErrorCode = ErrorCodes.RPC_S_OUT_OF_RESOURCES;
                    datas.DebugMessage = "今日互动次数已经用完。";
                    return;
                }
                //修改数据
                datas.Hudong.TodayValues.Add(datas.MountsId);   //记录互动痕迹
                datas.Counter.LastValue--; //减少可互动次数
                datas.SetDateTime(sr, datas.Today);
                datas.SetOtherCharId(sr, datas.OtherCharId);
                var gim = World.ItemManager;
                var max = gim.GetBodyTemplate(datas.Mount).GetSdpDecimalOrDefault("fht", 7);   //互动次数
                //datas.Counter.LastValue--;
                if (++sr.Flag >= max)  //若此次互动有了结果
                {
                    sr.Flag = 0;
                    //var gameItem = datas.UserContext.Set<GameItem>().Include(c => c.Children).ThenInclude(c => c.Children).AsNoTracking().Single(c => c.Id == sr.Id2);
                    var gameItem = datas.Mount;
                    GameItem sendGi = World.ItemManager.CloneMounts(gameItem, ProjectConstant.HomelandPatCard); //创建幻影
                    sendGi.SetSdp("charDisplayName", datas.OtherChar.DisplayName);
                    World.ItemManager.MoveItem(sendGi, sendGi.Count ?? 1, datas.GameChar.GetItemBag(), null, datas.PropertyChanges); //放入道具背包
                }
            }
            //删除其他签约坐骑
            var removbes = datas.Visitors.Where(c => datas.GetOtherCharId(sr) == datas.GetOtherCharId(c) && sr != c).ToList();
            removbes.ForEach(c => datas.Visitors.Remove(c));
            datas.Save();
            datas.World.CharManager.NotifyChange(datas.GameChar.GameUser);
            datas.PropertyChanges.CopyTo(datas.ChangeItems);
            datas.CommandContext.GetCommandManager().Handle(new HomelandInteractionedCommand());
            return;
        }

        /// <summary>
        /// PatWithMounts 使用的数据封装对象。
        /// </summary>
        public class PatWithMountsDatas : BinaryRelationshipGameContext
        {
            public PatWithMountsDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar, Guid mountsId, DateTime today) : base(service, gameChar, Guid.Empty)
            {
                MountsId = mountsId;
                Initialize();
            }

            public PatWithMountsDatas([NotNull] VWorld world, [NotNull] GameChar gameChar, Guid mountsId, DateTime today) : base(world, gameChar, Guid.Empty)
            {
                MountsId = mountsId;
                Initialize();
            }

            public PatWithMountsDatas([NotNull] VWorld world, [NotNull] string token, Guid mountsId, DateTime today) : base(world, token, Guid.Empty)
            {
                MountsId = mountsId;
                Initialize();
            }

            private void Initialize()
            {
                _LazyMounts = new Lazy<GameItem>(() => UserDbContext.Set<GameItem>().AsNoTracking().FirstOrDefault(c => c.Id == MountsId), true);
                var pid = _LazyMounts.Value.ParentId.Value;
                OtherCharId = UserDbContext.Set<GameItem>().AsNoTracking().FirstOrDefault(c => c.Id == pid).OwnerId.Value;
            }

            /// <summary>
            /// 是否是解约。
            /// </summary>
            /// <value>true解约，否则为false。</value>
            public bool IsRemove { get; set; }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            /// <returns></returns>
            public override IDisposable LockAll()
            {
                var ary = new Guid[] { GameChar.Id, OtherCharId };
                return World.CharManager.LockOrLoadWithCharIds(ary, World.CharManager.Options.DefaultLockTimeout);
            }

            /// <summary>
            /// 要互动的坐骑Id。注意，指定该Id将导致该玩家所有其他坐骑的亲密度清零。
            /// </summary>
            public Guid MountsId { get; set; }

            /// <summary>
            /// 当前时间。
            /// </summary>
            /// <value>默认值：<see cref="DateTime.UtcNow"/></value>
            public DateTime Now { get; set; } = DateTime.UtcNow;

            /// <summary>
            /// 当前日期。
            /// </summary>
            public DateTime Today => Now.Date;

            /// <summary>
            /// 如果友好度已满，使用此坐骑与对方坐骑杂交。
            /// </summary>
            public Guid CurrentMountsId { get; set; }

            /// <summary>
            /// 通过邮件发送了物品集合。
            /// Changes属性都可能有数据（部分放入）。
            /// 其中<see cref="ChangesItem.ContainerId"/>是邮件的Id。
            /// </summary>
            public List<ChangeItem> MailItems { get; set; } = new List<ChangeItem>();

            #region 工作函数内部使用
            /// <summary>
            /// 计数器的名字。挂在家园对象下。
            /// </summary>
            public const string FcpName = "PatWithMountsCounter";

            /// <summary>
            /// 扩展属性的名字。
            /// </summary>
            public const string ExPropName = "PatWithMountsExProp";

            public const string PatTodayName = "patMounts";

            private FastChangingProperty _Counter;

            /// <summary>
            /// 互动数据剩余次数。
            /// </summary>
            public FastChangingProperty Counter => _Counter ??= GameChar.GetHomeland().Name2FastChangingProperty[FcpName];

            private TodayDataLog<Guid> _Hudong;
            /// <summary>
            /// 互动数据。
            /// </summary>
            public TodayDataLog<Guid> Hudong
            {
                get
                {
                    if (_Hudong is null)
                    {
                        _Hudong = TodayDataLog<Guid>.Create(GameChar.GetHomeland().Properties, PatTodayName, Now);
                    }
                    return _Hudong;
                }
            }

            private ObservableCollection<GameSocialRelationship> _Visitors;

            /// <summary>
            /// 详细信息。
            /// </summary>
            public ObservableCollection<GameSocialRelationship> Visitors
            {
                get
                {
                    if (_Visitors is null)
                    {
                        _Visitors = new ObservableCollection<GameSocialRelationship>(UserDbContext.Set<GameSocialRelationship>().Where(c => c.Id == GameChar.Id && c.KeyType == (int)SocialKeyTypes.PatWithMounts));
                        _Visitors.CollectionChanged += VisitorsCollectionChanged;
                    }
                    return _Visitors;
                }
            }

            /// <summary>
            /// 当前要互动的坐骑的数据项。
            /// </summary>
            /// <returns></returns>
            public GameSocialRelationship GetOrAddSr()
            {
                var sr = Visitors.FirstOrDefault(c => c.Id2 == MountsId);
                if (sr is null)
                {
                    sr = new GameSocialRelationship()
                    {
                        Id = GameChar.Id,
                        Id2 = MountsId,
                        KeyType = (int)SocialKeyTypes.PatWithMounts,
                        Flag = 0,
                    };
                    SetDateTime(sr, (Today - TimeSpan.FromDays(1)));
                    SetOtherCharId(sr, OtherCharId);
                    Visitors.Add(sr);
                }
                return sr;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void VisitorsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        UserDbContext.AddRange(e.NewItems.OfType<GameSocialRelationship>());
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        UserDbContext.RemoveRange(e.OldItems.OfType<GameSocialRelationship>());
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        throw new NotSupportedException("不支持替换操作。");
                    case NotifyCollectionChangedAction.Reset:
                        UserDbContext.RemoveRange(e.OldItems.OfType<GameSocialRelationship>());
                        UserDbContext.AddRange(e.NewItems.OfType<GameSocialRelationship>());
                        break;
                    case NotifyCollectionChangedAction.Move:
                        break;
                    default:
                        break;
                }
            }

            public override void Save()
            {
                if (null != _Hudong)
                    _Hudong.Save();
                base.Save();
            }

            /// <summary>
            /// 试图与指定玩家的指定座机互动。
            /// </summary>
            /// <param name="charId">要互动的坐骑所属的角色Id。</param>
            /// <param name="itemId">要互动坐骑的Id。</param>
            /// <param name="now">使用的时间点。</param>
            /// <returns>true成功互动，false因种种原因没有成功。仅会修改数据，不会有后续连锁动作。</returns>
            //public bool TryVisit(Guid charId, Guid itemId, DateTime now)
            //{
            //    var dt = now;
            //    if (Counter.GetCurrentValue(ref dt) < 1)
            //    {
            //        VWorld.SetLastErrorMessage("访问次数用尽。");
            //        return false;
            //    }
            //    var dtToday = dt.Date;
            //    if (Visitors.Any(c => c.CharId == charId && dtToday == c.LastDateTime.Date))  //若今日互动过的坐骑
            //    {
            //        VWorld.SetLastErrorMessage("已经和该角色的宠物互动过。");
            //        return false;
            //    }
            //    var item = Visitors.FirstOrDefault(c => c.CharId == charId && c.ItemId == itemId);  //访问过的数据项
            //    if (item is null)    //若没有访问过该坐骑
            //    {
            //        item = new PatWithMountsItem()
            //        {
            //            CharId = charId,
            //            ItemId = itemId,
            //            Count = 1,
            //            LastDateTime = now,
            //        };
            //        Visitors.Add(item);
            //    }
            //    else
            //    {
            //        item.Count++;
            //        item.LastDateTime = now;
            //    }
            //    //修正数据
            //    Counter.LastValue--;
            //    return true;
            //}

            /// <summary>
            /// 获取最后互动时间。
            /// </summary>
            /// <param name="sr"></param>
            public DateTime GetDateTime(GameSocialRelationship sr)
            {
                return DateTime.Parse(sr.PropertyString);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="sr"></param>
            /// <param name="dateTime"></param>
            public void SetDateTime(GameSocialRelationship sr, DateTime dateTime)
            {
                sr.PropertyString = dateTime.ToString("s");
            }

            public Guid GetOtherCharId(GameSocialRelationship sr)
            {
                return sr.GetSdpGuidOrDefault("charid");
            }

            public void SetOtherCharId(GameSocialRelationship sr, Guid charId)
            {
                sr.SetSdp("charid", charId.ToString());
            }

            private Lazy<GameItem> _LazyMounts;

            /// <summary>
            /// 要互动的坐骑。找不到则返回null。
            /// </summary>
            public GameItem Mount => OtherChar.AllChildren.First(c => c.Id == MountsId);

            /// <summary>
            /// 是否已经访问过了该玩家。
            /// </summary>
            public bool IsVisited
            {
                get
                {
                    return Visitors.Any(c => GetDateTime(c).Date == Today.Date && GetOtherCharId(c) == OtherCharId);
                }
            }

            public GameCommandContext CommandContext { get; set; }
            #endregion 工作函数内部使用

        }

        public class GetPvpCharsWorkDatas : ChangeItemsWorkDatasBase
        {
            public GetPvpCharsWorkDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
            {
            }

            public GetPvpCharsWorkDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
            {
            }

            public GetPvpCharsWorkDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
            {
            }

            /// <summary>
            /// 强制指定一个角色Id。仅调试接口可用。
            /// </summary>
            public Guid CharId { get; set; }

            private DateTime _Now;

            /// <summary>
            /// 当前时间。
            /// </summary>
            public DateTime Now { get => _Now; set => _Now = value; }

            private List<Guid> _CharIds;

            /// <summary>
            /// 可pvp角色的列表。
            /// </summary>
            public List<Guid> CharIds => _CharIds ??= new List<Guid>();

            private GameItem _PvpObject;

            /// <summary>
            /// PVP对象。
            /// </summary>
            public GameItem PvpObject
            {
                get
                {
                    return _PvpObject ??= GameChar.GetPvpObject();
                }
            }
            /// <summary>
            /// 是否强制使用钻石刷新。
            /// false,不刷新，获取当日已经刷的最后一次数据,如果今日未刷则自动刷一次。
            /// true，强制刷新，根据设计可能需要消耗资源。
            /// </summary>
            public bool IsRefresh { get; set; }

            /// <summary>
            /// 建立的战斗对象，里面含有需要结算时用到的资源快照。
            /// 随后使用该对象的id和指定的角色id启动战斗。
            /// </summary>
            public GameCombat Combat { get; set; }

        }

        #endregion  项目特定功能
    }

    public class GetCharInfoDatas : BinaryRelationshipGameContext
    {
        public GetCharInfoDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar, Guid otherGCharId) : base(service, gameChar, otherGCharId)
        {
        }

        public GetCharInfoDatas([NotNull] VWorld world, [NotNull] GameChar gameChar, Guid otherGCharId) : base(world, gameChar, otherGCharId)
        {
        }

        public GetCharInfoDatas([NotNull] VWorld world, [NotNull] string token, Guid otherGCharId) : base(world, token, otherGCharId)
        {
        }

        List<GameItem> _Homeland;
        /// <summary>
        /// 返回家园信息。
        /// </summary>
        public List<GameItem> Homeland { get => _Homeland ??= new List<GameItem>(); }

        List<GameItem> _Mounts;
        /// <summary>
        /// 返回指定角色的所有坐骑信息。
        /// </summary>
        public List<GameItem> Mounts { get => _Mounts ??= new List<GameItem>(); }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                _Homeland = null;
                _Mounts = null;
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// 工作函数数据封装类。
    /// </summary>
    public class GetCharIdsForRequestFriendDatas : ComplexWorkGameContext
    {
        public GetCharIdsForRequestFriendDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public GetCharIdsForRequestFriendDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public GetCharIdsForRequestFriendDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        private string _DisplayName;

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
        /// 仅当按身体模板过滤时，此属性才有效。
        /// false若今天曾经刷新过数据则返回该数据，true强制刷新数据返回。
        /// </summary>
        public bool DonotRefresh { get; set; }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null

                base.Dispose(disposing);
            }
        }
    }

    /// <summary>
    /// 带变化物品和发送邮件返回值的类的接口
    /// </summary>
    public abstract class ChangeItemsAndMailWorkDatsBase : ChangeItemsWorkDatasBase
    {

        public ChangeItemsAndMailWorkDatsBase([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public ChangeItemsAndMailWorkDatsBase([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public ChangeItemsAndMailWorkDatsBase([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        private List<Guid> _MailIds;

        /// <summary>
        /// 工作后发送邮件的邮件Id。
        /// </summary>
        public List<Guid> MailIds => _MailIds ??= new List<Guid>();

        protected override void Dispose(bool disposing)
        {
            _MailIds = null;
            base.Dispose(disposing);
        }

    }

    /// <summary>
    /// <see cref="GameSocialManager.RequestFriend(RequestFriendData)"/>接口的工作数据块。
    /// </summary>
    public class RequestFriendData : BinaryRelationshipGameContext
    {
        public RequestFriendData([NotNull] IServiceProvider service, [NotNull] GameChar gameChar, Guid otherGCharId) : base(service, gameChar, otherGCharId)
        {
        }

        public RequestFriendData([NotNull] VWorld world, [NotNull] GameChar gameChar, Guid otherGCharId) : base(world, gameChar, otherGCharId)
        {
        }

        public RequestFriendData([NotNull] VWorld world, [NotNull] string token, Guid otherGCharId) : base(world, token, otherGCharId)
        {
        }
    }
}
