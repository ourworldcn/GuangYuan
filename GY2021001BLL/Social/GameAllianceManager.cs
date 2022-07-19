using GuangYuan.GY001.BLL;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using OW.Game;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OW.Game.PropertyChange;
using System.Threading;
using GuangYuan.GY001.BLL.GeneralManager;
using OW.Game.Log;
using System.Collections.ObjectModel;

namespace GuangYuan.GY001.UserDb.Social
{
    public class GameAllianceManagerOptions
    {
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(2);

    }

    /// <summary>
    /// 联盟/行会管理器。
    /// </summary>
    public class GameAllianceManager : GameManagerBase<GameAllianceManagerOptions>
    {
        #region 构造函数

        public GameAllianceManager()
        {
            Initialize();
        }

        public GameAllianceManager(IServiceProvider service) : base(service)
        {
            Initialize();
        }

        public GameAllianceManager(IServiceProvider service, GameAllianceManagerOptions options) : base(service, options)
        {
            Initialize();
        }

        void Initialize()
        {
            using var db = World.CreateNewUserDbContext();
            foreach (var item in db.Set<GameGuild>())
            {
                World.EventsManager.GameGuildLoaded(item);
                _Id2Guild[item.Id] = item;
            }
        }

        #endregion 构造函数
        IMemoryCache _Cache;
        public IMemoryCache Cache => _Cache ??= Service.GetRequiredService<IMemoryCache>();

        ConcurrentDictionary<Guid, GameGuild> _Id2Guild = new ConcurrentDictionary<Guid, GameGuild>();

        /// <summary>
        /// 所有工会，键是工会id，值是工会对象。
        /// </summary>
        public ConcurrentDictionary<Guid, GameGuild> Id2Guild => _Id2Guild;

        #region 基础操作
        public bool Lock(Guid guildId, TimeSpan timeout, out GameGuild guild)
        {
            if (!_Id2Guild.TryGetValue(guildId, out guild))
            {
                VWorld.SetLastError(ErrorCodes.ERROR_INVALID_DATA);
                VWorld.SetLastErrorMessage($"无此工会，id={guild}");
                return false;
            }
            if (!Monitor.TryEnter(guild, timeout))  //若锁定超时
            {
                VWorld.SetLastError(ErrorCodes.WAIT_TIMEOUT);
                return false;
            }
            if (guild.IsDisposed)   //若已经无效
            {
                Monitor.Exit(guild);
                VWorld.SetLastError(ErrorCodes.E_CHANGED_STATE);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 锁定角色所处的工会对象。无论角色是否已经加入工会（申请的工会也锁定）
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="guild">若锁定成功则返回工会对象。</param>
        /// <returns></returns>
        public bool Lock(GameChar gameChar, out GameGuild guild)
        {
            var slot = gameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.GuildSlotId);
            if (slot is null || !OwConvert.TryToGuid(slot.ExtraString, out var guildId))
            {
                VWorld.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                VWorld.SetLastErrorMessage("用户不在工会中。");
                guild = default;
                return false;
            }
            return Lock(guildId, Options.DefaultTimeout, out guild);
        }

        public void Unlock(GameGuild guild)
        {
            Monitor.Exit(guild);
        }

        /// <summary>
        /// 获取行会信息。会刷新工会成员信息。
        /// </summary>
        /// <param name="id"></param>
        /// <returns>返回行会对象，没有找到则返回null。</returns>
        public GameGuild GetGuild(Guid id)
        {
            if (!_Id2Guild.TryGetValue(id, out var result))
                return null;
            return result;
        }

        /// <summary>
        /// 获取指定角色当前所处行会。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns>指定角色当前所处行会(不含申请未通过的)。若没有加入行会则返回null。</returns>
        public GameGuild GetGuild(GameChar gameChar)
        {
            var slot = gameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.GuildSlotId && c.ExtraDecimal >= 10);
            if (slot is null)
                return null;
            if (!OwConvert.TryToGuid(slot.ExtraString, out var guildId))
                return null;
            return GetGuild(guildId);
        }

        /// <summary>
        /// 设置行会信息。
        /// </summary>
        public void SetGuild(SetGuildContext datas)
        {
            using var dw = datas.LockUser();
            var slot = datas.GameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.GuildSlotId);
            if (slot is null || !OwConvert.TryToGuid(slot.ExtraString, out var guildId) || slot.ExtraDecimal < 20)
            {
                datas.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                datas.ErrorMessage = "只能由工会会长设置。";
                return;
            }
            if (!Lock(guildId, Options.DefaultTimeout, out var guild))
            {
                datas.FillErrorFromWorld();
                return;
            }
            using var dwGuild = DisposeHelper.Create(Unlock, guild);
            guild.DisplayName = datas.DisplayName;
            guild.Properties["AutoAccept"] = datas.AutoAccept;
            guild.Properties["IconIndex"] = datas.IconIndex;
            guild.Properties["Bulletin"] = datas.Bulletin;
        }

