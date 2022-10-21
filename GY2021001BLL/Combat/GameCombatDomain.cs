using AutoMapper;
using GuangYuan.GY001.BLL;
using GuangYuan.GY001.BLL.Homeland;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using OW.DDD;
using OW.Game;
using OW.Game.Caching;
using OW.Game.Item;
using OW.Game.PropertyChange;
using OW.Game.Store;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GuangYuan.GY001.UserDb.Combat
{
    public class IdAndNumber
    {
        public Guid Id { get; set; }

        public decimal Number { get; set; }

        public void Clear()
        {
            Id = default;
            Number = default;
        }
    }

    /// <summary>
    /// 战斗的聚合根。
    /// </summary>
    public class GameCombat : VirtualThingEntityBase, IAggregateRoot
    {
        #region 构造函数

        /// <summary>
        /// TODO 不可直接构造。
        /// </summary>
        public GameCombat()
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="thing"></param>
        public GameCombat(VirtualThing thing) : base(thing)
        {
        }

        #endregion 构造函数

        public IDisposable LockAll(VWorld world)
        {
            var AllCharIds = Attackers.Select(c => c.CharId).Union(Others.Select(c => c.CharId)).Union(Defensers.Select(c => c.CharId)).ToArray();
            var result = world.CharManager.LockOrLoadWithCharIds(AllCharIds, world.CharManager.Options.DefaultLockTimeout * AllCharIds.Length * 0.8);
            return result;
        }

        public IDisposable GetOtherChar(VWorld world, out GameChar gc)
        {
            var result = world.CharManager.LockOrLoad(Defensers.FirstOrDefault()?.CharId ?? Others.First().CharId, out var user);
            if (result is null)
            {
                gc = null;
                return null;
            }
            gc = user.CurrentChar;
            return result;
        }

        #region 进攻方信息

        List<GameSoldier> _Attackers;

        /// <summary>
        /// 攻击方角色集合。(当前可能只有一个)
        /// </summary>
        [JsonIgnore]
        public IReadOnlyList<GameSoldier> Attackers
        {
            get
            {
                if (_Attackers is null)
                {
                    _Attackers = new List<GameSoldier>();
                    _Attackers.AddRange(Thing.Children.Where(c => c.ExtraDecimal == 1).Select(c => c.GetJsonObject<GameSoldier>()));
                }
                return _Attackers;
            }
        }

        /// <summary>
        /// 加入一个攻击者。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="world"></param>
        /// <returns></returns>
        public GameSoldier AddAttacker(GameChar gameChar, VWorld world)
        {
            VirtualThing thing = new VirtualThing() { Parent = Thing, ParentId = Id };
            Thing.Children.Add(thing);

            GameSoldier soldier = thing.GetJsonObject<GameSoldier>();
            soldier.CharId = gameChar.Id;
            soldier.DisplayName = gameChar.DisplayName;

            soldier.Pets.AddRange(world.ItemManager.GetLineup(gameChar, 2));    //获取出阵阵容
            var pvp = gameChar.GetPvpObject();
            if (pvp is null)    //若未解锁pvp
                return null;
            soldier.ScoreBefore = (int)pvp.ExtraDecimal;
            soldier.RankBefore = world.CombatManager.GetPvpRank(gameChar);

            //world.ItemManager.GetLineup(gameChar, 100000,199999)
            return soldier;
        }
        #endregion 进攻方坐骑信息

        #region 防御方信息

        List<GameSoldier> _Defensers;
        /// <summary>
        /// 防御方角色Id集合。(当前可能只有一个)
        /// </summary>
        [JsonIgnore]
        public IReadOnlyList<GameSoldier> Defensers
        {
            get
            {
                if (_Defensers is null)
                {
                    _Defensers = new List<GameSoldier>();
                    _Defensers.AddRange(Thing.Children.Where(c => c.ExtraDecimal == 2).Select(c => c.GetJsonObject<GameSoldier>()));
                }
                return _Defensers;
            }
        }

        /// <summary>
        /// 防御方附属信息。
        /// </summary>
        public IEnumerable<GameItem> GetDefenserMounts()
        {
            throw new NotImplementedException();
            //if (_DefenserMounts is null || _DefenserMounts.Length == 0)
            //    return Array.Empty<GameItem>();
            //return (IEnumerable<GameItem>)JsonSerializer.Deserialize(_DefenserMounts, typeof(GameItem[]));
        }

        /// <summary>
        /// 防御方附属信息。
        /// </summary>
        public void SetDefenserMounts(IEnumerable<GameItem> value)
        {
            throw new NotImplementedException();
            //_DefenserMounts = JsonSerializer.SerializeToUtf8Bytes(value);
        }
        #endregion 防御方坐骑信息

        #region 战场摘要

        #endregion 战场摘要

        List<GameSoldier> _Others;
        [JsonIgnore]
        public IReadOnlyList<GameSoldier> Others
        {
            get
            {
                if (_Others is null)
                {
                    _Others = new List<GameSoldier>();
                    _Others.AddRange(Thing.Children.Where(c => c.ExtraDecimal == -1).Select(c => c.GetJsonObject<GameSoldier>()));
                }
                return _Others;
            }
        }

        /// <summary>
        /// 原始战斗的id,若自身就是原始战斗对象，这里为空。
        /// </summary>
        public Guid? OldCombatId { get; set; }

        /// <summary>
        /// 地图Id。就是关卡模板Id。
        /// 这可以表示该战斗是什么种类。
        /// </summary>
        public Guid MapTId { get; set; }

        /// <summary>
        /// 该战斗开始的Utc时间。如果此战斗并未开始过，则是空。
        /// </summary>
        public DateTime? StartUtc { get; set; }

        /// <summary>
        /// 该战斗结束的Utc时间。
        /// 空表示未结束。
        /// </summary>
        public DateTime? EndUtc { get; set; }

        /// <summary>
        /// 获取或设置是否正在请求协助。
        /// </summary>
        public bool Assistancing { get; set; }

        /// <summary>
        /// 获取或设置是否已经协助完毕。
        /// </summary>
        public bool Assistanced { get; set; }

        /// <summary>
        /// 是否已经反击。
        /// </summary>
        public bool Retaliationed { get; set; }

        /// <summary>
        /// 设置或获取协助者的角色Id。
        /// </summary>
        public Guid? AssistanceId { get; set; }

        /// <summary>
        /// 获取或设置战斗结果，true进攻方胜利，false进攻方失败。null无胜负。
        /// </summary>
        public bool? IsAttckerWin { get; set; }

        /// <summary>
        /// 获取或设置该流程是否已经结束。
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                }
                //清理大型字段
                _Attackers = null;
                _Defensers = null;
                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// 创建一个新的战斗对象。
        /// </summary>
        /// <param name="world"></param>
        /// <param name="id">对象的Id。省略或为null则自己生成一个新Id。</param>
        /// <returns></returns>
        public static GameCombat CreateNew(VWorld world, Guid? id = null)
        {
            var cache = world.GameCache;
            var innerId = id ?? Guid.NewGuid();
            var thing = new VirtualThing(innerId) { ExtraGuid = ProjectConstant.CombatReportTId };
            var key = thing.IdString;
            using var dw = cache.Lock(key);
            if (dw.IsEmpty)
                return null;
            var db = world.CreateNewUserDbContext();
            thing.RuntimeProperties["DbContext"] = db;
            db.Add(thing);
            using (var entry = (GameObjectCache.GameObjectCacheEntry)cache.CreateEntry(key))
            {
                entry.Value = thing;
                entry.SetSaveAndEviction(db)
                    .RegisterPostEvictionCallback((key, value, reason, state) => (state as IDisposable)?.Dispose(), db)
                    .SetSlidingExpiration(TimeSpan.FromMinutes(15));
            }
            var result = thing.GetJsonObject<GameCombat>();

            return result;
        }

        /// <summary>
        /// 设置保存即驱逐回调。
        /// </summary>
        /// <param name="entry"></param>
        public void SetEntry(GameObjectCache.GameObjectCacheEntry entry)
        {
            var db = Thing.GetDbContext();
            entry.SetSaveCallback((value, state) =>
            {
                if (value is GameCombat combat)
                {
                    if (combat.StartUtc.HasValue)
                    {
                        try
                        {
                            ((DbContext)state).SaveChanges();
                        }
                        catch (Exception)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }, db)
                .RegisterBeforeEvictionCallback((key, value, reason, state) =>
                {
                    if (value is GameCombat combat)
                    {
                        if (combat.StartUtc.HasValue)
                        {
                            ((DbContext)state).SaveChanges();
                        }
                    }
                    return;
                }, db)
                .RegisterPostEvictionCallback((key, value, reason, state) =>
                {
                    if (value is GameCombat combat)
                        combat.Dispose();
                    (state as DbContext)?.Dispose();
                }, db)
                .SetSlidingExpiration(TimeSpan.FromMinutes(15));
        }

        /// <summary>
        /// 记录所有资源。
        /// </summary>
        /// <param name="soldier"></param>
        /// <param name="gameChar"></param>
        public static void RecordResource(GameSoldier soldier, GameChar gameChar)
        {
            soldier.Resource.AddRange(gameChar.GetCurrencyBag().Children);  //记录所有货币
            //记录玉米田
            var gi = gameChar.GetHomeland().GetAllChildren().FirstOrDefault(c => c.ExtraGuid == ProjectConstant.YumitianTId);
            soldier.Resource.Add(gi);
            //记录木材树
            gi = gameChar.GetHomeland().GetAllChildren().FirstOrDefault(c => c.ExtraGuid == ProjectConstant.MucaishuTId);
            soldier.CharId = gameChar.Id;
            soldier.Resource.Add(gi);
            //其它
            soldier.DisplayName = gameChar.DisplayName;
            soldier.IconIndex = (int)gameChar.GetSdpDecimalOrDefault("charIcon", decimal.Zero);
            soldier.MainControlRoomLevel = (int)gameChar.GetMainControlRoom().GetSdpDecimalOrDefault("lv");
            soldier.Level = (int)gameChar.GetSdpDecimalOrDefault("lv");
            soldier.ScoreBefore = (int)(gameChar.GetPvpObject()?.ExtraDecimal ?? 0);
        }

        /// <summary>
        /// 在指定的战斗下创建一个新的参战方对象。
        /// </summary>
        /// <param name="bloc">阵营号，1=进攻方，2=防御方，0=未设置，-1=资源记录。</param>
        /// <returns></returns>
        public GameSoldier CreateSoldier(int bloc = 0)
        {
            var thing = new VirtualThing()
            {
                ExtraGuid = ProjectConstant.GameSoldierTId,
                Parent = Thing,
                ParentId = Id,
                ExtraDecimal = bloc,
            };
            Thing.Children.Add(thing);

            var soldier = thing.GetJsonObject<GameSoldier>();
            return soldier;
        }

        /// <summary>
        /// 在战斗开始前初始化参战方的信息。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="soldier"></param>
        /// <param name="world"></param>
        public static void FillSoldierBefroeCombat(GameChar gameChar, GameSoldier soldier, VWorld world)
        {
            soldier.CharId = gameChar.Id;
            soldier.DisplayName = gameChar.DisplayName;

            var pvp = gameChar.GetPvpObject();
            if (pvp != null)    //若已解锁pvp
            {
                soldier.ScoreBefore = (int)pvp.ExtraDecimal;
                soldier.RankBefore = world.CombatManager.GetPvpRank(gameChar);
            }
            soldier.MainControlRoomLevel = (int)(gameChar.GetMainControlRoom()?.GetSdpDecimalOrDefault(world.PropertyManager.LevelPropertyName) ?? 0);
            soldier.Level = (int)(gameChar.GetSdpDecimalOrDefault(world.PropertyManager.LevelPropertyName));
            soldier.IconIndex = (int)gameChar.GetSdpDecimalOrDefault("charIcon", decimal.Zero);
            
        }

        /// <summary>
        /// 在战斗开始前填充作为攻击方的信息。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="soldier"></param>
        /// <param name="world"></param>
        /// <returns></returns>
        public void FillAttakerBefroeCombat(GameChar gameChar, GameSoldier soldier, VWorld world)
        {
            soldier.Pets.AddRange(world.ItemManager.GetLineup(gameChar, 2));    //获取出阵阵容
        }

        /// <summary>
        /// 将指定参战方设置为进攻方。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="soldier"></param>
        /// <param name="world"></param>
        public void SetAttacker(GameChar gameChar, GameSoldier soldier, VWorld world)
        {
            FillSoldierBefroeCombat(gameChar, soldier, world);
            soldier.Thing.ExtraDecimal = 1;
            FillAttakerBefroeCombat(gameChar, soldier, world);
            _Attackers = null;
        }

        /// <summary>
        /// 在战斗开始前填充作为防御方的信息。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="soldier"></param>
        /// <param name="world"></param>
        public void FillDefenerBefroeCombat(GameChar gameChar, GameSoldier soldier, VWorld world)
        {
            var pets = world.ItemManager.GetLineup(gameChar, 100000, 199999);    //获取守卫阵容
            soldier.Pets.AddRange(pets);
        }

        /// <summary>
        /// 将指定参战方设置为防御方。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="soldier"></param>
        /// <param name="world"></param>
        public void SetDefener(GameChar gameChar, GameSoldier soldier, VWorld world)
        {
            FillSoldierBefroeCombat(gameChar, soldier, world);
            soldier.Thing.ExtraDecimal = 2;
            FillDefenerBefroeCombat(gameChar, soldier, world);
            _Defensers = null;
        }

    }

    /// <summary>
    /// 参与战斗的实体。或叫参战方，通常是个体，但也可能有个别对象指代阵营。
    /// </summary>
    public class GameSoldier : VirtualThingEntityBase, IEntity
    {
        public GameSoldier()
        {
        }

        public GameSoldier(VirtualThing thing) : base(thing)
        {
        }

        /// <summary>
        /// 角色Id（将来可能是其它实体Id。）
        /// </summary>
        [JsonIgnore]
        public Guid CharId { get => Guid.TryParse(Thing.ExtraString, out var id) ? id : Guid.Empty; set => Thing.ExtraString = value.ToString(); }

        /// <summary>
        /// 参与战斗时刻的显示名字。
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 战斗前积分。
        /// </summary>
        public int ScoreBefore { get; set; }

        /// <summary>
        /// 战斗后积分。
        /// </summary>
        public int ScoreAfter { get; set; }

        /// <summary>
        /// 战斗前排名。
        /// </summary>
        public int RankBefore { get; set; }

        /// <summary>
        /// 战斗后排名。
        /// </summary>
        public int RankAfter { get; set; }

        /// <summary>
        /// 主基地等级。
        /// </summary>
        public int MainControlRoomLevel { get; set; }

        /// <summary>
        /// 头像索引号。默认为0。
        /// </summary>
        public int IconIndex { get; set; }

        /// <summary>
        /// 最后一次下线时间。空表示当前在线。
        /// </summary>
        public DateTime? LastLogoutDatetime { get; set; }

        /// <summary>
        /// 角色等级。
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// 角色战力。
        /// </summary>
        public decimal CombatCap { get; set; }

        /// <summary>
        /// 携带的出战坐骑/或家园的上阵坐骑。即与此战斗相关的坐骑。
        /// </summary>
        public List<GameItem> Pets { get; set; } = new List<GameItem>();

        /// <summary>
        /// 战利品。
        /// </summary>
        public List<GameItem> Booties { get; set; } = new List<GameItem>();

        /// <summary>
        /// 战斗开始时，相关资源的时点副本。为以后计算使用。
        /// </summary>
        public List<GameItem> Resource { get; set; } = new List<GameItem>();

    }

    /// <summary>
    /// 战斗实体的仓库。
    /// </summary>
    public class GameCombatRepository : IRepository<GameCombat>
    {
        public GameCombatRepository(VWorld world)
        {
            _World = world;
        }

        VWorld _World;

        ConcurrentDictionary<Guid, VirtualThing> _Datas = new ConcurrentDictionary<Guid, VirtualThing>();

        public void Save(GameCombat entity)
        {
        }

        public GameCombat Load(Guid id)
        {
            var key = id.ToString();
            var re = new { Key = key };
            var thing = _World.GameCache.GetOrLoad<VirtualThing>(key, c => c.Id == id, entry => entry.SetCreateCallback((key, state) =>
                {
                    var result = new VirtualThing(id) { };
                    return result;
                }, key)
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(15)));
            var result = thing.GetJsonObject<GameCombat>();
            if (result.MapTId == Guid.Empty)    //若需设置地图模板id
                result.MapTId = ProjectConstant.PvpDungeonTId;
            _Datas.AddOrUpdate(id, thing, (c1, c2) => thing);
            result.Thing = thing;
            OwHelper.SetLastError(0);
            return result;
        }

        public GameCombat Add(Guid id)
        {
            GameCombat result = new GameCombat() { };
            return result;
        }
    }

    public static class GameCombatExtensions
    {
    }

}
