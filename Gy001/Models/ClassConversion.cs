﻿using AutoMapper;
using GuangYuan.GY001.BLL;
using GuangYuan.GY001.BLL.GeneralManager;
using GuangYuan.GY001.BLL.Homeland;
using GuangYuan.GY001.BLL.Specific;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using GuangYuan.GY001.UserDb.Combat;
using GuangYuan.GY001.UserDb.Social;
using Microsoft.Extensions.DependencyInjection;
using OW.Extensions.Game.Store;
using OW.Game;
using OW.Game.PropertyChange;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace GY2021001WebApi.Models
{
    #region 基础数据

    public partial class ChatMessageDto
    {
        public static implicit operator ChatMessageDto(ChatMessage obj)
        {
            var result = new ChatMessageDto()
            {
                ChannelId = obj.ChannelId,
                Message = obj.Message as string,
                SendDateTimeUtc = obj.SendDateTimeUtc,
                Sender = obj.Sender,
            };
            if (!string.IsNullOrEmpty(obj.ExString))
            {
                var ary = obj.ExString.Split(OwHelper.CommaArrayWithCN);
                if (ary.Length == 2)
                {
                    result.DisplayName = ary[1];
                    if (int.TryParse(ary[0], out var ci))
                        result.IconIndex = ci;
                }
            }
            return result;
        }
    }

    public partial class ReturnDtoBase
    {
        public void FillFrom(IResultWorkData result)
        {
            HasError = result.HasError;
            ErrorCode = result.ErrorCode;
            DebugMessage = result.DebugMessage;
        }

        public void FillFromWorld()
        {
            ErrorCode = OwHelper.GetLastError();
            DebugMessage = OwHelper.GetLastErrorMessage();
        }
    }

    public partial class GameMissionTemplateDto
    {
        public static implicit operator GameMissionTemplateDto(GameMissionTemplate obj)
        {
            var result = new GameMissionTemplateDto
            {
                DisplayName = obj.Remark,
                Id = obj.Id.ToBase64String(),
                GroupNumber = obj.GroupNumber,
            };
            result.PreMissionIds.AddRange(obj.PreMissionIds.Select(c => c.ToBase64String()));
            foreach (var item in obj.Properties)
            {
                result.Properties[item.Key] = item.Value;
            }
            return result;
        }
    }

    public partial class ShoppingItemDto
    {
        public static ShoppingItemDto FromGameShoppingTemplate(GameShoppingTemplate template, GameChar gameChar, DateTime now, VWorld world)
        {
            DateTime dt = now;
            var view = new ShoppingSlotView(world, gameChar, now);
            var result = new ShoppingItemDto()
            {
                AutoUse = template.AutoUse,
                Genus = template.Genus,
                GroupNumber = template.GroupNumber,
                Id = template.Id.ToBase64String(),
                ItemTemplateId = template.ItemTemplateId.HasValue ? template.ItemTemplateId.Value.ToBase64String() : null,
                MaxCount = template.MaxCount,
                StartDateTime = template.StartDateTime,
                SellPeriod = template.SellPeriod,
                ValidPeriod = template.ValidPeriod,
                Start = template.GetStart(dt),
                End = template.GetEnd(dt),
                CountOfBuyed = view.GetCountOfBuyed(template),
            };
            foreach (var item in template.Properties)
            {
                result.Properties[item.Key] = item.Value;
            }
            return result;
        }

        public static implicit operator ShoppingItemDto(GameShoppingTemplate template)
        {
            var result = new ShoppingItemDto()
            {
                AutoUse = template.AutoUse,
                Genus = template.Genus,
                GroupNumber = template.GroupNumber,
                Id = template.Id.ToBase64String(),
                ItemTemplateId = template.ItemTemplateId.HasValue ? template.ItemTemplateId.Value.ToBase64String() : null,
                MaxCount = template.MaxCount,
                StartDateTime = template.StartDateTime,
                SellPeriod = template.SellPeriod,
                ValidPeriod = template.ValidPeriod,
                Start = template.StartDateTime,
                //End = template.,
                //CountOfBuyed = view.GetCountOfBuyed(template),
            };
            OwHelper.Copy(template.Properties, result.Properties);
            return result;
        }
    }

    public partial class ChangeDataDto
    {
        public static implicit operator ChangeDataDto(ChangeData obj)
        {
            //TODO pvp输了时，此处obj可能为空
            var result = new ChangeDataDto()
            {
                ActionId = obj.ActionId,
                NewValue = obj.NewValue,
                ObjectId = obj.ObjectId.ToBase64String(),
                OldValue = obj.OldValue,
                PropertyName = obj.PropertyName,
                TemplateId = obj.TemplateId.ToBase64String(),
                CreateUtc = obj.CreateUtc,
            };
            foreach (var item in obj.Properties)
            {
                result.Properties[item.Key] = item.Value;
            }
            return result;
        }
    }

    public partial class IdAndCountDto
    {
        public static implicit operator ValueTuple<Guid, decimal>(IdAndCountDto obj) =>
            (OwConvert.ToGuid(obj.Id), obj.Count);

        public static implicit operator IdAndCountDto(ValueTuple<Guid, decimal> obj) =>
            new IdAndCountDto { Id = obj.Item1.ToBase64String(), Count = obj.Item2 };
    }

    public partial class GameActionRecordDto
    {
        public static implicit operator GameActionRecordDto(GameActionRecord obj)
        {
            var result = new GameActionRecordDto
            {
                ActionId = obj.ActionId,
                DateTimeUtc = obj.DateTimeUtc,
                ParentId = obj.ParentId.ToBase64String(),
                Remark = obj.Remark,
            };
            foreach (var item in obj.Properties)
            {
                result.Properties[item.Key] = item.Value;
            }
            return result;
        }
    }

    public partial class GameItemDto
    {
        /// <summary>
        /// 从传输对象获取数据对象。
        /// </summary>
        /// <param name="obj"></param>
        public static explicit operator GameItem(GameItemDto obj)
        {
            //explicit
            var result = new GameItem()
            {
                Id = OwConvert.ToGuid(obj.Id),
                Count = obj.Count,
                ExtraGuid = string.IsNullOrEmpty(obj.ExtraGuid) ? Guid.Empty : OwConvert.ToGuid(obj.ExtraGuid),
                OwnerId = string.IsNullOrEmpty(obj.OwnerId) ? Guid.Empty : OwConvert.ToGuid(obj.OwnerId),
                ParentId = string.IsNullOrEmpty(obj.ParentId) ? Guid.Empty : OwConvert.ToGuid(obj.ParentId),
            };
            result.GenerateIdIfEmpty();
            foreach (var item in obj.Properties)
            {
                result.Properties[item.Key] = item.Value is JsonElement je ? (je.ValueKind switch { JsonValueKind.Number => je.GetDecimal(), _ => throw new InvalidOperationException(), }) : item.Value;
            }
            result.Children.AddRange(obj.Children.Select(c => (GameItem)c));
            return result;
        }

        public static GameItemDto FromGameItem(GameItem obj, bool includeChildren = false)
        {
            var result = new GameItemDto()
            {
                Id = obj.Id.ToBase64String(),
                Count = obj.Count,
                ExtraGuid = obj.ExtraGuid.ToBase64String(),
                OwnerId = obj.OwnerId?.ToBase64String(),
                ParentId = obj.ParentId?.ToBase64String(),
                ClientString = obj.GetClientString(),
            };
            result.Properties[nameof(GameItem.ExtraString)] = obj.ExtraString;
            result.Properties[nameof(GameItem.ExtraDecimal)] = obj.ExtraDecimal;

            if (obj.Name2FastChangingProperty.TryGetValue("Count", out var fcp))
                result.Count = fcp.LastValue;
            result._Properties = new Dictionary<string, object>(obj.Properties);
            if (includeChildren && obj.Children.Count > 0)  //若有孩子需要转换
                result.Children.AddRange(obj.Children.Select(c => FromGameItem(c, includeChildren)));
            return result;
        }
    }

    public partial class GameCharDto
    {
        /// <summary>
        /// 从传输对象获取数据对象。
        /// </summary>
        /// <param name="obj"></param>
        public static explicit operator GameChar(GameCharDto obj)
        {
            var result = new GameChar()
            {
                Id = OwConvert.ToGuid(obj.Id),
                ExtraGuid = string.IsNullOrEmpty(obj.TemplateId) ? Guid.Empty : OwConvert.ToGuid(obj.TemplateId),
                CreateUtc = obj.CreateUtc,
                GameUserId = string.IsNullOrEmpty(obj.GameUserId) ? Guid.Empty : OwConvert.ToGuid(obj.GameUserId),
                CurrentDungeonId = OwConvert.ToGuid(obj.CurrentDungeonId),
                CombatStartUtc = obj.CombatStartUtc,
            };

            result.GameItems.AddRange(obj.GameItems.Select(c => (GameItem)c));
            foreach (var item in obj.Properties)
            {
                result.Properties[item.Key] = item.Value;
            }
            return result;
        }

    }

    public partial class GameItemTemplateDto
    {
        public static explicit operator GameItemTemplateDto(GameItemTemplate obj)
        {
            var result = new GameItemTemplateDto()
            {
                Id = obj.Id.ToBase64String(),
                GId = obj.GId,
                ChildrenTemplateIdString = obj.ChildrenTemplateIdString,
                DisplayName = obj.DisplayName,
            };
            foreach (var item in obj.Properties)
            {
                result.Properties[item.Key] = item.Value;
            }
            return result;
        }

        public static explicit operator GameItemTemplate(GameItemTemplateDto obj)
        {
            var result = new GameItemTemplate()
            {
                Id = OwConvert.ToGuid(obj.Id),
                GId = obj.GId,
                ChildrenTemplateIdString = obj.ChildrenTemplateIdString,
                DisplayName = obj.DisplayName,
            };
            foreach (var item in obj.Properties)
            {
                result.Properties[item.Key] = item.Value;
            }
            return result;
        }
    }


    public partial class VWorldInfomationDto
    {
        public static implicit operator VWorldInfomationDto(VWorldInfomation obj)
        {
            var result = new VWorldInfomationDto()
            {
                CurrentDateTime = obj.CurrentDateTime,
                StartDateTime = obj.StartDateTime,
                Version = obj.Version,
            };

            return result;
        }
    }

    #endregion 基础数据

    #region 家园相关

    public partial class ApplyHomelandStyleParamsDto : TokenDtoBase
    {
    }

    public partial class ApplyHomelandStyleReturnDto : ReturnDtoBase
    {
    }

    #endregion 家园相关

    #region 社交相关

    public partial class GameSocialRelationshipDto
    {

        public static implicit operator GameSocialRelationshipDto(GameSocialRelationship obj)
        {
            var result = new GameSocialRelationshipDto()
            {
                Id = obj.Id.ToBase64String(),
                ObjectId = obj.Id2.ToBase64String(),
                KeyType = obj.KeyType,
                Friendliness = (sbyte)obj.Flag,
                PropertyString = obj.PropertyString,
            };
            foreach (var item in obj.Properties)
            {
                result.Properties[item.Key] = item.Value;
            }
            return result;
        }
    }


    #endregion 社交相关

    #region 排行相关
    public partial class RankDataItemDto
    {
        //public static explicit operator RankDataItemDto(GameExtendProperty obj)
        //{
        //    var result = new RankDataItemDto()
        //    {
        //        CharId = obj.Base64IdString,
        //        DisplayName = obj.StringValue,
        //        Metrics = (int)obj.DecimalValue,
        //    };
        //    return result;
        //}

        public static explicit operator RankDataItemDto((Guid, decimal, string, decimal) obj)
        {
            var result = new RankDataItemDto()
            {
                CharId = obj.Item1.ToBase64String(),
                Metrics = (int)obj.Item2,
                DisplayName = obj.Item3,
                IconIndex = (int)obj.Item4,
            };
            return result;
        }
    }
    #endregion  排行相关

    #region 商城相关
    public partial class GameCardTemplateDto
    {
        public static implicit operator GameCardTemplateDto(GameCardPoolTemplate obj)
        {
            var result = new GameCardTemplateDto()
            {
                AutoUse = obj.AutoUse,
                CardPoolGroupString = obj.CardPoolGroupString,
                EndDateTime = obj.EndDateTime,
                Id = obj.Id.ToBase64String(),
                Remark = obj.Remark,
                SellPeriod = obj.SellPeriod,
                StartDateTime = obj.StartDateTime,
                SubCardPoolString = obj.SubCardPoolString,
                ValidPeriod = obj.ValidPeriod,
            };
            OwHelper.Copy(obj.Properties, result.Properties);
            return result;
        }
    }
    #endregion 商城相关

    #region 行会相关

    public partial class GameGuildDto
    {
        /// <summary>
        /// 填充成员信息。
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="dto"></param>
        /// <param name="world">根服务。</param>
        public static void FillMembers(GameGuild guild, GameGuildDto dto, VWorld world)
        {
            if (!world.AllianceManager.Lock(guild.Id, world.AllianceManager.Options.DefaultTimeout, out guild))
                return;
            using var dw = DisposeHelper.Create(c => world.AllianceManager.Unlock(c), guild);
            //var db = guild.GetDbContext();
            using var db = world.CreateNewUserDbContext();
            var coll = from slot in world.AllianceManager.GetAllMemberSlotQuery(guild.Id, db)
                       join gc in db.Set<GameChar>()
                       on slot.OwnerId equals gc.Id
                       join tuiguan in db.Set<GameItem>()
                       on gc.Id equals tuiguan.Parent.OwnerId.Value
                       where tuiguan.ExtraGuid == ProjectConstant.TuiGuanTId
                       select new { gc, slot, tuiguan };
            dto.Members.AddRange(coll.AsEnumerable().Select(c =>
            {
                var r = new GuildMemberDto()
                {
                    DisplayName = c.gc.DisplayName,
                    Id = c.gc.Base64IdString,
                    Title = (int)(c.slot.ExtraDecimal ?? 0),
                    Level = (int)c.gc.Properties.GetDecimalOrDefault("lv"),
                    IconIndex = (int)c.gc.Properties.GetDecimalOrDefault("charIcon", 0),
                    Power = c.tuiguan.ExtraDecimal ?? 0,
                };
                return r;
            }));
            dto.Id = guild.Base64IdString;
            dto.DisplayName = guild.DisplayName;
            OwHelper.Copy(guild.Properties, dto.Properties);
        }
    }

    #endregion 行会相关
}
