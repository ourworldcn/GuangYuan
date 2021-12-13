﻿using GuangYuan.GY001.BLL;
using Gy001.Controllers;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OW.Game;
using System.Buffers;
using System.IO;
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
        private readonly VWorld _World;

        public AdminController(VWorld world)
        {
            _World = world;
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
            using var datas = new CloneUserDatas(_World, model.Token)
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
        /// <returns></returns>
        [HttpPost]
        public ActionResult ImportUsers(IFormFile file)
        {
            using (var stream = file.OpenReadStream())
            {
                var trustedFileNameForFileStorage = Path.GetRandomFileName();
                using var sr = new StreamReader(stream);

                var r = sr.ReadLine();
                //await WriteFileAsync(stream, Path.Combine(_targetFilePath, trustedFileNameForFileStorage));
            }
            return Ok();
        }

        public class ExportUsersParaamsDto
        {
            public string Prefix { get; set; }

            public int StartIndex { get; set; }

            public int EndIndex { get; set; }
        }
        /// <summary>
        /// 导出用户。
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        //[ProducesResponseType(typeof(FileResult), Status200OK)]
        public FileResult ExportUsers(ExportUsersParaamsDto model)
        {
            string str = "test:this is a test of downfile";
            MemoryStream ms = new MemoryStream();
            var buff = Encoding.UTF8.GetBytes(str);
            ms.Write(buff, 0, buff.Length);
            ms.Seek(0, SeekOrigin.Begin);

            var actionresult = new FileStreamResult(ms, "application/text");
            actionresult.FileDownloadName = "zCarinfos.txt";
            return actionresult;
        }


    }

}