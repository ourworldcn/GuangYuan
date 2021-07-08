﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GY2021001BLL;
using GY2021001DAL;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using OwGame;

namespace GY2021001WebApi.Controllers
{
    /// <summary>
    /// 角色相关操作。
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class GameCharController : ControllerBase
    {
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
            var gu = gitm.GetUserFromToken(GameHelper.FromBase64String(model.Token));
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
            var gu = gitm.GetUserFromToken(GameHelper.FromBase64String(model.Token));
            if (null == gu) //若令牌无效
                return Unauthorized();
            var objectId = GameHelper.FromBase64String(model.ObjectId);
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
        public ActionResult<bool> ModifyClientExtendProperty(ModifyClientExtendPropertyParamsDto model)
        {
            var world = HttpContext.RequestServices.GetRequiredService<VWorld>();
            if (!world.CharManager.Lock(GameHelper.FromBase64String(model.Token), out GameUser gu))
            {
                return Unauthorized("无效令牌。");
            }
            try
            {
                var gc = gu.CurrentChar;
                if (model.IsRemove)
                {
                    if (!gc.ClientExtendProperties.Remove(model.Name, out GameClientExtendProperty val))
                        return false;
                    gu.DbContext.Set<GameClientExtendProperty>().Remove(val);
                }
                else if (gc.ClientExtendProperties.TryGetValue(model.Name, out GameClientExtendProperty gep))
                    gep.Value = model.Value;
                else
                {
                    gep = new GameClientExtendProperty()
                    {
                        Name = model.Name,
                        Value = model.Value,
                        ParentId = gc.Id,
                    };
                    gu.DbContext.Set<GameClientExtendProperty>().Add(gep);
                    gc.ClientExtendProperties[gep.Name] = gep;
                }
                world.CharManager.Nope(gu);
            }
            finally
            {
                world.CharManager.Unlock(gu);
            }
            return true;
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
            if (!world.CharManager.Lock(GameHelper.FromBase64String(model.Token), out GameUser gu))
                return Unauthorized("令牌无效");
            List<GameItem> lst = null;
            try
            {
                lst = world.ObjectPoolListGameItem.Get();   //获取列表
                var gc = gu.CurrentChar;

                var zuoqiBag = gc.GameItems.First(c => c.TemplateId == ProjectConstant.ZuojiBagSlotId); //背包容器
                var combatSlot = gc.GameItems.First(c => c.TemplateId == ProjectConstant.DangqianZuoqiSlotId);  //出战容器
                if (!world.ItemManager.GetItems(model.GameItemDtos.Select(c => GameHelper.FromBase64String(c.Id)), lst, zuoqiBag.Children.Concat(combatSlot.Children)))    //获取所有坐骑对象
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
        /// </summary>
        [HttpPost]
        public ActionResult<SellReturnDto> Sell(SellParamsDto model)
        {
            var world = HttpContext.RequestServices.GetRequiredService<VWorld>();
            var result = new SellReturnDto();
            if (0 == model.Ids.Count)
                return result;
            List<GameItem> removes = null;
            if (!world.CharManager.Lock(GameHelper.FromBase64String(model.Token), out GameUser gu))
                return Unauthorized("令牌无效");
            try
            {
                var gc = gu.CurrentChar;
                var shoulan = gc.GameItems.First(c => c.TemplateId == ProjectConstant.ShoulanSlotId); //兽栏
                HashSet<Guid> ids = new HashSet<Guid>(shoulan.Children.Select(c => c.Id));  //所有兽栏动物Id
                var sellIds = new HashSet<Guid>(model.Ids.Select(c => GameHelper.FromBase64String(c)));    //要卖的物品Id
                var errItem = sellIds.FirstOrDefault(c => !ids.Contains(c));
                if (Guid.Empty != errItem)   //若找不到某个Id
                {
                    result.HasError = true;
                    result.DebugMessage = $"至少有一个对象无法找到，Id={errItem}";
                }
                else
                {
                    removes = world.ObjectPoolListGameItem.Get();
                    var golden = gc.GameItems.First(c => c.TemplateId == ProjectConstant.JinbiId);  //金币
                    world.ItemManager.RemoveItemsWhere(shoulan, c => sellIds.Contains(c.Id), removes);  //移除所有野兽
                    foreach (var item in removes)   //计算出售所得金币
                    {
                        var totalNe = Convert.ToDecimal(item.Properties.GetValueOrDefault("neatk", 0m)) +   //总资质值
                         Convert.ToDecimal(item.Properties.GetValueOrDefault("nemhp", 0m)) +
                         Convert.ToDecimal(item.Properties.GetValueOrDefault("neqlt", 0m));
                        totalNe = Math.Round(totalNe);  //取整，容错
                        decimal mul;

                        if (totalNe >= 0 && totalNe <= 60) mul = 1;
                        else if (totalNe >= 61 && totalNe <= 120) mul = 1.5m;
                        else if (totalNe >= 121 && totalNe <= 180) mul = 2;
                        else if (totalNe >= 181 && totalNe <= 240) mul = 3;
                        else if (totalNe >= 241 && totalNe <= 300) mul = 4;
                        else throw new InvalidOperationException("资质总和过大。");
                        golden.Count += mul * totalNe;
                    }
                    //移除的对象
                    var chn = new ChangesItemDto()
                    {
                        ContainerId = shoulan.Id.ToBase64String(),
                    };
                    chn.Removes.AddRange(removes.Select(c => c.Id.ToBase64String()));
                    result.ChangesItems.Add(chn);
                    //变化的对象
                    chn = new ChangesItemDto()
                    {
                        ContainerId = gc.Id.ToBase64String(),
                    };
                    chn.Changes.Add((GameItemDto)golden);
                    result.ChangesItems.Add(chn);
                }
            }
            finally
            {
                if (null != removes)
                    world.ObjectPoolListGameItem.Return(removes);
                world.CharManager.Unlock(gu, true);
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
            SetLineupReturnDto result = new SetLineupReturnDto();
            var world = HttpContext.RequestServices.GetRequiredService<VWorld>();
            if (!world.CharManager.Lock(GameHelper.FromBase64String(model.Token), out GameUser gu))
                return Unauthorized("令牌无效");
            try
            {
                var gc = gu.CurrentChar;
                var gim = world.ItemManager;
                var allDic = gim.GetAllChildrenDictionary(gc);
                var coll = from tmp in model.Settings
                           select new { Id = tmp.Id, GItem = allDic.GetValueOrDefault(GameHelper.FromBase64String(tmp.Id), null), tmp.Position, Index = tmp.ForIndex };
                var tmpGi = coll.FirstOrDefault(c => c.GItem == null);
                if (tmpGi != null)  //若有无效Id
                {
                    result.HasError = true;
                    result.DebugMessage = $"至少有一个坐骑Id无效:Id={tmpGi.Id}";
                }
                else
                {
                    var slot = gc.GameItems.First(c => c.TemplateId == ProjectConstant.ZuojiBagSlotId);
                    var ci = new ChangesItem() { ContainerId = slot.Id };
                    foreach (var item in coll)
                    {
                        if (item.Position == -1)    //若去除该阵营出阵位置编号
                        {
                            item.GItem.Properties.Remove($"{ProjectConstant.ZhenrongPropertyName}{item.Index}");
                        }
                        else //设置出阵
                        {
                            item.GItem.Properties[$"{ProjectConstant.ZhenrongPropertyName}{item.Index}"] = (decimal)item.Position;
                        }
                        ci.Changes.Add(item.GItem);
                    }
                    var changes = new List<ChangesItem>();
                    changes.Add(ci);
                    ChangesItem.Reduce(changes);
                    result.ChangesItems.AddRange(changes.Select(c => (ChangesItemDto)c));
                    world.CharManager.NotifyChange(gu);
                }
            }
            finally
            {
                world.CharManager.Unlock(gu, true);
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
            if (!world.CharManager.Lock(GameHelper.FromBase64String(model.Token), out GameUser gu))
                return Unauthorized("令牌无效");
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
                           select (Id: GameHelper.FromBase64String(tmp.ItemId), Count: tmp.Count, PId: GameHelper.FromBase64String(tmp.DestContainerId));
                var allGi = gim.GetAllChildrenDictionary(gc);
                var tmpGi = coll.FirstOrDefault(c => !allGi.ContainsKey(c.Id) || !allGi.ContainsKey(c.PId));
                if (tmpGi.Id != Guid.Empty)
                {
                    result.HasError = true;
                    result.DebugMessage = $"至少有一个物品没有找到。Id={tmpGi.Id},PId={tmpGi.PId}";
                    return result;
                }
                try
                {
                    List<ChangesItem> changesItems = new List<ChangesItem>();
                    foreach (var item in coll)
                    {
                        world.ItemManager.MoveItem(allGi[item.Id], item.Count, allGi[item.PId], changesItems);
                    }
                    ChangesItem.Reduce(changesItems);
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

            if (!world.CharManager.Lock(GameHelper.FromBase64String(model.Token), out GameUser gu))
                return Unauthorized("令牌无效");
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
                        var mounts = gim.CreateMounts(gi);
                        mounts.ParentId = gi.ParentId;
                        lst.Add(mounts);
                    }
                    else
                    {
                        var tmp = gim.CreateGameItem(gi.TemplateId);
                        tmp.Count = gi.Count;
                        tmp.ParentId = gi.ParentId;
                        lst.Add(tmp);
                    }
                }
                var dic = OwHelper.GetAllSubItemsOfTree(gc.GameItems, c => c.Children).ToDictionary(c => c.Id);
                List<ChangesItem> changes = new List<ChangesItem>();
                foreach (var item in lst)   //加入
                {
                    if (item.ParentId.Value == gc.Id)
                        gim.AddItems(new GameItem[] { item }, gc, null, changes);
                    else
                        gim.AddItems(new GameItem[] { item }, dic[item.ParentId.Value], null, changes);

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
            if (!world.CharManager.Lock(GameHelper.FromBase64String(model.Token), out GameUser gu))
                return Unauthorized("令牌无效");
            try
            {
                var gc = gu.CurrentChar;
                var result = new GetItemsReturnDto();
                HashSet<Guid> guids = new HashSet<Guid>(model.Ids.Select(c => GameHelper.FromBase64String(c)));
                var coll = gc.AllChildren.Where(c => guids.Contains(c.Id)).Select(c => GameItemDto.FromGameItem(c, model.IncludeChildren));
                result.GameItems.AddRange(coll);
                return result;
            }
            finally
            {
                world.CharManager.Unlock(gu, true);
            }
        }
    }
}

