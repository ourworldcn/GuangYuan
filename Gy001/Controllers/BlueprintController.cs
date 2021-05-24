using GY2021001BLL;
using GY2021001DAL;
using Gy2021001Template;
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

        /// <summary>
        /// 测试。
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<bool> Test()
        {
            var world = HttpContext.RequestServices.GetService<VWorld>();
            var gitm = world.ItemTemplateManager;
            var gim = world.ItemManager;
            string pwd = "123456";
            var loginName = world.CharManager.QuicklyRegister(ref pwd).LoginName;
            var gu = world.CharManager.Login(loginName, pwd, "");
            //执行蓝图制造
            if (!world.BlueprintManager.Id2BlueprintTemplate.TryGetValue(new Guid("d40c1818-06cf-4d19-9f6e-5ba54472b6fc"), out BlueprintTemplate blueprint))
                return false;
            var gc = gu.GameChars[0];
            ApplyBluprintDatas applyBluprintDatas = new ApplyBluprintDatas()
            {
                Count = 1,
                Blueprint = blueprint,
                GameChar = gu.GameChars[0],

            };
            var shenwenBag = gc.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.ShenWenSlotId);
            applyBluprintDatas.GameItems.Add(new GameItem()
            {
                Id = shenwenBag.Children.First(c=>c.TemplateId==new Guid("69542017-0C98-41C4-A66D-5758733F457E")).Id,
            });
            var bpm = world.BlueprintManager;
            bpm.ApplyBluprint(applyBluprintDatas);
            return Ok();
        }
    }
}
