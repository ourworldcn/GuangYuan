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
            CreateMap<GameItem, GameItemDto>().IncludeBase<GameThingBase,GameItemDto>()
                .ForMember(c => c.ClientString, c => c.MapFrom(p => p.GetClientString()));

            CreateMap<CombatReport, CombatDto>();
            CreateMap<LoginT89ParamsDto, T89LoginData>();
            //基础类型
            CreateMap<Guid, string>().ConstructUsing(c => c.ToBase64String());
            //CreateMap<GameGuildEntity, Dictionary<string, object>>().ConstructUsing((src, context) =>
            //{
            //    //context.Items.Add("Cap", 11);
            //    var props = TypeDescriptor.GetProperties(src).OfType<PropertyDescriptor>();
            //    var result = props.Where(p => p.Name != nameof(VirtualThingEntityBase.Thing)).ToDictionary(p => p.Name, p => p.GetValue(src));
            //    return new Dictionary<string, object>();
            //});//.ForAllMembers(c =>c c.Ignore());
        }
    }
}