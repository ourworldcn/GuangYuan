﻿using GuangYuan.GY001.BLL.Homeland;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using GuangYuan.GY001.UserDb.Combat;
using OW.Game;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace GuangYuan.GY001.BLL
{
    /// <summary>
    /// pvp结束战斗调用接口的数据封装类。
    /// </summary>
    public class EndCombatPvpWorkData : BinaryRelationshipWorkDataBase
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

        WarNewspaper _Combat;
        /// <summary>
        /// 获取战斗对象，如果找不到则设置错误信息。
        /// </summary>
        public WarNewspaper Combat
        {
            get
            {
                if (_Combat is null)
                {
                    _Combat = UserContext.Set<WarNewspaper>().Find(CombatId);
                    if (_Combat is null)
                    {
                        HasError = true;
                        ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                        ErrorMessage = $"找不到指定的战报对象，Id={CombatId}";
                    }
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
        /// 计算战斗收益。
        /// </summary>
        /// <param name="bootyOfAttacker">进攻方收益。为空则不设置。</param>
        /// <param name="bootyOfDefenser">防御方收益。为空则不设置。</param>
        private void ComputeBooty([AllowNull] List<(Guid, decimal)> bootyOfAttacker, [AllowNull] List<(Guid, decimal)> bootyOfDefenser)
        {
            int dMucaiCount; //摧毁木材仓库数
            if (DestroyTIds.Any(c => c.Item1 == ProjectConstant.MucaiStoreTId))
                dMucaiCount = (int)DestroyTIds.First(c => c.Item1 == ProjectConstant.MucaiStoreTId).Item2;
            else
                dMucaiCount = 0;
            var dYumi = OtherChar.GetHomeland().AllChildren.FirstOrDefault(c => c.TemplateId == ProjectConstant.YumitianTId); //防御方玉米田
            var dt = Now;
            var dJinbi = dYumi.Name2FastChangingProperty["Count"].GetCurrentValue(ref dt); //防御方玉米田存量
            var dMucaiShu = OtherChar.GetHomeland().AllChildren.FirstOrDefault(c => c.TemplateId == ProjectConstant.MucaishuTId);   //防御方木材树
            dt = Now;
            var dMucai = dMucaiShu.Name2FastChangingProperty["Count"].GetCurrentValue(ref dt);  //防御方木材树的数量
            var lvAttacker = GameChar.Properties.GetDecimalOrDefault(ProjectConstant.LevelPropertyName);    //进攻方等级
            var lvDefenser = OtherChar.Properties.GetDecimalOrDefault(ProjectConstant.LevelPropertyName);    //防御方等级
            var mucaiRank = OtherChar.GetMucai().Count.Value; //防御方仓库内木材
            var jinbiOfAttacker = lvAttacker * 100 + dJinbi * 0.5m;  //进攻方获益金币基数
            var mucaiOfAttacker = lvAttacker * 30 + dMucai * 0.5m + mucaiRank * 0.2m; //进攻方获益木材基数
            var jinbiOfDefenser = 0m;    //防御方金币获益
            var mucaiShuOfDefenser = 0m;    //防御方木材树获益
            var mucaiOfDefenser = 0m;    //防御方木材仓库获益
            if (IsWin)   //进攻方获胜
            {
                jinbiOfDefenser = -dJinbi * 0.5m;
                mucaiShuOfDefenser = -dMucai * 0.5m;
                mucaiOfDefenser = -mucaiRank * 0.2m;
            }
            else //进攻方未获胜
            {
                jinbiOfAttacker *= dMucaiCount * 0.1m;
                mucaiOfAttacker *= dMucaiCount * 0.1m;
            }
            if (World.CharManager.IsOnline(OtherCharId) || OtherChar.CharType.HasFlag(CharType.Npc))    //若防御方在线或是机器人
            {
                jinbiOfDefenser = decimal.Zero;
                mucaiShuOfDefenser = decimal.Zero;
                mucaiOfDefenser = decimal.Zero;
            }

            //进攻方收益
            if (jinbiOfAttacker != decimal.Zero)
                bootyOfAttacker?.Add((ProjectConstant.JinbiId, jinbiOfAttacker));
            if (mucaiOfAttacker != decimal.Zero)
                bootyOfAttacker?.Add((ProjectConstant.MucaiId, jinbiOfAttacker));
            //防御方收益
            if (jinbiOfDefenser != decimal.Zero)
                bootyOfDefenser?.Add((ProjectConstant.JinbiId, jinbiOfDefenser));
            if (mucaiShuOfDefenser != decimal.Zero)
                bootyOfDefenser?.Add((ProjectConstant.MucaishuTId, mucaiShuOfDefenser));
            if (mucaiOfDefenser != decimal.Zero)
                bootyOfDefenser?.Add((ProjectConstant.MucaiId, mucaiOfDefenser));
        }

        /// <summary>
        /// 计算战利品。
        /// </summary>
        /// <param name="attackerBooty"></param>
        /// <param name="defencerBooty"></param>
        public void GetBooty(ICollection<GameBooty> attackerBooty, ICollection<GameBooty> defencerBooty)
        {
            var dHl = OtherChar.GetHomeland();
            var dYumi = dHl.AllChildren.FirstOrDefault(c => c.TemplateId == ProjectConstant.YumitianTId); //防御方玉米田

            var dMucaiShu = dHl.AllChildren.FirstOrDefault(c => c.TemplateId == ProjectConstant.MucaishuTId);   //防御方木材树
            var dMucai = OtherChar.GetMucai();  //防御方木材货币数
            var eventMng = World.EventsManager;
            var lvAtt = GameChar.Properties.GetDecimalOrDefault(World.PropertyManager.LevelPropertyName);   //进攻方等级
            var aMucai = new GameBooty() { CharId = GameChar.Id };   //攻击方木材
            aMucai.Properties["tid"] = ProjectConstant.MucaiId.ToString();

            var aJinbi = new GameBooty() { CharId = GameChar.Id };   //攻击方金币
            aJinbi.Properties["tid"] = ProjectConstant.JinbiId.ToString();

            var defMucai = new GameBooty() { CharId = OtherChar.Id };   //防御方木材
            defMucai.Properties["tid"] = ProjectConstant.JinbiId.ToString();

            var defShu = new GameBooty() { CharId = OtherChar.Id };   //防御方木材树
            defShu.Properties["tid"] = ProjectConstant.MucaishuTId.ToString();

            var defJinbi = new GameBooty() { CharId = OtherChar.Id };   //防御方金币
            defJinbi.Properties["tid"] = ProjectConstant.JinbiId.ToString();

            var defYumi = new GameBooty() { CharId = OtherChar.Id };    //防御方玉米田
            defYumi.Properties["tid"] = ProjectConstant.YumitianTId.ToString();

            var attJinbiBase = lvAtt * 30 + dMucai.Count.Value * 0.2m + dMucaiShu.Count.Value * 0.5m;   //进攻方获得金币的基数
            var attMucaiBase = lvAtt * 100 + dYumi.Count.Value * 0.5m;  //进攻方获得木材的基数
            var defYumiBase = dYumi.Count.Value * 0.5m; //防御方玉米损失基数
            var defMucaiBase = dMucai.Count.Value * 0.2m;   //防御方木材损失基数
            var defMucaiShuBase = dMucaiShu.Count.Value * 0.5m; //防御方木材树损失基数
            if (IsWin)   //若击溃主控室
            {
                //攻击方木材
                aMucai.Properties["count"] = Math.Round(attMucaiBase, MidpointRounding.AwayFromZero);
                //攻击方获得金币
                aJinbi.Properties["count"] = Math.Round(attJinbiBase, MidpointRounding.AwayFromZero);
                //防御方玉米田损失
                defYumi.Properties["count"] = -Math.Round(defYumiBase, MidpointRounding.AwayFromZero);
                //防御方木材损失
                defMucai.Properties["count"] = -Math.Round(defMucaiBase, MidpointRounding.AwayFromZero);
                defShu.Properties["count"] = -Math.Round(defMucaiShuBase, MidpointRounding.AwayFromZero);
            }
            else //若未击溃主控室
            {
                var dMucaiCount = DestroyTIds.Count(c => c.Item1 == ProjectConstant.MucaiStoreTId);   //击溃木材仓库的数量
                //攻击方木材
                aMucai.Properties["count"] = Math.Round(attMucaiBase * dMucaiCount * 0.1m, MidpointRounding.AwayFromZero);
                //攻击方获得金币
                aJinbi.Properties["count"] = Math.Round(attJinbiBase * dMucaiCount * 0.1m, MidpointRounding.AwayFromZero);
            }
            if (!World.CharManager.IsOnline(OtherCharId) && !OtherChar.CharType.HasFlag(CharType.Npc))    //若防御方不在线在线且不是机器人
            {
                if (defYumi.Properties.GetDecimalOrDefault("count", decimal.Zero) != decimal.Zero)  //若要记录防御方损失玉米
                    defencerBooty.Add(defYumi);
                if (defMucai.Properties.GetDecimalOrDefault("count", decimal.Zero) != decimal.Zero)  //若要记录防御方损失木材
                    defencerBooty.Add(defMucai);
                if (defShu.Properties.GetDecimalOrDefault("count", decimal.Zero) != decimal.Zero)  //若要记录防御方损失木材树
                    defencerBooty.Add(defShu);
            }
            if (aJinbi.Properties.GetDecimalOrDefault("count", decimal.Zero) != decimal.Zero)  //若要记录进攻方获得金币
                attackerBooty.Add(aJinbi);
            if (aMucai.Properties.GetDecimalOrDefault("count", decimal.Zero) != decimal.Zero)  //若要记录进攻方获得木材
                attackerBooty.Add(aMucai);
        }

        private List<(Guid, decimal)> _BootyOfAttacker;
        /// <summary>
        /// 进攻者战利品。
        /// Item1=模板Id,Item2=数量
        /// </summary>
        public List<(Guid, decimal)> BootyOfAttacker
        {
            get
            {
                if (_BootyOfAttacker is null)
                {
                    NormalizeDestroyTIds();
                    _BootyOfAttacker = new List<(Guid, decimal)>();
                    ComputeBooty(_BootyOfAttacker, null);
                }
                return _BootyOfAttacker;
            }
        }

        private List<(Guid, decimal)> _BootyOfDefenser;
        /// <summary>
        /// 防御者战利品。
        /// </summary>
        public List<(Guid, decimal)> BootyOfDefenser
        {
            get
            {
                if (_BootyOfDefenser is null)
                {
                    NormalizeDestroyTIds();
                    _BootyOfDefenser = new List<(Guid, decimal)>();
                    ComputeBooty(null, _BootyOfDefenser);
                }
                return _BootyOfDefenser;
            }
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
