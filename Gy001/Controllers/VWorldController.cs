using GuangYuan.GY001.BLL;
using Gy2021001Template;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;

namespace GY2021001WebApi.Controllers
{
    /// <summary>
    /// 世界服务器的相关操作。
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class VWorldController : ControllerBase
    {
        //public VWorldController()
        //{

        //}
        private readonly VWorld _World;
        public VWorldController(VWorld world)
        {
            _World = world;
        }

        /// <summary>
        /// 获取服务器非敏感信息。
        /// </summary>
        /// <returns></returns>
        /// <response code="401">管理员账号或密码错误。</response>
        [HttpGet]
        public ActionResult<VWorldInfomationDto> GetVWorldInfo(string userName, string pwd)
        {
            if (userName != "gy001" || pwd != "210115")
                return Unauthorized("用户名或密码错误。");
            var world = HttpContext.RequestServices.GetRequiredService<VWorld>();
            return (VWorldInfomationDto)world.GetInfomation();
        }

        /// <summary>
        /// 通告服务器下线。
        /// </summary>
        /// <param name="admin"></param>
        /// <param name="pwd"></param>
        /// <returns></returns>
        /// <response code="401">管理员账号或密码错误。</response>
        [HttpPost]
        public ActionResult NotifyShutdown(string admin, string pwd)
        {
            if (admin != "gy001" || pwd != "guangyuan123")
                return Unauthorized();
            var world = HttpContext.RequestServices.GetRequiredService<VWorld>();
            world.NotifyShutdown();
            return Ok();
        }


        /// <summary>
        /// 获取所有模板。
        /// 缓存120s。
        /// </summary>
        /// <returns>所有模板的集合。</returns>
        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 120)]
        public ActionResult<List<GameItemTemplateDto>> GetTemplates()
        {
            var gitm = HttpContext.RequestServices.GetRequiredService<GameItemTemplateManager>();
            return gitm.Id2Template.Values.Select(c => (GameItemTemplateDto)c).ToList();
        }

        /// <summary>
        /// 获取资源服务器地址。
        /// </summary>
        /// <returns></returns>
        /// <response code="201">调用过于频繁。</response>
        [HttpGet]
        public ActionResult<string> GetResourceServerUrl()
        {
            var config = HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
            string str = config.GetValue<string>("ResourceServerUrl"); //"🐆"
            return str;
        }

        /// <summary>
        /// 获取一个随机的名字。
        /// </summary>
        /// <param name="sex">0是传统意义上的女性，1是传统意义上的男性。未来可能添加多元性别，目前未得到此需求。</param>
        /// <returns>一个随机的中文名。</returns>
        [HttpGet]
        public ActionResult<string> GetNewCnName(int sex)
        {
            return CnNames.GetName(1==sex);
        }

        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 120)]
        [HttpGet]
        public ActionResult<List< MainbaseUpgradePrv>> GetMainbaseUpgradePrv()
        {
            return MainbaseUpgradePrv.Alls;
        }
    }
}