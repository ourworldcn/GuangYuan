using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GY2021001BLL;
using GY2021001DAL;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GY2021001WebApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class GameCharController : ControllerBase
    {


        /// <summary>
        /// 测试获取用户信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<GameCharDtoBase> GetChar(TokenDtoBase model)
        {
            var result = new GameCharDtoBase()
            {
                Id = Guid.NewGuid().ToBase64String(),
                ClientGutsString = "{\"客户端记录属性键\":\"值示例\"}",
            };
            result.Properties.Add("atk", 100);
            result.Properties.Add("hp", 1000.1f);
            result.Properties.Add("qult", 500.5f);
            return result;
        }

    }
}

