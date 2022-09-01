using AutoMapper;
using Game.Social;
using GuangYuan.GY001.BLL;
using GuangYuan.GY001.BLL.Specific;
using GuangYuan.GY001.UserDb;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OW.Extensions.Game.Store;
using OW.Game;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

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
                var db = HttpContext.RequestServices.GetService<GY001UserContext>();
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
                if (null != gu)    //若成功注册
                {
                    //发送欢迎邮件 ，已被停止该功能
                    var mail = new GameMail()
                    {
                        Subject = "Welcome",
                        Body = "Good day, all bumpers! Thanks for downloading our game! This is the first time to open a closed beta test, so we prepared some gifts for all bumpers every day. Have fun! " +
                            Environment.NewLine + "What's more, we don't have any customer service in our game.If you have any questions," +
                            Environment.NewLine + "please do not hesitate to contact our Facebook page @Harvest Bumpers. Thank you for your continuous support for Harvest Bumpers!" +
                            Environment.NewLine + "Harvest Bumpers Team",
                    };

                    GameItem gi = new GameItem();
                    gcm.World.EventsManager.GameItemCreated(gi, ProjectConstant.ZuanshiId);
                    gi.Count = 500;
                    //gcm.World.SocialManager.SendMail(mail, new Guid[] { gu.CurrentChar.Id }, SocialConstant.FromSystemId,
                    //    new (GameItem, Guid)[] { (gi, ProjectConstant.CurrencyBagTId) });
                }
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
        /// <response code="503">登录人数过多。请稍后登录。目前按每颗CPU带10000在线计算，未来更具实际数据增减。</response>
        [HttpPost]
        public ActionResult<LoginReturnDto> Login(LoginParamsDto loginParamsDto)
        {
            //[ProducesResponseType(StatusCodes.Status400BadRequest)]
            var gm = HttpContext.RequestServices.GetService(typeof(GameCharManager)) as GameCharManager;
            if (gm.Id2OnlineChar.Count > 10000 * Environment.ProcessorCount)
                return StatusCode((int)HttpStatusCode.ServiceUnavailable, "登录人数过多，请稍后登录");
            var gu = gm.Login(loginParamsDto.LoginName, loginParamsDto.Pwd, loginParamsDto.Region);

            var worldServiceHost = $"{Request.Scheme}://{Request.Host}";
            var chartServiceHost = $"{Request.Scheme}://{Request.Host}";
            var result = new LoginReturnDto()
            {
                WorldServiceHost = worldServiceHost,
                ChartServiceHost = chartServiceHost,
            };
            if (null != gu)
            {
                result.Token = gu.CurrentToken.ToBase64String();
                using var dwUsers = gm.LockAndReturnDisposer(gu);
                var mapper = gm.World.GetMapper();
                result.GameChars.AddRange(gu.GameChars.Select(c => mapper.Map(c)));
            }
            return result;
        }

        /// <summary>
        /// 特定发行商sdk创建或登录用户。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<LoginT78ReturnDto> LoginT78(LoginT78ParamsDto model)
        {
            var gm = HttpContext.RequestServices.GetService(typeof(GameCharManager)) as GameCharManager;
            if (gm.Id2OnlineChar.Count > 10000 * Environment.ProcessorCount)
                return StatusCode((int)HttpStatusCode.ServiceUnavailable, "登录人数过多，请稍后登录");
            string pwd = null;
            var gu = gm.LoginT78(model.Sid, out pwd);

            var worldServiceHost = $"{Request.Scheme}://{Request.Host}";
            var chartServiceHost = $"{Request.Scheme}://{Request.Host}";
            var result = new LoginT78ReturnDto()
            {
                WorldServiceHost = worldServiceHost,
                ChartServiceHost = chartServiceHost,
            };
            if (null != gu)
            {
                result.Token = gu.CurrentToken.ToBase64String();
                using var dwUsers = gm.LockAndReturnDisposer(gu);
                var mapper = gm.World.GetMapper();
                result.GameChars.AddRange(gu.GameChars.Select(c => mapper.Map(c)));
                result.ResultString = gu.RuntimeProperties.GetStringOrDefault("T78LoginResultString");
                result.LoginName = gu.LoginName;
                result.Pwd = pwd;
            }
            return result;
        }

        /// <summary>
        /// 特定发行商sdk创建或登录用户。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<LoginT89ReturnDto> LoginT89(LoginT89ParamsDto model)
        {
            var gm = HttpContext.RequestServices.GetService(typeof(GameCharManager)) as GameCharManager;
            if (gm.Id2OnlineChar.Count > 10000 * Environment.ProcessorCount)
                return StatusCode((int)HttpStatusCode.ServiceUnavailable, "登录人数过多，请稍后登录");

            var datas = new T89LoginData(HttpContext.RequestServices)
            {
            };
            var mapper = HttpContext.RequestServices.GetRequiredService<GameMapperManager>();
            mapper.Map(model, datas);
            gm.LoginT89(datas);

            var worldServiceHost = $"{Request.Scheme}://{Request.Host}";
            var chartServiceHost = $"{Request.Scheme}://{Request.Host}";
            var result = new LoginT89ReturnDto()
            {
                WorldServiceHost = worldServiceHost,
                ChartServiceHost = chartServiceHost,
            };

            if (!datas.HasError)
            {
                result.LoginName = datas.LoginName;
                result.Token = datas.InnerToken.ToBase64String();
                result.Pwd = datas.Pwd;
                result.GameChars.AddRange(datas.GameChars.Select(c => mapper.Map(c)));
            }
            return result;
        }

        /// <summary>
        /// 发送一个空操作以保证闲置下线重新开始计时。
        /// </summary>
        /// <param name="model">令牌。</param>
        /// <returns></returns>
        /// <response code="401">令牌错误。</response>
        /// <response code="200">成功刷新。</response>
        [HttpPost]
        public ActionResult Nop(NopParamsDto model)
        {
            var gm = HttpContext.RequestServices.GetService(typeof(GameCharManager)) as GameCharManager;
            return gm.Nope(OwConvert.ToGuid(model.Token)) ? base.Ok() as ActionResult : base.Unauthorized("令牌错误。");
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
            Guid token = OwConvert.ToGuid(model.Token);
            return gm.ChangePwd(token, model.NewPwd);
        }

        /// <summary>
        /// 快速注册一个新账号并登录。
        /// </summary>
        /// <returns></returns>
        /// <response code="400">意外错误。</response>
        [HttpGet]
        public ActionResult<LoginReturnDto> QuicklyRegisterAndLogin()
        {


#if DEBUG
            using var db = HttpContext.RequestServices.GetRequiredService<VWorld>().CreateNewUserDbContext();
            GameItem gi = db.Set<GameItem>().First(c => c.Children.Count > 0);
            gi.SetClientString("tname=ds");
            var mapper1 = HttpContext.RequestServices.GetRequiredService<IMapper>();
            var world = HttpContext.RequestServices.GetRequiredService<VWorld>();
            var giDto = mapper1.Map<GameItem,GameItemDto>(gi);
            
#endif //DEBUG
            try
            {
                var services = HttpContext.RequestServices;
                string pwd = null;
                var gcm = services.GetService<GameCharManager>();
                var gu = gcm.QuicklyRegister(ref pwd);
                gu = gcm.Login(gu.LoginName, pwd, "IOS/Test");
                var worldServiceHost = $"{Request.Scheme}://{Request.Host}";

                var result = new LoginReturnDto()
                {
                    Token = gu.CurrentToken.ToBase64String(),
                    WorldServiceHost = worldServiceHost,
                };
                var mapper = gcm.World.GetMapper();
                result.GameChars.AddRange(gu.GameChars.Select(c => mapper.Map(c)));
                return result;
            }
            catch (Exception err)
            {
                return base.BadRequest(err.Message);
            }
        }

    }

}