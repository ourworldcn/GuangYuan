using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GY2021001BLL;
using GY2021001DAL;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GY2021001WebApi.Controllers
{
    /// <summary>
    /// 与账号相关的操作控制器。
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        public AccountController()
        {

        }

        /// <summary>
        /// 快速注册。
        /// </summary>
        /// <returns>返回自动生成的用户名和密码。</returns>
        [HttpGet]
        public ActionResult<QuicklyRegisterReturnDto> QuicklyRegister()
        {
            try
            {
                var db = HttpContext.RequestServices.GetService<GY2021001DbContext>();
                var logger = HttpContext.RequestServices.GetService(typeof(ILogger<AccountController>)) as ILogger<AccountController>;
                //生成返回值
                var gcm = HttpContext.RequestServices.GetService(typeof(GameCharManager)) as GameCharManager;
                string pwd = null;
                var gu = gcm.QuicklyRegister(ref pwd);
                var result = new QuicklyRegisterReturnDto() //gy210415123456 密码12位大小写
                {
                    LoginName = gu.LoginName,
                    Pwd = pwd,
                };
                return result;
            }
            catch (Exception err)
            {
                return BadRequest(err.Message);
            }
        }

        /// <summary>
        /// 登录接口，调用此接口获取令牌，才可以和服务器进一步交互。
        /// </summary>
        /// <param name="loginParamsDto">登陆参数,参见<seealso cref="LoginParamsDto"/> </param>
        /// <returns>Token为空则是用户名或密码错误。</returns>
        /// <response code="500">用户名或密码错误。</response>
        /// <response code="429">登录人数过多。请稍后登录。目前按每颗CPU带1000在线计算，未来更具实际数据增减。</response>
        [HttpPost]
        public ActionResult<LoginReturnDto> Login(LoginParamsDto loginParamsDto)
        {
            //[ProducesResponseType(StatusCodes.Status400BadRequest)]
            var gm = HttpContext.RequestServices.GetService(typeof(GameCharManager)) as GameCharManager;
            if (gm.OnlineCount > 1000 * Environment.ProcessorCount)
                return StatusCode((int)HttpStatusCode.TooManyRequests, "登录人数过多，请稍后登录");
            var gu = gm.Login(loginParamsDto.LoginName, loginParamsDto.Pwd, loginParamsDto.Region);

            var worldServiceHost = $"{Request.Scheme}://{Request.Host}";
            var result = new LoginReturnDto()
            {
                WorldServiceHost = worldServiceHost,
            };
            if (null != gu)
                result.Token = gu.CurrentToken.ToBase64String();
            result.GameChars.AddRange(gu.GameChars.Select(c => (GameCharDto)c));
            return result;
        }

        /// <summary>
        /// 发送一个空操作以保证闲置下线重新开始计时。
        /// </summary>
        /// <param name="model">令牌。</param>
        /// <returns>true成功的延迟下线时间，false指定令牌无效。</returns>
        [HttpPost]
        public ActionResult<bool> Nop(NopParamsDto model)
        {
            var gm = HttpContext.RequestServices.GetService(typeof(GameCharManager)) as GameCharManager;
            return gm.Nope(GameHelper.FromBase64String(model.Token));
        }

        /// <summary>
        /// 更改密码。需要已经登录用户。
        /// </summary>
        /// <param name="model">参数</param>
        /// <returns>true则更改成功，false没有更改成功。</returns>
        [HttpPost]
        public ActionResult<bool> ChangePwd(ChangePwdParamsDto model)
        {
            var gm = HttpContext.RequestServices.GetService(typeof(GameCharManager)) as GameCharManager;
            Guid token = GameHelper.FromBase64String(model.Token);
            return gm.ChangePwd(token, model.NewPwd);
        }

    }
}