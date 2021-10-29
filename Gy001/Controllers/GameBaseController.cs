using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using OW.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gy001.Controllers
{
    /// <summary>
    /// WebApi通讯封装的游戏服务器接口的控制器基类。
    /// </summary>
    public class GameBaseController : ControllerBase
    {


        public GameBaseController()
        {
        }


        public GameBaseController(VWorld world)
        {
            _World = world;
        }

        VWorld _World;

        public VWorld World { get => _World ??= HttpContext.RequestServices.GetService<VWorld>(); set => _World = value; }
    }
}
