using GuangYuan.GY001.BLL;
using GuangYuan.GY001.BLL.Specific;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using OW.Game;
using System;
using System.Linq;

namespace Gy001.Controllers
{

    /// <summary>
    /// 外围系统。
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class BlueprintController : ControllerBase
    {
        private readonly VWorld _World;
        public BlueprintController(VWorld world)
        {
            _World = world;
        }

#if DEBUG

        /// <summary>
        /// 测试。
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<ApplyBlueprintReturnDto> Test()
        {
            var world = _World;   //获取总服务
            var gitm = world.ItemTemplateManager;
            var gim = world.ItemManager;
            //注册并登录新账号
            string pwd = "123456";
            var loginName = world.CharManager.QuicklyRegister(ref pwd).LoginName;
            var gu = world.CharManager.Login(loginName, pwd, "");
            //执行蓝图制造
            if (!world.BlueprintManager.Id2BlueprintTemplate.TryGetValue(new Guid("{DD5095F8-929F-45A5-A86C-4A1792E9D9C8}"), out BlueprintTemplate blueprint))
                return NotFound("未发现蓝图");
            var gc = gu.CurrentChar;
            ApplyBlueprintDatas applyBluprintDatas = new ApplyBlueprintDatas(world.Service, gu.CurrentChar)
            {
                Count = 1,
                Blueprint = blueprint,
            };
            var hl = gc.GameItems.FirstOrDefault(c => c.ExtraGuid == ProjectConstant.HomelandSlotId);
            var goldTId = new Guid("7a00740c-035e-4846-a619-2d0855f60b55");
            var diam = gc.GetZuanshi();
            diam.Count += 1000;
            var bpm = world.BlueprintManager;
            bpm.ApplyBluprint(applyBluprintDatas);
            var mapper = world.GetMapper();
            return mapper.Map(applyBluprintDatas); 
        }

#endif

        /// <summary>
        /// 使用蓝图制造或升级物品。
        /// </summary>
        /// <param name="model">参见 ApplyBlueprintParamsDto 的说明。</param>
        /// <returns>物品变化数据。</returns>
        /// <response code="401">令牌错误。</response>
        /// <response code="400">参数错误。</response>
        [HttpPost]
        public ActionResult<ApplyBlueprintReturnDto> ApplyBlueprint(ApplyBlueprintParamsDto model)
        {
            var result = new ApplyBlueprintReturnDto()
            {
                HasError = false,
            };
            var world = HttpContext.RequestServices.GetService<VWorld>();
            if (!world.CharManager.Lock(OwConvert.ToGuid(model.Token), out GameUser gu))
            {
                return base.Unauthorized("令牌无效");
            }
            ApplyBlueprintDatas datas = null;
            try
            {
                datas = new ApplyBlueprintDatas(_World.Service, gu.CurrentChar)
                {
                    Blueprint = world.BlueprintManager.GetTemplateFromId(OwConvert.ToGuid(model.BlueprintId)) as BlueprintTemplate,  //这里不处理是空的情况
                    Count = model.Count,
                };
                if (OwConvert.TryToGuid(model.ActionId, out var actionId))
                    datas.ActionId = actionId;
                datas.GameItems.AddRange(model.GameItems.Select(c => (GameItem)c));
                world.BlueprintManager.ApplyBluprint(datas);

                var mapper = world.GetMapper();
                result = mapper.Map(datas);
                result.FillFrom(datas);
                world.CharManager.NotifyChange(gu);
            }
            catch (Exception err)
            {
                result.HasError = true;
                result.DebugMessage = err.Message;
            }
            finally
            {
                world.CharManager.Unlock(gu, true);
            }
            if (datas != null && datas.Blueprint != null && !result.HasError)
                switch (datas.Blueprint.Id.ToString("D").ToLower())
                {
                    case "c7051e47-0a73-4319-85dc-7b02f26f14f4":    //兽栏背包扩容
                        {
                            var tidStr = ProjectConstant.ShoulanSlotId.ToBase64String();
                            result.ChangesItems.SelectMany(c => c.Changes).FirstOrDefault(c => c.ExtraGuid == tidStr)?.Children?.Clear();  //清空子对象。
                        }
                        break;
                    default:
                        break;
                }
            return result;
        }
    }
}
