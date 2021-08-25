using Game.Social;
using GuangYuan.GY001.BLL;
using GuangYuan.GY001.UserDb;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OW.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using static GuangYuan.GY001.BLL.GameSocialManager;

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
                var changes = new List<ChangeItem>();
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
        /// <param name="model">参见<seealso cref="GetCharSummaryParamsDto"/>。</param>
        /// <returns>一组可添加为好友的角色摘要集合。如果没有符合条件的数据则返回空集合。</returns>
        /// <response code="401">令牌错误。</response>
        /// <response code="500">参数错误。</response>
        [HttpPut]
        public ActionResult<GetCharSummaryReturnDto> GetCharSummary(GetCharSummaryParamsDto model)
        {
            using var data = new GetCharIdsForRequestFriendDatas()
            {
            };
            data.BodyTIds.AddRange(model.BodyTIds.Select(c => GameHelper.FromBase64String(c)));
            using var disposer = data.SetTokenStringAndLock(model.Token, _World.CharManager);
            if (disposer is null)
            {
                return StatusCode(data.ResultCode, data.DebugMessage);
            }
            var result = new GetCharSummaryReturnDto();
            try
            {
                data.BodyTIds.AddRange(model.BodyTIds.Select(c => GameHelper.FromBase64String(c)));
                data.DbContext = _World.CreateNewUserDbContext();
                _World.SocialManager.GetCharIdsForRequestFriend(data);
                if (data.HasError)
                {
                    result.HasError = true;
                    result.DebugMessage = data.DebugMessage;
                    return result;
                }
                var coll = _World.SocialManager.GetCharSummary(data.CharIds, data.DbContext);
                result.CharSummaries.AddRange(coll.Select(c => (CharSummaryDto)c));
            }
            catch (Exception err)
            {
                result.HasError = true;
                result.DebugMessage = err.Message;
            }
            return result;
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
        /// <code>
        ///             GameSocialRelationship sr;
        ///             if(sr.Friendliness&lt;-5)  //若是黑名单
        ///             {
        ///             }
        ///             else
        ///             {
        ///                 var confirmed = sr.Properties.GetValueOrDefault(SocialConstant.ConfirmedFriendPName, decimal.Zero);
        ///                 if (confirmed == decimal.Zero) //若在申请好友中
        ///                 {
        ///                 }
        ///                 else if(sr.Friendliness>5)//若已经是好友
        ///                 {
        ///                 }
        ///                 else    //一般关系，不是黑白名单，但两人可能曾经是有社交关系的，此项通常可以忽略。未来有复杂社交关系时，此处有更多判断和意义
        ///                 {
        ///                 }
        ///             }
        /// </code>
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

        /// <summary>
        /// 确认或拒绝好友申请。
        /// </summary>
        /// <returns></returns>
        /// <response code="401">令牌错误。</response>
        [HttpPost]
        public ActionResult<ConfirmRequestFriendReturnDto> ConfirmRequestFriend(ConfirmRequestFriendParamsDto model)
        {
            if (!_World.CharManager.Lock(GameHelper.FromBase64String(model.Token), out GameUser gu))
            {
                return Unauthorized("令牌无效");
            }
            try
            {
                var result = new ConfirmRequestFriendReturnDto();
                var succ = _World.SocialManager.ConfirmFriend(gu.CurrentChar, GameHelper.FromBase64String(model.FriendId), model.IsRejected);
                if (!succ)
                {
                    result.DebugMessage = VWorld.GetLastErrorMessage();
                    result.HasError = true;
                }
                return result;
            }
            finally
            {
                _World.CharManager.Unlock(gu, true);
            }
        }

        /// <summary>
        /// 移除好友。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="401">令牌错误。</response>
        [HttpDelete]
        public ActionResult<ModifySrReturnDto> RemoveFriend(ModifySrParamsDto model)
        {
            var result = new ModifySrReturnDto();
            if (!_World.CharManager.Lock(GameHelper.FromBase64String(model.Token), out GameUser gu))
            {
                return Unauthorized("令牌无效");
            }
            try
            {
                if (!_World.SocialManager.RemoveFriend(gu.CurrentChar, GameHelper.FromBase64String(model.FriendId)))
                    result.DebugMessage = VWorld.GetLastErrorMessage();
            }
            catch (Exception err)
            {
                result.DebugMessage = err.Message;
                result.HasError = true;
            }
            finally
            {
                _World.CharManager.Unlock(gu, true);
            }
            return result;
        }

        /// <summary>
        /// 拉黑其他角色。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="401">令牌错误。</response>
        [HttpPost]
        public ActionResult<ModifySrReturnDto> SetBlack(ModifySrParamsDto model)
        {
            var result = new ModifySrReturnDto();
            if (!_World.CharManager.Lock(GameHelper.FromBase64String(model.Token), out GameUser gu))
            {
                return Unauthorized("令牌无效");
            }
            try
            {
                if (!_World.SocialManager.SetFrindless(gu.CurrentChar, GameHelper.FromBase64String(model.FriendId)))
                    result.DebugMessage = VWorld.GetLastErrorMessage();
            }
            catch (Exception err)
            {
                result.DebugMessage = err.Message;
                result.HasError = true;
            }
            finally
            {
                _World.CharManager.Unlock(gu, true);
            }
            return result;
        }

        /// <summary>
        /// 进行社交互动的通用接口。
        /// </summary>
        /// <param name="model">参见 InteractParamsDto 说明。</param>
        /// <returns>参见 InteractReturnDto 说明。</returns>
        /// <response code="401">令牌错误。</response>
        [HttpPost]
        public ActionResult<InteractReturnDto> Interact(InteractParamsDto model)
        {
            if (!_World.CharManager.Lock(GameHelper.FromBase64String(model.Token), out GameUser gu))
            {
                return Unauthorized("令牌无效");
            }
            InteractReturnDto result = new InteractReturnDto();
            try
            {
                var actioveId = GameHelper.FromBase64String(model.ActiveId);
                if (actioveId == InteractActiveIds.PatForTili)  //获取体力
                {
                    if (PatForTiliResult.Success != _World.SocialManager.PatForTili(gu.CurrentChar, GameHelper.FromBase64String(model.ObjectId)))
                    {
                        result.DebugMessage = VWorld.GetLastErrorMessage();
                        result.HasError = true;
                    }
                }
                else if (InteractActiveIds.PatWithMounts == actioveId)  //与坐骑互动
                {

                }
                else
                {
                    result = new InteractReturnDto()
                    {
                        HasError = true,
                        DebugMessage = $"未知的行为Id={actioveId}",
                    };
                }
            }
            finally
            {
                _World.CharManager.Unlock(gu, true);
            }
            return result;
        }

        /// <summary>
        /// 与好友家园的展示坐骑互动。
        /// </summary>
        /// <param name="model"><seealso cref="PatWithMountsParamsDto"/></param>
        /// <returns><seealso cref="PatWithMountsReturnDto"/></returns>
        /// <response code="401">令牌错误。</response>
        [HttpPost]
        public ActionResult<PatWithMountsReturnDto> PatWithMounts(PatWithMountsParamsDto model)
        {
            if (!_World.CharManager.Lock(GameHelper.FromBase64String(model.Token), out GameUser gu))
            {
                return Unauthorized("令牌无效");
            }
            var result = new PatWithMountsReturnDto();
            try
            {
                var datas = new PatWithMountsDatas()
                {
                    CurrentMountsId = GameHelper.FromBase64String(model.CurrentMountsId),
                    GameChar = gu.CurrentChar,
                    MountsId = GameHelper.FromBase64String(model.MountsId),
                };
                _World.SocialManager.PatWithMounts(datas);
                result.HasError = datas.HasError;
                result.DebugMessage = datas.DebugMessage;
                result.Changes.AddRange(datas.Changes.Select(c => (ChangesItemDto)c));
                result.MailItems.AddRange(datas.MailItems.Select(c => (ChangesItemDto)c));
            }
            finally
            {
                _World.CharManager.Unlock(gu, true);
            }
            return result;
        }

        /// <summary>
        /// 获取指定用户家园数据的接口。
        /// </summary>
        /// <param name="model"><seealso cref="GetHomelandDataParamsDto"/></param>
        /// <returns><seealso cref="GetHomelandDataReturnDto"/> </returns>
        /// <response code="401">令牌错误。</response>
        [HttpGet]
        public ActionResult<GetHomelandDataReturnDto> GetHomelandData([FromQuery] GetHomelandDataParamsDto model)
        {
            var world = HttpContext.RequestServices.GetRequiredService<VWorld>();   //获取虚拟世界的根服务
            //构造调用参数
            var datas = new GetHomelandDataDatas();
            using var disposer = datas.SetTokenStringAndLock(model.Token, world.CharManager);
            if (disposer is null)   //若锁定失败
                return StatusCode(datas.ResultCode, datas.DebugMessage);
            //填写其他参数
            datas.OtherCharId = GameHelper.FromBase64String(model.OtherCharId);
            world.SocialManager.GetHomelandData(datas);  //调用服务
            //构造返回参数
            var result = new GetHomelandDataReturnDto()
            {
                HasError = datas.HasError,
                DebugMessage = datas.DebugMessage,
            };
            if (!result.HasError)
            {
                result.CurrentFengge = (HomelandFenggeDto)datas.CurrentFengge;
                result.Lands.AddRange(datas.Lands.Select(c => (GameItemDto)c));
                result.Mounts.AddRange(datas.Mounts.Select(c => (GameItemDto)c));
            }
            return result;
        }

        /// <summary>
        /// 获取可以或已经pvp的角色的列表。
        /// </summary>
        /// <param name="model"><seealso cref="GetPvpListParamsDto"/></param>
        /// <returns><seealso cref="GetPvpListReturnDto"/></returns>
        /// <response code="401">令牌错误。</response>
        [HttpGet]
        public ActionResult<GetPvpListReturnDto> GetPvpList([FromQuery] GetPvpListParamsDto model)
        {
            if (!_World.CharManager.Lock(GameHelper.FromBase64String(model.Token), out GameUser gu))
            {
                return Unauthorized("令牌无效");
            }
            GetPvpListReturnDto result = null;
            try
            {
                result = new GetPvpListReturnDto();
                var returnData = _World.SocialManager.GetPvpChars(gu.CurrentChar, DateTime.UtcNow);
                result.PvpList.AddRange(returnData.Select(c => (GameActionRecordDto)c));
            }
            catch (Exception err)
            {
                result.HasError = true;
                result.DebugMessage = err.Message;
            }
            finally
            {
                _World.CharManager.Unlock(gu);
            }
            return result;
        }
    }

}
