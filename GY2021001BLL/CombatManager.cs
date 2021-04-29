using GY2021001DAL;
using Gy2021001Template;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GY2021001BLL
{
    /// <summary>
    /// 战斗管理器的配置类。
    /// </summary>
    public class CombatManagerOptions
    {
        public CombatManagerOptions()
        {

        }

        /// <summary>
        /// 战斗开始时被调用。
        /// </summary>
        public Func<IServiceProvider, GameChar, bool> CombatStart { get; set; }

        /// <summary>
        /// 战斗结束时被调用。返回true表示
        /// </summary>
        public Func<IServiceProvider, GameChar, IList<GameItem>, bool> CombatEnd { get; set; }
    }

    /// <summary>
    /// 战斗管理器。
    /// </summary>
    public class CombatManager
    {
        private readonly IServiceProvider _ServiceProvider;
        private readonly CombatManagerOptions _Options;

        #region 构造函数

        /// <summary>
        /// 构造函数。
        /// </summary>
        public CombatManager()
        {
            Initialize();
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="serviceProvider"></param>
        public CombatManager(IServiceProvider serviceProvider)
        {
            _ServiceProvider = serviceProvider;
            Initialize();
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="options"></param>
        public CombatManager(IServiceProvider serviceProvider, CombatManagerOptions options)
        {
            _ServiceProvider = serviceProvider;
            _Options = options;
            Initialize();
        }

        #endregion 构造函数
        private void Initialize()
        {

        }

        List<GameItemTemplate> _Dungeons = new List<GameItemTemplate>();

        /// <summary>
        /// 所有副本信息。
        /// </summary>
        public IReadOnlyList<GameItemTemplate> Dungeons
        {
            get
            {
                lock (this)
                    if (null == _Dungeons)
                    {
                        var gitm = _ServiceProvider.GetService<GameItemTemplateManager>();
                        var coll = from tmp in gitm.Id2Template.Values
                                   where tmp.GId / 1000 == 7
                                   select tmp;
                        _Dungeons = coll.ToList();
                    }
                return _Dungeons;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="dungeon">场景。</param>
        /// <returns>true正常启动，false没有找到指定的场景或角色当前正在战斗。</returns>
        public bool StartCombat(GameChar gameChar, GameItemTemplate dungeon)
        {
            if (null != gameChar.CurrentDungeonId && gameChar.CurrentDungeonId.HasValue)
                if (gameChar.CurrentDungeonId.Value != dungeon.Id)
                    return false;
            try
            {
                if (!_Options?.CombatStart?.Invoke(_ServiceProvider, gameChar) ?? true)
                    return false;
            }
            catch (Exception)
            {
            }
            gameChar.CurrentDungeonId = dungeon.Id;
            gameChar.CombatStartUtc = DateTime.UtcNow;
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameChar"></param>
        /// <param name="dungeon">场景。</param>
        /// <param name="gameItems">获取的收益。</param>
        /// <returns>true正常结束，false发生错误。</returns>
        public bool EndCombat(GameChar gameChar, GameItemTemplate dungeon, List<GameItem> gameItems, out string msg)
        {
            var gcm = _ServiceProvider.GetService<GameCharManager>();
            if (!gcm.Lock(gameChar.GameUser))
            {
                msg = "用户已经无效";
                return false;
            }
            try
            {
                if (null != gameChar.CurrentDungeonId && gameChar.CurrentDungeonId.HasValue)
                    if (gameChar.CurrentDungeonId.Value != dungeon.Id)
                    {
                        msg = "角色在另外一个场景中战斗。";
                        return false;
                    }
                try
                {
                    bool succ = _Options?.CombatEnd?.Invoke(_ServiceProvider, gameChar, gameItems) ?? true;
                    if (!succ)
                    {
                        msg = "收益错误。";
                        return false;
                    }
                }
                catch (Exception)
                {
                }
            }
            finally
            {
                gcm.Unlock(gameChar.GameUser);
            }
            msg = string.Empty;
            return true;
        }
    }
}
