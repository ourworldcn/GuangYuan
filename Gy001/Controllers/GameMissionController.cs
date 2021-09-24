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
        public ActionResult<GetMissionReturnDto> GetMission(GetMissionParamsDto model)
        {
            var result = new GetMissionReturnDto();
            return result;
        }

        public class GetMissionReturnDto
        {
        }

        public class GetMissionParamsDto
        {
        }
    }
}
