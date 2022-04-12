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

namespace GuangYuan.GY001.UserDb.Social
{
    public class GameAllianceManagerOptions
    {

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
            var template = World.ItemTemplateManager.GetTemplateFromeId(ProjectConstant.GuildTemplateId);
            var costs = template.Properties.GetValuesWithoutPrefix("create"); 
            
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
            datas.Id = guild.Id;
            DictionaryPool<string, object>.Shared.Return(pg);
        }

        /// <summary>
        /// 移交行会。
        /// </summary>
        /// <param name="datas"></param>
        public void SendGuild(SendGuildContext datas)
        {

        }

        /// <summary>
        /// 解散行会。
        /// </summary>
        /// <param name="datas"></param>
        public void DeleteGuild(DeleteGuildContext datas)
        {

        }

        /// <summary>
        /// 获取行会信息。会刷新工会成员信息。
        /// </summary>
        /// <param name="id"></param>
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
    }
}
