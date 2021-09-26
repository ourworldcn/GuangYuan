using GuangYuan.GY001.BLL;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace OW.Game.Mission
{
    /// <summary>
    /// 任务成就管理器的配置数据类。
    /// </summary>
    public class GameMissionManagerOptions
    {
        public GameMissionManagerOptions()
        {

        }
    }

    /// <summary>
    /// 成就的辅助视图数据对象。
    /// </summary>
    public class GameaChieveView
    {
        /// <summary>
        /// 送物品的前缀。
        /// </summary>
        const string TidPrefix = "mtid";

        public GameaChieveView(VWorld world, GameItemTemplate template)
        {
            _Template = template;
            _World = world;
        }
        VWorld _World;

        /// <summary>
        /// 存储模板对象。
        /// </summary>
        GameItemTemplate _Template;

        List<(decimal, GameItem)> _Metrics;
        /// <summary>
        /// 指标值的数据。按 Item1上升排序。
        /// Item1=指标值。Item2=送的物品对象模板Id。Item3=送的对象的数量。
        /// </summary>
        public List<(decimal, GameItem)> Metrics
        {
            get
            {
                if (_Metrics is null)
                    lock (this)
                        if (_Metrics is null)
                        {
                            var vals = _Template.Properties.Keys.Where(c => c.StartsWith(TidPrefix)).Select(c => decimal.Parse(c[TidPrefix.Length..])); //指标值的集合
                            _Metrics = new List<(decimal, GameItem)>();
                            foreach (var item in vals)  //对每个指标值给出辅助元组
                            {

                            }
                        }
                return _Metrics;
            }
        }
    }

    /// <summary>
    /// 任务/成就管理器。
    /// </summary>
    public class GameMissionManager : GameManagerBase<GameMissionManagerOptions>
    {
        public GameMissionManager()
        {
            Initialize();
        }

        public GameMissionManager(IServiceProvider service) : base(service)
        {
            Initialize();
        }

        public GameMissionManager(IServiceProvider service, GameMissionManagerOptions options) : base(service, options)
        {
            Initialize();
        }

        private void Initialize()
        {
            _MissionTemplates = new Lazy<List<GameItemTemplate>>(() => World.ItemTemplateManager.GetTemplates(c => c.CatalogNumber == 51).ToList(),
                LazyThreadSafetyMode.ExecutionAndPublication);


        }

        private Lazy<List<GameItemTemplate>> _MissionTemplates;
        /// <summary>
        /// 与成就任务相关的所有模板。
        /// </summary>
        public List<GameItemTemplate> MissionTemplates => _MissionTemplates.Value;

        Dictionary<Guid, decimal[]> _TId2Values;

        /// <summary>
        /// 扫描发生变化的任务数据。
        /// </summary>
        /// <param name="gChar"></param>
        /// <param name="tid">相关的任务/成就模板Id。</param>
        /// <param name="newCount">新的指标值。</param>
        /// <returns>成功更新了成就的指标值则返回true,false没有找到指标值或指标值没有变化。</returns>
        public bool SetMetricsChange(GameChar gChar, Guid tid, decimal newCount)
        {
            var result = false;
            var logger = Service.GetService<ILogger<GameMissionManager>>();
            var gu = gChar?.GameUser;
            var dwUser = World.CharManager.LockAndReturnDisposer(gu);
            if (dwUser is null)
            {
                logger?.LogWarning($"无法扫描指定角色的成就变化数据,ErrorCode={VWorld.GetLastError()},ErrorMessage={VWorld.GetLastErrorMessage()}");
                return false;
            }
            var slot = gChar.GetRenwuSlot();    //任务/成就槽对象
            var missionObj = slot.Children.FirstOrDefault(c => c.TemplateId == tid);    //任务/成就的数据对象
            if (tid == ProjectMissionConstant.玩家等级成就)
            {
                if (missionObj.Count != newCount)   //若确实发生变化了
                {

                }
            }
            else if (tid == ProjectMissionConstant.关卡成就)
            {
            }
            else if (tid == ProjectMissionConstant.坐骑最高等级成就)
            {
            }
            else if (tid == ProjectMissionConstant.LV20坐骑数量)
            {
            }
            else if (tid == ProjectMissionConstant.关卡模式总战力成就)
            {
            }
            else if (tid == ProjectMissionConstant.纯种坐骑数量成就)
            {
            }
            else if (tid == ProjectMissionConstant.孵化成就)
            {
            }
            else if (tid == ProjectMissionConstant.最高资质成就)
            {
            }
            else if (tid == ProjectMissionConstant.最高神纹等级成就)
            {
            }
            else if (tid == ProjectMissionConstant.神纹突破次数成就)
            {
            }
            else if (tid == ProjectMissionConstant.累计访问好友天次成就)
            {
            }
            else if (tid == ProjectMissionConstant.累计塔防模式次数成就)
            {
            }
            else if (tid == ProjectMissionConstant.PVP进攻成就)
            {
            }
            else if (tid == ProjectMissionConstant.PVP防御成就)
            {
            }
            else if (tid == ProjectMissionConstant.PVP助战成就)
            {
            }
            else if (tid == ProjectMissionConstant.方舟成就)
            {
            }
            else if (tid == ProjectMissionConstant.炮塔成就)
            {
            }
            else if (tid == ProjectMissionConstant.陷阱成就)
            {
            }
            else if (tid == ProjectMissionConstant.旗帜成就)
            {
            }
            else //若不认识该槽
            {

            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="missionSlot"></param>
        /// <param name="guid">任务/成就模板Id。</param>
        /// <param name="newValue"></param>
        void SetNewValue(GameItem missionSlot, Guid tid, decimal newValue)
        {
            //mcid5af7a4f2-9ba9-44e0-b368-1aa1bd9aed6d=10;50;100,...
            var mObj = missionSlot.Children.FirstOrDefault(c => c.TemplateId == tid);   //任务/成就对象
            var keyName = $"mcid{mObj.Id}"; //键名
            var template = missionSlot.Template;    //模板数据
            var oldVal = missionSlot.Properties.GetStringOrDefault(keyName);   //原值
            var lst = oldVal.Split(OwHelper.SemicolonArrayWithCN, StringSplitOptions.RemoveEmptyEntries).Select(c => decimal.Parse(c)); //级别完成且未领取的指标值。

            missionSlot.Properties[keyName] = $"{oldVal};{newValue}";
        }
    }

    /// <summary>
    /// 与任务/成就系统相关的扩展方法封装类。
    /// </summary>
    public static class GameMissionExtensions
    {
        /// <summary>
        /// 获取任务/成就槽。
        /// </summary>
        /// <param name="gChar"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameItem GetRenwuSlot(this GameChar gChar) => gChar.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.RenwuSlotTId);
    }

}
