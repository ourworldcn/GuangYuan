using AutoMapper;
using GuangYuan.GY001.BLL.Specific;
using GuangYuan.GY001.UserDb;
using GuangYuan.GY001.UserDb.Combat;
using GuangYuan.GY001.UserDb.Social;
using GY2021001WebApi.Models;
using OW.Extensions.Game.Store;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Gy01.AutoMapper.Profiles
{
    public class Gy01Profile : Profile
    {
        public Gy01Profile()
        {
            IncludeSourceExtensionMethods(typeof(OW.Extensions.Game.Store.GameThingBaseExtensions));
            IncludeSourceExtensionMethods(typeof(GuangYuan.GY001.UserDb.GameThingBaseExtensions));
            //基础类型映射
            CreateMap<Guid, string>().ConstructUsing(c => c.ToBase64String());

            //DTO映射
            CreateMap<GameItem, GameItemDto>();

            CreateMap<CombatReport, CombatDto>();
            CreateMap<LoginT89ParamsDto, T89LoginData>();
            //CreateMap<GameGuildEntity, Dictionary<string, object>>().ConstructUsing((src, context) =>
            //{
            //    //context.Items.Add("Cap", 11);
            //    var props = TypeDescriptor.GetProperties(src).OfType<PropertyDescriptor>();
            //    var result = props.Where(p => p.Name != nameof(VirtualThingEntityBase.Thing)).ToDictionary(p => p.Name, p => p.GetValue(src));
            //    return new Dictionary<string, object>();
            //});//.ForAllMembers(c =>c c.Ignore());    

            CreateMap<IdAndCountDto, (Guid, decimal)>().ConstructUsing((src, context) => (OwConvert.ToGuid(src.Id), src.Count)).ForAllMembers(c => c.Ignore());
            CreateMap<(Guid, decimal), IdAndCountDto>().ConstructUsing((src, context) => new IdAndCountDto { Id = src.Item1.ToBase64String(), Count = src.Item2 }).ForAllMembers(c => c.Ignore());

            //战斗相关映射
            CreateMap<GameSoldier, GameSoldierDto>();
            CreateMap<GameCombat, GameCombatDto>();
        }
    }
}