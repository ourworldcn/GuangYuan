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
        public GameAllianceManager()
        {
        }

        public GameAllianceManager(IServiceProvider service) : base(service)
        {
        }

        public GameAllianceManager(IServiceProvider service, GameAllianceManagerOptions options) : base(service, options)
        {
        }

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

        public void Unlock(GameGuild guild)
        {
            Monitor.Exit(guild);
        }
        #endregion 基础操作

        /// <summary>
        /// 获取所有指定行会成员的行会槽的查询。
        /// </summary>
        /// <param name="guidId"></param>
        /// <param name="db"></param>
        /// <returns>成员角色的工会槽，跟踪器不会跟踪相关对象。</returns>
        public IQueryable<GameItem> GetAllMemberSlotQuery(Guid guidId, DbContext db)
        {
            var str = guidId.ToString();
            return db.Set<GameItem>().AsNoTracking().Where(c => c.TemplateId == ProjectConstant.GuildSlotId && c.ExtraString == str && c.ExtraDecimal > 0);
        }

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
            var coll = World.EventsManager.LookupItems(datas.GameChar, template.Properties, "create");  //获取代价
            foreach (var item in coll)  //校验代价
            {
                GameItem gi = item.Item1 as GameItem;
                var count = item.Item2.GetDecimalOrDefault("count");
                if (gi is null || gi.Count < count)
                {
                    datas.HasError = true;
                    datas.ErrorCode = ErrorCodes.RPC_S_OUT_OF_RESOURCES;
                    return;
                }
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
                World.EventsManager.GameGuildCreated(guild, pg);
            }
            lock (guild)
            {
                if (_Id2Guild.TryAdd(guild.Id, guild))  //若成功加入集合
                {
                    guild.GetMemberIds().Add(datas.GameChar.Id);    //追加工会成员
                    guild.GetDbContext().SaveChanges();
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
            foreach (var item in coll)  //修正数据
            {
                GameItem gi = item.Item1 as GameItem;
                var count = item.Item2.GetDecimalOrDefault("count");
                World.ItemManager.ForcedSetCount(gi, gi.Count.Value - count, datas.Changes);
            }
            datas.Id = guild.Id;
            DictionaryPool<string, object>.Shared.Return(pg);
        }

        /// <summary>
        /// 移交行会。
        /// </summary>
        /// <param name="datas"></param>
        public void SendGuild(SendGuildContext datas)
        {
            var dw = datas.LockAll();
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
            if (otherSlot is null || otherSlot.ExtraString != otherSlot.ExtraString)
            {
                datas.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                datas.ErrorMessage = "只能转移给本工会的人";
                return;
            }
            if (OwConvert.TryToGuid(guildSlot.ExtraString, out var guildId))
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
                        datas.ErrorMessage = "只有会长才能解散工会";
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
            var charIds = db.Set<GameItem>().AsNoTracking().Where(c => c.TemplateId == ProjectConstant.GuildSlotId && c.OwnerId.HasValue).Select(c => c.OwnerId.Value); //会员角色id集合
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
                var slot = gc.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.GuildSlotId);
                if (slot is null)
                    continue;
                World.ItemManager.ForcedDelete(slot);
                World.CharManager.NotifyChange(gc.GameUser);
            }
            //删除工会对象。
            db.Set<GameGuild>().Remove(guild);
            db.SaveChanges();
            guild.Dispose();
            _Id2Guild.Remove(guildId, out _);
            //获取所有工会成员
        }

        /// <summary>
        /// 获取行会信息。会刷新工会成员信息。
        /// </summary>
        /// <param name="id"></param>
        /// <returns>返回行会对象，没有找到则返回null。</returns>
        public GameGuild GetGuild(Guid id)
        {
            if (_Id2Guild.TryGetValue(id, out var result))
                return null;
            return result;
        }

        /// <summary>
        /// 调整权限。
        /// </summary>
        /// <param name="datas"></param>
        public void ModifyPermissions(ModifyPermissions datas)
        {

        }
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
    public class ModifyPermissions : BinaryRelationshipGameContext
    {
        public ModifyPermissions([NotNull] IServiceProvider service, [NotNull] GameChar gameChar, Guid otherGCharId) : base(service, gameChar, otherGCharId)
        {
        }

        public ModifyPermissions([NotNull] VWorld world, [NotNull] GameChar gameChar, Guid otherGCharId) : base(world, gameChar, otherGCharId)
        {
        }

        public ModifyPermissions([NotNull] VWorld world, [NotNull] string token, Guid otherGCharId) : base(world, token, otherGCharId)
        {
        }
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

        public List<Guid> GuildIds { get; set; } = new List<Guid>();

        public List<GameGuild> Guild { get; set; } = new List<GameGuild>();
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
        /// 返回行会Id。使用此Id查询行会的详细信息。
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 详细的变化信息。
        /// </summary>
        public List<GamePropertyChangeItem<object>> Changes { get; } = new List<GamePropertyChangeItem<object>>();
    }
}
