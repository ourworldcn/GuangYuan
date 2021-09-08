﻿using Game.Social;
using GuangYuan.GY001.BLL;
using GuangYuan.GY001.BLL.Homeland;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using OW.Game;
using System;
using System.Linq;
using System.Text.Json;

namespace GY2021001WebApi.Models
{
    #region 基础数据

    public partial class IdAndCountDto
    {
        public static implicit operator ValueTuple<Guid, decimal>(IdAndCountDto obj) =>
            (GameHelper.FromBase64String(obj.Id), obj.Count);

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
                Id = GameHelper.FromBase64String(obj.Id),
                Count = obj.Count,
                CreateUtc = obj.CreateUtc,
                TemplateId = string.IsNullOrEmpty(obj.TemplateId) ? Guid.Empty : GameHelper.FromBase64String(obj.TemplateId),
                OwnerId = string.IsNullOrEmpty(obj.OwnerId) ? Guid.Empty : GameHelper.FromBase64String(obj.OwnerId),
                ParentId = string.IsNullOrEmpty(obj.ParentId) ? Guid.Empty : GameHelper.FromBase64String(obj.ParentId),
                ClientGutsString = obj.ClientString,
            };
            foreach (var item in obj.Properties)
            {
                result.Properties[item.Key] = item.Value is JsonElement je ? (je.ValueKind switch { JsonValueKind.Number => je.GetDecimal(), _ => throw new InvalidOperationException(), }) : item.Value;
            }
            result.Children.AddRange(obj.Children.Select(c => (GameItem)c));
            result.GenerateIdIfEmpty();
            return result;
        }

        /// <summary>
        /// 从数据对象获取传输对象。
        /// </summary>
        /// <param name="obj"></param>
        public static implicit operator GameItemDto(GameItem obj)
        {
            var result = new GameItemDto()
            {
                Id = obj.Id.ToBase64String(),
                Count = obj.Count,
                TemplateId = obj.TemplateId.ToBase64String(),
                CreateUtc = obj.CreateUtc,
                OwnerId = obj.OwnerId?.ToBase64String(),
                ParentId = obj.ParentId?.ToBase64String(),
                ClientString = obj.ClientGutsString,
            };
            foreach (var item in obj.Name2FastChangingProperty)
            {
                item.Value.GetCurrentValueWithUtc();
                FastChangingPropertyExtensions.ToDictionary(item.Value, obj.Properties, item.Key);
            }
            result._Properties = new System.Collections.Generic.Dictionary<string, object>(obj.Properties);
            //特殊处理处理木材堆叠数
            if (ProjectConstant.MucaiId == obj.TemplateId)
                result.Properties[ProjectConstant.StackUpperLimit] = obj.GetDecimalOrDefault(ProjectConstant.StackUpperLimit);
            result.Children.AddRange(obj.Children.Select(c => (GameItemDto)c));
            return result;
        }

        static public GameItemDto FromGameItem(GameItem obj, bool includeChildren = false)
        {
            var result = new GameItemDto()
            {
                Id = obj.Id.ToBase64String(),
                Count = obj.Count,
                TemplateId = obj.TemplateId.ToBase64String(),
                CreateUtc = obj.CreateUtc,
                OwnerId = obj.OwnerId?.ToBase64String(),
                ParentId = obj.ParentId?.ToBase64String(),
                ClientString = obj.ClientGutsString,
            };
            if (obj.Name2FastChangingProperty.TryGetValue("Count", out var fcp))
                result.Count = fcp.LastValue;
            result._Properties = new System.Collections.Generic.Dictionary<string, object>(obj.Properties);
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
                Id = GameHelper.FromBase64String(obj.Id),
                TemplateId = string.IsNullOrEmpty(obj.TemplateId) ? Guid.Empty : GameHelper.FromBase64String(obj.TemplateId),
                CreateUtc = obj.CreateUtc,
                GameUserId = string.IsNullOrEmpty(obj.GameUserId) ? Guid.Empty : GameHelper.FromBase64String(obj.GameUserId),
                CurrentDungeonId = GameHelper.FromBase64String(obj.CurrentDungeonId),
                CombatStartUtc = obj.CombatStartUtc,
            };

            result.GameItems.AddRange(obj.GameItems.Select(c => (GameItem)c));
            foreach (var item in obj.Properties)
            {
                result.Properties[item.Key] = item.Value;
            }
            return result;
        }

        /// <summary>
        /// 从数据对象获取传输对象。
        /// </summary>
        /// <param name="obj"></param>
        public static explicit operator GameCharDto(GameChar obj)
        {
            var result = new GameCharDto()
            {
                Id = obj.Id.ToBase64String(),
                ClientGutsString = obj.ClientGutsString,
                CreateUtc = obj.CreateUtc,
                DisplayName = obj.DisplayName,
                GameUserId = obj.GameUserId.ToBase64String(),
                TemplateId = obj.TemplateId.ToBase64String(),
                CurrentDungeonId = obj.CurrentDungeonId?.ToBase64String(),
                CombatStartUtc = obj.CombatStartUtc,
            };
            result.GameItems.AddRange(obj.GameItems.Select(c => (GameItemDto)c));
            foreach (var item in obj.Properties)
            {
                result.Properties[item.Key] = item.Value;
            }
            foreach (var item in obj.ExtendProperties.Where(c => c.Name == GameExtendProperty.ClientPropertyName))    //初始化客户端扩展属性
            {
                result.ClientExtendProperties[item.StringValue] = item.Text;
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
                Id = GameHelper.FromBase64String(obj.Id),
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


    public partial class ChangesItemDto
    {

        public static implicit operator ChangesItemDto(ChangeItem obj)
        {
            var result = new ChangesItemDto()
            {
                ContainerId = obj.ContainerId.ToBase64String(),
                DateTimeUtc = obj.DateTimeUtc,
            };
            result.Adds.AddRange(obj.Adds.Select(c => (GameItemDto)c));
            result.Changes.AddRange(obj.Changes.Select(c => (GameItemDto)c));
            result.Removes.AddRange(obj.Removes.Select(c => c.ToBase64String()));
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
            };
            return result;
        }
    }

    public partial class GameExtendPropertyDto
    {
        public static implicit operator GameExtendPropertyDto(GameExtendProperty obj)
        {
            var result = new GameExtendPropertyDto()
            {
                DateTimeValue = obj.DateTimeValue,
                DecimalValue = obj.DecimalValue,
                DoubleValue = obj.DoubleValue,
                IntValue = obj.IntValue,
                Name = obj.Name,
                StringValue = obj.StringValue,
                Text = obj.Text,
                ParentId = obj.Id,
            };
            return result;
        }
    }
    #endregion 基础数据

    public partial class CombatStartReturnDto
    {
        public static explicit operator CombatStartReturnDto(StartCombatData obj)
        {
            var result = new CombatStartReturnDto()
            {
                TemplateId = obj.Template?.Id.ToBase64String(),
                HasError = obj.HasError,
                DebugMessage = obj.DebugMessage,
            };
            return result;
        }
    }

    public partial class CombatEndReturnDto
    {
        public static explicit operator CombatEndReturnDto(EndCombatData obj)
        {
            var result = new CombatEndReturnDto()
            {
                NextDungeonId = obj.NextTemplate?.Id.ToBase64String(),
                HasError = obj.HasError,
                DebugMessage = obj.DebugMessage,
            };
            result.ChangesItems.AddRange(obj.ChangesItems.Select(c => (ChangesItemDto)c));
            return result;
        }

    }


    public partial class ApplyBlueprintReturnDto
    {
        public static explicit operator ApplyBlueprintReturnDto(ApplyBlueprintDatas obj)
        {
            var result = new ApplyBlueprintReturnDto()
            {
                HasError = obj.HasError,
                DebugMessage = obj.DebugMessage,
                SuccCount = obj.SuccCount,
            };
            if (!result.HasError)
            {
                result.ChangesItems.AddRange(obj.ChangesItem.Select(c => (ChangesItemDto)c));
                result.FormulaIds.AddRange(obj.FormulaIds.Select(c => c.ToBase64String()));
                result.ErrorTIds.AddRange(obj.ErrorItemTIds.Select(c => c.ToBase64String()));
                result.MailIds.AddRange(obj.MailIds.Select(c => c.ToBase64String()));
            }
            return result;
        }

    }


    #region 家园相关

    public partial class HomelandFenggeDto
    {
        public Guid StyleTemplateId
        {
            get => default;
            set
            {
            }
        }

        public static explicit operator HomelandFengge(HomelandFenggeDto obj)
        {
            var result = new HomelandFengge()
            {
                ClientString = obj.ClientString,
                Number = obj.Id,
            };
            result.Fangans.AddRange(obj.Fangans.Select(c => (HomelandFangan)c));
            return result;
        }

        public static explicit operator HomelandFenggeDto(HomelandFengge obj)
        {
            var result = new HomelandFenggeDto()
            {
                ClientString = obj.ClientString,
                Id = obj.Number,
            };
            result.Fangans.AddRange(obj.Fangans.Select(c => (HomelandFanganDto)c));
            return result;
        }
    }

    public partial class HomelandFanganDto
    {
        public static explicit operator HomelandFanganDto(HomelandFangan obj)
        {
            var result = new HomelandFanganDto()
            {
                ClientString = obj.ClientString,
                Id = obj.Id.ToBase64String(),
                IsActived = obj.IsActived,
                OrderNumber = obj.OrderNumber,
            };
            result.FanganItems.AddRange(obj.FanganItems.Select(c => (HomelandFanganItemDto)c));
            return result;
        }

        public static explicit operator HomelandFangan(HomelandFanganDto obj)
        {
            var result = new HomelandFangan()
            {
                ClientString = obj.ClientString,
                Id = GameHelper.FromBase64String(obj.Id),
                IsActived = obj.IsActived,
                OrderNumber = obj.OrderNumber,
            };
            result.FanganItems.AddRange(obj.FanganItems.Select(c => (HomelandFanganItem)c));
            return result;
        }


    }

    public partial class HomelandFanganItemDto
    {
        public static explicit operator HomelandFanganItem(HomelandFanganItemDto obj)
        {
            var result = new HomelandFanganItem()
            {
                ContainerId = GameHelper.FromBase64String(obj.ContainerId),
                NewTemplateId = GameHelper.FromBase64String(obj.NewTemplateId),
                ClientString = obj.ClientString,
            };
            result.ItemIds.AddRange(obj.ItemIds.Select(c => GameHelper.FromBase64String(c)));
            return result;
        }

        public static explicit operator HomelandFanganItemDto(HomelandFanganItem obj)
        {
            var result = new HomelandFanganItemDto()
            {
                ContainerId = obj.ContainerId.ToBase64String(),
                NewTemplateId = obj.NewTemplateId?.ToBase64String(),
                ClientString = obj.ClientString,
            };
            result.ItemIds.AddRange(obj.ItemIds.Select(c => c.ToBase64String()));
            return result;
        }


    }

    public partial class ApplyHomelandStyleParamsDto : TokenDtoBase
    {
    }

    public partial class ApplyHomelandStyleReturnDto : ReturnDtoBase
    {
    }

    #endregion 家园相关

    #region 社交相关

    public partial class CharSummaryDto
    {

        public static implicit operator CharSummaryDto(CharSummary obj)
        {
            var result = new CharSummaryDto()
            {
                CombatCap = obj.CombatCap,
                DisplayName = obj.DisplayName,
                Id = obj.Id.ToBase64String(),
                LastLogoutDatetime = obj.LastLogoutDatetime,
                Level = obj.Level,
            };
            result.HomelandShows.AddRange(obj.HomelandShows.Select(c => (GameItemDto)c));
            return result;
        }
    }

    public partial class GameSocialRelationshipDto
    {

        static public implicit operator GameSocialRelationshipDto(GameSocialRelationship obj)
        {
            var result = new GameSocialRelationshipDto()
            {
                Id = obj.Id.ToBase64String(),
                ObjectId = obj.Id2.ToBase64String(),
                KeyType = obj.KeyType,
                Friendliness = (sbyte)obj.Flag,
            };
            foreach (var item in obj.Properties)
            {
                result.Properties[item.Key] = item.Value;
            }
            return result;
        }
    }


    #endregion 社交相关


}
