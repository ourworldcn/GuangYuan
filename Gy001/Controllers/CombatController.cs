﻿using GuangYuan.GY001.BLL;
using GuangYuan.GY001.UserDb;
using GuangYuan.GY001.UserDb.Combat;
using Gy001.Controllers;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OW.Game;
using System;
using System.Linq;

namespace GY2021001WebApi.Controllers
{
    /// <summary>
    /// 战斗相关的操作。这里的功能以后可能依据需求改为Udp。
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class CombatController : GameBaseController
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="world"></param>
        public CombatController(VWorld world) : base(world)
        {
        }

        /// <summary>
        /// 通知服务器，客户端开始一场战斗。
        /// </summary>
        /// <param name="model">参见 CombatStartParamsDto</param>
        /// <returns>参见 CombatStartReturnDto</returns>
        [HttpPost]
        public ActionResult<CombatStartReturnDto> Start(CombatStartParamsDto model)
        {
            var cbm = World.CombatManager;
            using var data = new StartCombatData(World, model.Token)
            {
                Template = World.ItemTemplateManager.GetTemplateFromeId(GameHelper.FromBase64String(model.DungeonId)),
            };
            cbm.StartCombat(data);
            return (CombatStartReturnDto)data;
        }

        /// <summary>
        /// 通知服务器客户端结束了一场战斗。
        /// 如果正常进入下一关，可以不必调用启动战斗的接口。
        /// </summary>
        /// <param name="model">参见 CombatEndParamsDto</param>
        /// <returns>参见 CombatEndReturnDto</returns>
        [HttpPost]
        public ActionResult<CombatEndReturnDto> End(CombatEndParamsDto model)
        {
            EndCombatData result = null;
            try
            {
                result = new EndCombatData()
                {
                    GameChar = World.CharManager.GetUserFromToken(GameHelper.FromBase64String(model.Token))?.CurrentChar,
                    Template = World.ItemTemplateManager.GetTemplateFromeId(GameHelper.FromBase64String(model.DungeonId)),
                    EndRequested = model.EndRequested,
                    OnlyMark = model.OnlyMark,
                    IsWin = model.IsWin,
                };
                if (null != model.GameItems)
                    result.GameItems.AddRange(model.GameItems.Select(c => (GameItem)c));
                World.CombatManager.EndCombat(result);
            }
            catch (Exception err)
            {
                if (null != result)
                {
                    result.HasError = true;
                    result.DebugMessage = err.Message;
                }
            }
            return (CombatEndReturnDto)result;
        }

#if DEBUG
        [HttpPost]
        public ActionResult<CombatEndReturnDto> Test()
        {
            return null;
        }
#endif

        /// <summary>
        /// PVP战斗结算。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<CombatEndPvpReturnDto> CombatEndPvp(CombatEndPvpParamsDto model)
        {
            var result = new CombatEndPvpReturnDto();
            using var datas = new EndCombatPvpWorkData(World, model.Token, GameHelper.FromBase64String(model.OtherGCharId))
            {
                CombatId = GameHelper.FromBase64String(model.CombatId),
                Now = DateTime.UtcNow,
                DungeonId = GameHelper.FromBase64String(model.DungeonId),
                IsWin = model.IsWin,
            };
            datas.DestroyTIds.AddRange(model.Destroies.Select(c => (ValueTuple<Guid, decimal>)c));
            World.CombatManager.EndCombatPvp(datas);
            result.HasError = datas.HasError;
            result.DebugMessage = datas.ErrorMessage;
            result.ChangesItems.AddRange(datas.ChangeItems.Select(c => (ChangesItemDto)c));
            result.Combat = datas.Combat;
            return result;
        }

        /// <summary>
        /// 获取指定的战斗对象。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut]
        public ActionResult<GetCombatObjectResultDto> GetCombatObject(GetCombatObjectParamsDto model)
        {
            var result = new GetCombatObjectResultDto();
            using GetCombatDatas datas = new GetCombatDatas(World, model.Token)
            {
                CombatId = GameHelper.FromBase64String(model.CombatId),
            };
            World.CombatManager.GetCombat(datas);
            result.HasError = datas.HasError;
            result.ErrorCode = datas.ErrorCode;
            result.DebugMessage = datas.ErrorMessage;
            if (!datas.HasError)    //若成功返回
            {
                result.CombatObject = datas.CombatObject;
                var view = new WarNewspaperView(datas.CombatObject, World.Service);
                result.AttackerMounts.AddRange(view.GetAttackerMounts().Select(c => (GameItemDto)c));
                result.DefenserMounts.AddRange(view.GetDefenserMounts().Select(c => (GameItemDto)c));
                result.Booty.AddRange(datas.UserContext.Set<GameBooty>().AsNoTracking().Where(c => c.ParentId == datas.CombatObject.Id).AsEnumerable().Select(c => (GameBootyDto)c));
            }
            return result;
        }

        /// <summary>
        /// 放弃pvp的反击和请求协助的权力。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut]
        public ActionResult<AbortPvpResultDto> AbortPvp(AbortPvpParamsDto model)
        {
            AbortPvpResultDto result = new AbortPvpResultDto();
            return result;
        }
    }

    public class AbortPvpParamsDto
    {
    }

    public class AbortPvpResultDto
    {
    }
}