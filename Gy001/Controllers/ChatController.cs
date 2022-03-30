using GuangYuan.GY001.BLL;
using GuangYuan.GY001.BLL.GeneralManager;
using GY2021001WebApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OW.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gy001.Controllers
{
    /// <summary>
    /// 聊天功能控制器。
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ChatController : GameBaseController
    {
        public ChatController(VWorld world) : base(world)
        {
        }

        /// <summary>
        /// 获取当前用户的暂存消息。此接口建议1秒到数秒之间调用一次。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<GetMessagesReturnDto> GetMessages(GetMessagesParamsDto model)
        {
            var result = new GetMessagesReturnDto();
            var token = OwConvert.ToGuid(model.Token);
            using var gContext = new GetMessageContext()
            {
                CharId = World.CharManager.GetUserFromToken(token).CurrentChar.Id.ToString(),
            };
            World.ChatManager.GetMessages(gContext);
            result.FillFrom(gContext);
            if (!result.HasError)
            {
                result.Messages.AddRange(gContext.Messages.Select(c =>
                {
                    var tmp = (ChatMessageDto)c;
                    //if (OwConvert.TryToGuid(c.Sender, out var gcId))
                    //{
                    //    var gc = World.CharManager.GetCharFromId(gcId);
                    //    if (null != gc)
                    //    {
                    //        using var dw = World.CharManager.LockAndReturnDisposer(gc.GameUser);
                    //        if (dw != null)
                    //        {
                    //            tmp.DisplayName = gc.DisplayName;
                    //            tmp.IconIndex = (int)gc.Properties.GetDecimalOrDefault("charIcon", 0);   //客户端要求再没有该键时返回默认值0
                    //        }
                    //    }
                    //}
                    return tmp;
                }));
            }
            return result;
        }

        /// <summary>
        /// 发送消息的接口。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<SendMessagesReturnDto> SendMessages(SendMessagesParamsDto model)
        {
            var result = new SendMessagesReturnDto();
            var token = OwConvert.ToGuid(model.Token);
            using var dwUser = World.CharManager.LockAndReturnDisposer(token, out var gu);
            if (dwUser is null)
            {
                result.HasError = true;
                result.ErrorCode = VWorld.GetLastError();
                result.DebugMessage = VWorld.GetLastErrorMessage();
                return result;
            }
            using var gContext = new SendMessageContext()
            {
                CharId = World.CharManager.GetUserFromToken(token).CurrentChar.Id.ToString(),
            };
            foreach (var msg in model.Messages)
            {
                result.HasError = false; result.ErrorCode = ErrorCodes.NO_ERROR; result.DebugMessage = string.Empty;
                gContext.Message = msg.Message;
                gContext.ChannelId = NormChannelId(msg.ChannelId);
                gContext.ExString = $"{gu.CurrentChar.ClientProperties.GetValueOrDefault("charIcon", "0")},{gu.CurrentChar.DisplayName}";
                World.ChatManager.SendMessages(gContext);
            }
            result.FillFrom(gContext);
            return result;
        }

        /// <summary>
        /// 规范化频道id。
        /// </summary>
        /// <param name="channelId"></param>
        /// <returns></returns>
        string NormChannelId(string channelId)
        {
            var ary = channelId.Split(OwHelper.CommaArrayWithCN);
            if (ary.Length == 2)
            {
                var coll = from tmp in ary
                           where OwConvert.TryToGuid(tmp, out var id)
                           let id = OwConvert.ToGuid(tmp)
                           orderby id
                           select id;
                if (coll.Count() == 2)
                    return string.Join(',', coll.Select(c => c.ToString()));
            }
            return channelId;
        }

    }

}
