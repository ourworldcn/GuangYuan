using GuangYuan.GY001.BLL;
using GuangYuan.GY001.BLL.Social;
using GuangYuan.GY001.TemplateDb;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OW.DDD;
using OW.Game;
using System;
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
            return CnNames.GetName(1 == sex);
        }

        /// <summary>
        /// 获取全服推关战力Top 50。
        /// </summary>
        /// <returns></returns>
        //[ResponseCache(Location = ResponseCacheLocation.Any, Duration = 120)]
        [HttpGet]
        public ActionResult<GetRankOfTuiguanQueryReturnDto> GetRankOfTuiguanQuery()
        {
            var command = new GetTotalPowerTopRankCommand() { Top = 50 };

            var svc = HttpContext.RequestServices.GetRequiredService<OwCommandManager>();
            var commandResult = svc.Handle<GetTotalPowerTopRankCommand, GetTotalPowerTopRankCommandResult>(command);

            var result = new GetRankOfTuiguanQueryReturnDto();
            result.Datas.AddRange(commandResult.ResultCollction.Select(c => new RankDataItemDto
            {
                CharId = c.Item1.ToBase64String(),
                DisplayName = c.Item3,
                Metrics = c.Item2,
            }));
            for (int i = 0; i < result.Datas.Count; i++)
            {
                result.Datas[i].OrderNumber = i;
            }
            return result;
        }

        /// <summary>
        /// 获取所有卡池数据。
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 120)]
        public ActionResult<List<GameCardTemplateDto>> GetCardPoolTemplates()
        {
            return _World.ItemTemplateManager.Id2CardPool.Values.Select(c => (GameCardTemplateDto)c).ToList();
        }
    }

}