        /// <summary>
        /// 获取该工会的当日工会任务。
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public SmallGameLogCollection GetMissionOrCreate(GameGuild guild, DateTime now)
        {
            //if (!Lock(guild.Id, Options.DefaultTimeout, out guild))
            //    return null;
            //using var dw = DisposeHelper.Create(c => Unlock(c), guild);
            var sgc = SmallGameLogCollection.Parse(guild.Properties, "mission");
            sgc.RemoveAll(c => c.DateTime < now.Date);
            if (sgc.Count <= 0)
            {
                var coll = World.ItemTemplateManager.Id2Mission.Values.Where(c => c.GroupNumber == "1001").ToList();
                foreach (var item in GameHelper.GetRandom(coll, VWorld.WorldRandom, 5).Select(c => c.Id))
                {
                    var tmp = sgc.Add(string.Empty, item, 1);
                    tmp.DateTime = now;
                }
                sgc.Save();
            }
            return sgc;
        }

        /// <summary>
        /// 增加工会经验并计算升级。
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="expInc"></param>
        public void Upgrade(GameGuild guild, decimal expInc, ICollection<GamePropertyChangeItem<object>> changes = null)
        {
            var exp = guild.GetDecimalWithFcpOrDefault("exp");  //经验
            var tt = guild.GetTemplate();
            var ary = tt.Properties.GetValueOrDefault("expLimit", Array.Empty<decimal>()) as decimal[];
            if (ary is null || ary.Length < 1) //若没有升级要求
                return;
            var lv = (int)guild.GetDecimalWithFcpOrDefault(World.PropertyManager.LevelPropertyName);
            exp += expInc;
            var index = Array.FindIndex(ary, c => c <= exp);
            if (index > -1 && index != lv + 1)    //若找到了匹配项
                World.ItemManager.SetLevel(guild, index + 1, changes);
            guild.SetPropertyAndMarkChanged("exp", exp, changes);
        }
        #endregion 基础操作

        /// <summary>
        /// 获取所有指定行会成员的行会槽的查询。
        /// </summary>
        /// <param name="guidId"></param>
        /// <param name="db"></param>
        /// <returns>成员角色的工会槽，跟踪器不会跟踪相关对象。包括申请者的槽。</returns>
        public IQueryable<GameItem> GetAllMemberSlotQuery(Guid guidId, DbContext db)
        {
            var str = guidId.ToString();
            var result = db.Set<GameItem>().Where(c => c.TemplateId == ProjectConstant.GuildSlotId && c.ExtraString == str);
            return result;
        }

