using GuangYuan.GY001.BLL;
using Gy001.Controllers;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using OW.Game;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Mime;
using System.Text;

namespace GY2021001WebApi.Controllers
{
    /// <summary>
    /// 与账号相关的操作控制器。
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AdminController : GameBaseController
    {

        public AdminController(VWorld world) : base(world)
        {
        }

        /// <summary>
        /// 复制账号。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<CloneAccountReturnDto> CloneAccount(CloneAccountParamsDto model)
        {
            var result = new CloneAccountReturnDto();
            using var datas = new CloneUserDatas(World, model.Token)
            {
                Count = model.Count,
                LoginNamePrefix = model.LoginNamePrefix,
            };
            World.AdminManager.CloneUser(datas);
            result.HasError = datas.HasError;
            result.ErrorCode = datas.ErrorCode;
            result.DebugMessage = datas.ErrorMessage;
            if (!result.HasError)    //若成功执行任务
            {
                result.Account.AddRange(datas.Account.Select(c => new AccountSummery()
                {
                    LoginName = c.Item1,
                    Pwd = c.Item2,
                }));
            }
            return result;
        }

        /// <summary>
        /// 给当前角色设置经验值。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut]
        public ActionResult<SetCharExpReturnDto> SetCharExp(SetCharExpParamsDto model)
        {
            var result = new SetCharExpReturnDto();
            using var dwUser = World.CharManager.LockAndReturnDisposer(model.Token, out var gu);
            if (dwUser is null)
                return Unauthorized(VWorld.GetLastErrorMessage());
            World.CharManager.SetExp(gu.CurrentChar, model.Exp);
            return result;
        }

        /// <summary>
        /// 上传
        /// </summary>
        /// <param name="file"></param>
        /// <param name="token">令牌。</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ImportUsers(IFormFile file, string token)
        {
            using var stream = file.OpenReadStream();
            using var datas = new ImportUsersDatas(World, token) { Store = stream };
            World.AdminManager.ImportUsers(datas);
            if (datas.HasError)
                return BadRequest();
            else
                return Ok();
        }

        /// <summary>
        /// 导出用户。
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        //[ProducesResponseType(typeof(FileResult), Status200OK)]
        public FileResult ExportUsers(ExportUsersParaamsDto model)
        {
            var tempFileName = Path.GetTempFileName();
            var fullPath = Path.Combine(Path.GetTempPath(), tempFileName);
            using (var stream = new FileStream(fullPath, FileMode.Truncate))
            {
                using ExportUsersDatas dates = new ExportUsersDatas(World, model.Token)
                {
                    LoginNamePrefix = model.Prefix,
                    StartIndex = model.StartIndex,
                    EndIndex = model.EndIndex,
                    Store = stream,
                };
#if DEBUG
#else
                using var cStream = new BrotliStream(stream, CompressionMode.Compress);
                dates.Store=cStream;
#endif
                World.AdminManager.ExportUsers(dates);
                if (dates.HasError)
                {
                    Response.StatusCode = 500;
                    return null;
                }
            }
#if DEBUG
            string fileDownloadName = "Gy001UsersInfo.txt";
#else
            string fileDownloadName = "Gy001UsersInfo.bin";
#endif
            var result = new PhysicalFileResult(fullPath, "application/text") { FileDownloadName = fileDownloadName };
            return result;
        }


    }

}