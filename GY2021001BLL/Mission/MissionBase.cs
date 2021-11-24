using GuangYuan.GY001.BLL;
using GuangYuan.GY001.UserDb;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace OW.Game.Mission
{
    public class CharMissionView : GameCharWorkDataBase
    {
        public CharMissionView([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public CharMissionView([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
            
        }

        public CharMissionView([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }


    }
}
