using GuangYuan.GY001.UserDb;
using GuangYuan.GY001.UserDb.Combat;
using OW.Game;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace GuangYuan.GY001.BLL
{
    public class RequestAssistanceDatas : RelationshipWorkDataBase
    {
        public RequestAssistanceDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar, Guid otherGCharId) : base(service, gameChar, otherGCharId)
        {
        }

        public RequestAssistanceDatas([NotNull] VWorld world, [NotNull] GameChar gameChar, Guid otherGCharId) : base(world, gameChar, otherGCharId)
        {
        }

        public RequestAssistanceDatas([NotNull] VWorld world, [NotNull] string token, Guid otherGCharId) : base(world, token, otherGCharId)
        {
        }

        /// <summary>
        /// 初始战斗的Id，从被pvp的邮件中获取。
        /// </summary>
        public Guid RootCombatId { get; set; }

        private PvpCombat _RootCombat;

        /// <summary>
        /// 初始战斗对象。
        /// </summary>
        public PvpCombat RootCombat
        {
            get
            {
                if (_RootCombat is null)
                {
                    _RootCombat = UserContext.Set<PvpCombat>().FirstOrDefault(c => c.Id == RootCombatId);
                }
                return _RootCombat;
            }
        }
    }

    public static class SocialPvpCombatExtensions
    {
        /// <summary>
        /// 是否已经请求了协助。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Guid? GetRequestAssistance(this PvpCombat obj)
        {
            var tmp = obj.Properties.GetGuidOrDefault("RequestAssistanceDatas", Guid.Empty);
            return tmp == Guid.Empty ? null as Guid? : tmp;
        }

        /// <summary>
        /// 设置是否请求了协助。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetRequestAssistance(this PvpCombat obj, Guid? charId)
        {
            if (charId.HasValue)
                obj.Properties["RequestAssistanceDatas"] = charId.Value.ToString();
            else
                obj.Properties.Remove("RequestAssistanceDatas");
        }
    }
}