using GuangYuan.GY001.BLL;
using GuangYuan.GY001.BLL.Homeland;
using GuangYuan.GY001.UserDb;
using Gy001.Controllers;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OW.Game;
using OW.Game.Item;
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
            var gc = gu.CurrentChar;
            if (null != gc.DisplayName) //若已经有名字
                return StatusCode((int)HttpStatusCode.PaymentRequired); //TO DO
            gc.DisplayName = model.DisplayName;
            gitm.NotifyChange(gu);
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
                gu.CurrentChar.ClientGutsString = model.ClientString;
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
                    if (!gc.ClientProperties.Remove(model.Name))    //已经移除
                    {
                        result.DebugMessage = $"没有找到要移除的键{model.Name}。";
                        return result;
                    }
                }
                else //若尚不存在
                {
                    gc.ClientProperties[model.Name] = model.Value;
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
        /// 设置出战坐骑列表。过期。
        /// </summary>
        /// <param name="model">GameItemDto 中元素仅需Id有效填写。</param>
        /// <returns>true成功设置，false可能是设置数量超过限制。</returns>
        /// <response code="401">令牌错误。</response>
        [HttpPut]
        [Obsolete]
        public ActionResult<bool> SetCombatMounts(SetCombatMountsParamsDto model)
        {
            var world = HttpContext.RequestServices.GetService<VWorld>();
            if (!world.CharManager.Lock(OwConvert.ToGuid(model.Token), out GameUser gu))
                return base.Unauthorized("令牌无效");
            List<GameItem> lst = null;
            try
            {
                lst = world.ObjectPoolListGameItem.Get();   //获取列表
                var gc = gu.CurrentChar;

                var zuoqiBag = gc.GameItems.First(c => c.TemplateId == ProjectConstant.ZuojiBagSlotId); //背包容器
                var combatSlot = gc.GameItems.First(c => c.TemplateId == ProjectConstant.DangqianZuoqiSlotId);  //出战容器
                if (!world.ItemManager.GetItems(model.GameItemDtos.Select(c => OwConvert.ToGuid(c.Id)), lst, zuoqiBag.Children.Concat(combatSlot.Children)))    //获取所有坐骑对象
                    return false;
                world.ItemManager.MoveItems(combatSlot, c => true, zuoqiBag);   //卸下所有出战坐骑
                world.ItemManager.AddItems(lst, combatSlot);    //装上坐骑
                //TO DO
                world.CharManager.NotifyChange(gu);
            }
            finally
            {
                if (null != lst)
                    world.ObjectPoolListGameItem.Return(lst);
                world.CharManager.Unlock(gu, true);
            }
            return true;
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
            using var datas = new UseItemsWorkDatas(world, model.Token) { UserContext = HttpContext.RequestServices.GetRequiredService<GY001UserContext>() };
            datas.ItemIds.AddRange(model.Items.Select(c => (OwConvert.ToGuid(c.Id), c.Count)));
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
                var allGi = gim.GetAllChildrenDictionary(gc);
                var (Id, Count, PId) = coll.FirstOrDefault(c => !allGi.ContainsKey(c.Id) || !allGi.ContainsKey(c.PId));
                if (Id != Guid.Empty)
                {
                    result.HasError = true;
                    result.DebugMessage = $"至少有一个物品没有找到。Number={Id},PId={PId}";
                    return result;
                }
                try
                {
                    List<ChangeItem> changesItems = new List<ChangeItem>();
                    foreach (var item in coll)
                    {
                        world.ItemManager.MoveItem(allGi[item.Id], item.Count, allGi[item.PId], changesItems);
                    }
                    ChangeItem.Reduce(changesItems);
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
        /// <param name="model">要增加的物品对象数组，需要设置Count,TemplateId,ParentId属性。Properties属性中的键值可以设置会原样超入</param>
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
                List<GameItem> lst = new List<GameItem>();
                foreach (var item in model.Items)
                {
                    GameItem gi = (GameItem)item;
                    if (gim.IsMounts(gi))  //若要创建坐骑
                    {
                        var mounts = gim.CreateMounts(gi, ProjectConstant.ZuojiZuheRongqi);
                        mounts.ParentId = gi.ParentId;
                        lst.Add(mounts);
                    }
                    else
                    {
                        var tmp = new GameItem();
                        world.EventsManager.GameItemCreated(tmp, gi.TemplateId, null, null);
                        tmp.Count = gi.Count;
                        lst.Add(tmp);
                    }
                }
                var dic = OwHelper.GetAllSubItemsOfTree(gc.GameItems, c => c.Children).ToDictionary(c => c.Id);
                List<ChangeItem> changes = new List<ChangeItem>();
                foreach (var item in lst)   //加入
                {
                    if (item.ParentId == gc.Id)
                        gim.AddItems(new GameItem[] { item }, gc, null, changes);
                    else
                    {
                        var parent = gim.GetDefaultContainer(gc, item);
                        gim.AddItems(new GameItem[] { item }, parent, null, changes);
                    }

                }
                var coll = changes.SelectMany(c => c.Adds.Concat(c.Changes)).Distinct();
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
            var world = HttpContext.RequestServices.GetRequiredService<VWorld>();
            if (!world.CharManager.Lock(OwConvert.ToGuid(model.Token), out GameUser gu))
                return base.Unauthorized("令牌无效");
            try
            {
                var gc = gu.CurrentChar;
                var result = new GetItemsReturnDto();
                HashSet<Guid> guids = new HashSet<Guid>(model.Ids.Select(c => OwConvert.ToGuid(c)));
                var list = gc.AllChildren.Where(c => guids.Contains(c.Id)).ToList();
                list.ForEach(c => c.FcpToProperties());
                if (model.IncludeChildren)
                    foreach (var item in OwHelper.GetAllSubItemsOfTree(list, c => c.Children).ToArray())
                        item.FcpToProperties();

                var coll = list.Select(c => GameItemDto.FromGameItem(c, model.IncludeChildren));
                result.GameItems.AddRange(coll);
                if (model.Ids.Contains(gc.Id.ToBase64String())) //若需要获取角色信息
                {
                    var gcDto = new GameCharDto()
                    {
                        Id = gc.Id.ToBase64String(),
                        ClientGutsString = gc.ClientGutsString,
                        CreateUtc = gc.CreateUtc,
                        DisplayName = gc.DisplayName,
                        GameUserId = gc.GameUserId.ToBase64String(),
                        TemplateId = gc.TemplateId.ToBase64String(),
                        CurrentDungeonId = gc.CurrentDungeonId?.ToBase64String(),
                        CombatStartUtc = gc.CombatStartUtc,
                    };
                    OwHelper.Copy(gc.Properties, gcDto.Properties);
                    result.GameChar = gcDto;
                }


                return result;
            }
            finally
            {
                world.CharManager.Unlock(gu, true);
            }
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
                result.Changes.AddRange(gc.ChangesItems.Select(c => (ChangesItemDto)c));
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
            gc.ChangesItems.Clear();
            return Ok();
        }

        /// <summary>
        /// 获取家园方案。
        /// 此接口不重置下线计时器。
        /// </summary>
        /// <param name="model"></param>
        /// <returns>建立账号后第一次返回的所有方案中，除了Id是有效的其他属性是空或空集合。</returns>
        /// <response code="401">令牌错误。</response>
        [HttpPut]
        public ActionResult<GetHomelandFenggeReturnDto> GetHomelandStyle(GetHomelandFenggeParamsDto model)
        {
            var result = new GetHomelandFenggeReturnDto() { };
            var world = HttpContext.RequestServices.GetRequiredService<VWorld>();
            if (!world.CharManager.Lock(OwConvert.ToGuid(model.Token), out GameUser gu))
                return base.Unauthorized("令牌无效");
            Guid[] filterTIds = new Guid[] { ProjectConstant.WorkerOfHomelandTId, ProjectConstant.HomelandPlanBagTId, ProjectConstant.HomelandBuilderBagTId };
            string[] ary = null;
            try
            {
                var gitm = world.ItemTemplateManager;
                var gc = gu.CurrentChar;
                var fengges = gc.GetFengges();
                //if (fengges.Count == 0) //若未初始化
                gc.MergeFangans(fengges, gitm);
                result.Plans.AddRange(fengges.Select(c => (HomelandFenggeDto)c));
                ary = gc.GetHomeland().AllChildren.Where(c => filterTIds.Contains(c.TemplateId)).Select(c => c.Id.ToBase64String()).ToArray(); //排除的容器Id集合
            }
            catch (Exception err)
            {
                result.DebugMessage = err.Message + err.StackTrace;
                result.HasError = true;
            }
            finally
            {
                world.CharManager.Unlock(gu, true);
            }
            if (null != ary)
                foreach (var fengge in result.Plans)
                {
                    for (int i = fengge.Fangans.Count - 1; i >= 0; i--)
                    {
                        var fangan = fengge.Fangans[i];
                        for (int j = fangan.FanganItems.Count - 1; j >= 0; j--)
                        {
                            if (ary.Contains(fangan.FanganItems[j].ContainerId)) //若需要删除
                                fangan.FanganItems.RemoveAt(j);
                        }
                    }
                }
            return result;
        }

        /// <summary>
        /// 设置家园方案。
        /// </summary>
        /// <param name="model"></param>
        /// <returns>如果有错大概率是不认识的Id。</returns>
        /// <response code="401">令牌错误。</response>
        [HttpPost]
        public ActionResult<SetHomelandFenggeReturnDto> SetHomelandStyle(SetHomelandFenggeParamsDto model)
        {
            var result = new SetHomelandFenggeReturnDto() { };
            var world = HttpContext.RequestServices.GetRequiredService<VWorld>();
            using var dwUser = world.CharManager.LockAndReturnDisposer(model.Token, out var gu);
            if (dwUser is null)
                return Unauthorized(VWorld.GetLastErrorMessage());
            try
            {
                var gc = gu.CurrentChar;
                var gitm = world.ItemTemplateManager;
                var fengges = model.Fengges.Select(c => (HomelandFengge)c).ToArray();
                world.CharManager.SetHomelandPlans(fengges, gc);
            }
            catch (Exception err)
            {
                result.HasError = true;
                result.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                result.DebugMessage = err.Message;
            }
            return result;
        }

        /// <summary>
        /// 应用指定方案。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<ApplyHomelandStyleReturnDto> ApplyHomelandStyle(ApplyHomelandStyleParamsDto model)
        {
            var result = new ApplyHomelandStyleReturnDto();
            var world = HttpContext.RequestServices.GetRequiredService<VWorld>();
            if (!world.CharManager.Lock(OwConvert.ToGuid(model.Token), out GameUser gu))
                return base.Unauthorized("令牌无效");
            try
            {
                var gc = gu.CurrentChar;
                var gitm = world.ItemTemplateManager;
                var lstFengge = gc.GetFengges();
                var id = OwConvert.ToGuid(model.FanganId);
                var fangan = lstFengge.SelectMany(c => c.Fangans).FirstOrDefault(c => c.Id == id);
                var gim = world.ItemManager;
                var datas = new ActiveStyleDatas()
                {
                    Fangan = fangan,
                    GameChar = gc,
                };
                fangan.IsActived = true;
                gim.ActiveStyle(datas);
                foreach (var item in lstFengge.SelectMany(c => c.Fangans))
                    item.IsActived = false;
                fangan.IsActived = true;
                result.DebugMessage = datas.Message;
                result.HasError = datas.HasError;
            }
            catch (Exception err)
            {
                result.DebugMessage = err.Message;
                result.HasError = true;
            }
            finally
            {
                world.CharManager.Unlock(gu, true);
            }
            return result;
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
        /// <returns></returns>
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
    }

}

