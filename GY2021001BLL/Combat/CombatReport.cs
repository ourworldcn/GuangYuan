using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OW.Game.Item;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

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

        /// <summary>
        /// 进攻方附属信息。
        /// </summary>
        public byte[] AttackerExInfo { get; set; }

        /// <summary>
        /// 防御方附属信息。
        /// </summary>
        public byte[] DefenserExInfo { get; set; }

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

        public IEnumerable<GameItem> GetAttackerMounts(IServiceProvider services)
        {
            //TODO 
            //throw new NotImplementedException();
            var gim = services.GetRequiredService<GameItemManager>();
            return gim.ToGameItems(AttackerExInfo);
        }

        public IEnumerable<GameItem> GetDefenserMounts(IServiceProvider services)
        {
            //TODO 
            //throw new NotImplementedException();
            var gim = services.GetRequiredService<GameItemManager>();
            return gim.ToGameItems(DefenserExInfo);
        }

        public string AttackerDisplayName { get; set; }

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

        /// <summary>
        /// 进攻者排名。
        /// </summary>
        public int attackerRankBefore { get; set; }

        /// <summary>
        /// 进攻者积分。
        /// </summary>
        public decimal? attackerScoreBefore { get; set; }

        public int defenderRankBefore { get; set; }

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

    }

}
