using GY2021001DAL;
using Gy2021001Template;
using Microsoft.EntityFrameworkCore;
using OwGame;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GY2021001BLL
{
    /// <summary>
    /// 使用蓝图的数据。
    /// </summary>
    public class ApplyBluprintDatas
    {
        public ApplyBluprintDatas()
        {

        }

        /// <summary>
        /// 蓝图的模板Id。
        /// </summary>
        public Guid BlueprintId { get; set; }

        /// <summary>
        /// 角色对象。
        /// </summary>
        public GameChar GameChar { get; set; }

        /// <summary>
        /// 要执行的目标对象Id集合。目前仅有唯一元素，神纹的对象Id。
        /// </summary>
        public List<Guid> ObjectIds { get; set; }

        /// <summary>
        /// 要执行的次数。
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// 应用蓝图后，物品变化数据。
        /// </summary>
        public List<ChangesItem> ChangesItem { get; } = new List<ChangesItem>();

        /// <summary>
        /// 是否有错误。
        /// </summary>
        public bool HasError { get; set; }

        /// <summary>
        /// 调试信息，如果发生错误，这里给出简要说明。
        /// </summary>
        public string DebugMessage { get; set; }
    }

    public class SequencePropertyData
    {
        public static bool TryParse(string str, out SequencePropertyData sequenceProperty)
        {
            sequenceProperty = null;
            if (decimal.TryParse(str, out decimal resultDec))
            {
                sequenceProperty = new SequencePropertyData()
                {
                    Value = resultDec,
                };
                return true;
            }
            var ary = str.Split(OwHelper.ColonArrayWithCN);
            if (2 != ary.Length)
            {
                return false;
            }
            if (!OwHelper.AnalyseSequence(ary[1], out decimal[] vals))  //若分析获得序列
                return false;
            var leftAry = ary[0].Split(OwHelper.PathSeparatorChar, StringSplitOptions.RemoveEmptyEntries);
            string pn;
            Guid? keyId = null;
            if (leftAry.Length == 1)   //若只有属性名
                pn = leftAry[0];
            else if (leftAry.Length == 2) //若存在指定的父属性
            {
                if (!Guid.TryParse(leftAry[0], out Guid key))
                    return false;
                pn = leftAry[1];
                keyId = key;
            }
            else
                return false;
            sequenceProperty = new SequencePropertyData()
            {
                Values = vals,
                PropertyName = pn,
                KeyId = keyId,
            };
            return true;
        }

        public SequencePropertyData()
        {

        }

        public decimal? Value { get; set; }

        /// <summary>
        /// 序列。
        /// </summary>
        public decimal[] Values { get; set; }

        /// <summary>
        /// 属性名。
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// 属性绑定的对象Id。
        /// </summary>
        public Guid? KeyId { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="things"></param>
        /// <returns></returns>
        public decimal GetValue(IEnumerable<(Guid, GameThingBase)> things)
        {
            if (Value.HasValue)
                return Value.Value;
            if (KeyId.HasValue)
            {
                var obj = things.First(c => c.Item1 == KeyId.Value);
                return (decimal)obj.Item2.Properties.GetValueOrDefault(PropertyName, 0m);
            }
            return 0;
        }
    }

    public class GameCondition
    {
        public string Left { get; set; }

        public string Operator { get; set; }

        public string Right { get; set; }
    }

    public class GameConditionCollection
    {
        private readonly string[] RelationshipArray = new string[] { "=", ">", "<" };

        private readonly string[] MultRelationshipArray = new string[] { "==", ">=", "<=" };

        readonly string pat = @"[^\=\<\>]";

        private readonly string comparePattern = @"(?<left>[^\=\<\>]+)(?<op>[\=\<\>]{1,2})(?<right>[^\=\<\>]+)[\,，]?";

        public static bool TryParse(string str, out GameConditionCollection sequenceProperty)
        {
            var ary = str.Split(OwHelper.CommaArrayWithCN, StringSplitOptions.RemoveEmptyEntries);
            sequenceProperty = null;
            
            return true;
        }

        public GameConditionCollection()
        {

        }
    }

    /// <summary>
    /// 蓝图管理器配置数据。
    /// </summary>
    public class BlueprintManagerOptions
    {
        public BlueprintManagerOptions()
        {

        }

        public Func<IServiceProvider, ApplyBluprintDatas, bool> DoApply { get; set; }
    }

    /// <summary>
    /// 蓝图管理器。
    /// </summary>
    public class BlueprintManager : GameManagerBase<BlueprintManagerOptions>
    {
        public BlueprintManager()
        {
            Initialize();
        }

        public BlueprintManager(IServiceProvider service) : base(service)
        {
            Initialize();
        }

        public BlueprintManager(IServiceProvider service, BlueprintManagerOptions options) : base(service, options)
        {
            Initialize();
        }

        private void Initialize()
        {
            lock (ThisLocker)
                _InitializeTask ??= Task.Run(() =>
                {
                    _Id2BlueprintTemplate = Context.Set<BlueprintTemplate>().ToDictionary(c => c.Id);
                });
        }

        Task _InitializeTask;

        private DbContext _DbContext;

        public DbContext Context { get => _DbContext ??= World.CreateNewTemplateDbContext(); }

        Dictionary<Guid, BlueprintTemplate> _Id2BlueprintTemplate;

        public Dictionary<Guid, BlueprintTemplate> Id2BlueprintTemplate
        {
            get
            {
                _InitializeTask.Wait();
                return _Id2BlueprintTemplate;
            }
        }

        public void ApplyBluprint(ApplyBluprintDatas datas)
        {
            _InitializeTask.Wait();
            if (!World.CharManager.Lock(datas.GameChar.GameUser))    //若无法锁定用户
            {
                datas.HasError = true;
                datas.DebugMessage = $"指定用户无效。";
                return;
            }
            try
            {
                if (datas.ObjectIds.Count != 1)
                {
                    datas.HasError = true;
                    datas.DebugMessage = $"目标对象过多";
                    return;
                }
                var objectId = datas.ObjectIds[0];
                var slot = datas.GameChar.GameItems.First(c => c.TemplateId == ProjectConstant.ShenWenSlotId);  //神纹装备槽
                var obj = slot.Children.FirstOrDefault(c => c.Id == objectId);    //目标物品对象,目前是神纹
                if (null == obj)    //找不到指定的目标物品
                {
                    datas.HasError = true;
                    datas.DebugMessage = $"找不到指定的目标物品。";
                    return;
                }

                if (datas.BlueprintId == ProjectConstant.ShenwenLvUpBlueprint)  //若要进行神纹升级
                {
                    var info = GetShenwenInfo(datas.GameChar, obj);
                    if (info.Level + datas.Count > info.MaxLv)
                    {
                        datas.HasError = true;
                        datas.DebugMessage = $"已达最大等级或升级次数过多。";
                        return;
                    }
                    var daojuSlot = datas.GameChar.GameItems.First(c => c.TemplateId == ProjectConstant.DaojuBagSlotId);
                    //var suipian = from tmp in daojuSlot.Children
                    //              let gid=

                    var lv = Convert.ToInt32(obj.Properties[ProjectConstant.LevelPropertyName]);

                }
                else if (datas.BlueprintId == ProjectConstant.ShenWenTupoBlueprint) //若要进行神纹突破
                {
                }
                else
                {
                    datas.HasError = true;
                    datas.DebugMessage = $"找不到指定蓝图的Id:{datas.BlueprintId}";
                }
            }
            finally
            {
                World.CharManager.Unlock(datas.GameChar.GameUser);
            }
        }

        public (int Level, int MaxLv, int TupoCount) GetShenwenInfo(GameChar gameChar, GameItem shenwen)
        {
            var tt = World.ItemTemplateManager.GetTemplateFromeId(shenwen.TemplateId);  //模板
            int lv = 0; //等级
            if (!shenwen.Properties.TryGetValue(ProjectConstant.LevelPropertyName, out object lvObj) && lvObj is int lvVal)
                lv = lvVal;
            int tp = 0; //突破次数
            //if (!shenwen.Properties.TryGetValue(ProjectConstant.ShenwenTupoCountPropertyName, out object tpObj) && lvObj is int tpVal)
            //    tp = tpVal;
            //else
            //    shenwen.Properties[ProjectConstant.ShenwenTupoCountPropertyName] = (decimal)tp; //加入属性
            int maxLv;  //当前突破次数下最大等级。
            BlueprintTemplate tbp = Id2BlueprintTemplate[ProjectConstant.ShenWenTupoBlueprint];
            var seq = (decimal[])tbp.Properties["ssl"];
            maxLv = (int)seq[lv];
            return (lv, maxLv, tp);
        }
    }
}
