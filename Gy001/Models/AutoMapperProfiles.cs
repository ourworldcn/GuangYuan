using AutoMapper;
using GuangYuan.GY001.BLL.Specific;
using GuangYuan.GY001.UserDb;
using GuangYuan.GY001.UserDb.Combat;
using GuangYuan.GY001.UserDb.Social;
using GY2021001WebApi.Models;
using System.Collections.Generic;

namespace Gy01.AutoMapper.Profiles
{
    public class Gy01Profile : Profile
    {
        public Gy01Profile()
        {
            CreateMap<GameItem, GameItemDto>();
            CreateMap<CombatReport, CombatDto>();
            CreateMap<LoginT89ParamsDto, T89LoginData>();
            CreateMap<GameGuildEntity, Dictionary<string, object>>().ForAllMembers(opt =>
            {
                if(opt.DestinationMember.Name == "Items");
                opt.MapFrom(c => c.AutoAccept);
            });

        }
    }
}