using GY2021001BLL;
using GY2021001DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.ObjectModel;
using Gy2021001Template;

namespace GY2021001WebApi.Models
{
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
            };
            foreach (var item in obj.Properties)
            {
                result.Properties[item.Key] = item.Value;
            }
            result.Children.AddRange(obj.Children.Select(c => (GameItem)c));
            return result;
        }

        /// <summary>
        /// 从数据对象获取传输对象。
        /// </summary>
        /// <param name="obj"></param>
        public static explicit operator GameItemDto(GameItem obj)
        {
            var result = new GameItemDto()
            {
                Id = obj.Id.ToBase64String(),
                Count = obj.Count,
                TemplateId = obj.TemplateId.ToBase64String(),
                CreateUtc = obj.CreateUtc,
                OwnerId = obj.OwnerId?.ToBase64String(),
                ParentId = obj.ParentId?.ToBase64String(),
            };

            foreach (var item in obj.Properties)
            {
                result.Properties[item.Key] = item.Value;
            }

            result.Children.AddRange(obj.Children.Select(c => (GameItemDto)c));
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
                GId = obj.GId ?? 0,
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
            };
            foreach (var item in obj.Properties)
            {
                result.Properties[item.Key] = item.Value;
            }
            return result;
        }
    }
}
