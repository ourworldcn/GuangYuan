using GuangYuan.GY001.BLL;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using OW.Game.Item;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;

namespace OW.Game.Mission
{
    /// <summary>
    /// 成就数据视图。
    /// </summary>
    public class CharAchieveView : GameCharGameContext
    {
        public CharAchieveView([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public CharAchieveView([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {

        }

        public CharAchieveView([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 成就模板Id。
        /// </summary>
        public Guid TemplateId { get; set; }

        private GameItem _Slot;
        /// <summary>
        /// 成就槽对象。
        /// </summary>
        public GameItem Slot => _Slot ??= GameChar.GetRenwuSlot();

        private GameItem _ObjectItem;
        /// <summary>
        /// 特定的成就对象。
        /// </summary>
        public GameItem ObjectItem => _ObjectItem ??= Slot.Children.FirstOrDefault(c => c.ExtraGuid == TemplateId);

        private List<decimal> _TemplateMetrics;
        /// <summary>
        /// 模板指定指标值。
        /// </summary>
        public List<decimal> TemplateMetrics
        {
            get
            {
                if (_TemplateMetrics is null)
                {
                    var tt = World.ItemTemplateManager.GetTemplateFromeId(TemplateId)?.Properties;
                    var gis = World.ItemManager.ToGameItems(tt, "m");
                    _TemplateMetrics = new List<decimal> { };
                    var ary = tt.GetValuesWithoutPrefix("m");
                    foreach (var item in ary)
                    {

                    }
                }
                return _TemplateMetrics;
            }
        }


        public void SetMetrics(decimal newVal)
        {
        }

        public override void Save()
        {
            base.Save();
        }
    }

    public class GameMission
    {
        public static GameMission FromChar(GameChar gameChar)
        {
            var slot = gameChar.GetRenwuSlot();
            var result = slot.GetJsonObject<GameMission>();
            result._Slot = slot;
            return result;
        }

        public GameMission()
        {

        }

        private GameItem _Slot;
        internal GameItemTemplateManager Manager { get; set; }

        private List<GameMissionItem> _Items;
        [JsonIgnore]
        public IReadOnlyCollection<GameMissionItem> Items => _Items ??= _Slot.Children.Select(c => GameMissionItem.From(c, Manager)).ToList();

        /// <summary>
        /// 创建一个新任务记录项。
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        internal GameMissionItem Create(Guid guid)
        {
            var gi = new GameItem() { ExtraGuid = guid, Parent = _Slot, ParentId = _Slot.Id };
            _Slot.Children.Add(gi);
            var result = GameMissionItem.From(gi, Manager);
            if (result != null)
                _Items?.Add(result);
            return result;
        }

        public GameMissionItem GetOrCreate(Guid tId)
        {
            var result = Items.FirstOrDefault(c => c.TId == tId);
            if (result is null)  //若没找到任务项
            {
                result = Create(tId);
            }
            return result;
        }
    }

    public class GameMissionItem
    {

        public static GameMissionItem From(GameItem gameItem, GameItemTemplateManager manager)
        {
            var result = gameItem.GetJsonObject<GameMissionItem>();
            result._GameItem = gameItem;
            if (manager.Id2Mission.TryGetValue(gameItem.ExtraGuid, out var tt))
                result.Template = tt;
            else
            {
                OwHelper.SetLastError(ErrorCodes.ERROR_BAD_ARGUMENTS);
                OwHelper.SetLastErrorMessage($"找不到指定id的任务模板,id={gameItem.ExtraGuid}");
                return null;
            }
            return result;
        }

        public GameMissionItem()
        {

        }

        private GameItem _GameItem;

        /// <summary>
        /// 已经完成的次数
        /// </summary>
        public int ComplateCount { get => (int)_GameItem.ExtraDecimal; set => _GameItem.ExtraDecimal = value; }

        /// <summary>
        /// 任务的模板id。
        /// </summary>
        public Guid TId { get => _GameItem.ExtraGuid; }

        /// <summary>
        /// 最后修改日期。最后一次完成任务的Utc时间。
        /// </summary>
        public DateTime LastUtc { get; set; }

        GameMissionTemplate _Template;
        [JsonIgnore]
        public GameMissionTemplate Template { get; set; }
    }
}