        /// <summary>
        /// 获取当前角色所处的工会信息。
        /// </summary>
        /// <param name="datas"></param>
        public void GetGuild(GetGuildContext datas)
        {
            using var dw = datas.LockUser();
            var slot = datas.GameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.GuildSlotId);
            if (slot is null || !OwConvert.TryToGuid(slot.ExtraString, out var gid) || slot.ExtraDecimal < 10)
                return;
            if (!_Id2Guild.TryGetValue(gid, out var guild))
                return;
            datas.Guild = guild;
            var now = DateTime.UtcNow;
            //个人已完成的工会任务模板Id
            var guildMissions = World.MissionManager.GetGuildMission(datas.GameChar);
            datas.DoneGuildMissionTIds.AddRange(guildMissions.Select(c =>
            {
                if (!OwConvert.TryToGuid(c.Params[0], out var tid))
                    return Guid.Empty;
                return tid;
            }));
            //工会发布的任务
            var missions = GetMissionOrCreate(datas.Guild, now);
            datas.GuildMissionTIds.AddRange(missions.Select(c =>
            {
                if (!OwConvert.TryToGuid(c.Params[0], out var tid))
                    return Guid.Empty;
                return tid;
            }));
            return;
        }

        /// <summary>
        /// 获取指定工会的聊天频道号。
        /// </summary>
        /// <param name="guildId">工会的id,当前不校验有效性。</param>
        /// <returns>工会聊天频道号。</returns>
        public string GetGuildChatChannelId(Guid guildId)
        {
            return $"Guild{guildId}";
        }

        /// <summary>
        /// 获取指定工会的聊天频道号。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns>没有加入工会则返回null。</returns>
        public string GetGuildChatChannelId(GameChar gameChar)
        {
            var slot = gameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.GuildSlotId);
            if (slot is null || !OwConvert.TryToGuid(slot.ExtraString, out var guildId) || !_Id2Guild.ContainsKey(guildId))
            {
                VWorld.SetLastError(ErrorCodes.ERROR_INVALID_DATA);
                VWorld.SetLastErrorMessage("找不到指定行会。");
                return null;
            }
            return GetGuildChatChannelId(guildId);
        }

        #region 工会操作

        /// <summary>
        /// 创建行会。
        /// </summary>
        /// <param name="datas"></param>
        public void CreateGuild(CreateGuildContext datas)
        {
            using var dw = datas.LockUser();
            if (dw is null)
                return;
            var template = World.ItemTemplateManager.GetTemplateFromeId(ProjectConstant.GuildTemplateId);   //工会模板
            //校验代价
            List<(GameThingBase, IReadOnlyDictionary<string, object>)> list = new List<(GameThingBase, IReadOnlyDictionary<string, object>)>();
            var b = World.EventsManager.LookupItems(datas.GameChar, template.Properties, "create", list);  //获取代价
            if (!b || !World.EventsManager.Verify(list)) //若代价不足
            {
#if !DEBUG   //调试状态下不处理代价不足问题
                datas.HasError = true;
                datas.ErrorCode = ErrorCodes.RPC_S_OUT_OF_RESOURCES;
                return;
#endif
            }
            //校验人员是否已经有公会
            var slot = datas.GameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.GuildSlotId && c.ExtraDecimal >= 10);
            if (slot != null && OwConvert.TryToGuid(slot.ExtraString, out var guildId) && GetGuild(guildId) != null)
            {
                datas.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                datas.ErrorMessage = "已经在工会中，不可创建工会。";
                return;
            }
            //创建工会对象
            var guild = new GameGuild();
            var pg = DictionaryPool<string, object>.Shared.Get();
            lock (_Id2Guild)
            {
                if (_Id2Guild.Values.Any(c => c.DisplayName == datas.DisplayName))   //若重名
                {
                    datas.HasError = true;
                    datas.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                    datas.ErrorMessage = "重名的工会。";
                    return;
                }
                pg["tid"] = ProjectConstant.GuildTemplateId;
                pg["CreatorId"] = datas.GameChar.Id;
                pg[nameof(GameGuild.DisplayName)] = datas.DisplayName;
                World.EventsManager.GameGuildCreated(guild, pg);
                guild.Properties["AutoAccept"] = datas.AutoAccept;
                guild.Properties["IconIndex"] = datas.IconIndex;
            }
            lock (guild)
            {
                if (_Id2Guild.TryAdd(guild.Id, guild))  //若成功加入集合
                {
                    if (slot is null)
                    {
                        slot = new GameItem() { };
                        World.EventsManager.GameItemCreated(slot, ProjectConstant.GuildSlotId);
                        World.ItemManager.ForcedAdd(slot, datas.GameChar, datas.PropertyChanges);
                        datas.GameChar.GetDbContext().Add(slot);
                    }
                    guild.GetMemberIds().Add(datas.GameChar.Id);    //追加工会成员
                    guild.GetDbContext().Add(guild);
                    guild.GetDbContext().SaveChanges();
                    slot.ExtraString = guild.IdString;
                    slot.ExtraDecimal = (int)GuildDivision.会长;

                    var deletes = datas.GameChar.GameItems.Where(c => c.TemplateId == ProjectConstant.GuildSlotId && c != slot).ToArray();  //需要删除的槽
                    foreach (var item in deletes)
                        World.ItemManager.ForcedDelete(item, null, datas.PropertyChanges);
                }
                else
                {
                    datas.HasError = true;
                    datas.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                    datas.ErrorMessage = "并发创建。";
                    return;
                }
            }
            //修正代价
            foreach (var item in list)  //修正数据
            {
                if (item.Item1 is GameItem gi)
                {
                    var count = item.Item2.GetDecimalOrDefault("count");
                    World.ItemManager.ForcedSetCount(gi, gi.Count.Value - count, datas.PropertyChanges);
                }
            }
            datas.GameChar.GameUser.DbContext.SaveChanges();
            datas.Id = guild.Id;
            DictionaryPool<string, object>.Shared.Return(pg);
            //加入行会聊天
            this.JoinGuildChatChannel(datas.GameChar);
        }

        /// <summary>
        /// 移交行会。
        /// </summary>
        /// <param name="datas"></param>
        public void SendGuild(SendGuildContext datas)
        {
            using var dw = datas.LockAll();
            if (dw is null)
                return;
            var guildSlot = datas.GameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.GuildSlotId);
            if (guildSlot is null || guildSlot.ExtraDecimal < (int)GuildDivision.会长)    //若非会长
            {
                datas.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                datas.ErrorMessage = "只有会长才能转移工会";
                return;
            }
            var otherSlot = datas.OtherChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.GuildSlotId); //对方的工会槽
            if (otherSlot is null || otherSlot.ExtraString != guildSlot.ExtraString)
            {
                datas.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                datas.ErrorMessage = "只能转移给本工会的人";
                return;
            }
            if (!OwConvert.TryToGuid(guildSlot.ExtraString, out var guildId))
            {
                datas.ErrorCode = ErrorCodes.ERROR_INVALID_DATA;
                datas.ErrorMessage = "工会数据错误。";
                return;
            }
            if (!Lock(guildId, Options.DefaultTimeout, out var guild))
            {
                datas.FillErrorFromWorld();
                return;
            }
            using var dwGuild = DisposeHelper.Create(Unlock, guild);
            //移交工会
            guildSlot.ExtraDecimal = (int)GuildDivision.见习会员;
            otherSlot.ExtraDecimal = (int)GuildDivision.会长;
            World.CharManager.NotifyChange(datas.GameChar.GameUser);
            World.CharManager.NotifyChange(datas.OtherChar.GameUser);
        }

        /// <summary>
        /// 解散行会。
        /// </summary>
        /// <param name="datas"></param>
        public void DeleteGuild(DeleteGuildContext datas)
        {
            Guid guildId;
            GameGuild guild;
            using (var dw = datas.LockUser())
            {
                if (dw is null)
                    return;

                var slot = datas.GameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.GuildSlotId);
                if (slot is null || slot.ExtraDecimal < (int)GuildDivision.会长 || !OwConvert.TryToGuid(slot.ExtraString, out guildId))
                {
                    datas.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                    datas.ErrorMessage = "只有会长才能解散工会";
                    return;
                }
                else
                {
                    guild = GetGuild(guildId);
                    if (guild is null)
                    {
                        datas.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                        datas.ErrorMessage = "找不到指定行会。";
                        return;
                    }
                }
            }
            if (!Lock(guildId, Options.DefaultTimeout, out guild))
            {
                datas.FillErrorFromWorld();
                return;
            }
            using var dwGuild = DisposeHelper.Create(Unlock, guild);
            var db = guild.GetDbContext();
            var charIds = GetAllMemberSlotQuery(guildId, db).Select(c => c.OwnerId.Value); //会员角色id集合
            using var dwChars = World.CharManager.LockOrLoadWithCharIds(charIds, charIds.Count() * TimeSpan.FromSeconds(1));
            if (dwChars is null)
            {
                datas.ErrorCode = ErrorCodes.WAIT_TIMEOUT;
                datas.ErrorMessage = "无法锁定所有成员";
                return;
            }
            foreach (var charId in charIds) //删除每个成员的所属工会信息
            {
                var gc = World.CharManager.GetCharFromId(charId);
                var slot = gc.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.GuildSlotId && c.ExtraString == guild.IdString);
                if (slot is null)
                    continue;
                World.ItemManager.ForcedDelete(slot, null, datas.PropertyChanges);
                World.CharManager.NotifyChange(gc.GameUser);
            }
            //删除工会对象。
            db.Set<GameGuild>().Remove(guild);
            db.SaveChanges();
            guild.Dispose();
            _Id2Guild.Remove(guildId, out _);
            //获取所有工会成员
            World.ChatManager.RemoveChannel(GetGuildChatChannelId(guildId));
        }

        #endregion 工会操作

        #region 人事管理

        /// <summary>
        /// 调整权限。
        /// </summary>
        /// <param name="datas"></param>
        public void ModifyPermissions(ModifyPermissionsContext datas)
        {
            if (datas.Division >= 20 || datas.Division <= 0)
            {
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.ErrorMessage = "不能用此函数改变会长或除名。";
                return;
            }
            Guid guildId;
            using (var dwUser = datas.LockUser())
            {
                if (dwUser is null)
                    return;
                var slot = datas.GameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.GuildSlotId);
                if (slot is null || !OwConvert.TryToGuid(slot.ExtraString, out guildId))
                {
                    datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                    datas.ErrorMessage = "用户不在工会内。";
                    return;
                }
            }
            if (!Lock(guildId, Options.DefaultTimeout, out var guild))
            {
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.ErrorMessage = "找不到指定工会。";
                return;
            }
            using var dw = DisposeHelper.Create(Unlock, guild);
            //校验
            if (datas.Division == 14) //若设置管理
            {
                var countOfExists = GetAllMemberSlotQuery(guildId, guild.GetDbContext()).Where(c => c.ExtraDecimal == 14).Count();  //已有管理数量
                var maxCount = guild.GetDecimalWithFcpOrDefault("maxManagerCount");    //最大管理数量
                if (countOfExists + datas.CharIds.Count > maxCount)  //若超过
                {
                    datas.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                    datas.ErrorMessage = "超过最大管理员的数量。";
                    return;
                }
            }
            using var dwUsers = World.CharManager.LockOrLoadWithCharIds(datas.CharIds, Options.DefaultTimeout * datas.CharIds.Count);
            if (dwUsers is null)
            {
                datas.FillErrorFromWorld();
                return;
            }
            foreach (var charId in datas.CharIds)
            {
                var gc = World.CharManager.GetCharFromId(charId);
                var slot = gc.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.GuildSlotId);
                if (slot is null || guild.IdString != slot.ExtraString)
                {
                    datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                    datas.ErrorMessage = "至少一个成员不属于工会。";
                    return;
                }
                if (slot.ExtraDecimal < 10 || slot.ExtraDecimal >= 20)
                {
                    datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                    datas.ErrorMessage = "不能用此函数改变会长或除名。";
                    return;
                }
            }
            foreach (var charId in datas.CharIds)
            {
                var gc = World.CharManager.GetCharFromId(charId);
                var slot = gc.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.GuildSlotId);
                slot.ExtraDecimal = datas.Division;
                World.CharManager.NotifyChange(gc.GameUser);
            }
        }

        /// <summary>
        /// 申请加入行会。
        /// </summary>
        /// <param name="datas"></param>
        public void RequestJoin(RequestJoinContext datas)
        {
            if (!Lock(datas.GuildId, Options.DefaultTimeout, out var guild))
            {
                datas.FillErrorFromWorld();
                return;
            }
            using var dw = DisposeHelper.Create(Unlock, guild);
            using var dwUser = datas.LockUser();
            if (dwUser is null)
                return;
            //校验条件
            var slot = datas.GameChar.GameItems.FirstOrDefault(c => c.ExtraDecimal >= 10);

            if (slot != null)    //若已经处于一个工会内
            {
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.ErrorMessage = "已经处于一个工会内，不可申请新工会。";
                return;
            }
            slot = datas.GameChar.GameItems.FirstOrDefault(c => c.ExtraString == guild.IdString);
            if (slot != null)    //若已经在申请了
            {
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.ErrorMessage = "已经申请加入此工会，不可重复申请。";
                return;
            }
            var count = GetAllMemberSlotQuery(guild.Id, guild.GetDbContext()).Where(c => c.ExtraDecimal >= 10).Count();  //当前成员数
            if (guild.GetDecimalWithFcpOrDefault("maxMemberCount") <= count)   //若满员
            {
                datas.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;    //
                datas.ErrorMessage = "行会已满员。";    //
                return;
            }
            //开始申请
            if (slot is null)
            {
                slot = new GameItem();
                World.EventsManager.GameItemCreated(slot, ProjectConstant.GuildSlotId);
                World.ItemManager.ForcedAdd(slot, datas.GameChar, datas.PropertyChanges);
                datas.GameChar.GetDbContext().Add(slot);
            }
            slot.ExtraString = guild.IdString;
            var autoAccept = guild.Properties.GetBooleanOrDefaut("AutoAccept");
            if (autoAccept) //若自动接受
            {
                var subData = new AcceptJoinContext(datas.World, datas.GameChar)
                {
                    IsAccept = true,
                };
                subData.CharIds.Add(datas.GameChar.Id);
                AcceptJoin(subData);
                datas.FillErrorFrom(subData);
                if (datas.HasError)
                    return;
                datas.PropertyChanges.AddRange(subData.PropertyChanges);
            }
            else //未设置未自动接受
            {
                slot.ExtraDecimal = 0;
                World.CharManager.NotifyChange(datas.GameChar.GameUser);
                var db = datas.GameChar.GetDbContext();
                var guildIdString = datas.GuildId.ToString();
                var charIds = db.Set<GameItem>().AsNoTracking().Where(c => c.TemplateId == ProjectConstant.GuildSlotId && c.ExtraString == guildIdString && c.ExtraDecimal >= 14) //工会管理层
                     .Select(c => c.OwnerId.Value);
                foreach (var charId in charIds)
                {
                    NotifyChar(charId);
                }
            }
        }

        /// <summary>
        /// 批准加入行会的申请。
        /// </summary>
        /// <param name="datas"></param>
        public void AcceptJoin(AcceptJoinContext datas)
        {
            Guid guidId;
            GameGuild guild;
            using (var dw = datas.LockUser())
            {
                if (dw is null)
                    return;
                var slot = datas.GameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.GuildSlotId);
                if (slot is null || slot.ExtraDecimal < (int)GuildDivision.执事)
                {
                    datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                    datas.ErrorMessage = "工会高层才能批准加入申请。";
                    return;
                }
                guidId = OwConvert.ToGuid(slot.ExtraString);
            }
            if (!Lock(guidId, Options.DefaultTimeout, out guild))
            {
                datas.FillErrorFromWorld();
                return;
            }
            using var dwGuild = DisposeHelper.Create(Unlock, guild);
            var db = guild.GetDbContext();
            var qs = GetAllMemberSlotQuery(guidId, db).AsEnumerable().Where(c => c.ExtraDecimal ==decimal.Zero).Select(c => c.OwnerId.Value);  //所有申请者
            if (!datas.CharIds.All(c => qs.Contains(c)))
            {
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.ErrorMessage = "至少有一个角色没有申请该工会";
                return;
            }
            using var dwChars = World.CharManager.LockOrLoadWithCharIds(datas.CharIds, Options.DefaultTimeout * datas.CharIds.Count);   //锁定这组用户
            if (dwChars is null)
            {
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.ErrorMessage = "至少有一个角色id无效。";
                return;
            }
            foreach (var charId in datas.CharIds)
            {
                var gc = World.CharManager.GetCharFromId(charId);
                var slot = gc.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.GuildSlotId);
                if (slot is null || guild.IdString != slot.ExtraString || slot.ExtraDecimal != 0)
                {
                    datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                    datas.ErrorMessage = "至少有一个角色没有申请该工会";
                    return;
                }
            }
            if (datas.IsAccept) //若接受
                foreach (var charId in datas.CharIds)
                {
                    var gc = World.CharManager.GetCharFromId(charId);
                    var slot = gc.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.GuildSlotId);
                    slot.ExtraDecimal = (int)GuildDivision.见习会员;
                    slot.ExtraString = guild.IdString;
                    this.JoinGuildChatChannel(gc);  //加入工会聊天
                    NotifyChar(gc.Id);
                    //清理其它申请
                    var deletes = gc.GameItems.Where(c => c.TemplateId == ProjectConstant.GuildSlotId && c != slot).ToArray();
                    deletes.ForEach(c => World.ItemManager.ForcedDelete(c, null, datas.PropertyChanges));
                    gc.GetDbContext().SaveChanges();
                }
            else //若拒绝
                foreach (var charId in datas.CharIds)
                {
                    var gc = World.CharManager.GetCharFromId(charId);
                    var slot = gc.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.GuildSlotId);
                    World.ItemManager.ForcedDelete(slot, null, datas.PropertyChanges);
                    NotifyChar(gc.Id);
                    gc.GetDbContext().SaveChanges();
                }

        }

        /// <summary>
        /// 获取工会所有的申请人列表。
        /// </summary>
        /// <param name="datas"></param>
        public void GetRequestCharIds(GetRequestCharIdsContext datas)
        {
            Guid guildId;
            using (var dwUser = datas.LockUser())
            {
                if (dwUser is null)
                    return;
                var slot = datas.GameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.GuildSlotId);
                if (slot is null || !OwConvert.TryToGuid(slot.ExtraString, out guildId))
                {
                    datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                    datas.ErrorMessage = "找不到工会。";
                    return;
                }
            }
            List<Guid> result = new List<Guid>();
            if (!Lock(guildId, Options.DefaultTimeout, out var guild))
            {
                datas.FillErrorFromWorld();
                return;
            }
            using var dw = DisposeHelper.Create(Unlock, guild);
            var db = guild.GetDbContext();
            datas.CharIds.AddRange(GetAllMemberSlotQuery(guildId, db).Where(c => c.ExtraDecimal == 0).Select(c => c.OwnerId.Value));
        }

        /// <summary>
        /// 移除工会成员。
        /// </summary>
        /// <param name="datas"></param>
        public void RemoveMembers(RemoveMembersContext datas)
        {
            Guid charId;
            Guid guildId;
            IEnumerable<Guid> allCharIds;
            using (var dwUser = datas.LockUser())
            {
                if (dwUser is null)
                    return;
                var slot = datas.GameChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.GuildSlotId);
                if (slot is null || !OwConvert.TryToGuid(slot.ExtraString, out guildId))
                {
                    datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                    datas.ErrorMessage = "找不到工会。";
                    return;
                }
                charId = datas.GameChar.Id;
            }
            //锁定工会
            if (!Lock(guildId, Options.DefaultTimeout, out var guild))
            {
                datas.FillErrorFromWorld();
                return;
            }
            using var dwGuild = DisposeHelper.Create(Unlock, guild);
            //锁定角色
            allCharIds = datas.CharIds.Append(charId);  //获取所有角色id
            using var dw = World.CharManager.LockOrLoadWithCharIds(allCharIds, allCharIds.Count() * Options.DefaultTimeout);
            if (dw is null)
            {
                datas.FillErrorFromWorld();
                return;
            }
            var gc = World.CharManager.GetCharFromId(charId);
            var slotGc = gc.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.GuildSlotId);
            if (slotGc is null || !OwConvert.TryToGuid(slotGc.ExtraString, out var tmpId) || tmpId != guildId)
            {
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.ErrorMessage = "至少有一个角色不是该行会成员";
                return;
            }
            foreach (var id in datas.CharIds)   //逐一校验角色
            {
                var tmp = World.CharManager.GetCharFromId(id);
                var slot = tmp.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.GuildSlotId);
                if (slot is null || guild.IdString != slot.ExtraString)
                {
                    datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                    datas.ErrorMessage = "至少有一个角色不是该行会成员";
                    return;
                }
                if (slot.ExtraDecimal >= slotGc.ExtraDecimal && slot.Id != slotGc.Id)
                {
                    datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                    datas.ErrorMessage = "没有足够权限删除成员。";
                    return;
                }
                if (slotGc.ExtraDecimal >= (int)GuildDivision.会长)
                {
                    datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                    datas.ErrorMessage = "会长不能退出工会，应先移交会长权限。";
                    return;
                }
            }
            var channelId = GetGuildChatChannelId(guildId); //工会聊天频道id

            foreach (var id in datas.CharIds)   //逐一删除
            {
                var tmp = World.CharManager.GetCharFromId(id);
                var slot = tmp.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.GuildSlotId);
                World.ItemManager.ForcedDelete(slot);
                World.CharManager.NotifyChange(tmp.GameUser);
                World.ChatManager.LeaveChannel(tmp.Id.ToString(), channelId, Options.DefaultTimeout);
                //通知被删除的在线个人
                NotifyChar(id);
            }
        }
        #endregion 人事管理

        /// <summary>
        /// 通知在线角色，工会信息已经变化。
        /// </summary>
        /// <param name="charId"></param>
        /// <returns></returns>
        public bool NotifyChar(Guid charId)
        {
            var gc = World.CharManager.GetCharFromId(charId);
            using var dwUser = World.CharManager.LockAndReturnDisposer(gc.GameUser);
            if (dwUser is null)  //若无法锁定用户
                return false;
            var lst = World.CharManager.GetChangeData(gc);  //通知数据对象
            var slot = gc.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.GuildSlotId);
            var np = new ChangeData()
            {
                ActionId = 2,
                NewValue = 0,
                ObjectId = slot?.Id ?? Guid.Empty,
                OldValue = 0,
                PropertyName = "Count",
                TemplateId = ProjectConstant.GuildSlotId,
            };
            lst.Add(np);
            return true;
        }
    }

    public class SetGuildContext : GameCharGameContext
    {
        public SetGuildContext([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public SetGuildContext([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public SetGuildContext([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 行会名。
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 工会图标。
        /// </summary>
        public int IconIndex { get; set; }

        /// <summary>
        /// 是否自动接受加入申请。
        /// </summary>
        public bool AutoAccept { get; set; }

        /// <summary>
        /// 设置群公告。
        /// </summary>
        public string Bulletin { get; set; }

    }

    public class RemoveMembersContext : GameCharGameContext
    {
        public RemoveMembersContext([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public RemoveMembersContext([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public RemoveMembersContext([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 要除名成员。
        /// </summary>
        public List<Guid> CharIds { get; } = new List<Guid>();

    }

    public class GetRequestCharIdsContext : GameCharGameContext
    {
        public GetRequestCharIdsContext([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public GetRequestCharIdsContext([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public GetRequestCharIdsContext([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        public List<Guid> CharIds { get; } = new List<Guid>();
    }

    public class AcceptJoinContext : GameCharGameContext
    {
        public AcceptJoinContext([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public AcceptJoinContext([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public AcceptJoinContext([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 要批准加入的角色id集合。
        /// </summary>
        public List<Guid> CharIds { get; } = new List<Guid>();

        /// <summary>
        /// 是否接受。true表示接受，false表示拒绝。
        /// </summary>
        public bool IsAccept { get; set; }
    }

    public class RequestJoinContext : GameCharGameContext
    {
        public RequestJoinContext([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public RequestJoinContext([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public RequestJoinContext([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 申请加入的行会。
        /// </summary>
        public Guid GuildId { get; set; }
    }

    public static class GameGuildExtensions
    {
        /// <summary>
        /// 获取成员角色Id的集合。
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public static List<Guid> GetMemberIds(this GameGuild guild)
        {
            return guild.RuntimeProperties.GetOrAdd("MemberIds", c => new List<Guid>()) as List<Guid>;
        }

        /// <summary>
        /// 加入工会聊天频道。仅当指定角色已经由用户登录的时候才有效。
        /// </summary>
        /// <param name="mng"></param>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        public static bool JoinGuildChatChannel(this GameAllianceManager mng, GameChar gameChar)
        {
            var cm = mng.World.ChatManager;
            if (cm != null)  //若有聊天服务
            {
                if (!mng.World.CharManager.IsOnline(gameChar.Id))    //若用户未登录
                {
                    return false;
                }
                var channelId = mng.GetGuildChatChannelId(gameChar);
                if (channelId is null)
                    return false;
                return cm.JoinOrCreateChannel(gameChar.Id.ToString(), channelId, cm.Options.LockTimeout, null);

            }
            else
            {
                VWorld.SetLastError(ErrorCodes.ERROR_IMPLEMENTATION_LIMIT);
                VWorld.SetLastErrorMessage("没有聊天服务");
                return false;
            }
        }

    }

    public class DeleteGuildContext : ComplexWorkGameContext
    {
        public DeleteGuildContext([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public DeleteGuildContext([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public DeleteGuildContext([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }
    }

    public class SendGuildContext : BinaryRelationshipGameContext
    {
        public SendGuildContext([NotNull] IServiceProvider service, [NotNull] GameChar gameChar, Guid otherGCharId) : base(service, gameChar, otherGCharId)
        {
        }

        public SendGuildContext([NotNull] VWorld world, [NotNull] GameChar gameChar, Guid otherGCharId) : base(world, gameChar, otherGCharId)
        {
        }

        public SendGuildContext([NotNull] VWorld world, [NotNull] string token, Guid otherGCharId) : base(world, token, otherGCharId)
        {
        }
    }

    /// <summary>
    /// 调整权限。
    /// </summary>
    public class ModifyPermissionsContext : GameCharGameContext
    {
        public ModifyPermissionsContext([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public ModifyPermissionsContext([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public ModifyPermissionsContext([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 要修改成的权限。10=普通会员，14=管理。
        /// </summary>
        public int Division { get; set; }

        /// <summary>
        /// 要修改的角色id集合。
        /// </summary>
        public List<Guid> CharIds { get; } = new List<Guid>();
    }

    /// <summary>
    /// 获取行会信息。
    /// </summary>
    public class GetGuildContext : ComplexWorkGameContext
    {
        public GetGuildContext([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public GetGuildContext([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public GetGuildContext([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 返回的工会信息。若角色没有加入工会则返回null。
        /// </summary>
        public GameGuild Guild { get; set; }

        /// <summary>
        /// 个人已经完成的工会任务模板id。
        /// </summary>
        public List<Guid> DoneGuildMissionTIds { get; set; } = new List<Guid>();

        /// <summary>
        /// 工会今天发布的任务模板id。
        /// </summary>
        public List<Guid> GuildMissionTIds { get; set; } = new List<Guid>();

    }

    /// <summary>
    /// 创建行会函数使用的上下文。
    /// </summary>
    public class CreateGuildContext : ComplexWorkGameContext
    {
        public CreateGuildContext([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public CreateGuildContext([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public CreateGuildContext([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 行会名。
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 工会图标。
        /// </summary>
        public int IconIndex { get; set; }

        /// <summary>
        /// 是否自动接受加入申请。
        /// </summary>
        public bool AutoAccept { get; set; }


        /// <summary>
        /// 返回行会Id。使用此Id查询行会的详细信息。
        /// </summary>
        public Guid Id { get; set; }

    }
}
