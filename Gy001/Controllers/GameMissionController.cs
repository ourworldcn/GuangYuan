using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using OW.Game;

namespace Gy001.Controllers
{
    /// <summary>
    /// 任务/成就相关操作。
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class GameMissionController : GameBaseController
    {
        public GameMissionController(VWorld world) : base(world)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut]
        public ActionResult<GetMissionRewardReturnDto> GetMissionReward(GetMissionRewardParamsDto model)
        {
            var result = new GetMissionRewardReturnDto();
            return result;
        }

    }
}
