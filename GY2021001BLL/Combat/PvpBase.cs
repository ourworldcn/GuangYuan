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
    public class StartCombatPvpData : BinaryRelationshipGameContext
    {
        public StartCombatPvpData([NotNull] IServiceProvider service, [NotNull] GameChar gameChar, Guid otherGCharId) : base(service, gameChar, otherGCharId)
        {
        }

        public StartCombatPvpData([NotNull] VWorld world, [NotNull] GameChar gameChar, Guid otherGCharId) : base(world, gameChar, otherGCharId)
        {
        }

        public StartCombatPvpData([NotNull] VWorld world, [NotNull] string token, Guid otherGCharId) : base(world, token, otherGCharId)
        {
        }

        public Guid CombatId { get; set; }

        /// <summary>
        /// 原始战斗id,如果没有则为空。
        /// </summary>
        public Guid? OldCombatId { get; set; }

        /// <summary>
        /// 关卡Id。
        /// </summary>
        public Guid DungeonId { get; set; }

    }

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
                        DebugMessage = $"找不到指定的战报对象，Id={CombatId}";
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
        /// 主控室剩余血量的百分比。
        /// 0表示空血，1表示满血。
        /// </summary>
        public decimal MainRoomRhp { get; set; }

        /// <summary>
        /// 木材仓剩余血量的百分比。合并多个木材仓的总血量剩余的百分比。
        /// 0表示空血，1表示满血。
        /// </summary>
        public decimal StoreOfWoodRhp { get; set; }

        /// <summary>
        /// 玉米田剩余血量的百分比。
        /// 0表示空血，1表示满血。
        /// </summary>
        public decimal GoldRhp { get; set; }

        /// <summary>
        /// 木材林剩余血量的百分比。
        /// 0表示空血，1表示满血。
        /// </summary>
        public decimal WoodRhp { get; set; }

        /// <summary>
        /// 摧毁建筑的模板Id集合。
        /// </summary>
        public List<(Guid, decimal)> DestroyTIds { get; } = new List<(Guid, decimal)>();

        /// <summary>
        /// 计算战利品。
        /// </summary>
        /// <param name="attackerBooty"></param>
        /// <param name="defencerBooty"></param>
        public void GetBooty(ICollection<GameBooty> attackerBooty, ICollection<GameBooty> defencerBooty)
        {
            var dHl = OtherChar.GetHomeland();
            var dYumi = dHl.GetAllChildren().FirstOrDefault(c => c.ExtraGuid == ProjectConstant.YumitianTId); //防御方玉米田

            var dMucaiShu = dHl.GetAllChildren().FirstOrDefault(c => c.ExtraGuid == ProjectConstant.MucaishuTId);   //防御方木材树
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
            if (MainRoomRhp<=0)   //若击溃主控室
            {
                //攻击方木材
                aMucai.StringDictionary["count"] = Math.Round(attMucaiBase, MidpointRounding.AwayFromZero).ToString();
                //攻击方获得金币
                aJinbi.StringDictionary["count"] = Math.Round(attJinbiBase, MidpointRounding.AwayFromZero).ToString();
                //防御方玉米田损失
                defYumi.StringDictionary["count"] = (-Math.Round(defYumiBase, MidpointRounding.AwayFromZero)).ToString();
                //防御方木材损失
                defMucai.StringDictionary["count"] = (-Math.Round(defMucaiBase, MidpointRounding.AwayFromZero)).ToString();
                defShu.StringDictionary["count"] = (-Math.Round(defMucaiShuBase, MidpointRounding.AwayFromZero)).ToString();
            }
            else //若未击溃主控室
            {
                var dMucaiCount = DestroyTIds.Where(c => World.ItemTemplateManager.GetTemplateFromeId(c.Item1)?.CatalogNumber == (int)ThingGId.家园建筑_木材仓/1000).Sum(c => c.Item2);   //击溃木材仓库的数量
                //攻击方木材
                aMucai.StringDictionary["count"] = Math.Round(attMucaiBase * dMucaiCount * 0.1m, MidpointRounding.AwayFromZero).ToString();
                //攻击方获得金币
                aJinbi.StringDictionary["count"] = Math.Round(attJinbiBase * dMucaiCount * 0.1m, MidpointRounding.AwayFromZero).ToString();
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

    }
}
