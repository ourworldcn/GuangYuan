using GuangYuan.GY001.BLL;
using GuangYuan.GY001.UserDb;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace OW.Game.Mission
{
    /// <summary>
    /// 成就数据视图。
    /// </summary>
    public class CharAchieveView : GameCharWorkDataBase
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
        public GameItem ObjectItem => _ObjectItem ??= Slot.Children.FirstOrDefault(c => c.TemplateId == TemplateId);

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
                    const string tid = "tid";
                    const string count = "count";
                    const string htid = "htid";
                    const string btid = "btid";
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
}
