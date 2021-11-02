﻿using GuangYuan.GY001.BLL;
using Gy001.Controllers;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using OW.Game;
using System.Linq;

namespace GY2021001WebApi.Controllers
{
    /// <summary>
    /// 与账号相关的操作控制器。
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AdminController : GameBaseController
    {
        VWorld _World;

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
                Count=model.Count,
                LoginNamePrefix=model.LoginNamePrefix,
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
    }

}