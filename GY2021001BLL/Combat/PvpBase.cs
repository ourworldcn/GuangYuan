using GuangYuan.GY001.BLL.Homeland;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using GuangYuan.GY001.UserDb.Combat;
using OW.Game;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace GuangYuan.GY001.BLL
{
    public class StartCombatPvpData : BinaryRelationshipGameContext
    {
        public StartCombatPvpData([NotNull] IServiceProvider service, [NotNull] GameChar gameChar, Guid otherGCharId) : base(service, gameChar, otherGCharId)
        {
        }

        public StartCombatPvpData([NotNull] VWorld world, [NotNull] GameChar gameChar, Guid otherGCharId) : base(world, gameChar, otherGCharId)
        {
        }

        public StartCombatPvpData([NotNull] VWorld world, [NotNull] string token, Guid otherGCharId) : base(world, token, otherGCharId)
        {
        }

        public Guid CombatId { get; set; }

        /// <summary>
        /// 原始战斗id,如果没有则为空。
        /// </summary>
        public Guid? OldCombatId { get; set; }

        /// <summary>
        /// 关卡Id。
        /// </summary>
        public Guid DungeonId { get; set; }

    }

    /// <summary>
    /// pvp结束战斗调用接口的数据封装类。
    /// </summary>
    public class EndCombatPvpWorkData : BinaryRelationshipGameContext
    {
        public EndCombatPvpWorkData([NotNull] IServiceProvider service, [NotNull] GameChar gameChar, Guid otherGCharId) : base(service, gameChar, otherGCharId)
        {
        }

        public EndCombatPvpWorkData([NotNull] VWorld world, [NotNull] GameChar gameChar, Guid otherGCharId) : base(world, gameChar, otherGCharId)
        {
        }

        public EndCombatPvpWorkData([NotNull] VWorld world, [NotNull] string token, Guid otherGCharId) : base(world, token, otherGCharId)
        {
        }

        /// <summary>
        /// 战斗对象唯一Id。从邮件的 mail.Properties["CombatId"] 属性中获取。
        /// 反击和协助才需要填写。直接pvp时可以省略。
        /// </summary>
        public Guid CombatId { get; set; }

        CombatReport _Combat;
        /// <summary>
        /// 获取战斗对象，如果找不到则设置错误信息。
        /// </summary>
        public CombatReport Combat
        {
            get
            {
                if (_Combat is null)
                {
                    var thing = UserDbContext.Set<VirtualThing>().Find(CombatId);
                    if (thing is null)
                    {
                        HasError = true;
                        ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                        DebugMessage = $"找不到指定的战报对象，Id={CombatId}";
                    }
                    _Combat = thing.GetJsonObject<CombatReport>();
                }
                return _Combat;
            }
            set => _Combat = value;
        }

        /// <summary>
        /// 当前日期。
        /// </summary>
        public DateTime Now { get; set; }

        /// <summary>
        /// 关卡Id
        /// </summary>
        public Guid DungeonId { get; set; }

        private GameItemTemplate _DungeonTemplate;

        /// <summary>
        /// 关卡模板。
        /// </summary>
        public GameItemTemplate DungeonTemplate => _DungeonTemplate ??= World.ItemTemplateManager.GetTemplateFromeId(DungeonId);

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
    }
}
