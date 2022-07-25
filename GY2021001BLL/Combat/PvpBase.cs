using GuangYuan.GY001.BLL.Homeland;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using GuangYuan.GY001.UserDb.Combat;
using OW.Game;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace GuangYuan.GY001.BLL
{
    /// <summary>
    /// pvp结束战斗调用接口的数据封装类。
    /// </summary>
    public class EndCombatPvpWorkData : BinaryRelationshipGameContext
    {
        public EndCombatPvpWorkData([NotNull] IServiceProvider service, [NotNull] GameChar gameChar, Guid otherGCharId) : base(service, gameChar, otherGCharId)
        {
        }

        public EndCombatPvpWorkData([NotNull] VWorld world, [NotNull] GameChar gameChar, Guid otherGCharId) : base(world, gameChar, otherGCharId)
        {
        }

        public EndCombatPvpWorkData([NotNull] VWorld world, [NotNull] string token, Guid otherGCharId) : base(world, token, otherGCharId)
        {
        }

        /// <summary>
        /// 战斗对象唯一Id。从邮件的 mail.Properties["CombatId"] 属性中获取。
        /// 反击和协助才需要填写。直接pvp时可以省略。
        /// </summary>
        public Guid CombatId { get; set; }

        CombatReport _Combat;
        /// <summary>
        /// 获取战斗对象，如果找不到则设置错误信息。
        /// </summary>
        public CombatReport Combat
        {
            get
            {
                if (_Combat is null)
                {
                    var thing = UserDbContext.Set<VirtualThing>().Find(CombatId);
                    if (thing is null)
                    {
                        HasError = true;
                        ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                        ErrorMessage = $"找不到指定的战报对象，Id={CombatId}";
                    }
                    _Combat = thing.GetJsonObject<CombatReport>();
                }
                return _Combat;
            }
            set => _Combat = value;
        }

        /// <summary>
        /// 当前日期。
        /// </summary>
        public DateTime Now { get; set; }

        /// <summary>
        /// 关卡Id
        /// </summary>
        public Guid DungeonId { get; set; }

        private GameItemTemplate _DungeonTemplate;

        /// <summary>
        /// 关卡模板。
        /// </summary>
        public GameItemTemplate DungeonTemplate => _DungeonTemplate ??= World.ItemTemplateManager.GetTemplateFromeId(DungeonId);

        /// <summary>
        /// 是否胜利了。
        /// </summary>
        public bool IsWin { get; set; }

        /// <summary>
        /// 摧毁建筑的模板Id集合。
        /// </summary>
        public List<(Guid, decimal)> DestroyTIds { get; } = new List<(Guid, decimal)>();

        /// <summary>
        /// 规范化摧毁物数据。
        /// </summary>
        public void NormalizeDestroyTIds()
        {
            var coll = (from tmp in DestroyTIds
                        group tmp by tmp.Item1 into g
                        select (g.Key, g.Sum(c => c.Item2))).ToArray();
            if (coll.Length != DestroyTIds.Count)   //若有变化
            {
                DestroyTIds.Clear();
                DestroyTIds.AddRange(coll);
            }
        }

        /// <summary>
        /// 计算战利品。
        /// </summary>
        /// <param name="attackerBooty"></param>
        /// <param name="defencerBooty"></param>
        public void GetBooty(ICollection<GameBooty> attackerBooty, ICollection<GameBooty> defencerBooty)
        {
            var dHl = OtherChar.GetHomeland();
            var dYumi = dHl.GetAllChildren().FirstOrDefault(c => c.TemplateId == ProjectConstant.YumitianTId); //防御方玉米田

            var dMucaiShu = dHl.GetAllChildren().FirstOrDefault(c => c.TemplateId == ProjectConstant.MucaishuTId);   //防御方木材树
            var dMucai = OtherChar.GetMucai();  //防御方木材货币数
            var eventMng = World.EventsManager;
            var lvAtt = GameChar.Properties.GetDecimalOrDefault(World.PropertyManager.LevelPropertyName);   //进攻方等级
            var aMucai = new GameBooty() { CharId = GameChar.Id };   //攻击方木材
            aMucai.StringDictionary["tid"] = ProjectConstant.MucaiId.ToString();

            var aJinbi = new GameBooty() { CharId = GameChar.Id };   //攻击方金币
            aJinbi.StringDictionary["tid"] = ProjectConstant.JinbiId.ToString();

            var defMucai = new GameBooty() { CharId = OtherChar.Id };   //防御方木材
            defMucai.StringDictionary["tid"] = ProjectConstant.JinbiId.ToString();

            var defShu = new GameBooty() { CharId = OtherChar.Id };   //防御方木材树
            defShu.StringDictionary["tid"] = ProjectConstant.MucaishuTId.ToString();

            var defJinbi = new GameBooty() { CharId = OtherChar.Id };   //防御方金币
            defJinbi.StringDictionary["tid"] = ProjectConstant.JinbiId.ToString();

            var defYumi = new GameBooty() { CharId = OtherChar.Id };    //防御方玉米田
            defYumi.StringDictionary["tid"] = ProjectConstant.YumitianTId.ToString();

            var attJinbiBase = lvAtt * 100 + (dYumi.Count ?? 0) * 0.5m + dMucaiShu.Count.Value * 0.5m;   //进攻方获得金币的基数
            var attMucaiBase = lvAtt * 30 + dMucai.Count.Value * 0.5m;  //进攻方获得木材的基数

            var defYumiBase = dYumi.Count.Value * 0.5m; //防御方玉米损失基数
            var defMucaiBase = dMucai.Count.GetValueOrDefault() * 0.2m;   //防御方木材损失基数
            var defMucaiShuBase = dMucaiShu.Count.Value * 0.5m; //防御方木材树损失基数
            if (IsWin)   //若击溃主控室
            {
                //攻击方木材
                aMucai.StringDictionary["count"] = (Math.Round(attMucaiBase, MidpointRounding.AwayFromZero)).ToString();
                //攻击方获得金币
                aJinbi.StringDictionary["count"] =( Math.Round(attJinbiBase, MidpointRounding.AwayFromZero)).ToString();
                //防御方玉米田损失
                defYumi.StringDictionary["count"] = (-Math.Round(defYumiBase, MidpointRounding.AwayFromZero)).ToString();
                //防御方木材损失
                defMucai.StringDictionary["count"] = (-Math.Round(defMucaiBase, MidpointRounding.AwayFromZero)).ToString();
                defShu.StringDictionary["count"] = (-Math.Round(defMucaiShuBase, MidpointRounding.AwayFromZero)).ToString();
            }
            else //若未击溃主控室
            {
                var dMucaiCount = DestroyTIds.Count(c => c.Item1 == ProjectConstant.MucaiStoreTId);   //击溃木材仓库的数量
                //攻击方木材
                aMucai.StringDictionary["count"] = (Math.Round(attMucaiBase * dMucaiCount * 0.1m, MidpointRounding.AwayFromZero)).ToString();
                //攻击方获得金币
                aJinbi.StringDictionary["count"] = (Math.Round(attJinbiBase * dMucaiCount * 0.1m, MidpointRounding.AwayFromZero)).ToString();
            }
            if (!World.CharManager.IsOnline(OtherCharId) && !OtherChar.CharType.HasFlag(CharType.Robot))    //若防御方不在线在线且不是机器人
            {
                if (defYumi.StringDictionary.GetDecimalOrDefault("count", decimal.Zero) != decimal.Zero)  //若要记录防御方损失玉米
                    defencerBooty.Add(defYumi);
                if (defMucai.StringDictionary.GetDecimalOrDefault("count", decimal.Zero) != decimal.Zero)  //若要记录防御方损失木材
                    defencerBooty.Add(defMucai);
                if (defShu.StringDictionary.GetDecimalOrDefault("count", decimal.Zero) != decimal.Zero)  //若要记录防御方损失木材树
                    defencerBooty.Add(defShu);
            }
            if (aJinbi.StringDictionary.GetDecimalOrDefault("count", decimal.Zero) != decimal.Zero)  //若要记录进攻方获得金币
                attackerBooty.Add(aJinbi);
            if (aMucai.StringDictionary.GetDecimalOrDefault("count", decimal.Zero) != decimal.Zero)  //若要记录进攻方获得木材
                attackerBooty.Add(aMucai);
        }

        /// <summary>
        /// 获取物品默认的容器对象。
        /// </summary>
        /// <returns></returns>
        public List<GameThingBase> GetDefaultContainers(List<GameItem> gameItems)
        {
            var result = new List<GameThingBase>();
            var gc = GameChar;
            for (int i = 0; i < gameItems.Count; i++)
            {
                var item = gameItems[i];
                if (item.TemplateId == ProjectConstant.JinbiId)
                {
                    result.Add(gc.GetCurrencyBag());
                }
                else if (item.TemplateId == ProjectConstant.MucaiId)
                {
                    result.Add(gc.GetCurrencyBag());
                }
                else
                    throw new InvalidOperationException("不可获得物品。");
            }
            return result;
        }

    }
}
