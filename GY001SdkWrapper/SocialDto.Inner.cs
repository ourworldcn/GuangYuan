using GuangYuan.GY001.UserDb;
using OW.Game;
using OW.Game.Store;
using System;
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
                Kind = (MailAddressKindDto)obj.Kind,
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

    /// <summary>
    /// 关系对象扩展方法封装类。
    /// </summary>
    public static class GameSocialRelationshipExtensions
    {

        /// <summary>
        /// 获取指示，该对象是否是一个黑名单。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        static public bool IsBlack(this GameSocialRelationship obj) =>
             obj.Flag < SocialConstant.MiddleFriendliness - 5 && obj.Flag >= SocialConstant.MinFriendliness;

        /// <summary>
        /// 获取指示，该对象是否指示了一个好友。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        static public bool IsFriendOrRequesting(this GameSocialRelationship obj) =>
             obj.Flag > SocialConstant.MiddleFriendliness + 5 && obj.Flag <= SocialConstant.MaxFriendliness;

        /// <summary>
        /// 获取指示，该对象是否是一个已经确定的好友。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        static public bool IsFriend(this GameSocialRelationship obj) =>
             obj.IsFriendOrRequesting() && obj.Properties.GetDecimalOrDefault(SocialConstant.ConfirmedFriendPName, decimal.Zero) != decimal.Zero;

        /// <summary>
        /// 设置该对象表示好友关系。
        /// </summary>
        /// <param name="obj"></param>
        static public void SetFriend(this GameSocialRelationship obj) => obj.Flag = SocialConstant.MiddleFriendliness + 6;

        /// <summary>
        /// 设置该对象指示，中立关系。
        /// </summary>
        /// <param name="obj"></param>
        static public void SetNeutrally(this GameSocialRelationship obj)
        {
            obj.Flag = SocialConstant.MiddleFriendliness;
            obj.Properties[SocialConstant.ConfirmedFriendPName] = decimal.One;
        }


        /// <summary>
        /// 设置该对象指示，黑名单关系。
        /// </summary>
        /// <param name="obj"></param>
        static public void SetBlack(this GameSocialRelationship obj) => obj.Flag = SocialConstant.MiddleFriendliness - 6;

        /// <summary>
        /// 设置正在申请标志。
        /// </summary>
        /// <param name="obj"></param>
        static public void SetRequesting(this GameSocialRelationship obj) => obj.Properties[SocialConstant.ConfirmedFriendPName] = decimal.Zero;

        /// <summary>
        /// 设置确定标志。
        /// </summary>
        /// <param name="obj"></param>
        static public void SetConfirmed(this GameSocialRelationship obj) => obj.Properties[SocialConstant.ConfirmedFriendPName] = decimal.One;
    }

}