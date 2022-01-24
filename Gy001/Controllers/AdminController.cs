using GuangYuan.GY001.BLL;
using GuangYuan.GY001.UserDb;
using Gy001.Controllers;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
using OW.Game;
using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Mime;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;

namespace GY2021001WebApi.Controllers
{
    /// <summary>
    /// 与账号相关的操作控制器。
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AdminController : GameBaseController
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="world"></param>
        public AdminController(VWorld world) : base(world)
        {
        }

        /// <summary>
        /// 设置pvp积分，超管和运营可以使用此功能。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<SetCombatScoreReturnDto> SetCombatScore(SetCombatScoreParamsDto model)
        {
            using var datas = new SetCombatScoreDatas(World, model.Token)
            {
                Prefix = model.Prefix,
                StartIndex = model.StartIndex,
                EndIndex = model.EndIndex,
                PveScore = model.PveScore,
            };
            World.AdminManager.SetCombatScore(datas);
            var result = new SetCombatScoreReturnDto();
            result.FillFrom(datas);
            return result;
        }

        /// <summary>
        /// 给指定的一组用户追加权限。只有超级管理员账号可以成功调用该函数。当前版本可以给其他用户设置超管权限。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<AddPowersReturnDto> AddPowers(AddPowersParamsDto model)
        {
            using var datas = new AddPowersDatas(World, model.Token)
            {
                CharType = model.CharType,
                EndIndex = model.EndIndex,
                StartIndex = model.StartIndex,
                Prefix = model.Prefix,
            };
            World.AdminManager.AddPowers(datas);
            var result = new AddPowersReturnDto();
            result.FillFrom(datas);
            return result;
        }

        /// <summary>
        /// 删除指定登录名的一组用户。目前除了整体回滚数据库以外，无法恢复该操作，请慎重使用。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpDelete]
        public ActionResult<DeleteUsersResultDto> DeleteUsers(DeleteUsersParamsDto model)
        {
            var result = new DeleteUsersResultDto();
            result.HasError = !World.CharManager.Delete(model.LoginNames);
            if (result.HasError)
            {
                result.ErrorCode = VWorld.GetLastError();
                result.DebugMessage = VWorld.GetLastErrorMessage();
            }
            return result;
        }

        /// <summary>
        /// 复制账号。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="401">令牌无效。</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
        /// 上传用户数据。
        /// </summary>
        /// <param name="file"></param>
        /// <param name="token">令牌。</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ImportUsers(IFormFile file, string token)
        {
            using var stream = file.OpenReadStream();
            using var datas = new ImportUsersDatas(World, token) { Store = stream };
#if DEBUG
            using var cStream = new BrotliStream(stream, CompressionMode.Decompress);
            datas.Store = cStream;
#else
            using var cStream = new BrotliStream(stream, CompressionMode.Decompress);
            datas.Store = cStream;
#endif
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
                using ExportUsersDatas datas = new ExportUsersDatas(World, model.Token)
                {
                    LoginNamePrefix = model.Prefix,
                    StartIndex = model.StartIndex,
                    EndIndex = model.EndIndex,
                    Store = stream,
                };
#if DEBUG
                using var cStream = new BrotliStream(stream, CompressionMode.Compress);
                datas.Store = cStream;
#else
                using var cStream = new BrotliStream(stream, CompressionMode.Compress);
                datas.Store=cStream;
#endif
                World.AdminManager.ExportUsers(datas);
                if (datas.HasError)
                {
                    Response.StatusCode = 500;
                    return null;
                }
                else
                    datas.Store.Flush();
            }
#if DEBUG
            //string fileDownloadName = "Gy001UsersInfo.txt";
            string fileDownloadName = "Gy001UsersInfo.bin";
#else
            string fileDownloadName = "Gy001UsersInfo.bin";
#endif
            var result = new PhysicalFileResult(fullPath, MediaTypeNames.Application.Octet) { FileDownloadName = fileDownloadName };
            return result;
        }

        /// <summary>
        /// 获取服务器的一些统计数据。
        /// </summary>
        /// <param name="model"></param>
        /// <returns><seealso cref="GetInfosResultDto"/></returns>
        [HttpPut]
        public ActionResult<GetInfosResultDto> GetInfos(GetInfosParamsDto model)
        {
            using var datas = new GetInfosDatas(World, model.Token);
            World.AdminManager.GetInfos(datas);
            var result = new GetInfosResultDto() { };
            result.FillFrom(datas);
            if (!result.HasError)
            {
                result.LoadRate = datas.LoadRate;
                result.OnlineCount = datas.OnlineCount;
                result.TotalCount = datas.TotalCount;
            }
            return result;
        }

        /// <summary>
        /// 重启服务器。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<ReturnDtoBase> Reboot(TokenDtoBase model)
        {
            using var datas = new RebootDatas(World, model.Token);
            World.AdminManager.Reboot(datas);
            var result = new ReturnDtoBase()
            {
            };
            result.FillFrom(datas);
            return result;
        }

        /// <summary>
        /// 封停账号。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<BlockUserReturnDto> BlockUser(BlockUserParamsDto model)
        {
            BlockDatas datas = new BlockDatas(World, model.Token)
            {
                LoginName = model.LoginName,
                BlockUtc = model.BlockUtc,
            };
            World.AdminManager.Block(datas);
            var result = new BlockUserReturnDto();
            result.FillFrom(datas);
            return result;
        }

        /// <summary>
        /// 强制下线。
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<LetOutReturnDto> LetOut(LetOutParamsDto model)
        {
            LetOutDatas datas = new LetOutDatas(World, model.Token)
            {
                LoginName = model.LoginName
            };
            World.AdminManager.LetOut(datas);
            var result = new LetOutReturnDto();
            result.FillFrom(datas);
            return result;
        }

        /// <summary>
        /// 通过给特定角色或所有角色发送物品。
        /// </summary>
        /// <param name="model">参数封装对象。</param>
        /// <returns>返回值封装对象。</returns>
        [HttpPost]
        public ActionResult<SendThingsReturnDto> SendThings(SendThingsParamsDto model)
        {
            SendThingDatas datas = new SendThingDatas(World, model.Token) { Mail = (GameMail)model.Mail };
            datas.Tos.AddRange(model.Tos);
            foreach (var item in model.Propertyies)
            {
                datas.Propertyies[item.Key] = item.Value is JsonElement json ? json.ToString() : item.Value;
            }
            World.AdminManager.SendThing(datas);
            var result = new SendThingsReturnDto();
            result.FillFrom(datas);
            return result;
        }
    }

}