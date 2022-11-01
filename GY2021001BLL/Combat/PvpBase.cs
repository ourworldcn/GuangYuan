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
        public GetPvpListCommand()
        {

        }

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
            var pvp = GameContext.GameChar.GetPvpObject();
            if (pvp is null)
            {
                command.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                command.DebugMessage = "角色没有pvp战斗功能。";
                return;
            }
            var world = GameContext.Service.GetRequiredService<VWorld>();
            var info = pvp.GetJsonObject<PvpObjectJsonObject>();

            var gitm = world.ItemTemplateManager;
            var pvpTT = gitm.GetTemplateFromeId(pvp.ExtraGuid); //模板

            var topN = pvpTT.GetSdpDecimalOrDefault("TopN", 10);
            IEnumerable<Guid> ids;

            if (command.CharId.HasValue) //若强行指定了对手id
            {
                ids = new Guid[] { command.CharId.Value };
            }
            else if (info.PvpCount <= topN)    //如果是此角色前N次搜索
            {
                ids = GetListInRobot(Array.Empty<Guid>());
                if (!ids.Any())  //若未搜索到
                {
                    info.SearchList.RemoveAll(c => c.DateTime.Date == GameContext.UtcNow.Date);
                    ids = GetListInRobot(Array.Empty<Guid>());
                }
            }
            else //若不是前N次
            {
                ids = GetNewPvpCharIds(Array.Empty<Guid>());
                if (!ids.Any())  //若没有合适的角色
                {
                    info.SearchList.RemoveAll(c => c.DateTime.Date == GameContext.UtcNow.Date); //清空当日列表
                    ids = GetNewPvpCharIds(Array.Empty<Guid>());
                }
            }
            command.CharIds.AddRange(ids);
            info.SearchList.AddRange(ids.Select(c => new IdAndDateTime() { Id = c, DateTime = GameContext.UtcNow }));
            info.SearchCount++; //增加已搜索的次数
            var commandPost = new PvpListGotCommand() { Command = command };
            world.CharManager.NotifyChange(GameContext.GameChar.GameUser);    //修改用户数据
            GameContext.GetCommandManager().Handle(commandPost);
        }

        #endregion IGameCommandHandler接口及相关

        /// <summary>
        /// 在机器人中执行最小差值搜索。
        /// </summary>
        /// <param name="excludes">排除的角色Id集合。</param>
        /// <returns></returns>
        public IEnumerable<Guid> GetListInRobot(IEnumerable<Guid> excludes)
        {
            var pvpObject = GameContext.GameChar.GetPvpObject();
            decimal pvpScore = pvpObject.ExtraDecimal ?? 0;
            var info = pvpObject.GetJsonObject<PvpObjectJsonObject>();
            excludes = info.SearchList.Where(c => c.DateTime.Date == GameContext.UtcNow.Date).Select(c => c.Id).Union(excludes). //排除额外需要排除的角色
                Union(new Guid[] { GameContext.GameChar.Id }).ToArray();   //排除自己

            List<Guid> result = new List<Guid>();
            var db = GameContext.Service.GetRequiredService<GY001UserContext>();
            var collBase = from pvp in db.Set<GameItem>()
                           where pvp.ExtraGuid == ProjectConstant.PvpObjectTId
                           join slot in db.Set<GameItem>() on pvp.ParentId equals slot.Id   //货币槽
                           join gc in db.Set<GameChar>().Where(c => !excludes.Contains(c.Id) && (c.CharType == CharType.Unknow || c.CharType.HasFlag(CharType.Robot))) //限于特别机器人
                           on slot.OwnerId equals gc.Id    //角色
                           select new { pvp, gc };
            var great = collBase.OrderBy(c => c.pvp.ExtraDecimal).Where(c => c.pvp.ExtraDecimal >= pvpScore).Take(1);   //上手
            var less = collBase.OrderByDescending(c => c.pvp.ExtraDecimal).Where(c => c.pvp.ExtraDecimal < pvpScore).Take(1);   //下手
            var coll = great.Concat(less).Select(c => new { c.gc.Id, diff = Math.Abs((c.pvp.ExtraDecimal ?? 0) - pvpScore) }).ToArray();
            var collResult = coll.OrderBy(c => c.diff).Take(1).Select(c => c.Id);
            result.AddRange(collResult);
            return result;
        }

        public List<Guid> GetNewPvpCharIds(IEnumerable<Guid> excludes)
        {
            var db = GameContext.Service.GetRequiredService<GY001UserContext>();
            Guid charId = GameContext.GameChar.Id;
            var pvpObject = GameContext.GameChar.GetPvpObject();
            decimal pvpScore = pvpObject.ExtraDecimal ?? 0; //自身pvp积分
            var info = pvpObject.GetJsonObject<PvpObjectJsonObject>();
            var lv = (int)GameContext.GameChar.GetSdpDecimalOrDefault("lv");

            excludes = info.SearchList.Where(c => c.DateTime.Date == GameContext.UtcNow.Date).Select(c => c.Id).Union(excludes). //排除额外需要排除的角色
                Union(new Guid[] { GameContext.GameChar.Id }).ToArray();   //排除自己
            var maxDiff = pvpObject.GetSdpDecimalOrDefault("MaxDiff", 50);  //第一等级最大分差
            var charTypes = new CharType[] { CharType.Unknow, CharType.Robot, CharType.Test };

            var collBase = from pvp in db.Set<GameItem>()
                           where pvp.ExtraGuid == ProjectConstant.PvpObjectTId  //取pvp对象
                           join gc in db.Set<GameChar>()
                           on pvp.Parent.OwnerId equals gc.Id
                           where charTypes.Contains(gc.CharType) //过滤用户类型
                            && !excludes.Contains(gc.Id)  //排除指定角色
                           select new { gc, pvp };  //基础查询

            var coll = from tmp in collBase
                       where tmp.pvp.ExtraDecimal.Value <= pvpScore + maxDiff && tmp.pvp.ExtraDecimal.Value >= pvpScore - maxDiff   //在第一等级分差以内
                       orderby Math.Abs(tmp.pvp.ExtraDecimal.Value - pvpScore), //按分差
                       Math.Abs((string.IsNullOrWhiteSpace(SqlDbFunctions.JsonValue(tmp.gc.JsonObjectString, "$.Lv")) ? 0 : Convert.ToInt32(SqlDbFunctions.JsonValue(tmp.gc.JsonObjectString, "$.Lv"))) - lv) //按等级差升序排序
                       select tmp.gc.Id;
            var list = coll.ToList();   //获取所有
            if (list.Count <= 0)   //若没找到
            {
                coll = from pvp in db.Set<GameItem>()
                       join gc in db.Set<GameChar>()
                       on pvp.Parent.OwnerId equals gc.Id
                       where pvp.ExtraGuid == ProjectConstant.PvpObjectTId  //取pvp对象
                        && (pvp.ExtraDecimal.Value > pvpScore + maxDiff || pvp.ExtraDecimal.Value < pvpScore - maxDiff)    //分差在50以外
                        && charTypes.Contains(gc.CharType) //过滤用户类型
                        && !excludes.Contains(gc.Id)   //排除指定的角色id
                       orderby Math.Abs(pvp.ExtraDecimal.Value - pvpScore) //按分差
                       select gc.Id;
                list = coll.Take(1).ToList();
                //if (list.Count <= 0 && ary.Length > 0)   //若没找到
                //{
                //    var item = ary[VWorld.WorldRandom.Next(ary.Length)];    //取已经打过的随机一个人
                //    list.Add(item);
                //}
            }
            else //若找到
            {
                var rnd = VWorld.WorldRandom.Next(list.Count);
                var tmp = list[rnd];
                list = new List<Guid>() { tmp };
            }
            return list;
        }

    }

    public class IdAndDateTime
    {
        public IdAndDateTime()
        {

        }

        public Guid Id { get; set; }

        public DateTime DateTime { get; set; }
    }

    public class PvpObjectJsonObject
    {
        public PvpObjectJsonObject()
        {

        }

        /// <summary>
        /// 此用户在整个生命周期内，搜索PVP的总次数。
        /// </summary>
        public int SearchCount { get; set; }

        /// <summary>
        /// 被搜索到的角色Id集合。
        /// </summary>
        public List<IdAndDateTime> SearchList { get; set; } = new List<IdAndDateTime>();

        /// <summary>
        /// 已经进行的主动Pvp的总次数。
        /// </summary>
        public int PvpCount { get; set; }

        /// <summary>
        /// 已经进行pvp行为的人的集合。
        /// </summary>
        public List<Guid> PvpList { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 已经获取了一个pvp列表的后置事件。
    /// </summary>
    public class PvpListGotCommand : GameCommandBase
    {
        /// <summary>
        /// 获取pvp列表的命令。
        /// </summary>
        public GetPvpListCommand Command { get; set; }

    }

    public class PvpListGotCommandHandler : GameCommandHandlerBase<PvpListGotCommand>
    {

        public PvpListGotCommandHandler(GameCommandContext gameContext)
        {
            GameContext = gameContext;
        }

        public GameCommandContext GameContext { get; set; }

        public override void Handle(PvpListGotCommand command)
        {
            var world = GameContext.Service.GetRequiredService<VWorld>();
            var pvp = GameContext.GameChar.GetPvpObject();

            var ids = command.Command.CharIds;
            var info = pvp.GetJsonObject<PvpObjectJsonObject>();
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
                    command.Command.Combat = combat;
                    cache.SetDirty(key);
                }
            }
        }
    }
}
