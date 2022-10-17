using AutoMapper;
using Game.Social;
using GuangYuan.GY001.BLL;
using GuangYuan.GY001.BLL.Homeland;
using GuangYuan.GY001.BLL.Specific;
using GuangYuan.GY001.UserDb;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OW.Game;
using OW.Game.Store;
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

        /// <summary>
        /// 范围性的用户数据库上下文。
        /// </summary>
        private readonly GY001UserContext _UserContext;

        /// <summary>
        /// 构造函数，用于DI。
        /// </summary>
        /// <param name="world"></param>
        /// <param name="userContext">对于社交类功能，大概率需要范围的数据库上下文。</param>
        /// 
        public SocialController(VWorld world, GY001UserContext userContext)
        {
            _UserContext = userContext;
            _World = world;
        }

#if DEBUG
        [HttpGet]
        public ActionResult<bool> Test()
        {
            return true;
        }
#endif //DEBUG

        /// <summary>
        /// 获取指定用户的所有邮件。
        /// </summary>
        /// <param name="model">参见 GetMailsParamsDto</param>
        /// <returns>参见 GetMailsReturnDto</returns>
        /// <response code="401">令牌错误。</response>
        [HttpPut]
        public ActionResult<GetMailsReturnDto> GetMails(GetMailsParamsDto model)
        {
            var result = new GetMailsReturnDto();
            var social = _World.SocialManager;
            using var datas = new GetMailsDatas(_World, model.Token)
            {
                UserDbContext = _UserContext,
            };
            social.GetMails(datas);
            if (!datas.HasError)
                if (model.Ids is null || model.Ids.Count <= 0)  //若取所有邮件
                    result.Mails.AddRange(datas.Mails.Select(c => (GameMailDto)c));
                else
                {
                    result.Mails.AddRange(datas.Mails.Select(c => (GameMailDto)c));    //TO DO效率低下
                    result.Mails.RemoveAll(c => !model.Ids.Contains(c.Id));
                }
            else
            {
                result.HasError = true;
                result.ErrorCode = datas.ErrorCode;
                result.DebugMessage = datas.DebugMessage;
                if (datas.ErrorCode == ErrorCodes.ERROR_INVALID_TOKEN)
                    return Unauthorized(datas.DebugMessage);
            }
            return result;
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
            if (!_World.CharManager.Lock(OwConvert.ToGuid(model.Token), out GameUser gu))
            {
                return base.Unauthorized("令牌无效");
            }
            try
            {
                var result = new RemoveMailsRetuenDto();
                var social = _World.SocialManager;
                result.HasError = !social.RemoveMails(gu.CurrentChar, model.Ids.Select(c => OwConvert.ToGuid(c)));
                if (result.HasError)
                    result.DebugMessage = OwHelper.GetLastErrorMessage();
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
            if (!_World.CharManager.Lock(OwConvert.ToGuid(model.Token), out GameUser gu))
            {
                return base.Unauthorized("令牌无效");
            }
            var results = new List<(Guid, GetAttachmenteItemResult)>();
            var db = _UserContext;
            try
            {
                var social = _World.SocialManager;
                var changes = new List<ChangeItem>();
                social.GetAttachmentes(model.Ids.Select(c => OwConvert.ToGuid(c)), gu.CurrentChar, db, changes, results);
                var mapper = _World.Service.GetRequiredService<IMapper>();
                result.ChangesItems.AddRange(changes.Select(c => mapper.Map<ChangesItemDto>(c)));
                result.Results.AddRange(results.Select(c => new GetAttachmentesResultItemDto
                {
                    Id = c.Item1.ToBase64String(),
                    Result = c.Item2,
                }));
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
            using var data = new GetCharIdsForRequestFriendDatas(_World, model.Token)
            {
                DisplayName = model.DisplayName,
                UserDbContext = _UserContext,
            };
            var result = new GetCharSummaryReturnDto();
            using var disposer = data.LockUser();
            if (disposer is null)
            {
                result.FillFromWorld();
                return result;
            }
            try
            {
                data.BodyTIds.AddRange(model.BodyTIds.Select(c => OwConvert.ToGuid(c)));
                _World.SocialManager.GetCharIdsForRequestFriend(data);
                if (data.HasError)
                {
                    result.HasError = true;
                    result.DebugMessage = data.DebugMessage;
                    return result;
                }
                var coll = _World.SocialManager.GetCharSummary(data.CharIds, data.UserDbContext);
                var mapper = _World.GetMapper();
                result.CharSummaries.AddRange(coll.Select(c => mapper.Map(c)));
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
            if (!_World.CharManager.Lock(OwConvert.ToGuid(model.Token), out GameUser gu))
            {
                return base.Unauthorized("令牌无效");
            }
            try
            {
                var result = new RequestFriendReturnDto()
                {
                    FriendId = model.FriendId,
                };
                using var datas = new RequestFriendData(_World, model.Token, OwConvert.ToGuid(model.FriendId));
                var hr = _World.SocialManager.RequestFriend(datas);
                switch (hr)
                {
                    case RequestFriendResult.Success:
                        break;
                    case RequestFriendResult.Already:
                    case RequestFriendResult.Doing:
                        result.DebugMessage = OwHelper.GetLastErrorMessage();
                        break;
                    case RequestFriendResult.NotFoundThisChar:
                    case RequestFriendResult.NotFoundObjectChar:
                    case RequestFriendResult.BlackList:
                    case RequestFriendResult.AlreadyBlack:
                    case RequestFriendResult.UnknowError:
                    default:
                        result.DebugMessage = OwHelper.GetLastErrorMessage();
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
        ///                 var confirmed = sr.GetSdpValueOrDefault(SocialConstant.ConfirmedFriendPName, decimal.Zero);
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
            if (!_World.CharManager.Lock(OwConvert.ToGuid(token), out GameUser gu))
            {
                return base.Unauthorized("令牌无效");
            }
            var db = _UserContext;
            try
            {
                var result = new GetSocialRelationshipsReturnDto();
                var coll = _World.SocialManager.GetSocialRelationships(gu.CurrentChar, db).AsEnumerable().Where(c => c.Flag != SocialConstant.MiddleFriendliness); //过滤掉中立玩家
                result.SocialRelationships.AddRange(coll.Select(c => (GameSocialRelationshipDto)c));
                var ids = coll.Select(c => c.Id).Union(coll.Select(c => c.Id2)).Distinct();
                var summs = _World.SocialManager.GetCharSummary(ids, db);
                var mapper = _World.GetMapper();
                result.Summary.AddRange(summs.Select(c => mapper.Map(c)));
                return result;
            }
            finally
            {
                _World.CharManager.Unlock(gu, true);
            }
        }

        /// <summary>
        /// 通用获取关系数据集合接口。此接口数据用时再刷，不要缓存。
        /// </summary>
        /// <param name="model"></param>
        /// <returns>
        /// 返回集合中，每个元素的KeyType属性标识了该数据的类型：
        /// 如等于 SocialKeyTypes.PatWithMounts 就是指定了签约坐骑，进一步 其Id等于自己角色Id，则此时 Id2是对方坐骑的唯一Id。
        /// <code>
        ///             IEnumerable&lt;GameSocialRelationshipDto&gt; srds = null;   //返回的关系集合
        ///             string myCharIdString = "获取当前角色Id";  //己方角色唯一Id
        ///             var result = srds.Where(c => c.KeyType == (int)SocialKeyTypes.PatWithMounts &amp;&amp; c.Id == myCharIdString);    //所有签约坐骑关系数据
        ///             var mountsIds = result.Select(c => c.ObjectId); //所有签约坐骑的Id集合
        /// </code>
        /// </returns>
        /// <response code="401">令牌错误。</response>
        [HttpPut]
        public ActionResult<GetSocialRelationshipsReturnDto> GetSocialRelationships(GetSocialRelationshipsParamsDto model)
        {
            var gu = _World.CharManager.GetUserFromToken(OwConvert.ToGuid(model.Token));
            if (gu is null)
                return Unauthorized("令牌错误。");
            var gc = gu.CurrentChar;
            IEnumerable<GameSocialRelationship> coll;
            if (model.KeyTypes.Count > 0)  //若按键类型过滤
            {
                coll = _UserContext.SocialRelationships.Where(c => c.Id == gc.Id && model.KeyTypes.Contains(c.KeyType)).Concat(
                    _UserContext.SocialRelationships.Where(c => c.Id2 == gc.Id && model.KeyTypes.Contains(c.KeyType))).ToArray();
            }
            else //获取所有类型关系数据
            {
                coll = _UserContext.SocialRelationships.Where(c => model.KeyTypes.Contains(c.KeyType)).ToArray();
            }
            var result = new GetSocialRelationshipsReturnDto();
            result.SocialRelationships.AddRange(coll.Select(c => (GameSocialRelationshipDto)c));
            var ids = coll.Where(c => c.TryGetSdp("charid", out _)).Select(c => c.GetSdpGuidOrDefault("charid"));
            var mapper = _World.GetMapper();
            if (ids.Any())
            {
                result.Summary.AddRange(_World.SocialManager.GetCharSummary(ids, _UserContext).Select(c => mapper.Map(c)));
            }
            var relIds = coll.Select(c => c.Id2).Concat(coll.Select(c => c.Id)).ToArray();  //可能相关的物品信息
            result.GameItems.AddRange(_UserContext.Set<GameItem>().Where(c => relIds.Contains(c.Id)).ToArray().Select(c => mapper.Map(c)));   //补足物品信息
            return result;
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
            using var dwUser = _World.CharManager.LockAndReturnDisposer(model.Token, out var gu);
            if (dwUser is null)
            {
                return Unauthorized("令牌无效");
            }
            var result = new ConfirmRequestFriendReturnDto();
            try
            {
                foreach (var item in model.Items)
                {
                    var succ = _World.SocialManager.ConfirmFriend(gu.CurrentChar, OwConvert.ToGuid(item.FriendId), item.IsRejected);
                    var resultItem = new ConfirmRequestFriendReturnItemDto()
                    {
                        Id = item.FriendId,
                        Result = succ,
                    };
                    result.Results.Add(resultItem);
                }
            }
            catch (Exception err)
            {
                result.DebugMessage = err.Message;
                result.HasError = true;
            }
            return result;
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
            var result = new ModifySrReturnDto() { FriendId = model.FriendId };
            if (!_World.CharManager.Lock(OwConvert.ToGuid(model.Token), out GameUser gu))
            {
                return base.Unauthorized("令牌无效");
            }
            try
            {
                if (!_World.SocialManager.RemoveFriend(gu.CurrentChar, OwConvert.ToGuid(model.FriendId)))
                    result.DebugMessage = OwHelper.GetLastErrorMessage();

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
            if (!_World.CharManager.Lock(OwConvert.ToGuid(model.Token), out GameUser gu))
            {
                return base.Unauthorized("令牌无效");
            }
            try
            {
                if (!_World.SocialManager.SetFrindless(gu.CurrentChar, OwConvert.ToGuid(model.FriendId)))
                    result.DebugMessage = OwHelper.GetLastErrorMessage();
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
        /// 移除黑名单。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="401">令牌错误。</response>
        [HttpDelete]
        public ActionResult<RemoveBlackReturnDto> RemoveBlack(RemoveBlackParamsDto model)
        {
            var result = new RemoveBlackReturnDto();
            if (!_World.CharManager.Lock(OwConvert.ToGuid(model.Token), out GameUser gu))
            {
                return base.Unauthorized("令牌无效");
            }
            try
            {
                if (!_World.SocialManager.RemoveBlack(gu.CurrentChar, OwConvert.ToGuid(model.CharId)))
                {
                    result.HasError = true;
                    result.DebugMessage = OwHelper.GetLastErrorMessage();
                }
            }
            catch (Exception err)
            {
                result.HasError = true;
                result.DebugMessage = err.Message;
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
        /// <param name="model">参见 PatForTiliParamsDto 说明。</param>
        /// <returns>参见 PatForTiliReturnDto 说明。</returns>
        /// <response code="401">令牌错误。</response>
        [HttpPost]
        public ActionResult<PatForTiliReturnDto> PatForTili(PatForTiliParamsDto model)
        {
            var gc = _World.CharManager.GetGameCharFromToken(model.Token);
            if (gc is null)
                return Unauthorized("令牌无效");
            PatForTiliReturnDto result = new PatForTiliReturnDto();
            using var datas = new PatForTiliWorkData(_World, gc, OwConvert.ToGuid(model.ObjectId), DateTime.UtcNow)
            {
                UserDbContext = _UserContext
            };
            var r = _World.SocialManager.PatForTili(datas);
            if (PatForTiliResult.Success != r)
            {
                result.DebugMessage = OwHelper.GetLastErrorMessage();
                result.HasError = true;
            }
            result.Code = r;
            var mapper = _World.Service.GetRequiredService<IMapper>();
            result.ChangesItems.AddRange(datas.ChangeItems.Select(c => mapper.Map<ChangesItemDto>(c)));
            if (result.HasError = datas.HasError)
            {
                result.DebugMessage = datas.DebugMessage;
            }
            return result;
        }

        /// <summary>
        /// 与好友家园的展示坐骑互动。返回码：160=今日已经与该坐骑互动过了；1721=今日互动次数已经用完。
        /// </summary>
        /// <param name="model">参见 "PatWithMountsParamsDto" </param>
        /// <returns> 返回码：160=今日已经与该坐骑互动过了；1721=今日互动次数已经用完。</returns>
        /// <response code="401">令牌错误。</response>
        /// <response code="400">其他异常错误。</response>
        [HttpPost]
        public ActionResult<PatWithMountsReturnDto> PatWithMounts(PatWithMountsParamsDto model)
        {
            var result = new PatWithMountsReturnDto();
            try
            {
                using var datas = new PatWithMountsDatas(_World, model.Token, OwConvert.ToGuid(model.MountsId), DateTime.UtcNow)
                {
                    UserDbContext = _UserContext,
                    IsRemove = model.IsRemove,
                };
                using var dwChar = datas.LockUser();
                if (dwChar is null)
                    return Unauthorized("令牌无效");

                _World.SocialManager.PatWithMounts(datas);
                result.FillFrom(datas);
                if (!result.HasError)
                {
                    var mapper = _World.Service.GetRequiredService<IMapper>();
                    result.Changes.AddRange(datas.ChangeItems.Select(c => mapper.Map<ChangesItemDto>(c)));
                    result.MailItems.AddRange(datas.MailItems.Select(c => mapper.Map<ChangesItemDto>(c)));
                    result.Relationship = datas.GetOrAddSr();
                }
            }
            catch (Exception err)
            {
                result.HasError = true;
                result.DebugMessage = err.Message;
                return BadRequest(err.Message);
            }
            return result;
        }


        /// <summary>
        /// 获取可以或已经pvp的角色的列表。
        /// </summary>
        /// <param name="model"><seealso cref="GetPvpListParamsDto"/></param>
        /// <returns><seealso cref="GetPvpListReturnDto"/>
        /// ErrorCodes.RPC_S_OUT_OF_RESOURCES=1712 钻石不足
        /// ErrorCodes.ERROR_NOT_ENOUGH_QUOTA = 1816 超过刷新次数的上限
        /// </returns>
        /// <response code="401">令牌错误。</response>
        [HttpPost]
        public ActionResult<GetPvpListReturnDto> GetPvpList(GetPvpListParamsDto model)
        {
            using var datas = new GetPvpCharsWorkDatas(_World, model.Token)
            {
                IsRefresh = true,    //只能强制刷新了 model.IsRefresh,
                Now = DateTime.UtcNow,
            };
            if (OwConvert.TryToGuid(model.CharId, out Guid forceCharId))
                datas.CharId = forceCharId;

            GetPvpListReturnDto result = new GetPvpListReturnDto();
            try
            {
                using (var dwUser = datas.LockUser())
                {
                    if (dwUser is null)
                        return Unauthorized("令牌无效");
                    _World.SocialManager.GetPvpChars(datas);
                    result.HasError = datas.HasError;
                    if (datas.HasError) //若有错
                    {
                        result.ErrorCode = datas.ErrorCode;
                        result.DebugMessage = datas.DebugMessage;
                    }
                    else //若无错
                    {
                        var mapper = _World.Service.GetRequiredService<IMapper>();
                        result.ChangesItems.AddRange(datas.ChangeItems.Select(c => mapper.Map<ChangesItemDto>(c)));
                        result.CharIds.AddRange(datas.CharIds.Select(c => c.ToBase64String()));
                    }
                }
                if (!result.HasError)    //若没有错误
                {
                    //增补客户端需要的额外数据
                    var mapper = _World.GetMapper();
                    var summary = datas.World.SocialManager.GetCharSummary(datas.CharIds, datas.UserDbContext);
                    var mp = _World.Service.GetRequiredService<IMapper>();
                    result.Combat = mp.Map<GameCombatDto>(datas.Combat, opt => opt.Items["IgnorDefenerPets"] = true);
                    var sodier = datas.Combat.Others.FirstOrDefault();
                    var summry = summary.FirstOrDefault();
                    if (sodier != null && summary != null)    //矫正数据
                    {
                        var gold = sodier.Resource.FirstOrDefault(c => c.ExtraGuid == ProjectConstant.YumitianTId)?.GetDecimalWithFcpOrDefault("Count") ?? 0;   //金币基数

                        summry.GoldOfStore = gold;
                        summry.WoodOfStore = sodier.Resource.FirstOrDefault(c => c.ExtraGuid == ProjectConstant.MucaishuTId).Count ?? 0;
                    }
                    result.CharSummary.AddRange(summary.Select(c => mapper.Map(c)));
                }
            }
            catch (Exception err)
            {
                result.HasError = true;
                result.DebugMessage = err.Message;
            }
            return result;
        }

        /// <summary>
        /// 请求好友协助进行pvp复仇。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<RequestAssistanceReturnDto> RequestAssistance(RequestAssistanceParamsDto model)
        {
            var result = new RequestAssistanceReturnDto()
            {
            };
            using var datas = new RequestAssistanceDatas(_World, model.Token, OwConvert.ToGuid(model.OtherId))
            {
                UserDbContext = _UserContext,
                RootCombatId = OwConvert.ToGuid(model.CombatId),
            };
            _World.SocialManager.RequestAssistance(datas);
            result.HasError = datas.HasError;
            result.DebugMessage = datas.DebugMessage;
            result.ErrorCode = datas.ErrorCode;
            return result.ErrorCode switch
            {
                ErrorCodes.ERROR_INVALID_TOKEN => Unauthorized(result.DebugMessage),
                ErrorCodes.ERROR_BAD_ARGUMENTS => BadRequest(result.DebugMessage),
                _ => result,
            };
        }

        /// <summary>
        /// 获取指定角色家园数据的接口。OtherCharId 设置为自己的id即可获取自己的数据。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Obsolete("应使用GetItems接口替代。")]
        public ActionResult<GetCharInfoReturnDto> GetCharInfo(GetCharInfoParamsDto model)
        {
            var result = new GetCharInfoReturnDto();
            using var datas = new GetCharInfoDatas(_World, model.Token, OwConvert.ToGuid(model.OtherCharId));
            _World.SocialManager.GetCharInfo(datas);
            result.FillFrom(datas);
            var mapper = _World.Service.GetRequiredService<GameMapperManager>();
            if (!result.HasError)
            {
                result.HomeLand.AddRange(datas.Homeland.Select(c => mapper.Map(c)));
                result.Mounts.AddRange(datas.Mounts.Select(c => mapper.Map(c)));
            }
            return result;
        }

    }

}
