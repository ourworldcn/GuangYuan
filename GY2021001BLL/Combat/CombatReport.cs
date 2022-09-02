﻿using GuangYuan.GY001.BLL;
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
    /// <summary>
    /// CombatReport
    /// </summary>
    public class CombatReport : VirtualThingEntityBase
    {
        /// <summary>
        /// 
        /// </summary>
        public const string Separator = "`";

        /// <summary>
        /// TODO 不可直接构造。
        /// </summary>
        public CombatReport()
        {
        }

        private List<Guid> _AttackerIds;
        /// <summary>
        /// 攻击方角色Id集合。
        /// </summary>
        [NotMapped]
        public List<Guid> AttackerIds
        {
            get => _AttackerIds ??= new List<Guid>(); set => _AttackerIds = value;
        }

        private List<Guid> _DefenserIds;
        /// <summary>
        /// 防御方角色Id集合。
        /// </summary>
        public List<Guid> DefenserIds { get => _DefenserIds ??= new List<Guid>(); set => _DefenserIds = value; }

        #region 进攻方坐骑信息

        private byte[] _AttackerMounts;
        public byte[] AttackerMounts { get => _AttackerMounts; set => _AttackerMounts = value; }

        /// <summary>
        /// 进攻方附属信息。
        /// </summary>
        public IEnumerable<GameItem> GetAttackerMounts()
        {
            if (_AttackerMounts is null || _AttackerMounts.Length == 0)
                return Array.Empty<GameItem>();
            return (IEnumerable<GameItem>)JsonSerializer.Deserialize(_AttackerMounts, typeof(GameItem[]));
        }

        /// <summary>
        /// 进攻方附属信息。
        /// </summary>
        public void SetAttackerMounts(IEnumerable<GameItem> value)
        {
            _AttackerMounts = JsonSerializer.SerializeToUtf8Bytes(value);
        }
        #endregion 进攻方坐骑信息

        #region 防御方坐骑信息

        private byte[] _DefenserMounts;
        public byte[] DefenserMounts { get => _DefenserMounts; set => _DefenserMounts = value; }

        /// <summary>
        /// 防御方附属信息。
        /// </summary>
        public IEnumerable<GameItem> GetDefenserMounts()
        {
            if (_DefenserMounts is null || _DefenserMounts.Length == 0)
                return Array.Empty<GameItem>();
            return (IEnumerable<GameItem>)JsonSerializer.Deserialize(_DefenserMounts, typeof(GameItem[]));
        }

        /// <summary>
        /// 防御方附属信息。
        /// </summary>
        public void SetDefenserMounts(IEnumerable<GameItem> value)
        {
            _DefenserMounts = JsonSerializer.SerializeToUtf8Bytes(value);
        }
        #endregion 防御方坐骑信息

        /// <summary>
        /// 该战斗开始的Utc时间。
        /// </summary>
        public DateTime StartUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 该战斗结束的Utc时间。
        /// </summary>
        public DateTime EndUtc { get; set; } = DateTime.UtcNow;

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
        /// 攻击者昵称。
        /// </summary>
        public string AttackerDisplayName { get; set; }

        /// <summary>
        /// 防御者昵称。
        /// </summary>
        public string DefenserDisplayName { get; set; }

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
        /// 进攻者排名。
        /// </summary>
        public int attackerRankBefore { get; set; }

        /// <summary>
        /// 进攻者积分。
        /// </summary>
        public decimal? attackerScoreBefore { get; set; }

        /// <summary>
        /// 防御者此战前排名。
        /// </summary>
        public int defenderRankBefore { get; set; }

        /// <summary>
        /// 防御者此战前积分。
        /// </summary>
        public decimal? defenderScoreBefore { get; set; }

        /// <summary>
        /// 进攻者排名。
        /// </summary>
        public int attackerRankAfter { get; set; }

        /// <summary>
        /// 进攻者积分。
        /// </summary>
        public decimal? attackerScoreAfter { get; set; }

        public int defenderRankAfter { get; set; }

        public decimal? defenderScoreAfter { get; set; }

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
                _AttackerIds = null;
                _DefenserIds = null;
                base.Dispose(disposing);
            }
        }

    }

    /// <summary>
    /// 战利品记录。
    /// <see cref="SimpleDynamicPropertyBase.Properties"/>记录了物品信息，如tid是模板id,count是数量(可能是负数)。
    /// </summary>
    public class GameBooty : VirtualThingEntityBase
    {
        public GameBooty()
        {
        }

        /// <summary>
        /// 所属角色(参与战斗的角色Id)。
        /// </summary>
        public Guid CharId { get; set; }

        [JsonIgnore]
        public Guid? ParentId { get => Thing.ParentId; }
    }

    /// <summary>
    /// 战斗的聚合根。
    /// </summary>
    public class GameCombat : VirtualThingEntityBase, IAggregateRoot
    {
        /// <summary>
        /// TODO 不可直接构造。
        /// </summary>
        public GameCombat()
        {
        }

        public GameCombat(VirtualThing thing) : base(thing)
        {
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
        /// 进攻方附属信息。
        /// </summary>
        public IEnumerable<GameItem> GetAttackerMounts()
        {
            throw new NotImplementedException();
            //if (_AttackerMounts is null || _AttackerMounts.Length == 0)
            //    return Array.Empty<GameItem>();
            //return (IEnumerable<GameItem>)JsonSerializer.Deserialize(_AttackerMounts, typeof(GameItem[]));
        }

        /// <summary>
        /// 进攻方附属信息。
        /// </summary>
        public void SetAttackerMounts(IEnumerable<GameItem> value)
        {
            throw new NotImplementedException();
            //_AttackerMounts = JsonSerializer.SerializeToUtf8Bytes(value);
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

        /// <summary>
        /// 地图Id。就是关卡模板Id。
        /// 这可以表示该战斗是什么种类。
        /// </summary>
        public Guid MapTId { get; set; }

        /// <summary>
        /// 该战斗开始的Utc时间。
        /// </summary>
        public DateTime StartUtc { get; set; } = DateTime.UtcNow;

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

    }

    /// <summary>
    /// 参与战斗的实体。
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
        /// 携带的出战坐骑。
        /// </summary>
        public List<GameItem> Pets { get; set; } = new List<GameItem>();

        /// <summary>
        /// 战利品。
        /// </summary>
        public List<GameItem> Booties { get; set; } = new List<GameItem>();

    }

    /// <summary>
    /// 战斗实体的仓库。
    /// </summary>
    public class GameCombatRepository : IRepository<GameCombat>
    {
        public GameCombatRepository(VWorld world, GameObjectCache cache)
        {
            _World = world;
            _Cache = cache;
        }

        GameObjectCache _Cache;
        VWorld _World;

        ConcurrentDictionary<Guid, VirtualThing> _Datas = new ConcurrentDictionary<Guid, VirtualThing>();

        public void Save(GameCombat entity)
        {
        }

        public GameCombat Load(Guid id)
        {
            var key = id.ToString();
            var re = new { Key = key };
            var thing = _Cache.GetOrLoad<VirtualThing>(key, c => c.Id == id, type => _World.CreateNewUserDbContext(),
                entry => entry.SetCreateCallback((key, state) =>
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


}
