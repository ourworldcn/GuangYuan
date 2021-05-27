using System;
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
            var gc = gu.GameChars[0];
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
        /// <returns>总是返回成功</returns>
        /// <response code="401">令牌错误。</response>
        [HttpPut]
        public ActionResult<bool> ModifyClentString(ModifyClentStringParamsDto model)
        {
            var gitm = HttpContext.RequestServices.GetRequiredService<GameCharManager>();
            var gu = gitm.GetUserFromToken(GameHelper.FromBase64String(model.Token));
            if (null == gu) //若令牌无效
                return Unauthorized();
            var objectId = GameHelper.FromBase64String(model.ObjectId);
            if (gu.GameChars[0].Id == objectId)
            {
                gu.GameChars[0].ClientGutsString = model.ClientString;
                gitm.NotifyChange(gu);
            }
            else
                return gitm.ModifyClientString(gu.GameChars[0], objectId, model.ClientString);
            return true;
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
                var gc = gu.GameChars[0];
                if (model.IsRemove)
                {
                    if (!gc.ClientExtendProperties.Remove(model.Name, out GameExtendProperty val))
                        return false;
                    gu.DbContext.Set<GameExtendProperty>().Remove(val);
                }
                else if (gc.ClientExtendProperties.TryGetValue(model.Name, out GameExtendProperty gep))
                    gep.Value = model.Value;
                else
                {
                    gep = new GameExtendProperty()
                    {
                        Name = model.Name,
                        Value = model.Value,
                        ParentId = gc.Id,
                    };
                    gu.DbContext.Set<GameExtendProperty>().Add(gep);
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
        /// 设置出战坐骑列表。
        /// </summary>
        /// <param name="model">GameItemDto 中元素仅需Id有效填写。</param>
        /// <returns>true成功设置，false可能是设置数量超过限制。</returns>
        [HttpPut]
        public ActionResult<bool> SetCombatMounts(SetCombatMountsParamsDto model)
        {
            var world = HttpContext.RequestServices.GetService<VWorld>();
            if (!world.CharManager.Lock(GameHelper.FromBase64String(model.Token), out GameUser gu))
                return Unauthorized("令牌无效");
            List<GameItem> lst = null;
            try
            {
                lst = world.ObjectPoolListGameItem.Get();   //获取列表
                var gc = gu.GameChars[0];

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
                var gc = gu.GameChars[0];
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
    }
}

