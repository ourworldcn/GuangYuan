using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GY2021001BLL;
using GY2021001DAL;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace GY2021001WebApi.Controllers
{
    /// <summary>
    /// 角色相关操作。
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class GameCharController : ControllerBase
    {
        /// <summary>
        /// 更改角色的名字。当前只能更改一次。
        /// </summary>
        /// <param name="model">参数。</param>
        /// <returns>true成功更改。false有重名。</returns>
        /// <response code="401">令牌错误。</response>
        /// <response code="402">没有必要的道具。</response>
        [HttpPost]
        public ActionResult<bool> Rename(RenameParamsDto model)
        {
            var gitm = HttpContext.RequestServices.GetRequiredService<GameCharManager>();
            var gu = gitm.GetUserFromToken(GameHelper.FromBase64String(model.Token));
            if (null == gu) //若令牌无效
                return Unauthorized();
            var gc = gu.GameChars[0];
            if (null != gc.DisplayName) //若已经有名字
                return StatusCode((int)HttpStatusCode.PaymentRequired); //TO DO
            gc.DisplayName = model.DisplayName;
            gitm.NotifyChange(gu);
            return true;
        }

        /// <summary>
        /// 修改客户端字符串。
        /// </summary>
        /// <param name="model">参见 ModifyClentStringParamsDto。</param>
        /// <returns>总是返回成功</returns>
        /// <response code="401">令牌错误。</response>
        [HttpPut]
        public ActionResult ModifyClentString(ModifyClentStringParamsDto model)
        {
            var gitm = HttpContext.RequestServices.GetRequiredService<GameCharManager>();
            var gu = gitm.GetUserFromToken(GameHelper.FromBase64String(model.Token));
            if (null == gu) //若令牌无效
                return Unauthorized();
            gu.GameChars[0].ClientGutsString = model.ClientString;
            gitm.NotifyChange(gu);
            return Ok();
        }

        /// <summary>
        /// 设置客户端扩展属性。
        /// </summary>
        /// <param name="model">参见 ModifyClientExtendPropertyParamsDto </param>
        /// <returns>true成功设置,false,指定了删除属性标志，但没有找到指定名子的属性。</returns>
        /// <response code="401">令牌错误。</response>
        [HttpPut]
        public ActionResult<bool> ModifyClientExtendProperty(ModifyClientExtendPropertyParamsDto model)
        {
            var world = HttpContext.RequestServices.GetRequiredService<VWorld>();
            if (!world.CharManager.Lock(GameHelper.FromBase64String(model.Token), out GameUser gu))
            {
                return Unauthorized("无效令牌。");
            }
            try
            {
                var gc = gu.GameChars[0];
                if (model.IsRemove)
                {
                    if (!gc.ClientExtendProperties.Remove(model.Name, out GameExtendProperty val))
                        return false;
                    gu.DbContext.Set<GameExtendProperty>().Remove(val);
                }
                else if (gc.ClientExtendProperties.TryGetValue(model.Name, out GameExtendProperty gep))
                    gep.Value = model.Value;
                else
                {
                    gep = new GameExtendProperty()
                    {
                        Name = model.Name,
                        Value = model.Value,
                        ParentId = gc.Id,
                    };
                    gu.DbContext.Set<GameExtendProperty>().Add(gep);
                    gc.ClientExtendProperties[gep.Name] = gep;
                }
                world.CharManager.Nope(gu);
            }
            finally
            {
                world.CharManager.Unlock(gu);
            }
            return true;
        }
    }
}

