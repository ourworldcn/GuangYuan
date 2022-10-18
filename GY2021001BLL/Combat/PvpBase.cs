using GuangYuan.GY001.BLL.Homeland;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using GuangYuan.GY001.UserDb.Combat;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using OW.Extensions.Game.Store;
using OW.Game;
using OW.Game.Caching;
using OW.Game.Item;
using OW.Game.Log;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace GuangYuan.GY001.BLL
{
    public class StartCombatPvpData : ComplexWorkGameContext
    {
        public StartCombatPvpData([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public StartCombatPvpData([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public StartCombatPvpData([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        public Guid CombatId { get; set; }

        /// <summary>
        /// 原始战斗id,如果没有则为空(如主动pvp就没有)。
        /// </summary>
        public Guid? OldCombatId { get; set; }

        /// <summary>
        /// 关卡Id。pvp三类型的关卡大关模板id.
        /// </summary>
        public Guid DungeonId { get; set; }

    }

    /// <summary>
    /// pvp结束战斗调用接口的数据封装类。
    /// </summary>
    public class EndCombatPvpWorkData : ChangeItemsWorkDatasBase
    {
        public EndCombatPvpWorkData([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public EndCombatPvpWorkData([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public EndCombatPvpWorkData([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 战斗对象唯一Id。从邮件的 mail.Properties["CombatId"] 属性中获取。或从获取战斗对手接口中获取。
        /// </summary>
        public Guid CombatId { get; set; }

        /// <summary>
        /// 当前日期。
        /// </summary>
        public DateTime Now { get; set; }

        /// <summary>
        /// 主控室剩余血量的百分比。
        /// 0表示空血，1表示满血。
        /// </summary>
        public decimal MainRoomRhp { get; set; }

        /// <summary>
        /// 木材仓剩余血量的百分比。合并多个木材仓的总血量剩余的百分比。
        /// 0表示空血，1表示满血。
        /// </summary>
        public decimal StoreOfWoodRhp { get; set; }

        /// <summary>
        /// 玉米田剩余血量的百分比。
        /// 0表示空血，1表示满血。
        /// </summary>
        public decimal GoldRhp { get; set; }

        /// <summary>
        /// 木材林剩余血量的百分比。
        /// 0表示空血，1表示满血。
        /// </summary>
        public decimal WoodRhp { get; set; }

        /// <summary>
        /// 摧毁建筑的模板Id集合。
        /// </summary>
        public List<(Guid, decimal)> DestroyTIds { get; } = new List<(Guid, decimal)>();

        /// <summary>
        /// 击毁木材仓的数量。
        /// </summary>
        public int DestroyCountOfWoodStore { get; set; }

        /// <summary>
        /// 战利品。
        /// </summary>
        public List<GameItem> Booty { get; set; } = new List<GameItem>();

        /// <summary>
        /// 返回本次战斗的战斗对象。
        /// </summary>
        public GameCombat Combat { get; set; }

        public Guid MailId { get; internal set; }


    }

    /// <summary>
    /// 获取pvp列表的命令。
    /// </summary>
    public class GetPvpListCommand : WithChangesGameCommandBase
    {
        /// <summary>
        /// 是否强制使用钻石刷新。
        /// false,不刷新，获取当日已经刷的最后一次数据,如果今日未刷则自动刷一次。
        /// true，强制刷新，根据设计可能需要消耗资源。
        /// 目前保留为true。
        /// </summary>
        public bool IsRefresh { get; set; }

        /// <summary>
        /// 强制指定一个角色Id。仅调试接口可用。省略或为空均按规则搜索。
        /// </summary>
        public Guid? CharId { get; set; }

        /// <summary>
        /// 返回的战斗对象。
        /// </summary>
        public GameCombat Combat { get; internal set; }

        private List<Guid> _CharIds;
        /// <summary>
        /// 可pvp角色的列表。
        /// </summary>
        public List<Guid> CharIds => _CharIds ??= new List<Guid>();

    }

    /// <summary>
    /// 获取pvp列表的命令处理器。
    /// </summary>
    public sealed class GetPvpListCommandHandler : GameCommandHandlerBase<GetPvpListCommand>
    {
        public GetPvpListCommandHandler()
        {

        }

        public GetPvpListCommandHandler(GameCommandContext gameContext)
        {
            GameContext = gameContext;
        }

        public GameCommandContext GameContext { get; set; }

        #region IGameCommandHandler接口及相关

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="command"></param>
        public override void Handle(GetPvpListCommand command)
        {
            using var dwUser = GameContext.LockUser();    //锁定用户
            if (dwUser is null) //若无法锁定
            {
                command.FillErrorFromWorld();
                return;
            }
            var world = GameContext.Service.GetRequiredService<VWorld>();
            var pvp = GameContext.GameChar.GetPvpObject();
            var todayData = pvp?.GetOrCreateBinaryObject<TodayTimeGameLog<Guid>>();    //当日数据的帮助器类
            if (todayData is null)
            {
                command.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                command.DebugMessage = "角色没有pvp战斗功能。";
                return;
            }
            var hasData = todayData?.GetTodayData(GameContext.UtcNow).Any() ?? false;
            if (command.IsRefresh || !hasData) //若强制刷新或需要刷新
            {
                //修改数据
                //获取列表
                todayData.ResetLastData(GameContext.UtcNow);
                //var ids = RefreshPvpList(datas.GameChar, datas.UserDbContext, todayData.GetTodayData(datas.Now));
                var excludeIds = todayData.GetTodayData(GameContext.UtcNow);
                //todayData.AddLastDataRange(ids, datas.Now);
                //datas.CharIds.AddRange(todayData.GetLastData(datas.Now));

                var gc = GameContext.GameChar;
                var cj = gc.GetJsonObject<CharJsonEntity>();
                List<Guid> ids;
                if (!command.CharId.HasValue)
                    ids =world.SocialManager.GetNewPvpCharIds(GameContext.GameChar.GetDbContext(), gc.Id, pvp.ExtraDecimal.Value, cj.Lv, excludeIds);
                else //TODO 测试代码
                {
                    ids = new List<Guid>();
                    ids.Add(command.CharId.Value);
                }
                todayData.AddLastDataRange(ids, GameContext.UtcNow);
                command.CharIds.AddRange(ids);
                if (ids.Any())
                {
                    var charId = ids.First();
                    using var dwUser2 = world.CharManager.LockOrLoad(charId, out var gu);
                    if (dwUser2 != null)
                    {
                        //生成战斗对象
                        var id = Guid.NewGuid();
                        var key = id.ToString();
                        var cache = world.GameCache;
                        using var dw = cache.Lock(key);
                        var combat = GameCombat.CreateNew(world, id);
                        combat.MapTId = ProjectConstant.PvpDungeonTId;
                        var soldier = combat.CreateSoldier(-1);
                        GameCombat.RecordResource(soldier, gu.CurrentChar);
                        cache.GetOrCreate(key, (entry) =>
                        {
                            combat.SetEntry(entry as GameObjectCache.GameObjectCacheEntry);
                            return combat.Thing;
                        });
                        command.Combat = combat;
                        cache.SetDirty(key);
                    }
                }
            }
            else //不刷新
            {
                command.CharIds.AddRange(todayData.GetLastData(GameContext.UtcNow));
            }
            //两种情况都要修改的数据
            //todayData.Save();   //保存当日当次数据
            world.CharManager.NotifyChange(GameContext.GameChar.GameUser);    //修改用户数据
        }

        #endregion IGameCommandHandler接口及相关
    }
}
