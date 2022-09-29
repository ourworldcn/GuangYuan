﻿using AutoMapper;
using GuangYuan.GY001.BLL.Specific;
using GuangYuan.GY001.UserDb;
using GuangYuan.GY001.UserDb.Combat;
using GuangYuan.GY001.UserDb.Social;
using GY2021001WebApi.Models;
using OW.Extensions.Game.Store;
using OW.Game.Mission;
using OW.Game.PropertyChange;
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
            CreateMap<string, Guid>().ConstructUsing(c => OwConvert.ToGuid(c));

            CreateMap<ChangeItem, ChangesItemDto>();
            CreateMap<ChangesItemDto, ChangeItem>();

            CreateMap<GamePropertyChangeItem<object>, GamePropertyChangeItemDto>()
                .ForMember(dest => dest.ObjectId, opt => opt.MapFrom((src, dest) => (src.Object as GameThingBase)?.Base64IdString ?? Guid.Empty.ToBase64String()))
                .ForMember(dest => dest.TId, opt => opt.MapFrom((src, dest) => (src.Object as GameThingBase)?.ExtraGuid.ToBase64String() ?? Guid.Empty.ToBase64String()));
            //.ForMember(dest => dest.ObjectId, opt => opt.MapFrom(src => src.Object is GameObjectBase go ? go.Id : Guid.Empty));

            CreateMap<GameMissionItem, GameMissionItemDto>().ConstructUsing((src, context) =>
            {
                var result = new GameMissionItemDto()
                {
                    MaxComplateCount = (int)src.Template?.Properties.GetDecimalOrDefault("MaxComplateCount", int.MaxValue),
                };
                return result;
            }).ForMember(c => c.MaxComplateCount, opt => opt.Ignore());

            //DTO映射
            CreateMap<GameItem, GameItemDto>();
            CreateMap<GameItemDto, GameItem>().ConstructUsing(src => new GameItem() { Count = src.Count }).ForMember(dest => dest.Count, opt => opt.Ignore());

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
            CreateMap<GameSoldier, GameSoldierDto>().ForMember(src => src.Pets, opt =>
               {
                   opt.MapFrom(dest => dest.Pets);
                   opt.PreCondition((src, dest, context) =>
                   {
                       dynamic d = context.Options;

                       if (d.Items.ContainsKey("IgnorDefenerPets") ?? false && src.Pets.Count > 4) //TODO 当前忽略4个以上的坐骑，应忽略所有防御方坐骑
                           return false;
                       return true;
                   });
               });
            CreateMap<GameCombat, GameCombatDto>().ForMember(dest => dest.Others, opt =>
               {
                   opt.MapFrom(c => c.Others);
                   opt.PreCondition((src, dest, context) =>
                   {
                       dynamic d = context.Options;

                       if (d.Items.ContainsKey("IgnorOthers") ?? false)
                           return false;
                       return true;
                   });
               });
        }
    }
}