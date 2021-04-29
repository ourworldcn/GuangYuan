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

        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="serviceProvider"></param>
        public CombatManager(IServiceProvider serviceProvider)
        {
            _ServiceProvider = serviceProvider;
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
        }

        #endregion 构造函数

        Dictionary<Guid, List<Guid>> _Dungeon = new Dictionary<Guid, List<Guid>>();

        /// <summary>
        /// 副本信息。键是大关的模板Id,值是小关口模板Id的集合。
        /// </summary>
        public Dictionary<Guid, List<Guid>> Dungeon
        {
            get
            {
                lock (this)
                    if (null != _Dungeon)
                    {
                        var gitm = _ServiceProvider.GetService<GameItemTemplateManager>();
                        var coll = from tmp in gitm.Id2Template.Values
                                   where tmp.GId / 1000 == 7
                                   group tmp by new { typ = (int)tmp.Properties["typ"], mis = (int)tmp.Properties["mis"] } into g
                                   select new { mis = g.First(c => (int)c.Properties["sec"] == -1), sec = g.Where(c => (int)c.Properties["sec"] != -1).OrderBy(c => (int)c.Properties["sec"]) };
                        _Dungeon = coll.ToDictionary(c => c.mis.Id, c => c.sec.Select(subc => subc.Id).ToList());
                    }
                return _Dungeon;
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
