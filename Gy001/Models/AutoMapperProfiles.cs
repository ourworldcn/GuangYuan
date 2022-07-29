using AutoMapper;
using GuangYuan.GY001.UserDb;
using GuangYuan.GY001.UserDb.Combat;
using GY2021001WebApi.Models;

namespace Gy01.AutoMapper.Profiles
{
    public class Gy01Profile:Profile
    {
        public Gy01Profile()
        {
            CreateMap<GameItem, GameItemDto>();
            CreateMap<CombatReport, CombatDto>();
        }
    }
}