using Game.Social;
using GuangYuan.GY001.BLL;
using GuangYuan.GY001.BLL.Homeland;
using GuangYuan.GY001.UserDb;
using Gy001.Controllers;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OW.Extensions.Game.Store;
using OW.Game;
using OW.Game.Item;
using OW.Game.PropertyChange;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace GY2021001WebApi.Controllers
{
    /// <summary>
    /// 角色相关操作。
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class GameCharController : GameBaseController
    {
        public GameCharController(VWorld world) : base(world)
        {
        }

        /// <summary>
        /// 按指定的登录名获取其对应的角色Id。
        /// </summary>
        /// <param name="model"></param>
        /// <returns>角色id的顺序与指定登录名顺序相同。</returns>
        [HttpPost]
        public ActionResult<GetCharIdsFromLoginNamesReturnDto> GetCharIdsFromLoginNames(GetCharIdsFromLoginNamesParamsDto model)
        {
            var result = new GetCharIdsFromLoginNamesReturnDto();
            using var db = World.CreateNewUserDbContext();
            var coll = from gc in db.GameChars.AsNoTracking()
                       where model.LoginNames.Contains(gc.GameUser.LoginName)
                       select gc.Id;
            result.CharIds.AddRange(coll.AsEnumerable().Select(c => c.ToBase64String()));
            return result;
        }

        /// <summary>
        /// 修改对象属性接口。可以用此接口修改家园相关物品的属性。
        /// 如果包含无效对象id -或和- 不可更改属性，则忽略，不会报错。
        /// 特别地，对于家园地块旗帜可以更改其模板Id（tid)。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<ModifyPropertiesReturnDto> ModifyProperties(ModifyPropertiesParamsDto model)
        {
            var result = new ModifyPropertiesReturnDto();
            using var datas = new ModifyPropertiesDatas(World, model.Token);
            datas.Modifies.AddRange(model.Items.Select(c => (OwConvert.ToGuid(c.ObjectId), c.PropertyName, c.Value as object)));
            World.CharManager.ModifyProperties(datas);
            result.FillFrom(datas);
            return result;
        }

        /// <summary>
        /// 更改角色的名字。当前只能更改一次。
        /// </summary>
        /// <param name="model">参数。</param>
        /// <returns>true成功更改。false有重名。</returns>
        /// <response code="401">令牌错误。</response>
        /// <response code="402">没有必要的道具。</response>
        [HttpPost]
        public ActionResult<bool> Rename(RenameParamsDto model)
        {
            var gitm = HttpContext.RequestServices.GetRequiredService<GameCharManager>();
            var gu = gitm.GetUserFromToken(OwConvert.ToGuid(model.Token));
            if (null == gu) //若令牌无效
                return Unauthorized();
            var displayName = model.DisplayName;
            using var dw = World.LockStringAndReturnDisposer(ref displayName, TimeSpan.FromSeconds(2));
            using var dwUser = World.CharManager.LockAndReturnDisposer(gu);
            if (dw is null || dwUser is null)
                return false;
            var gc = gu.CurrentChar;
            //TODO 改名需要消耗道具
            //if (null != gc.DisplayName) //若已经有名字
            //    return StatusCode((int)HttpStatusCode.PaymentRequired);
            if (gu.DbContext.Set<GameChar>().Where(c => c.DisplayName == displayName && gc.Id != c.Id).Count() > 0) //若重名
                return false;
            gc.DisplayName = displayName;
            gu.DbContext.SaveChanges();
            World.CharManager.Nope(gu);
            return true;
        }

        /// <summary>
        /// 修改客户端字符串。
        /// </summary>
        /// <param name="model">参见 ModifyClentStringParamsDto。</param>
        /// <returns></returns>
        /// <response code="401">令牌错误。</response>
        [HttpPut]
        public ActionResult<ModifyClentReturnDto> ModifyClentString(ModifyClentStringParamsDto model)
        {
            var result = new ModifyClentReturnDto()
            {
                Result = model.ClientString,
                ObjectId = model.ObjectId,
            };
            var gitm = HttpContext.RequestServices.GetRequiredService<GameCharManager>();
            var gu = gitm.GetUserFromToken(OwConvert.ToGuid(model.Token));
            if (null == gu) //若令牌无效
                return Unauthorized();
            var objectId = OwConvert.ToGuid(model.ObjectId);
            if (gu.CurrentChar.Id == objectId)
            {
                gu.CurrentChar.SetClientString(model.ClientString);
                gitm.NotifyChange(gu);
            }
            else
            {
                var succ = gitm.ModifyClientString(gu.CurrentChar, objectId, model.ClientString);
                if (!succ)
                    result.HasError = true;
            }
            return result;
        }

        /// <summary>
        /// 设置客户端扩展属性。
        /// </summary>
        /// <param name="model">参见 ModifyClientExtendPropertyParamsDto </param>
        /// <returns>true成功设置,false,指定了删除属性标志，但没有找到指定名子的属性。</returns>
        /// <response code="401">令牌错误。</response>
        [HttpPut]
        public ActionResult<ModifyClientExtendPropertyReturn> ModifyClientExtendProperty(ModifyClientExtendPropertyParamsDto model)
        {
            var world = HttpContext.RequestServices.GetRequiredService<VWorld>();
            if (!world.CharManager.Lock(OwConvert.ToGuid(model.Token), out GameUser gu))
            {
                return base.Unauthorized("无效令牌。");
            }
            var result = new ModifyClientExtendPropertyReturn()
            {
                Name = model.Name,
                Value = model.Value,
            };
            try
            {
                var gc = gu.CurrentChar;
                if (model.IsRemove)
                {
                    if (!gc.GetOrCreateBinaryObject<CharBinaryExProperties>().ClientProperties.Remove(model.Name))    //已经移除
                    {
                        result.DebugMessage = $"没有找到要移除的键{model.Name}。";
                        return result;
                    }
                }
                else //若尚不存在
                {
                    gc.GetOrCreateBinaryObject<CharBinaryExProperties>().ClientProperties[model.Name] = model.Value;
                }
                world.CharManager.Nope(gu);
            }
            finally
            {
                world.CharManager.Unlock(gu);
            }
            return result;
        }

        /// <summary>
        /// 出售物品。
        /// 原子操作——指定物品要么全卖出，要么全没卖出。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="401">参数错误。详情参见说明字符串。</response>
        [HttpPost]
        public ActionResult<SellReturnDto> Sell(SellParamsDto model)
        {
            var world = HttpContext.RequestServices.GetRequiredService<VWorld>();   //获取虚拟世界的根服务
            //构造调用参数
            var datas = new SellDatas(world, model.Token);
            datas.SellIds.AddRange(model.Ids.Select(c => (OwConvert.ToGuid(c.Id), c.Count)));
            world.ItemManager.Sell(datas);  //调用服务
            //构造返回参数
            var result = new SellReturnDto()
            {
                HasError = datas.HasError,
                DebugMessage = datas.ErrorMessage,
            };
            if (!result.HasError)
                result.ChangesItems.AddRange(datas.ChangeItems.Select(c => (ChangesItemDto)c));
            return result;
        }

        /// <summary>
        /// 使用道具。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="401">参数错误。详情参见说明字符串。</response>
        [HttpPost]
        public ActionResult<UseItemsReturnDto> UseItems(UseItemsParamsDto model)
        {
            var result = new UseItemsReturnDto();
            var world = HttpContext.RequestServices.GetRequiredService<VWorld>();
            using var datas = new UseItemsWorkDatas(world, model.Token) { UserDbContext = HttpContext.RequestServices.GetRequiredService<GY001UserContext>() };
            datas.ItemId = OwConvert.ToGuid(model.Item.Id);
            datas.Count = (int)model.Item.Count;
            try
            {
                world.ItemManager.UseItems(datas);
                if (datas.HasError && datas.ErrorCode == 401)
                    return Unauthorized("令牌无效。");
                else
                {
                    result.HasError = datas.HasError;
                    result.DebugMessage = datas.ErrorMessage;
                    if (!result.HasError)
                    {
                        result.ChangesItems.AddRange(datas.ChangeItems.Select(c => (ChangesItemDto)c));
                        if (datas.Remainder.Count > 0) //若有剩余物品
                        {
                            var mail = new GameMail();
                            world.SocialManager.SendMail(mail, new Guid[] { datas.GameChar.Id }, SocialConstant.FromSystemId,
                                datas.Remainder.Select(c => (c, world.EventsManager.GetDefaultContainer(c, datas.GameChar).ExtraGuid)));
                        }
                    }
                    result.SuccCount = datas.SuccCount;
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
        /// 设置阵容。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="401">令牌错误。</response>
        [HttpPost]
        public ActionResult<SetLineupReturnDto> SetSetLineup(SetLineupParamsDto model)
        {
            SetLineupDatas datas = new SetLineupDatas(HttpContext.RequestServices.GetRequiredService<VWorld>(), model.Token)
            {
            };
            datas.Settings.AddRange(model.Settings.Select(c => (OwConvert.ToGuid(c.Id), c.ForIndex, (decimal)c.Position)));
            SetLineupReturnDto result = new SetLineupReturnDto();
            using var disposer = datas.LockUser();
            if (disposer is null)
                return StatusCode(datas.ErrorCode, datas.ErrorMessage);
            try
            {
                var gim = HttpContext.RequestServices.GetRequiredService<GameItemManager>();
                gim.SetLineup(datas);
                result.HasError = datas.HasError;
                result.DebugMessage = datas.ErrorMessage;
                if (!result.HasError)
                {
                    result.ChangesItems.AddRange(datas.ChangeItems.Select(c => (ChangesItemDto)c));
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
        /// 移动物品接口。如将锁定合成锁定石头放入特定槽。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="401">令牌错误。</response>
        [HttpPost]
        public ActionResult<MoveItemsReturnDto> MoveItems(MoveItemsParamsDto model)
        {
            var result = new MoveItemsReturnDto();
            var world = HttpContext.RequestServices.GetRequiredService<VWorld>();
            var gim = world.ItemManager;
            if (!world.CharManager.Lock(OwConvert.ToGuid(model.Token), out GameUser gu))
                return base.Unauthorized("令牌无效");
            try
            {
                if (model.Items.Count == 0)
                {
                    result.DebugMessage = "空的参数";
                    return result;
                }
                var gc = gu.CurrentChar;
                var lst = new List<GameItem>();
                var coll = from tmp in model.Items
                           select (Id: OwConvert.ToGuid(tmp.ItemId), tmp.Count, PId: OwConvert.ToGuid(tmp.DestContainerId));
                var allGi = gc.AllChildren.ToDictionary(c => c.Id);
                var (Id, Count, PId) = coll.FirstOrDefault(c => !allGi.ContainsKey(c.Id) || !allGi.ContainsKey(c.PId));
                if (Id != Guid.Empty)
                {
                    result.HasError = true;
                    result.DebugMessage = $"至少有一个物品没有找到。Number={Id},PId={PId}";
                    return result;
                }
                try
                {
                    List<GamePropertyChangeItem<object>> changes = new List<GamePropertyChangeItem<object>>();
                    List<ChangeItem> changesItems = new List<ChangeItem>();
                    foreach (var item in coll)
                    {
                        world.ItemManager.MoveItem(allGi[item.Id], item.Count, allGi[item.PId], null, changes);
                    }
                    changes.CopyTo(changesItems);
                    result.ChangesItems.AddRange(changesItems.Select(c => (ChangesItemDto)c));
                }
                catch (Exception)
                {
                    throw;
                }
                world.CharManager.NotifyChange(gu);
            }
            finally
            {
                world.CharManager.Unlock(gu, true);
            }

            return result;
        }

        /// <summary>
        /// 给角色强行增加物品。调试用接口，正式版本将删除。
        /// </summary>
        /// <param name="model">要增加的物品对象数组，需要设置Count,ExtraGuid,ParentId属性。Properties属性中的键值可以设置会原样超入</param>
        /// <returns></returns>
        /// <response code="401">令牌错误。</response>
        [HttpPost]
        public ActionResult<List<GameItemDto>> AddItems(AddItemsParamsDto model)
        {
            var result = new List<GameItemDto>();
            var world = HttpContext.RequestServices.GetRequiredService<VWorld>();

            if (!world.CharManager.Lock(OwConvert.ToGuid(model.Token), out GameUser gu))
                return base.Unauthorized("令牌无效");
            try
            {
                var gc = gu.CurrentChar;
                var gim = world.ItemManager;
                List<(GameItem, GameThingBase)> lst = new List<(GameItem, GameThingBase)>();
                foreach (var item in model.Items)
                {
                    GameItem gi = (GameItem)item;
                    if (gim.IsMounts(gi))  //若要创建坐骑
                    {
                        var mounts = gim.CreateMounts(gi, ProjectConstant.ZuojiZuheRongqi);
                        mounts.ParentId = gi.ParentId;
                        lst.Add((mounts, gc.AllChildren.FirstOrDefault(c => c.Id == OwConvert.ToGuid(item.ParentId))));
                    }
                    else
                    {
                        var tmp = new GameItem();
                        world.EventsManager.GameItemCreated(tmp, gi.ExtraGuid, null, null);
                        tmp.Count = gi.Count;
                        lst.Add((tmp, gc.AllChildren.FirstOrDefault(c => c.Id == OwConvert.ToGuid(item.ParentId))));
                    }
                }
                var dic = OwHelper.GetAllSubItemsOfTree(gc.GameItems, c => c.Children).ToDictionary(c => c.Id);
                List<GamePropertyChangeItem<object>> changes = new List<GamePropertyChangeItem<object>>();
                foreach (var item in lst)   //加入
                {
                    if (item.Item1.ParentId == gc.Id)
                        gim.MoveItem(item.Item1, item.Item1.Count ?? 1, gc, null, changes);
                    else
                    {
                        var parent = item.Item2;
                        gim.MoveItem(item.Item1, item.Item1.Count ?? 1, parent, null, changes);
                    }

                }
                List<ChangeItem> list = new List<ChangeItem>();
                changes.CopyTo(list);
                var coll = list.SelectMany(c => c.Adds.Concat(c.Changes)).Distinct();
                result.AddRange(coll.Select(c => (GameItemDto)c));
                world.CharManager.NotifyChange(gu);
            }
            finally
            {
                world.CharManager.Unlock(gu, true);
            }
            return result;
        }

        /// <summary>
        /// 获取物品的信息。
        /// 这个调用不会触发空闲重新计时，即使一直调用此方法用户也可能被登出。
        /// </summary>
        /// <param name="model">参见 GetItemsParamsDto.</param>
        /// <returns>参见 GetItemsReturnDto.</returns>
        /// <response code="401">令牌错误。</response>
        [HttpPut]
        public ActionResult<GetItemsReturnDto> GetItems(GetItemsParamsDto model)
        {
            //TODO 扩展功能。
            using var datas = new GetItemsContext(World, model.Token)
            {
                CharId = model.CharId is null ? null as Guid? : OwConvert.ToGuid(model.CharId),
                IncludeChildren = model.IncludeChildren,
            };
            OwHelper.Copy(model.Ids.Select(c => OwConvert.ToGuid(c)), datas.Ids);
            IDisposable disposable;
            if (datas.CharId is null)    //若取自身对象
            {
                disposable = datas.LockUser();
            }
            else //若取指定角色对象
            {
                disposable = World.CharManager.LockOrLoad(datas.CharId.Value, out _);
            }
            var result = new GetItemsReturnDto();
            if (disposable is null)
            {
                result.FillFromWorld();
                return result;
            }
            using var dw = disposable;

            World.CharManager.GetItems(datas);

            result.FillFrom(datas);
            if (!result.HasError)
            {
                if (null != datas.ResultGameChar)
                {
                    var gc = datas.ResultGameChar;
                    var gcDto = new GameCharDto()
                    {
                        Id = gc.Id.ToBase64String(),
                        ClientGutsString = gc.GetClientString(),
                        CreateUtc = gc.CreateUtc,
                        DisplayName = gc.DisplayName,
                        GameUserId = gc.GameUserId.ToBase64String(),
                        TemplateId = gc.ExtraGuid.ToBase64String(),
                        CurrentDungeonId = gc.CurrentDungeonId?.ToBase64String(),
                        CombatStartUtc = gc.CombatStartUtc,
                    };
                    OwHelper.Copy(gc.Properties, gcDto.Properties);
                    result.GameChar = gcDto;
                }
                datas.GameItems.ForEach(c => c.FcpToProperties());
                if (model.IncludeChildren)
                    foreach (var item in OwHelper.GetAllSubItemsOfTree(datas.GameItems, c => c.Children).ToArray())
                        item.FcpToProperties();

                var coll = datas.GameItems.Select(c => GameItemDto.FromGameItem(c, model.IncludeChildren));
                result.GameItems.AddRange(coll);

            }
            return result;
        }

        /// <summary>
        /// 获取自动变化的数据集合。
        /// </summary>
        /// <param name="model"></param>
        /// <returns>参见 GetChangesItemReturnDto 说明。</returns>
        /// <response code="401">令牌错误。</response>
        [HttpPost]
        public ActionResult<GetChangesItemReturnDto> GetChangesItem(GetChangesItemParamsDto model)
        {
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<GameCharController>>();
            var result = new GetChangesItemReturnDto();
            var world = HttpContext.RequestServices.GetRequiredService<VWorld>();
            using var dwUser = world.CharManager.LockAndReturnDisposer(OwConvert.ToGuid(model.Token), out GameUser gu);
            if (dwUser is null)
            {
                logger.LogWarning("[{dt}]{method}锁定失败。", DateTime.UtcNow, nameof(GetChangesItem));
                return Unauthorized("令牌无效");
            }
            try
            {
                var gc = gu.CurrentChar;
#if DEBUG
                logger.LogDebug($"[{DateTime.UtcNow}] Call GetChangesItem");
#endif //DEBUG
                var collTmp = ChangesItemSummary.ToChangesItem(gc.GetOrCreateBinaryObject<CharBinaryExProperties>().ChangeItems, gc);
                result.Changes.AddRange(collTmp.Select(c => (ChangesItemDto)c));
                //gc.ChangesItems.Clear();
            }
            catch (Exception err)
            {
                result.DebugMessage = err.ToString();
                result.HasError = true;
            }
            return result;
        }

        /// <summary>
        /// 清除自动变化的数据集合中所有数据。
        /// </summary>
        /// <returns></returns>
        /// <response code="401">令牌错误。</response>
        [HttpPost]
        public ActionResult ClearChangesItem(TokenDtoBase model)
        {
            var world = HttpContext.RequestServices.GetRequiredService<VWorld>();
            using var dwUser = world.CharManager.LockAndReturnDisposer(model.Token, out var gu);
            if (dwUser is null)
            {
                return Unauthorized(VWorld.GetLastErrorMessage());
            }
            var gc = gu.CurrentChar;
            gc.GetOrCreateBinaryObject<CharBinaryExProperties>().ChangeItems.Clear();
            return Ok();
        }

        /// <summary>
        /// 获取推关战力排名，
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut]
        public ActionResult<GetRankOfTuiguanForMeReturnDto> GetRankOfTuiguanForMe(GetRankOfTuiguanForMeParamsDto model)
        {
            var world = HttpContext.RequestServices.GetRequiredService<VWorld>();
            var datas = new GetRankOfTuiguanDatas(world, model.Token);
            {
            };
            world.ItemManager.GetRankOfTuiguan(datas);
            var result = new GetRankOfTuiguanForMeReturnDto
            {
                HasError = datas.HasError,
                ErrorCode = datas.ErrorCode,
                DebugMessage = datas.ErrorMessage
            };
            if (!datas.HasError)
            {
                result.Rank = datas.Rank;
                result.Scope = datas.Scope;
                result.Prv.AddRange(datas.Prv.Select(c => (RankDataItemDto)c).OrderByDescending(c => c.Metrics).ThenBy(c => c.DisplayName));
                for (int i = 0; i < result.Prv.Count; i++)  //设置排名号
                {
                    result.Prv[i].OrderNumber = i + result.Rank - result.Prv.Count;
                }
                result.Next.AddRange(datas.Next.Select(c => (RankDataItemDto)c).OrderByDescending(c => c.Metrics).ThenBy(c => c.DisplayName));
                for (int i = 0; i < result.Next.Count; i++) //设置排名号
                {
                    result.Next[i].OrderNumber = i + result.Rank + 1;
                }
            }
            return result;
        }

        /// <summary>
        /// 获取当前角色的变化数据。获取后可以使用ClearChangeData接口清理。
        /// </summary>
        /// <param name="model"></param>
        /// <returns>返回值ChangeDatas集合中莫格元素的 PropertyName 属性是8de0e03b-d138-43d3-8cce-e519c9da3065 表示指定对象发生了多处变化，需要全部刷新。</returns>
        /// <response code="401">令牌错误。</response>
        [HttpPut]
        public ActionResult<GetChangeDataResultDto> GetChangeData(GetChangeDataParamsDto model)
        {
            var result = new GetChangeDataResultDto();
            var world = HttpContext.RequestServices.GetRequiredService<VWorld>();
            using var dwUser = world.CharManager.LockAndReturnDisposer(model.Token, out var gu);
            if (dwUser is null)
                return Unauthorized("令牌错误");
            var data = world.CharManager.GetChangeData(gu.CurrentChar);
            if (data is null)
            {
                result.HasError = true;
                result.ErrorCode = VWorld.GetLastError();
            }
            else
                result.ChangeDatas.AddRange(data.Select(c => (ChangeDataDto)c));
            return result;
        }

        /// <summary>
        /// 清除所有变化通知数据。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="401">令牌错误。</response>
        [HttpDelete]
        public ActionResult<ClearChangeDataResult> ClearChangeData(ClearChangeDataParamsDto model)
        {
            var result = new ClearChangeDataResult();
            var world = HttpContext.RequestServices.GetRequiredService<VWorld>();
            using var dwUser = world.CharManager.LockAndReturnDisposer(model.Token, out var gu);
            if (dwUser is null)
                return Unauthorized("令牌错误");
            var data = world.CharManager.GetChangeData(gu.CurrentChar);
            if (data is null)
            {
                result.HasError = true;
                result.ErrorCode = VWorld.GetLastError();
            }
            else
                data.Clear();
            return result;
        }

        /// <summary>
        /// 解锁家园风格。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<AddHomelandStyleReturnDto> AddHomelandStyle(AddHomelandStyleParamsDto model)
        {
            var result = new AddHomelandStyleReturnDto();
            return result;
        }

    }

}

