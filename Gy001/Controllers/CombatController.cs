using AutoMapper;
using GuangYuan.GY001.BLL;
using GuangYuan.GY001.BLL.Specific;
using GuangYuan.GY001.UserDb;
using GuangYuan.GY001.UserDb.Combat;
using Gy001.Controllers;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OW.Game;
using OW.Game.Store;
using System;
using System.Linq;

namespace GY2021001WebApi.Controllers
{
    /// <summary>
    /// 战斗相关的操作。
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
                Template = World.ItemTemplateManager.GetTemplateFromeId(OwConvert.ToGuid(model.DungeonId)),
            };
            cbm.StartCombat(data);
            var result = new CombatStartReturnDto();
            result.FillFrom(data);
            if (!result.HasError)
            {
                result.TemplateId = data.Template?.Id.ToBase64String();
                var mapper = cbm.World.Service.GetRequiredService<IMapper>();
                result.Changes.AddRange(data.PropertyChanges.Select(c => mapper.Map<GamePropertyChangeItemDto>(c)));
            }
            return result;
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
            //#if DEBUG
            try
            {
                throw new ArgumentException("sdjks");
            }
            catch (Exception)
            {
                return new CombatEndReturnDto() { HasError = true, ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS };
            }
            //#endif
            EndCombatData result = null;
            try
            {
                result = new EndCombatData()
                {
                    GameChar = World.CharManager.GetUserFromToken(OwConvert.ToGuid(model.Token))?.CurrentChar,
                    Template = World.ItemTemplateManager.GetTemplateFromeId(OwConvert.ToGuid(model.DungeonId)),
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
                    result.DebugMessage = err.Message + " @ " + err.StackTrace;
                }
            }
            var mapper = World.GetMapper();
            return mapper.Map(result);
        }

        /// <summary>
        /// 开始一场pvp战斗。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<CombatStartPvpReturnDto> CombatStartPvp(CombatStartPvpParamsDto model)
        {
            var result = new CombatStartPvpReturnDto();
            var datas = new StartCombatPvpData(World, model.Token)
            {
                DungeonId = OwConvert.ToGuid(model.DungeonId),
                CombatId = OwConvert.ToGuid(model.CombatId),
                OldCombatId = OwConvert.TryToGuid(model.OldCombatId, out var oldId) ? oldId : Guid.Empty,
            };
            World.CombatManager.StartCombatPvp(datas);
            result.FillFrom(datas);
            if (!result.HasError)
            {
                var mapper = World.Service.GetRequiredService<IMapper>();
                result.Changes.AddRange(datas.PropertyChanges.Select(c => mapper.Map<GamePropertyChangeItemDto>(c)));
                result.GameCombatId = datas.CombatId.ToBase64String();
            }
            return result;
        }

        /// <summary>
        /// PVP战斗结算。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<CombatEndPvpReturnDto> CombatEndPvp(CombatEndPvpParamsDto model)
        {
            var result = new CombatEndPvpReturnDto();
            using var datas = new EndCombatPvpWorkData(World, model.Token)
            {
                CombatId = OwConvert.ToGuid(model.CombatId),
                Now = DateTime.UtcNow,
                MainRoomRhp = model.MainRoomRhp,
                WoodRhp = model.WoodRhp,
                GoldRhp = model.GoldRhp,
                StoreOfWoodRhp = model.StoreOfWoodRhp,
                DestroyCountOfWoodStore = model.DestroyCountOfWoodStore,
            };
            datas.DestroyTIds.AddRange(model.Destroy.Select(c => (ValueTuple<Guid, decimal>)c));
            var mapper = World.Service.GetRequiredService<IMapper>();
#if DEBUG
            if (model.Booty.Count <= 0)
            {
                model.Booty.Add(new GameItemDto()
                {
                    Id = Guid.NewGuid().ToBase64String(),
                    ExtraGuid = new Guid("{ac7d593c-ce82-4642-97a3-14025da633e4}").ToBase64String(),    //基因蛋
                    Count = 100,
                });
            }
#endif
            datas.Booty.AddRange(model.Booty.Select(c =>
            {
                var result = mapper.Map<GameItem>(c);
                result.Count = c.Count;
                return result;
            }));

            World.CombatManager.EndCombatPvp(datas);
            result.FillFrom(datas);
            if (!result.HasError)
            {
                result.Combat = mapper.Map<GameCombatDto>(datas.Combat);

                result.Changes.AddRange(datas.PropertyChanges.Select(c => mapper.Map<GamePropertyChangeItemDto>(c)));
            }
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
                CombatId = OwConvert.ToGuid(model.CombatId),
            };
            World.CombatManager.GetCombat(datas);
            result.FillFrom(datas);
            if (!result.HasError)
            {
                var mapper = World.Service.GetService<IMapper>();
                result.CombatObject = mapper.Map<GameCombatDto>(datas.Combat);
            }
            return result;
        }

        /// <summary>
        /// 放弃pvp请求协助。目前视同协助失败。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut]
        public ActionResult<AbortPvpResultDto> AbortPvp(AbortPvpParamsDto model)
        {
            AbortPvpResultDto result = new AbortPvpResultDto();
            var oldCombatId = OwConvert.ToGuid(model.CombatId); //原始战斗Id
            var db = HttpContext.RequestServices.GetService<GY001UserContext>();
            using var datas = new AbortPvpDatas(World, model.Token)
            {
                UserDbContext = db,
                CombatId = oldCombatId,
            };
            World.CombatManager.AbortPvp(datas);
            result.HasError = datas.HasError;
            result.ErrorCode = datas.ErrorCode;
            result.DebugMessage = datas.DebugMessage;
            return result;
        }
    }

}