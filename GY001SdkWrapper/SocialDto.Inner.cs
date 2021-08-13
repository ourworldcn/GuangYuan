using GuangYuan.GY001.UserDb;
using OW.Game;
using System.Linq;

namespace Game.Social
{
    public partial class GameMailDto
    {
        static public implicit operator GameMailDto(GameMail obj)
        {
            var result = new GameMailDto()
            {
                Body = obj.Body,
                CreateUtc = obj.CreateUtc,
                Id = obj.Id.ToBase64String(),
                Subject = obj.Subject,
            };
            result.To.AddRange(obj.To.Select(c => (GameMailAddressDto)c));
            foreach (var item in obj.Properties)
            {
                result.Properties[item.Key] = item.Value;
            }
            result.From = obj.From;
            result.Attachmentes.AddRange(obj.Attachmentes.Select(c => (GameMailAttachmentDto)c));

            return result;
        }
    }

    public partial class GameMailAddressDto
    {
        static public implicit operator GameMailAddressDto(GameMailAddress obj)
        {
            var result = new GameMailAddressDto()
            {
                DisplayName = obj.DisplayName,
                Id = obj.Id.ToBase64String(),
                Kind = obj.Kind,
                ThingId = obj.ThingId.ToBase64String(),
            };
            return result;
        }
    }

    public partial class GameMailAttachmentDto
    {
        static public implicit operator GameMailAttachmentDto(GameMailAttachment obj)
        {
            var result = new GameMailAttachmentDto()
            {
                Id = obj.Id.ToBase64String(),
                IdDeleted = obj.IdDeleted,
            };
            foreach (var item in obj.Properties)
            {
                result.Properties[item.Key] = item.Value;
            }
            return result;
        }
    }
}