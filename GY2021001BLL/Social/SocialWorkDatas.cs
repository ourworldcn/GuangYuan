using Game.Social;
using GuangYuan.GY001.UserDb;
using GuangYuan.GY001.UserDb.Combat;
using OW.Game;
using OW.Game.Store;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace GuangYuan.GY001.BLL
{
    public class RequestAssistanceDatas : BinaryRelationshipGameContext
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

        private GameCombat _RootCombat;

        /// <summary>
        /// 初始战斗对象。
        /// </summary>
        public GameCombat RootCombat
        {
            get
            {
                if (_RootCombat is null)
                {
                    var thing = UserDbContext.Set<VirtualThing>().FirstOrDefault(c => c.Id == RootCombatId);
                    _RootCombat = thing?.GetJsonObject<GameCombat>();
                }
                return _RootCombat;
            }
        }

        GameSocialRelationship _SocialRelationship;

        /// <summary>
        /// 请求协助的数据条目。
        /// </summary>
        public GameSocialRelationship SocialRelationship
        {
            get
            {
                if (_SocialRelationship is null)
                {
                    _SocialRelationship = UserDbContext.Set<GameSocialRelationship>().FirstOrDefault(c => c.Id == GameChar.Id && c.Id2 == OtherCharId
                      && c.KeyType == (int)SocialKeyTypes.AllowPvpForHelp);
                    if (_SocialRelationship is null) //若没有条目
                    {
                        _SocialRelationship = new GameSocialRelationship()
                        {
                            Id = GameChar.Id,
                            Id2 = OtherCharId,
                            KeyType = (int)SocialKeyTypes.AllowPvpForHelp,
                            Flag = 0,
                        };
                        UserDbContext.Add(_SocialRelationship);
                    }
                }
                return _SocialRelationship;
            }
        }
    }

}