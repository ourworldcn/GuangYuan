using GY2021001BLL;
using GY2021001DAL;
using Gy2021001Template;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            var world = HttpContext.RequestServices.GetService<VWorld>();   //获取总服务
            var gitm = world.ItemTemplateManager;
            var gim = world.ItemManager;
            //注册并登录新账号
            string pwd = "123456";
            var loginName = world.CharManager.QuicklyRegister(ref pwd).LoginName;
            var gu = world.CharManager.Login(loginName, pwd, "");
            //执行蓝图制造
            if (!world.BlueprintManager.Id2BlueprintTemplate.TryGetValue(new Guid("d40c1818-06cf-4d19-9f6e-5ba54472b6fc"), out BlueprintTemplate blueprint))
                return Unauthorized();
            var gc = gu.GameChars[0];
            ApplyBlueprintDatas applyBluprintDatas = new ApplyBlueprintDatas()
            {
                Count = 9,
                Blueprint = blueprint,
                GameChar = gu.GameChars[0],
            };
            var shenwenBag = gc.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.ShenWenSlotId);
            applyBluprintDatas.GameItems.Add(new GameItem()
            {
                Id = shenwenBag.Children.First(c => c.TemplateId == new Guid("69542017-0C98-41C4-A66D-5758733F457E")).Id,
            });
            var bpm = world.BlueprintManager;
            bpm.ApplyBluprint(applyBluprintDatas);
            return (ApplyBlueprintReturnDto)applyBluprintDatas;
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
            if (!world.CharManager.Lock(GameHelper.FromBase64String(model.Token), out GameUser gu))
            {
                return Unauthorized("令牌无效");
            }
            try
            {
                ApplyBlueprintDatas datas = new ApplyBlueprintDatas()
                {
                    Blueprint = world.BlueprintManager.GetTemplateFromId(GameHelper.FromBase64String(model.BlueprintId)) as BlueprintTemplate,  //这里不处理是空的情况
                    GameChar = gu.GameChars[0],
                    Count=model.Count,
                };
                datas.GameItems.AddRange(model.GameItems.Select(c => (GameItem)c));
                world.BlueprintManager.ApplyBluprint(datas);

                result.ChangesItems.AddRange(datas.ChangesItem.Select(c => (ChangesItemDto)c));
                result.SuccCount = datas.SuccCount;
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
            return result;
        }
    }
}
