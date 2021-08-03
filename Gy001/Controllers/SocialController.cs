﻿using Game.Social;
using GuangYuan.GY001.BLL;
using GuangYuan.GY001.UserDb;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using OW.Game;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gy001.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SocialController : ControllerBase
    {
        private readonly VWorld _World;
        public SocialController(VWorld world)
        {
            _World = world;
        }

        /// <summary>
        /// 获取指定用户的所有邮件。
        /// </summary>
        /// <param name="model">参见 GetMailsParamsDto</param>
        /// <returns>参见 GetMailsReturnDto</returns>
        /// <response code="401">令牌错误。</response>
        [HttpPut]
        public ActionResult<GetMailsReturnDto> GetMails(GetMailsParamsDto model)
        {
            if (!_World.CharManager.Lock(GameHelper.FromBase64String(model.Token), out GameUser gu))
            {
                return Unauthorized("令牌无效");
            }
            try
            {
                var result = new GetMailsReturnDto();
                var social = _World.SocialManager;
                var coll = social.GetMails(gu.CurrentChar);
                result.Mails.AddRange(coll.Select(c => (GameMailDto)c));
                return result;

            }
            finally
            {
                _World.CharManager.Unlock(gu);
            }
        }

        /// <summary>
        /// 删除指定id集合的所有邮件。
        /// </summary>
        /// <param name="model">参见 RemoveMailsParamsDto</param>
        /// <returns>参见 RemoveMailsRetuenDto</returns>
        /// <response code="401">令牌错误。</response>
        [HttpDelete]
        public ActionResult<RemoveMailsRetuenDto> RemoveMails(RemoveMailsParamsDto model)
        {
            if (!_World.CharManager.Lock(GameHelper.FromBase64String(model.Token), out GameUser gu))
            {
                return Unauthorized("令牌无效");
            }
            try
            {
                var result = new RemoveMailsRetuenDto();
                var social = _World.SocialManager;
                result.HasError = !social.RemoveMails(gu.CurrentChar, model.Ids.Select(c => GameHelper.FromBase64String(c)));
                if (result.HasError)
                    result.DebugMessage = VWorld.GetLastErrorMessage();
                return result;

            }
            finally
            {
                _World.CharManager.Unlock(gu);
            }

        }

        /// <summary>
        /// 获取指定的附件。
        /// </summary>
        /// <param name="model">Ids中是附件Id的集合。</param>
        /// <returns>ChangesItems中是收取的物品。</returns>
        /// <response code="401">令牌错误。</response>
        [HttpPost]
        public ActionResult<GetAttachmentesRetuenDto> GetAttachmentes(GetAttachmentesParamsDto model)
        {
            var result = new GetAttachmentesRetuenDto();
            if (!_World.CharManager.Lock(GameHelper.FromBase64String(model.Token), out GameUser gu))
            {
                return Unauthorized("令牌无效");
            }
            try
            {
                var social = _World.SocialManager;
                var changes = new List<ChangesItem>();
                social.GetAttachmentes(model.Ids.Select(c => GameHelper.FromBase64String(c)), gu.CurrentChar, changes);
                result.ChangesItems.AddRange(changes.Select(c => (ChangesItemDto)c));
            }
            catch (Exception err)
            {
                result.DebugMessage = err.Message + " @" + err.StackTrace;
                result.HasError = true;
            }
            finally
            {
                _World.CharManager.Unlock(gu);
            }
            return result;
        }

        #region 好友相关

        /// <summary>
        /// 获取一组角色的摘要信息。以供未来申请好友。
        /// </summary>
        /// <param name="token">当前角色的令牌。</param>
        /// <param name="displayName">指定角色的昵称。如果省略或为null，则不限定昵称而尽量返回活跃用户。</param>
        /// <returns>一组可添加为好友的角色摘要集合。如果没有符合条件的数据则返回空集合。</returns>
        /// <response code="401">令牌错误。</response>
        [HttpGet]
        public ActionResult<GetCharSummaryReturnDto> GetCharSummary(string token, string displayName = null)
        {
            if (!_World.CharManager.Lock(GameHelper.FromBase64String(token), out GameUser gu))
            {
                return Unauthorized("令牌无效");
            }
            try
            {
                var result = new GetCharSummaryReturnDto();
                IEnumerable<CharSummary> coll;
                if (string.IsNullOrWhiteSpace(displayName))
                    coll = _World.SocialManager.GetCharSummary(gu.CurrentChar);
                else
                    coll = _World.SocialManager.GetCharSummary(gu.CurrentChar, displayName);
                result.CharSummaries.AddRange(coll.Select(c => (CharSummaryDto)c));
                return result;
            }
            finally
            {
                _World.CharManager.Unlock(gu);
            }
        }

        /// <summary>
        /// 申请成为另一个角色的好友。
        /// </summary>
        /// <param name="model">参见 RequestFriendParamsDto。</param>
        /// <returns>true成功发送请求 -或- 已经发送过请求 -或- 已经成为好友；false出现错误，参见 DebugMessage 说明。</returns>
        /// <response code="401">令牌错误。</response>
        [HttpPost]
        public ActionResult<RequestFriendReturnDto> RequestFriend(RequestFriendParamsDto model)
        {
            if (!_World.CharManager.Lock(GameHelper.FromBase64String(model.Token), out GameUser gu))
            {
                return Unauthorized("令牌无效");
            }
            try
            {
                var result = new RequestFriendReturnDto();
                var hr = _World.SocialManager.RequestFriend(gu.CurrentChar, GameHelper.FromBase64String(model.FriendId));
                switch (hr)
                {
                    case RequestFriendResult.Success:
                        break;
                    case RequestFriendResult.Already:
                    case RequestFriendResult.Doing:
                        result.DebugMessage = VWorld.GetLastErrorMessage();
                        break;
                    case RequestFriendResult.NotFoundThisChar:
                    case RequestFriendResult.NotFoundObjectChar:
                    case RequestFriendResult.BlackList:
                    case RequestFriendResult.AlreadyBlack:
                    case RequestFriendResult.UnknowError:
                    default:
                        result.DebugMessage = VWorld.GetLastErrorMessage();
                        result.HasError = true;
                        break;
                }
                return result;
            }
            finally
            {
                _World.CharManager.Unlock(gu, true);
            }
        }

        /// <summary>
        /// 获取社交关系信息集合。
        /// </summary>
        /// <param name="token">令牌。</param>
        /// <returns>参见返回值。</returns>
        /// <response code="401">令牌错误。</response>
        [HttpGet]
        public ActionResult<GetSocialRelationshipsReturnDto> GetSocialRelationships(string token)
        {
            if (!_World.CharManager.Lock(GameHelper.FromBase64String(token), out GameUser gu))
            {
                return Unauthorized("令牌无效");
            }
            try
            {
                var result = new GetSocialRelationshipsReturnDto();
                var coll = _World.SocialManager.GetSocialRelationships(gu.CurrentChar);
                result.SocialRelationships.AddRange(coll.Select(c => (GameSocialRelationshipDto)c));
                return result;
            }
            finally
            {
                _World.CharManager.Unlock(gu, true);
            }
        }
        #endregion 好友相关
    }

}
