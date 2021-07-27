using Game.Social;
using GY2021001BLL;
using GY2021001DAL;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using OwGame;
using System.Linq;

namespace Gy001.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SocialController : ControllerBase
    {
        private readonly VWorld _World;
        public SocialController(VWorld world)
        {
            _World = world;
        }

        /// <summary>
        /// 获取指定用户的所有邮件。
        /// </summary>
        /// <param name="model">参见 GetMailsParamsDto</param>
        /// <returns>参见 GetMailsReturnDto</returns>
        /// <response code="401">令牌错误。</response>
        [HttpPut]
        public ActionResult<GetMailsReturnDto> GetMails(GetMailsParamsDto model)
        {
            if (!_World.CharManager.Lock(GameHelper.FromBase64String(model.Token), out GameUser gu))
            {
                return Unauthorized("令牌无效");
            }
            try
            {
                var result = new GetMailsReturnDto();
                var social = _World.SocialManager;
                var coll = social.GetMails(gu.CurrentChar);
                result.Mails.AddRange(coll.Select(c => (GameMailDto)c));
                return result;
            }
            finally
            {
                _World.CharManager.Unlock(gu);
            }
        }
    }

}
