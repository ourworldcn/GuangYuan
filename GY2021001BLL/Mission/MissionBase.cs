using GuangYuan.GY001.BLL;
using GuangYuan.GY001.UserDb;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace OW.Game.Mission
{
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
                    //foreach (var key in tt.Keys.Where(c => c.StartsWith(tidKey)))
                    //{
                    //    var indexStr = key[tidKey.Length..];
                    //}
                    //var coll = from tmp in tt
                    //           where tmp.Key.StartsWith(tidKey) //找到tid
                    //           let indexStr = tmp.Key[tidKey.Length..]  //尾号
                    //           let valid = string.IsNullOrEmpty(indexStr) || decimal.TryParse(indexStr, out _)  //尾号合法标志
                    //           where valid  //合法尾号
                    //           select tmp;
                }
                return _TemplateMetrics;
            }
        }


        public void SetMetrics(decimal newVal)
        {
        }

        public void Save()
        {

        }
    }
}
