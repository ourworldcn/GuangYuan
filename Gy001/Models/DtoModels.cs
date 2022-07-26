/*
 * 供Unity使用的SDK文件。
 * 目前使用C# 7.3版本语法。
 */

using Game.Social;
using GuangYuan.GY001.UserDb;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
#pragma warning disable IDE0057 // 使用范围运算符
#pragma warning disable IDE0074 // 使用复合分配

namespace GY2021001WebApi.Models
{

    #region 基础数据

    [DataContract]
    public class ModifyPropertyItemDto
    {
        /// <summary>
        /// 对象的Id.如xxxxxxxxxxxxxxxxxx==形式。
        /// </summary>
        [DataMember]
        public string ObjectId { get; set; }

        /// <summary>
        /// 属性名。
        /// </summary>
        [DataMember]
        public string PropertyName { get; set; }

        /// <summary>
        /// 属性值。
        /// </summary>
        [DataMember]
        public string Value { get; set; }
    }

    /// <summary>
    /// 修改属性返回值接口返回值封装类。
    /// </summary>
    [DataContract]
    public class ModifyPropertiesReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 修改属性接口参数封装类。
    /// </summary>
    [DataContract]
    public class ModifyPropertiesParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 设置要修改的项。
        /// </summary>
        [DataMember]
        public List<ModifyPropertyItemDto> Items { get; set; } = new List<ModifyPropertyItemDto>();
    }

    /// <summary>
    /// 用于精确描述变化数据的类。
    /// </summary>
    /// <remarks>这个类的新值和旧值都用Object表示，对于数据量极大的一些情况会使用具体的类表示如GamePropertyChangeFloatItemDto表示大量的即时战斗数据包导致的人物属性变化。</remarks>
    [DataContract]
    public partial class GamePropertyChangeItemDto
    {
        public GamePropertyChangeItemDto()
        {

        }

        /// <summary>
        /// 对象的模板Id。
        /// </summary>
        [DataMember]
        public string TId { get; set; }

        /// <summary>
        /// 对象Id。指出是什么对象变化了属性。
        /// </summary>
        [DataMember]
        public string ObjectId { get; set; }

        /// <summary>
        /// 属性的名字。事件发送者和处理者约定好即可，也可能是对象的其他属性名，如Children可以表示集合变化。
        /// </summary>
        [DataMember]
        public string PropertyName { get; set; }

        #region 旧值相关

        /// <summary>
        /// 指示<see cref="OldValue"/>中的值是否有意义。
        /// </summary>
        [DataMember]
        public bool HasOldValue { get; set; }

        /// <summary>
        /// 获取或设置旧值。
        /// </summary>
        [DataMember]
        public object OldValue { get; set; }

        #endregion 旧值相关

        #region 新值相关

        /// <summary>
        /// 指示<see cref="NewValue"/>中的值是否有意义。
        /// </summary>
        [DataMember]
        public bool HasNewValue { get; set; }

        /// <summary>
        /// 新值。
        /// </summary>
        [DataMember]
        public object NewValue { get; set; }

        #endregion 新值相关

        /// <summary>
        /// 属性发生变化的时间点。Utc计时。
        /// </summary>
        [DataMember]
        public DateTime DateTimeUtc { get; set; } = DateTime.UtcNow;
    }

    [DataContract]
    public class StringDecimalTuple
    {
        public StringDecimalTuple()
        {

        }

        [DataMember]
        public string Item1 { get; set; }

        [DataMember]
        public decimal Item2 { get; set; }
    }

    /// <summary>
    /// 变化通知内容的数据传输类。
    /// PropertyName 属性是8de0e03b-d138-43d3-8cce-e519c9da3065 表示指定对象发生了多处变化，需要全部刷新。
    /// </summary>
    [DataContract]
    public partial class ChangeDataDto
    {
        /// <summary>
        /// 行为Id，1增加（OldValue属性无效），2更改，4删除(NewValue属性无效)
        /// </summary>
        [DataMember]
        public int ActionId { get; set; }

        /// <summary>
        /// 变化的对象Id。
        /// </summary>
        [DataMember]
        public string ObjectId { get; set; }

        /// <summary>
        /// 变化对象的模板Id。
        /// </summary>
        /// <remarks>{7396db31-1d02-43d3-af05-c14f4ca2a5fc}好友位模板Id表示好友。
        /// {0C741F97-12EC-4463-85B0-C1782656E853}邮件槽模板Id表示邮件。
        /// 0CF39269-6301-470B-8527-07AF29C5EEEC角色的模板Id表示角色。
        /// 其它是成就的模板Id,如{25FFBEE1-F617-49BD-B0DE-32B3E3E975CB}表示 玩家等级成就。
        /// </remarks>
        [DataMember]
        public string TemplateId { get; set; }

        /// <summary>
        /// 变化的属性名。8de0e03b-d138-43d3-8cce-e519c9da3065 表示指定对象发生了多处变化，需要全部刷新。
        /// </summary>
        [DataMember]
        public string PropertyName { get; set; }

        /// <summary>
        /// 变化之前的值。
        /// 暂时未实现。
        /// </summary>
        [DataMember]
        public object OldValue { get; set; }

        /// <summary>
        /// 变化之后的值。
        /// 暂时未实现。
        /// </summary>
        [DataMember]
        public object NewValue { get; set; }

        /// <summary>
        /// 附属数据。如用户等级变化时，这里有类似{"exp",12360}的指出变化后的经验值。
        /// </summary>
        [DataMember]
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 创建此条数据的Utc时间。
        /// </summary>
        public DateTime CreateUtc { get; set; } = DateTime.UtcNow;

    }

    /// <summary>
    /// 战斗对象的数据传输类。
    /// </summary>
    [DataContract]
    public partial class CombatDto
    {
        /// <summary>
        /// Id。
        /// </summary>
        [DataMember]
        public string Id { get; set; }

        /// <summary>
        /// 动态属性字典。
        /// </summary>
        [DataMember]
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();


        /// <summary>
        /// 攻击方角色Id集合。
        /// </summary>
        [DataMember]
        public List<string> AttackerIds { get; set; } = new List<string>();

        /// <summary>
        /// 防御方角色Id集合。
        /// </summary>
        [DataMember]
        public List<string> DefenserIds { get; set; } = new List<string>();

        //private List<GameBooty> _BootyOfAttacker;
        ///// <summary>
        ///// 获取战利品。
        ///// </summary>
        ///// <param name="context"></param>
        ///// <returns></returns>
        //public List<GameBooty> BootyOfAttacker(DbContext context)
        //{
        //    if (_BootyOfAttacker is null)
        //    {
        //        _BootyOfAttacker = context.Set<GameBooty>().Where(c => c.ParentId == Id && AttackerIds.Contains(c.CharId)).ToList();
        //    }
        //    return _BootyOfAttacker;
        //}

        ///// <summary>
        ///// 该战斗开始的Utc时间。
        ///// </summary>
        //public DateTime StartUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 该战斗结束的Utc时间。
        /// </summary>
        [DataMember]
        public DateTime EndUtc { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 战利品数据传输对象。
    /// </summary>
    [DataContract]
    public partial class GameBootyDto
    {
        /// <summary>
        /// 所属战斗对象的Id。
        /// </summary>
        [DataMember]
        public string ParentId { get; set; }

        /// <summary>
        /// 所属角色(参与战斗的角色Id)。
        /// </summary>
        [DataMember]
        public string CharId { get; set; }

        /// <summary>
        /// 模板Id。
        /// </summary>
        [DataMember]
        public string TemplateId { get; set; }

        /// <summary>
        /// 数量。可能是负数，表示失去的数量。
        /// </summary>
        [DataMember]
        public decimal Count { get; set; }

        /// <summary>
        /// 封装额外的参数，目前仅对坐骑/野兽时，这里有htid和btid属性。
        /// </summary>
        [DataMember]
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 该项目使用的特定常量。
    /// </summary>
    public static class DtoConstant
    {
        #region 固定模板Id

        #region 废弃模板Id

        /// <summary>
        /// 当前装备的坐骑头容器模板Id。已废弃。
        /// </summary>
        [Obsolete("下个版本可能会被删除。")]
        public static readonly Guid ZuojiTou = new Guid("{A06B7496-F631-4D51-9872-A2CC84A56EAB}");

        /// <summary>
        /// 当前装备的坐骑身体容器模板Id。已废弃
        /// </summary>
        [Obsolete("下个版本可能会被删除。")]
        public static readonly Guid ZuojiShen = new Guid("{7D191539-11E1-49CD-8D0C-82E3E5B04D31}");
        /// <summary>
        /// 神纹碎片背包槽Id。放在此槽中是未装备的神纹(碎片)。
        /// </summary>
        [Obsolete("下个版本可能会被删除。")]
        public static readonly Guid ShenWenBagSlotId = new Guid("{2BAA3FCD-2BE8-4096-916A-FF2D47E084EF}");

        /// <summary>
        /// 当前坐骑的容器Id。出战坐骑包。
        /// </summary>
        [Obsolete("下个版本可能会被删除。")]
        public static readonly Guid DangqianZuoqiSlotId = new Guid("{B19EE5AB-57E3-4513-8228-9F2A8364358E}");

        /// <summary>
        /// 坐骑组合中的身体容器Id。
        /// </summary>
        [Obsolete("下个版本可能会被删除。")]
        public static readonly Guid ZuojiZuheShenti = new Guid("{F8B1987D-FDF3-4090-9E9B-EBAF1DB2DCCD}");
        #endregion 废弃模板Id

        #region 坐骑相关Id

        /// <summary>
        /// 坐骑头和身体需要一个容器组合起来。此类容器的模板Id就是这个。
        /// </summary>
        public static readonly Guid ZuojiZuheRongqi = new Guid("{6E179D54-5836-4E0B-B30D-756BD07FF196}");

        /// <summary>
        /// 坐骑组合中的头容器Id。
        /// </summary>
        public static readonly Guid ZuojiZuheTou = new Guid("{740FEBF3-7472-43CB-8A10-798F6C61335B}");

        #endregion 坐骑相关Id

        #region 角色直属槽及其相关

        /// <summary>
        /// 神纹槽Id。放在此槽中是装备的神纹。当前每种类型的野兽身体对应一种神纹。
        /// </summary>
        public static readonly Guid ShenWenSlotId = new Guid("{88A4EED6-0AEB-4A70-8FDE-67F75E5E2C0A}");

        /// <summary>
        /// 道具背包槽Id。这个就是初期规划的神纹碎片背包。
        /// </summary>
        public static readonly Guid DaojuBagSlotId = new Guid("{2BAA3FCD-2BE8-4096-916A-FF2D47E084EF}");

        /// <summary>
        /// 战斗收益槽。如果处于战斗中，此槽内表示大关的的总收益，用于计算收益限制。若不在战斗中，此槽为空（其中物品移动到各种背包中）。
        /// </summary>
        public static readonly Guid ShouyiSlotId = new Guid("{FEA0B277-8CC6-462F-B0ED-85409ABE9C79}");

        /// <summary>
        /// 兽栏槽Id，抓捕的野兽存于此槽内。
        /// </summary>
        public static readonly Guid ShoulanSlotId = new Guid("{1630A0A1-3540-479A-B2C5-10B63E7A5774}");

        /// <summary>
        /// 金币Id，这个不是槽，它的Count属性直接记录了金币数，目前其子代为空。这个省事，但未来在金币袋上开脑洞，不能保证不变。
        /// </summary>
        public static readonly Guid JinbiId = new Guid("{2B83C942-1E9C-4B45-9816-AD2CBF0E473F}");

        /// <summary>
        /// 木材Id，这个不是槽，它的Count属性直接记录了数量，目前其子代为空。
        /// </summary>
        public static readonly Guid MucaiId = new Guid("{01959584-E2C9-4E54-BBB7-FCC58A9484EC}");

        /// <summary>
        /// 钻石Id，这个不是槽，它的Count属性直接记录了数量，目前其子代为空。
        /// </summary>
        public static readonly Guid ZuanshiId = new Guid("{3E365BEC-F83D-467D-A58C-9EBA43458682}");
        /// <summary>
        /// 坐骑背包Id。
        /// </summary>
        public static readonly Guid ZuojiBagSlotId = new Guid("{BA2AEE89-0BC3-4612-B6FF-5DDFEF85C9E5}");

        /// <summary>
        /// 家园模板Id。
        /// </summary>
        public static readonly Guid HomelandSlotId = new Guid("{3a855606-a5ee-459b-b1ed-76e9b5847d7d}");

        /// <summary>
        /// 主基地模板Id。
        /// </summary>
        public static readonly Guid MainBaseSlotId = new Guid("{234f8c55-4c3c-4406-ad38-081d29564f20}");

        #endregion  角色直属槽及其相关
        /// <summary>
        /// 角色模板Id。当前只有一个模板。
        /// </summary>
        public static readonly Guid CharTemplateId = new Guid("{0CF39269-6301-470B-8527-07AF29C5EEEC}");

        #endregion 固定模板Id

        /// <summary>
        /// 神纹碎片的模板Id。
        /// </summary>
        public static readonly Guid RunesId = new Guid("{2B86FF50-0257-4913-8BEC-F5CF3C84B6D5}");

        /// <summary>
        /// 级别属性的名字。
        /// </summary>
        public const string LevelPropertyName = "lv";   //Runes

        /// <summary>
        /// 堆叠上限属性的名字。没有该属性的不可堆叠，无上限限制用-1表示。
        /// </summary>
        public const string StackUpperLimit = "stc";

        /// <summary>
        /// 容器容量上限属性。
        /// </summary>
        public const string ContainerCapacity = "cap";

        /// <summary>
        /// 裝備的神纹已经突破攻击的次数的属性名。
        /// </summary>
        public const string ShenwenTupoAtkCountPropertyName = "sscatk";

        /// <summary>
        /// 裝備的神纹已经突破最大血量的次数的属性名。
        /// </summary>
        public const string ShenwenTupoMHpCountPropertyName = "sscmhp";

        /// <summary>
        /// 裝備的神纹已经突破质量的次数的属性名。
        /// </summary>
        public const string ShenwenTupoQltCountPropertyName = "sscqlt";

        /// <summary>
        /// 阵容属性前缀。
        /// </summary>
        public const string ZhenrongPropertyName = "for";

        #region 类别号
        /// <summary>
        /// 血量神纹碎片的类别号。
        /// </summary>
        public const int ShenwenHPTCode = 15;

        /// <summary>
        /// 攻击神纹碎片的类别号。
        /// </summary>
        public const int ShenwenAtkTCode = 16;

        /// <summary>
        /// 质量神纹碎片的类别号。
        /// </summary>
        public const int ShenwenQltTCode = 17;

        /// <summary>
        /// 装备的神纹的类别号。
        /// </summary>
        public const int ShenwenTCode = 10;

        #endregion 类别号

        #region 蓝图常量
        /// <summary>
        /// 突破蓝图Id。
        /// </summary>
        public static readonly Guid ShenWenTupoBlueprint = new Guid("{92f63905-a39f-4e1a-ad17-ea648a99be7a}");

        /// <summary>
        /// 神纹升级蓝图Id。
        /// </summary>
        public static readonly Guid ShenwenLvUpBlueprint = new Guid("{31E0945A-94E4-43D5-835F-6546D68349F1}");

        #endregion 蓝图常量
    }

    public static class DtoHelper
    {
        /// <summary>
        /// 用Base64编码Guid类型。
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static string ToBase64String(Guid guid)
        {
            return Convert.ToBase64String(guid.ToByteArray());
        }

        /// <summary>
        /// 从Base64编码转换获取Guid值。
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Guid FromBase64String(string str)
        {
            return new Guid(Convert.FromBase64String(str));
        }
    }

    /// <summary>
    /// 使用令牌的基类。
    /// </summary>
    [DataContract]
    public class TokenDtoBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public TokenDtoBase()
        {

        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="token"></param>
        public TokenDtoBase(string token)
        {
            Token = token;
        }

        /// <summary>
        /// 令牌。
        /// </summary>
        [DataMember]
        public string Token { get; set; }

    }

    /// <summary>
    /// 设计到两个角色的功能接口数据封装类。
    /// Token指定了当前角色，OtherCharId是另一个角色的Id。
    /// </summary>
    [DataContract]
    public class SocialDtoBase : TokenDtoBase
    {
        public SocialDtoBase()
        {
        }

        public SocialDtoBase(string token, string otherCharId) : base(token)
        {
            OtherCharId = otherCharId;
        }

        /// <summary>
        /// 另一个角色对象的Id。
        /// </summary>
        [DataMember]
        public string OtherCharId { get; set; }
    }

    /// <summary>
    /// 分页控制数据。
    /// </summary>
    [DataContract]
    public class PagingWithTokenParamsDtoBase
    {
        /// <summary>
        /// 起始的索引号。从0开始表示第一个。
        /// </summary>
        public int StartIndex { get; set; }

        /// <summary>
        /// 获取的最多数量。注意返回的实际数量可能少于要求数量，因为可能末尾数据不够。
        /// </summary>
        public int Count { get; set; }
    }

    /// <summary>
    /// 返回的分页数据的基类。
    /// </summary>
    [DataContract]
    public class PagingRetunDtoBase
    {
        public PagingRetunDtoBase()
        {

        }

        public PagingRetunDtoBase(int maxCount)
        {
            MaxCount = maxCount;
        }

        /// <summary>
        /// 最多有多少数据。
        /// </summary>
        [DataMember(Name = nameof(MaxCount))]
        public int MaxCount { get; set; }
    }

    /// <summary>
    /// 游戏物品，道具，金币，积分等等对象的模板。
    /// </summary>
    [DataContract]
    public partial class GameItemTemplateDto
    {
        public GameItemTemplateDto()
        {

        }

        /// <summary>
        /// 唯一Id。
        /// </summary>
        [DataMember(Name = nameof(Id))]
        public string Id { get; set; }

        /// <summary>
        /// 对属性字符串的解释。键是属性名，字符串类型。值有三种类型，decimal,string,decimal[]。
        /// 特别注意，如果需要频繁计算，则应把用于战斗的属性单独放在其他字典中。该字典因大量操作皆为读取，反装箱问题不大。
        /// </summary>
        [DataMember]
        public Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();

        /// <summary>
        /// 该类的GId。
        /// </summary>
        [DataMember]
        public int? GId { get; set; }

        /// <summary>
        /// 该模板创建对象应有的子模板Id字符串集合。用逗号分割。
        /// </summary>
        [DataMember]
        public string ChildrenTemplateIdString { get; set; }

        /// <summary>
        /// 一个说明性文字，服务器不使用该属性。
        /// </summary>
        [DataMember]
        public string DisplayName { get; set; }
    }

    /// <summary>
    /// 游戏物品，道具，金币，积分等等的对象。
    /// </summary>
    public partial class GameItemDto
    {
        public GameItemDto()
        {

        }

        /// <summary>
        /// 唯一Id。
        /// </summary>
        [DataMember(Name = nameof(Id))]
        public string Id { get; set; }

        /// <summary>
        /// 物品模板的Id。
        /// </summary>
        [DataMember(Name = nameof(TemplateId))]
        public string TemplateId { get; set; }

        /// <summary>
        /// 数量。
        /// </summary>
        [DataMember(Name = nameof(Count))]
        public decimal? Count { get; set; }

        private Dictionary<string, object> _Properties;
        /// <summary>
        /// 对属性字符串的解释。键是属性名，字符串类型。值有三种类型，decimal,string,decimal[]。
        /// 特别注意，如果需要频繁计算，则应把用于战斗的属性单独放在其他字典中。该字典因大量操作皆为读取，仅频繁拆箱问题不大(相对于不太频繁的操作而言)。
        /// 属性集合。"Properties":{"atk":102,"qult":500,"catalogId":"shfdkjshfkjskfh=="}
        /// </summary>
        [DataMember(Name = nameof(Properties))]
        public Dictionary<string, object> Properties
        {
            get
            {
                return _Properties ?? (_Properties = new Dictionary<string, object>());
            }//"for0=101001"

            set => _Properties = value;
        }
        /// <summary>
        /// 所属Id。当前版本下，仅当此物直属于角色对象时，此属性才有值，且是所属角色的id。
        /// </summary>
        [DataMember]
        public string OwnerId { get; set; }

        /// <summary>
        /// 下属物品对象。
        /// </summary>
        [DataMember]
        public List<GameItemDto> Children { get; set; } = new List<GameItemDto>();

        /// <summary>
        /// 所属父Id。
        /// </summary>
        [DataMember]
        public string ParentId { get; set; }

        /// <summary>
        /// 获取坐骑的头，如果该物品不是坐骑则这里返回null。
        /// </summary>
        [IgnoreDataMember]
        public GameItemDto Head
        {
            get
            {
                var id = DtoHelper.ToBase64String(DtoConstant.ZuojiZuheTou);
                var result = Children.FirstOrDefault(c => c.TemplateId == id)?.Children?.FirstOrDefault();
                return result;
            }
        }

        /// <summary>
        /// 获取坐骑的身体，如果该物品不是坐骑则这里返回null。
        /// </summary>
        [IgnoreDataMember]
        [Obsolete]
        public GameItemDto Body
        {
            get
            {
                var id = DtoHelper.ToBase64String(DtoConstant.ZuojiZuheShenti);
                var result = Children.FirstOrDefault(c => c.TemplateId == id)?.Children?.FirstOrDefault();
                return result;
            }
        }

        /// <summary>
        /// 为该坐骑设置头。注意这将导致强制改写该对象的模板和Children以适应坐骑的结构。
        /// </summary>
        /// <param name="gameCharDto"></param>
        public void SetHead(GameCharDto gameCharDto)
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 为该坐骑设置身体。注意这将导致强制改写该对象的模板和Children以适应坐骑的结构。
        /// </summary>
        /// <param name="gameCharDto"></param>
        public void SetBody(GameCharDto gameCharDto)
        {
            //throw new NotImplementedException();
        }

        [DataMember]
        public string ClientString { get; set; }
    }

    /// <summary>
    /// 角色属性封装类。
    /// </summary>
    [DataContract]
    public partial class GameCharDto
    {
        public GameCharDto()
        {

        }

        /// <summary>
        /// 该角色的唯一Id。
        /// </summary>
        [DataMember]
        public string Id { get; set; }

        /// <summary>
        /// 角色自身属性。
        /// </summary>
        [DataMember]
        public Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();

        /// <summary>
        /// 角色显示用的名字。
        /// </summary>
        [DataMember]
        public string DisplayName { get; set; }

        /// <summary>
        /// 客户端要记录的一些属性，这个属性客户端可以随意更改，服务器不使用。
        /// </summary>
        [DataMember]
        public string ClientGutsString { get; set; }

        /// <summary>
        /// 模板Id。
        /// </summary>
        [DataMember]
        public string TemplateId { get; set; }

        /// <summary>
        /// 创建该对象的通用协调时间。
        /// </summary>
        [DataMember]
        public DateTime CreateUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 所属用户Id。
        /// </summary>
        [DataMember]
        public string GameUserId { get; set; }

        /// <summary>
        /// 用户的容器和物品集合。
        /// </summary>
        [DataMember]
        public List<GameItemDto> GameItems { get; } = new List<GameItemDto>();

        /// <summary>
        /// 快捷属性:角色当前骑乘的坐骑。如果没有则返回null
        /// </summary>
        [Obsolete("下个版本可能会被删除。")]
        [IgnoreDataMember]
        public GameItemDto CurrentMounts
        {
            get
            {
                var id = DtoHelper.ToBase64String(DtoConstant.DangqianZuoqiSlotId);
                var result = GameItems.FirstOrDefault(c => c.TemplateId == id)?.Children.FirstOrDefault();
                return result;
            }
        }

        /// <summary>
        /// 用户所处地图区域的Id,这也可能是战斗关卡的Id。如果没有在战斗场景中，则可能是空。
        /// </summary>
        [DataMember]
        public string CurrentDungeonId { get; set; }

        /// <summary>
        /// 进入战斗场景的时间。注意是Utc时间。如果没有在战斗场景中，则可能是空。
        /// </summary>
        [DataMember]
        public DateTime? CombatStartUtc { get; set; }

        /// <summary>
        /// 客户端使用的扩展属性集合，服务器不使用该属性，仅帮助保存和传回。
        /// 键最长64字符，值最长8000字符。（一个中文算一个字符）
        /// </summary>
        [DataMember]
        public Dictionary<string, string> ClientExtendProperties { get; } = new Dictionary<string, string>();

    }

    /// <summary>
    /// 增量变化数据传输类。
    /// </summary>
    [DataContract]
    public partial class ChangesItemDto
    {
        public ChangesItemDto()
        {

        }

        /// <summary>
        /// 容器的Id。如果是容器本身属性变化，这个成员是容器的上层容器Id,例如背包的容量变化了则这个成员就是角色Id。
        /// </summary>
        [DataMember]
        public string ContainerId { get; set; }

        /// <summary>
        /// 该变化产生的时间，服务器的UTC时间。
        /// </summary>
        [DataMember]
        public DateTime DateTimeUtc { get; set; }

        /// <summary>
        /// 增加的数据。
        /// </summary>
        [DataMember]
        public List<GameItemDto> Adds { get; } = new List<GameItemDto>();

        /// <summary>
        /// 删除的对象的唯一Id集合。
        /// </summary>
        [DataMember]
        public List<string> Removes { get; } = new List<string>();

        /// <summary>
        /// 变化的数据。
        /// </summary>
        [DataMember]
        public List<GameItemDto> Changes { get; } = new List<GameItemDto>();
    }

    [DataContract]
    public partial class VWorldInfomationDto
    {
        /// <summary>
        /// 服务器的本次启动Utc时间。
        /// </summary>
        [DataMember]
        public DateTime StartDateTime { get; set; }

        /// <summary>
        /// 服务器的当前时间。
        /// </summary>
        [DataMember]
        public DateTime CurrentDateTime { get; set; }

        /// <summary>
        /// 服务器的当前版本号。
        /// 格式为：主版本号.次要版本号.修正号，修正号仅仅是修复bug等，只要主要和次要版本号一致就是同一个版本。
        /// </summary>
        [DataMember]
        public string Version { get; set; }
    }

    /// <summary>
    /// 返回数据对象的基类。
    /// </summary>
    [DataContract]
    public partial class ReturnDtoBase
    {
        /// <summary>
        /// 返回时指示是否有错误。false表示正常计算完成，true表示规则校验认为有误。
        /// </summary>
        [DataMember]
        public bool HasError { get; set; }

        /// <summary>
        /// 调试信息。调试状态下返回时填写。
        /// </summary>
        [DataMember]
        public string DebugMessage { get; set; }

        /// <summary>
        /// 详细错误码。
        /// </summary>
        [DataMember]
        public int ErrorCode { get; set; }

    }

    /// <summary>
    /// 带变化物品集合的返回数据对象基类。
    /// </summary>
    [DataContract]
    public partial class ChangesReturnDtoBase : ReturnDtoBase
    {
        public ChangesReturnDtoBase()
        {

        }

        [IgnoreDataMember]
        private List<ChangesItemDto> _ChangesItems;
        /// <summary>
        /// 获取变化物品的数据。仅当成功返回时有意义。
        /// </summary>
        [DataMember]
        public List<ChangesItemDto> ChangesItems { get => _ChangesItems ?? (_ChangesItems = new List<ChangesItemDto>()); set => _ChangesItems = value; }
    }

    /// <summary>
    /// 新版变化数据的返回基类。
    /// </summary>
    [DataContract]
    public partial class ChangesReturnDtoBaseV2 : ReturnDtoBase
    {
        public ChangesReturnDtoBaseV2()
        {

        }

        [IgnoreDataMember]
        private List<GamePropertyChangeItemDto> _Changes = new List<GamePropertyChangeItemDto>();

        [DataMember]
        public List<GamePropertyChangeItemDto> Changes { get => _Changes; set => _Changes = value; }

    }

    /// <summary>
    /// 带变化物品和邮件结果的返回值对象的基类。
    /// </summary>
    [DataContract]
    public partial class ChangesAndMailReturnDtoBase : ChangesReturnDtoBase
    {
        public ChangesAndMailReturnDtoBase()
        {

        }

        /// <summary>
        /// 操作导致发送了邮件的Id集合。
        /// 如果是空集合则表示没有发送邮件。
        /// </summary>
        [DataMember]
        public List<string> MailIds { get; set; } = new List<string>();
    }

    /// <summary>
    /// 行为简要记录的数据封装类。
    /// </summary>
    [DataContract]
    public partial class GameActionRecordDto
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GameActionRecordDto()
        {

        }

        /// <summary>
        /// 主体对象的Id。
        /// </summary>
        [DataMember]
        public string ParentId { get; set; }

        /// <summary>
        /// 行为Id。
        /// </summary>
        [DataMember]
        public string ActionId { get; set; }

        /// <summary>
        /// 这个行为发生的时间。
        /// </summary>
        /// <value>默认是构造此对象的UTC时间。</value>
        [DataMember]
        public DateTime DateTimeUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 一个人眼可读的说明。
        /// </summary>
        [DataMember]
        public string Remark { get; set; }

        /// <summary>
        /// 简单扩展属性的字典。
        /// </summary>
        [DataMember]
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
    #endregion 基础数据

    #region 账号管理相关

    /// <summary>
    /// GetCharIdsFromLoginNames 接口参数封装类。
    /// </summary>
    [DataContract]
    public class GetCharIdsFromLoginNamesParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 登录名集合。
        /// </summary>
        [DataMember]
        public List<string> LoginNames { get; set; } = new List<string>();
    }

    /// <summary>
    /// GetCharIdsFromLoginNames 接口返回值封装类。
    /// </summary>
    [DataContract]
    public class GetCharIdsFromLoginNamesReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 角色id集合。
        /// </summary>
        [DataMember]
        public List<string> CharIds { get; set; } = new List<string>();
    }

    [DataContract]
    public class DeleteUsersParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要删除的用户的登录名的集合。
        /// </summary>
        [DataMember]
        public List<string> LoginNames { get; set; } = new List<string>();
    }

    [DataContract]
    public class DeleteUsersResultDto : ReturnDtoBase
    {
    }

    [DataContract]
    public class SendThingsParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要送的物品，tid&lt;数字后缀&gt;=模板物品Id，count&lt;数字后缀&gt;=数量，如果是生物则用htid,btid分别指出头身模板Id。
        /// 例如<code>
        ///             Propertyies["tid1"]=new Guid("{2B83C942-1E9C-4B45-9816-AD2CBF0E473F}");   //金币
        ///             Propertyies["count1"]= 1000;   //金币数量
        ///             Propertyies["ptid1"]= new Guid("{7066A96D-F514-42C7-A30E-5E7567900AD4}");   //父容器模板Id
        ///             Propertyies["tid2"]=new Guid("{3E365BEC-F83D-467D-A58C-9EBA43458682}");   //钻石
        ///             Propertyies["count2"]= 100;   //钻石数量
        ///             Propertyies["ptid2"]= new Guid("{7066A96D-F514-42C7-A30E-5E7567900AD4}");   //父容器模板Id
        /// </code>
        /// </summary>
        [DataMember]
        public Dictionary<string, object> Propertyies { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 发送给角色的Id。"{DA5B83F6-BB73-4961-A431-96177DE82BFF}"表示发送给所有角色。
        /// </summary>
        [DataMember]
        public List<string> Tos { get; set; } = new List<string>();

        /// <summary>
        /// 要发送的邮件。
        /// </summary>
        [DataMember]
        public GameMailDto Mail { get; set; }
    }

    [DataContract]
    public class SendThingsReturnDto : ReturnDtoBase
    {
    }

    [DataContract]
    public class LetOutParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要强制下线的用户登录名。
        /// </summary>
        [DataMember]
        public string LoginName { get; set; }
    }

    [DataContract]
    public class LetOutReturnDto : ReturnDtoBase
    {
    }

    [DataContract]
    public class BlockUserParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 封停账号的登录名。
        /// </summary>
        [DataMember]
        public string LoginName { get; set; }

        /// <summary>
        /// 封停的截止时间点，使用Utc时间。
        /// </summary>
        [DataMember]
        public DateTime BlockUtc { get; set; }
    }

    [DataContract]
    public class BlockUserReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 获取服务器的返回值封装类。未来此类可能添加多个属性。
    /// </summary>
    [DataContract]
    public class GetInfosResultDto : ReturnDtoBase
    {
        /// <summary>
        /// 在线用户数。
        /// </summary>
        [DataMember]
        public int OnlineCount { get; set; }

        /// <summary>
        /// 内存中总计用户数。
        /// </summary>
        [DataMember]
        public int TotalCount { get; set; }

        /// <summary>
        /// 负载率。[0,1]之间的一个数。
        /// </summary>
        [DataMember]
        public decimal LoadRate { get; set; }
    }

    /// <summary>
    /// 获取服务器的参数封装类。
    /// </summary>
    [DataContract]
    public class GetInfosParamsDto : TokenDtoBase
    {
    }

    /// <summary>
    /// 导出用户信息的参数封装类。
    /// </summary>
    [DataContract]
    public class ExportUsersParaamsDto : TokenDtoBase
    {
        /// <summary>
        /// 登录名的前缀。
        /// </summary>
        [DataMember]
        public string Prefix { get; set; }

        /// <summary>
        /// 登录名的起始后缀。
        /// </summary>
        [DataMember]
        public int StartIndex { get; set; }

        /// <summary>
        /// 登录名的终止后缀。
        /// </summary>
        [DataMember]
        public int EndIndex { get; set; }
    }

    /// <summary>
    /// 快速隐式注册接口的返回类。
    /// </summary>
    [DataContract]
    public class QuicklyRegisterReturnDto
    {
        /// <summary>
        /// 登录名。
        /// </summary>
        [DataMember(Name = nameof(LoginName))]
        public string LoginName { get; set; }

        /// <summary>
        /// 密码。
        /// </summary>
        [DataMember(Name = nameof(Pwd))]
        public string Pwd { get; set; }

    }

    [DataContract]
    public class LoginT78ParamsDto
    {
        /// <summary>
        /// 发行商SDK给的的sid。
        /// </summary>
        [DataMember]
        public string Sid { get; set; }

    }

    [DataContract]
    public class LoginT78ReturnDto: LoginReturnDto
    {
        /// <summary>
        /// T78服务器返回的值完整的放在此处。仅当成功登录时才有。
        /// </summary>
        [DataMember]
        public string ResultString { get; set; }

        /// <summary>
        /// 指示是否为初创接口。true是初始创建，false不是初始创建。
        /// </summary>
        [DataMember]
        public bool IsCreated { get; set; }

    }

    /// <summary>
    /// 登录接口返回类。
    /// </summary>
    [DataContract]
    public class LoginReturnDto : TokenDtoBase
    {
        public LoginReturnDto()
        {
        }

        /// <summary>
        /// 世界服务器的主机地址。使用此地址拼接后续的通讯地址。
        /// </summary>
        [DataMember]
        public string WorldServiceHost { get; set; }

        /// <summary>
        /// 聊天服务器的主机地址。使用此地址拼接后续的通讯地址。
        /// </summary>
        [DataMember]
        public string ChartServiceHost { get; internal set; }

        /// <summary>
        /// 该账号下所有角色信息的数组。目前有且仅有一个角色。
        /// </summary>
        [DataMember]
        public List<GameCharDto> GameChars { get; set; } = new List<GameCharDto>();

    }

    /// <summary>
    /// 登录接口参数类。
    /// </summary>
    [DataContract]
    public class LoginParamsDto
    {
        public LoginParamsDto()
        {

        }

        /// <summary>
        /// 登录名。
        /// </summary>
        [DataMember]
        public string LoginName { get; set; }

        /// <summary>
        /// 密码。
        /// </summary>
        [DataMember]
        public string Pwd { get; set; }

        /// <summary>
        /// 登录客户端类型。目前可能值是IOS或Android。
        /// </summary>
        [DataMember]
        public string Region { get; set; }
    }

    /// <summary>
    /// 改变密码接口的参数。
    /// </summary>
    [DataContract]
    public class ChangePwdParamsDto : TokenDtoBase
    {
        public ChangePwdParamsDto()
        {

        }

        /// <summary>
        /// 新密码。
        /// </summary>
        [DataMember]
        public string NewPwd { get; set; }
    }

    /// <summary>
    /// 发送一个空操作的参数。
    /// </summary>
    [DataContract]
    public class NopParamsDto : TokenDtoBase
    {
        public NopParamsDto()
        {

        }
    }

    /// <summary>
    /// 更改名字接口的参数。
    /// </summary>
    [DataContract]
    public class RenameParamsDto : TokenDtoBase
    {
        public RenameParamsDto()
        {

        }

        /// <summary>
        /// 要更改的名字。
        /// </summary>
        [DataMember]
        public string DisplayName { get; set; }
    }
    #endregion 账号管理相关

    #region 客户端辅助

    /// <summary>
    /// 修改客户端字符串的接口参数类。
    /// </summary>
    [DataContract]
    public class ModifyClentStringParamsDto : TokenDtoBase
    {
        public ModifyClentStringParamsDto()
        {
        }

        /// <summary>
        /// 对象的Id,如果是null或空字符串则改写相应角色的ClientString属性。否则改写物品/槽的属性，该物品必须被令牌代表角色直接或间接拥有的。
        /// </summary>
        [DataMember]
        public string ObjectId { get; set; }

        /// <summary>
        /// 新的客户端字符串。null表示删除该字符串。
        /// </summary>
        [DataMember]
        public string ClientString { get; set; }
    }

    /// <summary>
    /// ModifyClentString接口返回数据的类。
    /// </summary>
    [DataContract]
    public class ModifyClentReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 这里返回修改后的字符串。如果删除了则返回null。
        /// 仅当成功返回时，此成员才有用。
        /// </summary>
        [DataMember]
        public string Result { get; set; }

        /// <summary>
        /// 更改对象的Id。
        /// 仅当成功返回时，此成员才有用。
        /// </summary>
        [DataMember]
        public string ObjectId { get; set; }
    }


    /// <summary>
    /// 设置客户端扩展属性接口的参数封装类。
    /// </summary>
    [DataContract]
    public class ModifyClientExtendPropertyParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 是否移除指定属性。true移除Name中指定的属性，false追加或修改属性。
        /// </summary>
        [DataMember]
        public bool IsRemove { get; set; }

        /// <summary>
        /// 键最长64字符。（一个中文算一个字符）
        /// 没有指定键值则追加，有则修改其内容。
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// 值最长8000字符。（一个中文算一个字符）
        /// </summary>
        [DataMember]
        public string Value { get; set; }
    }

    /// <summary>
    /// ModifyClientExtendProperty 接口返回值数据封装类。
    /// </summary>
    [DataContract]
    public class ModifyClientExtendPropertyReturn : ReturnDtoBase
    {
        public ModifyClientExtendPropertyReturn()
        {

        }
        /// <summary>
        /// 这里返回修改后的字符串。如果删除了则返回null。
        /// 仅当成功返回时，此成员才有用。
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// 更改对象的Id。
        /// 仅当成功返回时，此成员才有用。
        /// </summary>
        [DataMember]
        public string Value { get; set; }
    }


    #endregion 客户端辅助

    #region 物品相关

    /// <summary>
    /// 清除变化通知接口参数数据传输类。
    /// </summary>
    [DataContract]
    public class ClearChangeDataParamsDto : TokenDtoBase
    {
    }

    /// <summary>
    /// 清除变化通知接口返回值数据传输类。
    /// </summary>
    [DataContract]
    public class ClearChangeDataResult : ReturnDtoBase
    {
    }

    /// <summary>
    /// 获取变化通知接口的参数传输类。
    /// </summary>
    [DataContract]
    public class GetChangeDataParamsDto : TokenDtoBase
    {
    }

    /// <summary>
    /// 获取变化通知接口的返回值传输类。
    /// </summary>
    [DataContract]
    public class GetChangeDataResultDto : ReturnDtoBase
    {
        /// <summary>
        /// 变化数据。
        /// </summary>
        [DataMember]
        public List<ChangeDataDto> ChangeDatas { get; set; } = new List<ChangeDataDto>();
    }

    /// <summary>
    /// 使用物品的具体项。
    /// </summary>
    [DataContract]
    public class UseItemsParamsItemDto
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public UseItemsParamsItemDto()
        {

        }
        /// <summary>
        /// 使用物品的唯一Id。
        /// </summary>
        [DataMember]
        public string Id { get; set; }

        /// <summary>
        /// 使用物品的数量，对非堆叠物品总设置1。
        /// </summary>
        [DataMember]
        public decimal Count { get; set; }
    }
    /// <summary>
    /// UseItems 接口使用的参数封装类。
    /// </summary>
    [DataContract]
    public class UseItemsParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public UseItemsParamsDto()
        {

        }

        /// <summary>
        /// 使用物品。
        /// </summary>
        [DataMember]
        public UseItemsParamsItemDto Item { get; set; } = new UseItemsParamsItemDto();
    }

    /// <summary>
    /// UseItems接口使用的返回数据封装类。
    /// </summary>
    [DataContract]
    public class UseItemsReturnDto : ChangesReturnDtoBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public UseItemsReturnDto()
        {
        }

        /// <summary>
        /// 实际成功使用的次数。
        /// </summary>
        [DataMember]
        public int SuccCount { get; set; }
    }

    /// <summary>
    /// 使用蓝图的数据传输对象。
    /// </summary>
    [DataContract]
    public partial class ApplyBlueprintParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 蓝图的模板Id。这个见另行说明文档。
        /// </summary>
        [DataMember]
        public string BlueprintId { get; set; }

        /// <summary>
        /// 要求进行工作的Id。
        /// 可能不需要。
        /// </summary>
        [DataMember]
        public string ActionId { get; set; }

        /// <summary>
        /// 要执行蓝图制造的对象集合。元素仅需要Id属性正确，当前版本忽略其他属性。
        /// 可以仅给出关键物品，在制造过成中会补足其他所需物品。如神纹升级与突破，给出神纹对象即可。
        /// </summary>
        [DataMember]
        public List<GameItemDto> GameItems { get; set; } = new List<GameItemDto>();

        /// <summary>
        /// 执行蓝图的次数，当前版本未实现该功能，保留为1(默认)。
        /// </summary>
        [DataMember]
        public int Count { get; set; } = 1;


    }

    /// <summary>
    /// ApplyBluprint接口返回时数据。
    /// </summary>
    [DataContract]
    public partial class ApplyBlueprintReturnDto : ChangesAndMailReturnDtoBase
    {
        public ApplyBlueprintReturnDto()
        {

        }

        /// <summary>
        /// 获取或设置成功执行的次数。
        /// </summary>
        [DataMember]
        public int SuccCount { get; set; }

        /// <summary>
        /// 返回命中公式的Id集合。
        /// </summary>
        [DataMember]
        public List<string> FormulaIds { get; set; } = new List<string>();

        /// <summary>
        /// 存在问题的物品的模板Id。
        /// </summary>
        [DataMember]
        public List<string> ErrorTIds { get; set; } = new List<string>();
    }

    /// <summary>
    /// SellParamsDto的详细项。
    /// </summary>
    [DataContract]
    public class SellParamsItemDto
    {
        /// <summary>
        /// 要卖物品的唯一Id。
        /// </summary>
        [DataMember]
        public string Id { get; set; }

        /// <summary>
        /// 售卖的数量。不可堆叠物品的数量一定是1。堆叠物品指定数量要么全正好能卖出去，要么，一个也卖不出。
        /// </summary>
        [DataMember]
        public decimal Count { get; set; }
    }

    /// <summary>
    /// Sell接口使用的参数封装类。
    /// </summary>
    [DataContract]
    public partial class SellParamsDto : TokenDtoBase
    {
        public SellParamsDto()
        {

        }

        /// <summary>
        /// 要出售物品的Id。这是一个事物操作，任何一个Id指定的物品无法成功卖出，将导致没有一个物品卖出。
        /// </summary>
        [DataMember]
        public List<SellParamsItemDto> Ids { get; set; } = new List<SellParamsItemDto>();
    }

    /// <summary>
    /// Sell接口返回的数据封装类。
    /// </summary>
    [DataContract]
    public partial class SellReturnDto : ChangesReturnDtoBase
    {
        public SellReturnDto()
        {

        }
    }

    /// <summary>
    /// 移动物品的详细信息。
    /// </summary>
    [DataContract]
    public class MoveItemsItemDto
    {
        /// <summary>
        /// 要移动到的容器的Id。
        /// </summary>
        [DataMember]
        public string DestContainerId { get; set; }

        /// <summary>
        /// 要移动物品的Id。
        /// </summary>
        [DataMember]
        public string ItemId { get; set; }

        /// <summary>
        /// 要移动的数量。大于物品的数量将导致调用出错。
        /// </summary>
        [DataMember]
        public decimal Count { get; set; }
    }

    /// <summary>
    /// MoveItems接口使用的参数类。
    /// </summary>
    [DataContract]
    public class MoveItemsParamsDto : TokenDtoBase
    {
        private List<MoveItemsItemDto> _Items;

        /// <summary>
        /// 要移动的物品的详细数据。
        /// </summary>
        [DataMember]
        public List<MoveItemsItemDto> Items { get => _Items ?? (_Items = new List<MoveItemsItemDto>()); set => _Items = value; }

    }

    /// <summary>
    /// MoveItems接口使用的返回类。
    /// </summary>
    [DataContract]
    public class MoveItemsReturnDto : ChangesReturnDtoBase
    {
        public MoveItemsReturnDto()
        {

        }
    }

    [DataContract]
    public class AddItemsParamsDto : TokenDtoBase
    {
        [DataMember]
        public List<GameItemDto> Items { get; set; } = new List<GameItemDto>();
    }

    /// <summary>
    /// 获取对象信息接口用的数据封装类。
    /// </summary>
    [DataContract]
    public class GetItemsParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 若取自己的对象则是对象Id集合。若取其他角色对象这里应为模板集合。特别地，取他人对象应取槽及下属对象，否则很容易返回大量重复信息。
        /// </summary>
        [DataMember]
        public List<string> Ids { get; set; } = new List<string>();

        /// <summary>
        /// 是否返回每个对象完整的孩子集合。
        /// true 则返回所有孩子；false则Children属性返回空集合。
        /// </summary>
        [DataMember]
        public bool IncludeChildren { get; set; }

        /// <summary>
        /// 如果需要取其他人的对象信息，这里应置为他人的角色id。省略或为null则取自己的对象。
        /// </summary>
        [DataMember]
        public string CharId { get; set; }
    }

    /// <summary>
    /// 获取对象信息接口返回的数据封装类。
    /// </summary>
    [DataContract]
    public class GetItemsReturnDto : ReturnDtoBase
    {
        public GetItemsReturnDto()
        {

        }

        /// <summary>
        /// 返回的物品信息。
        /// </summary>
        [DataMember]
        public List<GameItemDto> GameItems { get; set; } = new List<GameItemDto>();

        /// <summary>
        /// 如果获取了指定角色的接口，则这里返回角色信息。
        /// </summary>
        [DataMember]
        public GameCharDto GameChar { get; set; }
    }

    /// <summary>
    /// GetChangesItem 的参数封装类。
    /// </summary>
    [DataContract]
    public class GetChangesItemParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GetChangesItemParamsDto()
        {

        }
    }

    /// <summary>
    /// GetChangesItem 返回值封装类。
    /// </summary>
    [DataContract]
    public class GetChangesItemReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GetChangesItemReturnDto()
        {

        }

        /// <summary>
        /// 变化的对象数据，可能是空集合。
        /// </summary>
        [DataMember]
        public List<ChangesItemDto> Changes { get; set; } = new List<ChangesItemDto>();

    }

    /// <summary>
    /// Id和数量的封装。通常用于其他对象中。
    /// </summary>
    [DataContract]
    public partial class IdAndCountDto
    {
        /// <summary>
        /// Id。根据具体所属对象解释其含义。
        /// </summary>
        [DataMember]
        public string Id { get; set; }

        /// <summary>
        /// 数量。根据具体所属对象解释其含义。
        /// </summary>
        [DataMember]
        public decimal Count { get; set; }
    }

    #endregion 物品相关

    #region 家园建设方案

    /// <summary>
    /// 解锁家园风格接口参数封装类。
    /// </summary>
    [DataContract]
    public class AddHomelandStyleParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要激活的风格号。
        /// </summary>
        [DataMember]
        public int StyleId { get; set; }
    }

    /// <summary>
    /// 解锁家园风格接口返回值封装类。
    /// </summary>
    [DataContract]
    public class AddHomelandStyleReturnDto : ChangesReturnDtoBase
    {
    }

    /// <summary>
    /// SetHomelandStyle 接口使用的参数类。
    /// </summary>
    [DataContract]
    public class SetHomelandFenggeParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public SetHomelandFenggeParamsDto()
        {

        }

        /// <summary>
        /// 家园建设方案的集合。
        /// </summary>
        [DataMember]
        public List<HomelandFenggeDto> Fengges { get; set; } = new List<HomelandFenggeDto>();
    }

    /// <summary>
    /// SetHomelandStyle 接口返回数据的封装类。
    /// </summary>
    [DataContract]
    public class SetHomelandFenggeReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public SetHomelandFenggeReturnDto()
        {

        }
    }

    /// <summary>
    /// GetHomelandStyle 接口使用的参数类。
    /// </summary>
    [DataContract]
    public class GetHomelandFenggeParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GetHomelandFenggeParamsDto()
        {

        }
    }

    /// <summary>
    /// GetHomelandStyle 接口返回数据的封装类。
    /// </summary>
    [DataContract]
    public class GetHomelandFenggeReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GetHomelandFenggeReturnDto()
        {

        }

        /// <summary>
        /// 家园建设方案的集合。
        /// </summary>
        [DataMember]
        public List<HomelandFenggeDto> Plans { get; set; } = new List<HomelandFenggeDto>();
    }

    /// <summary>
    /// HomelandFangan。
    /// </summary>
    [DataContract]
    public partial class HomelandFenggeDto
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public HomelandFenggeDto()
        {

        }

        /// <summary>
        /// 方案号。
        /// </summary>
        [DataMember]
        public int Id { get; set; }

        /// <summary>
        /// 方案具体子项。
        /// </summary>
        [DataMember]
        public List<HomelandFanganDto> Fangans { get; set; } = new List<HomelandFanganDto>();

        /// <summary>
        /// 客户端记录一些额外信息。服务器不使用。
        /// 记录在风格对象的 ClientString 上。
        /// </summary>
        [DataMember]
        public string ClientString { get; set; }
    }

    /// <summary>
    /// 方案对象。
    /// </summary>
    [DataContract]
    public partial class HomelandFanganDto
    {
        public HomelandFanganDto()
        {

        }

        /// <summary>
        /// 唯一Id，暂时无用，但一旦生成则保持不变。
        /// </summary>
        [DataMember]
        public string Id { get; set; }

        /// <summary>
        /// 方案号。
        /// </summary>
        [DataMember]
        public int OrderNumber { get; set; }

        /// <summary>
        /// 下属具体加载物品及其位置信息。
        /// </summary>
        [DataMember]
        public List<HomelandFanganItemDto> FanganItems { get; set; } = new List<HomelandFanganItemDto>();

        /// <summary>
        /// 该方案是否被激活。
        /// </summary>
        [DataMember]
        public bool IsActived { get; set; }

        /// <summary>
        /// 客户端记录一些额外信息。服务器不使用。
        /// </summary>
        [DataMember]
        public string ClientString { get; set; }

    }

    /// <summary>
    /// 方案中的子项。
    /// </summary>
    [DataContract]
    public partial class HomelandFanganItemDto
    {
        public HomelandFanganItemDto()
        {

        }

        /// <summary>
        /// 要加入 ContainerId 指出容器的子对象Id。
        /// </summary>
        [DataMember]
        public List<string> ItemIds { get; set; } = new List<string>();

        /// <summary>
        /// 容器的Id。
        /// </summary>
        [DataMember]
        public string ContainerId { get; set; }

        /// <summary>
        /// 要替换的新的模板Id值。空表示不替换。
        /// </summary>
        [DataMember]
        public string NewTemplateId { get; set; }

        /// <summary>
        /// 客户端记录一些额外信息。服务器不使用。
        /// </summary>
        [DataMember]
        public string ClientString { get; set; }

    }

    /// <summary>
    ///  ApplyHomelandStyle 接口用的参数封装类。
    /// </summary>
    [DataContract]
    public partial class ApplyHomelandStyleParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要应用的方案号。
        /// </summary>
        [DataMember]
        public string FanganId { get; set; }
    }

    /// <summary>
    ///  ApplyHomelandStyle 接口返回值封装类。
    /// </summary>
    [DataContract]
    public partial class ApplyHomelandStyleReturnDto : ReturnDtoBase
    {
    }

    #endregion 家园建设方案

    #region 战斗相关

    /// <summary>
    /// 放弃pvp请求协助接口参数传输类。
    /// </summary>
    [DataContract]
    public class AbortPvpParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 原始战斗的Id。协助请求是针对这场战斗发出的。
        /// </summary>
        [DataMember]
        public string CombatId { get; set; }
    }

    /// <summary>
    /// 放弃pvp请求协助接口返回值传输类。
    /// </summary>
    [DataContract]
    public class AbortPvpResultDto : ReturnDtoBase
    {
    }


    /// <summary>
    /// 开始战斗的参数传输类。
    /// </summary>
    [DataContract]
    public partial class CombatStartParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public CombatStartParamsDto()
        {

        }

        /// <summary>
        /// 关卡Id。第一个小关卡Id或整个大关卡Id。
        /// </summary>
        [DataMember]
        public string DungeonId { get; set; }
    }

    /// <summary>
    /// 开始战斗的返回数据传输类
    /// </summary>
    [DataContract]
    public partial class CombatStartReturnDto : ChangesReturnDtoBaseV2
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public CombatStartReturnDto()
        {

        }

        /// <summary>
        /// 要启动的关卡。返回时可能更改为实际启动的小关卡（若指定了大关卡）。
        /// </summary>
        [DataMember]
        public string TemplateId { get; set; }

    }

    /// <summary>
    /// 结束战斗的参数传输类。
    /// </summary>
    [DataContract]
    public partial class CombatEndParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public CombatEndParamsDto()
        {
        }

        /// <summary>
        /// 关卡Id。如果是小关Id表示该小关，如果是大关Id则表示整个大关通关。
        /// </summary>
        [DataMember]
        public string DungeonId { get; set; }

        /// <summary>
        /// 获取或设置一个指示，当这个属性为true时，仅记录收益，并核准。不会试图结束当前关卡。此时忽略其他请求退出的属性。
        /// </summary>
        [DataMember]
        public bool OnlyMark { get; set; }

        /// <summary>
        /// 角色是否退出，true强制在结算后退出当前大关口，false试图继续(如果已经是最后一关则不起作用——必然退出)。
        /// </summary>
        [DataMember]
        public bool EndRequested { get; set; }

        /// <summary>
        /// 收益。
        /// </summary>
        [DataMember]
        public List<GameItemDto> GameItems { get; set; } = new List<GameItemDto>();

        /// <summary>
        /// 非玩家方或pvp中被动方的战斗战斗损失列表。
        /// </summary>
        [DataMember]
        public List<CombatLossesItemDto> PassiveCombatLosses { get; set; } = new List<CombatLossesItemDto>();

        /// <summary>
        /// 是否赢了该关卡。最后一小关或大关结算时，此数据才有效。
        /// </summary>
        [DataMember]
        public bool IsWin { get; set; }
    }

    /// <summary>
    /// 战损项类。
    /// </summary>
    [DataContract]
    public class CombatLossesItemDto
    {
        public CombatLossesItemDto()
        {

        }

        /// <summary>
        /// 可能是唯一Id（如果有），或其模板Id。
        /// </summary>
        [DataMember]
        public Guid Id { get; set; }

        /// <summary>
        /// 保留未用。将来可能用于保存野怪/坐骑的头模板Id。
        /// </summary>
        [DataMember]
        public Guid HeadTId { get; set; }

        /// <summary>
        /// 保留未用。将来可能用于保存野怪/坐骑的身体模板Id。
        /// </summary>
        [DataMember]
        public Guid BodyTId { get; set; }

        /// <summary>
        /// 数量。
        /// </summary>
        [DataMember]
        public decimal Count { get; set; }
    }

    /// <summary>
    /// 结束战斗的返回数据传输类。
    /// 变化数据中，角色下弃物槽（ExtraGuid={346A2F55-9CE8-47DE-B0E0-525FFB765A93}）的新增项，是被丢弃的物品。
    /// ChangesItems 仅当结算大关卡时这里才有数据。
    /// </summary>
    [DataContract]
    public partial class CombatEndReturnDto : ChangesReturnDtoBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public CombatEndReturnDto()
        {
        }

        /// <summary>
        /// 需要进入的下一关的Id。如果已经是最后一关结束或强制要求退出，则这里返回空引用或空字符串(string.IsNullOrEmpty测试为true)。
        /// 如果正常进入下一关，可以不必调用启动战斗的接口。
        /// </summary>
        [DataMember]
        public string NextDungeonId { get; set; }
    }

    /// <summary>
    /// SetCombatMounts接口的数据模型传输类。
    /// </summary>
    [DataContract]
    public class SetCombatMountsParamsDto : TokenDtoBase
    {
        public SetCombatMountsParamsDto()
        {

        }

        /// <summary>
        /// 元素中仅需Id有效即可。
        /// </summary>
        [DataMember]
        public List<GameItemDto> GameItemDtos { get; set; } = new List<GameItemDto>();
    }

    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public class CombatEndPvpParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 关卡Id。
        /// </summary>
        [DataMember]
        public string DungeonId { get; set; }

        /// <summary>
        /// 对方角色Id。
        /// </summary>
        [DataMember]
        public string OtherGCharId { get; set; }

        /// <summary>
        /// 是否胜利了。
        /// </summary>
        [DataMember]
        public bool IsWin { get; set; }

        /// <summary>
        /// 摧毁建筑的模板Id集合。
        /// </summary>
        [DataMember]
        public List<IdAndCountDto> Destroies { get; set; } = new List<IdAndCountDto>();

        /// <summary>
        /// 战斗对象唯一Id。从邮件的 mail.Properties["CombatId"] 属性中获取。
        /// 反击和协助才需要填写。直接pvp时可以省略。
        /// </summary>
        [DataMember]
        public string CombatId { get; set; }
    }

    [DataContract]
    public class CombatEndPvpReturnDto : ChangesAndMailReturnDtoBase
    {
        [DataMember]
        public CombatDto Combat { get; set; }
    }

    /// <summary>
    /// SetLineup接口返回的数据封装类。
    /// </summary>
    [DataContract]
    public class SetLineupReturnDto : ChangesReturnDtoBase
    {
    }

    /// <summary>
    /// 针对某个坐骑的阵容设置
    /// </summary>
    [DataContract]
    public class SetLineupItem
    {
        /// <summary>
        /// 坐骑的Id。
        /// </summary>
        [DataMember]
        public string Id { get; set; }

        /// <summary>
        /// 阵容号，从0开始。
        /// 推关阵容号是0。
        /// </summary>
        [DataMember]
        public int ForIndex { get; set; }

        /// <summary>
        /// -1表示下阵，相应的会删除 forXXX 动态属性键值。其他值会记录在 forXXX=Position。
        /// </summary>
        [DataMember]
        public int Position { get; set; }

    }

    /// <summary>
    /// SetLineup接口参数使用的数据传输类。
    /// </summary>
    [DataContract]
    public class SetLineupParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 阵容设置集合，参见 SetLineupItem 说明。
        /// </summary>
        [DataMember]
        public List<SetLineupItem> Settings { get; set; }
    }

    /// <summary>
    /// 获取战斗对象接口的返回值封装类
    /// </summary>
    [DataContract]
    public class GetCombatObjectResultDto : ReturnDtoBase
    {
        /// <summary>
        /// 战斗对象的数据，参见其具体说明。
        /// </summary>
        [DataMember]
        public CombatDto CombatObject { get; set; }

        /// <summary>
        /// 攻击者的坐骑集合。
        /// </summary>
        [DataMember]
        public List<GameItemDto> AttackerMounts { get; set; } = new List<GameItemDto>();

        /// <summary>
        /// 防御者的坐骑集合。
        /// </summary>
        [DataMember]
        public List<GameItemDto> DefenserMounts { get; set; } = new List<GameItemDto>();

        /// <summary>
        /// 战利品集合。
        /// </summary>
        [DataMember]
        public List<GameBootyDto> Booty { get; set; } = new List<GameBootyDto>();
    }

    /// <summary>
    /// 获取战斗对象接口的参数封装类
    /// </summary>
    [DataContract]
    public class GetCombatObjectParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 战斗对象的Id。
        /// </summary>
        [DataMember]
        public string CombatId { get; set; }
    }

    #endregion 战斗相关

    #region 社交相关

    /// <summary>
    /// 获取指定角色社交信息的接口返回值封装类。
    /// </summary>
    [DataContract]
    public class GetCharInfoReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 返回指定角色的家园数据，当前版本集合中仅有一个对象（角色的家园对象），但其Children属性中包含了完整的树对象。
        /// </summary>
        [DataMember]
        public List<GameItemDto> HomeLand { get; set; } = new List<GameItemDto>();

        /// <summary>
        /// 包含指定角色所有坐骑对象。
        /// </summary>
        [DataMember]
        public List<GameItemDto> Mounts { get; set; } = new List<GameItemDto>();

    }

    /// <summary>
    /// 获取指定角色社交信息的接口参数封装类
    /// </summary>
    [DataContract]
    public class GetCharInfoParamsDto : SocialDtoBase
    {
    }

    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public class RequestAssistanceParamsDto : TokenDtoBase
    {
        public RequestAssistanceParamsDto()
        {
        }

        public RequestAssistanceParamsDto(string token) : base(token)
        {
        }

        /// <summary>
        /// 要请求的好友角色Id。
        /// </summary>
        [DataMember]
        public string OtherId { get; set; }

        /// <summary>
        /// 原始战斗的Id。针对该战斗进行协助。从邮件中获取。
        /// </summary>
        [DataMember]
        public string CombatId { get; set; }

    }

    [DataContract]
    public class RequestAssistanceReturnDto : ReturnDtoBase
    {
        public RequestAssistanceReturnDto()
        {
        }
    }

    /// <summary>
    /// 获取邮件接口参数封装类数据。
    /// </summary>
    [DataContract]
    public class GetMailsParamsDto : TokenDtoBase
    {
        public GetMailsParamsDto()
        {

        }

        /// <summary>
        /// 指定邮件Id的集合，空集合表示取所有邮件。
        /// </summary>
        [DataMember]
        public List<string> Ids { get; set; } = new List<string>();
    }

    /// <summary>
    /// 获取邮件接口返回值封装数据类。
    /// </summary>
    [DataContract]
    public class GetMailsReturnDto : ReturnDtoBase
    {
        public GetMailsReturnDto()
        {

        }

        [DataMember]
        public List<GameMailDto> Mails { get; set; } = new List<GameMailDto>();
    }

    /// <summary>
    /// RemoveMails 接口返回数据封装类。
    /// </summary>
    [DataContract]
    public class RemoveMailsRetuenDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// RemoveMails参数数据封装类。
    /// </summary>
    [DataContract]
    public class RemoveMailsParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要删除的邮件Id集合。
        /// </summary>
        [DataMember]
        public List<string> Ids { get; set; } = new List<string>();
    }

    /// <summary>
    /// 获取附件接口使用的参数封装类。
    /// </summary>
    [DataContract]
    public class GetAttachmentesParamsDto : TokenDtoBase
    {
        public GetAttachmentesParamsDto()
        {
        }

        public GetAttachmentesParamsDto(string token) : base(token)
        {
        }

        /// <summary>
        /// 要获取的附件Id集合。
        /// </summary>
        [DataMember]
        public List<string> Ids { get; set; }
    }

    [DataContract]
    public class GetAttachmentesResultItemDto
    {
        public GetAttachmentesResultItemDto()
        {

        }

        /// <summary>
        /// 附件Id。
        /// </summary>
        [DataMember]
        public string Id { get; set; }

        /// <summary>
        /// 取得该附件的结果。
        /// </summary>
        [DataMember]
        public GetAttachmenteItemResult Result { get; set; }
    }

    /// <summary>
    /// 获取附件接口使用的返回值封装类。
    /// </summary>
    [DataContract]
    public class GetAttachmentesRetuenDto : ChangesReturnDtoBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GetAttachmentesRetuenDto()
        {
        }

        /// <summary>
        /// 每个附件获取的结果集合。
        /// </summary>
        [DataMember]
        public List<GetAttachmentesResultItemDto> Results { get; set; } = new List<GetAttachmentesResultItemDto>();
    }

    /// <summary>
    /// GetCharIdsForRequestFriend 接口返回值封装类。
    /// </summary>
    [DataContract]
    public class GetCharSummaryReturnDto : ReturnDtoBase
    {
        [DataMember]
        public List<CharSummaryDto> CharSummaries { get; set; } = new List<CharSummaryDto>();
    }

    /// <summary>
    /// GetCharSummary接口使用的参数封装类。
    /// </summary>
    [DataContract]
    public class GetCharSummaryParamsDto : TokenDtoBase
    {
        public GetCharSummaryParamsDto()
        {

        }

        /// <summary>
        /// 坐骑身体的模板Id集合。如果这里有数据则好友展示的坐骑要包含这种坐骑才会返回。
        /// </summary>
        [DataMember]
        public List<string> BodyTIds { get; set; } = new List<string>();

        /// <summary>
        /// 指定角色的昵称。如果省略或为null，则不限定昵称而尽量返回活跃用户。
        /// </summary>
        [DataMember]
        public string DisplayName { get; set; }
    }

    [DataContract]
    public partial class CharSummaryDto
    {
        public CharSummaryDto()
        {

        }

        /// <summary>
        /// 角色的Id。
        /// </summary>
        [DataMember]
        public string Id { get; set; }

        /// <summary>
        /// 角色的昵称。
        /// </summary>
        [DataMember]
        public string DisplayName { get; set; }

        /// <summary>
        /// 角色等级。
        /// </summary>
        [DataMember]
        public int Level { get; set; }

        /// <summary>
        /// 角色战力。
        /// </summary>
        [DataMember]
        public decimal CombatCap { get; set; }

        /// <summary>
        /// 最后一次下线时间。空表示当前在线。
        /// </summary>
        [DataMember]
        public DateTime? LastLogoutDatetime { get; set; }

        [DataMember]
        public List<GameItemDto> HomelandShows { get; set; } = new List<GameItemDto>();

        /// <summary>
        /// 玉米田中的金币数量
        /// </summary>
        [DataMember]
        public decimal GoldOfStore { get; set; }

        /// <summary>
        /// 金币数量
        /// </summary>
        [DataMember]
        public decimal Gold { get; set; }

        /// <summary>
        /// 木材数量。
        /// </summary>
        [DataMember]
        public decimal Wood { get; set; }

        /// <summary>
        /// 树林中的木材数量。
        /// </summary>
        [DataMember]
        public decimal WoodOfStore { get; set; }

        /// <summary>
        /// pvp积分。
        /// </summary>
        [DataMember]
        public decimal PvpScores { get; set; }

        /// <summary>
        /// 主基地等级。
        /// </summary>
        [DataMember]
        public int MainControlRoomLevel { get; set; }
    }

    /// <summary>
    /// RequestFriend 接口返回值封装类。
    /// </summary>
    [DataContract]
    public class RequestFriendReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 请求结果的详细情况，参见 RequestFriendResult 的说明。
        /// </summary>
        [DataMember]
        public RequestFriendResult Details { get; set; }

        [DataMember]
        public string FriendId { get; set; }
    }

    /// <summary>
    /// RequestFriend 接口使用的参数封装类。
    /// </summary>
    [DataContract]
    public class RequestFriendParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要添加的好友的角色Id。 从GetCharSummary接口获取。
        /// </summary>
        [DataMember]
        public string FriendId { get; set; }
    }

    /// <summary>
    /// GameSocialRelationship 对象的传输封装。
    /// </summary>
    [DataContract]
    public partial class GameSocialRelationshipDto : GameSocialBaseDto
    {
        /// <summary>
        /// 客体实体Id。
        /// </summary>
        [DataMember]
        public string ObjectId { get; set; }

        [DataMember]
        public int KeyType { get; set; }

        /// <summary>
        /// 左看右的友好度。
        /// 小于-5则是黑名单，大于5是好友。目前这个字段仅使用-6和6两个值。
        /// </summary>
        [DataMember]
        public int Friendliness { get; set; } = 0;

        /// <summary>
        /// 根据不同的 KeyType 值，这里的意义不同。
        /// </summary>
        [DataMember]
        public string PropertyString { get; set; }

    }

    /// <summary>
    ///  GetSocialRelationships 接口的返回数据类。
    /// </summary>
    [DataContract]
    public partial class GetSocialRelationshipsReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 社交关系对象的集合。
        /// 这个集合中，Id是当前角色Id的，表示好友和黑名单。ObjectId是当前角色Id的，表示有人申请成为好友。
        /// </summary>
        [DataMember]
        public List<GameSocialRelationshipDto> SocialRelationships { get; set; } = new List<GameSocialRelationshipDto>();

        /// <summary>
        /// 相关角色的摘要信息。用Id链接。
        /// </summary>
        [DataMember]
        public List<CharSummaryDto> Summary { get; set; } = new List<CharSummaryDto>();

        /// <summary>
        /// 相关生物/道具信息。
        /// </summary>
        [DataMember]
        public List<GameItemDto> GameItems { get; set; } = new List<GameItemDto>();

    }

    [DataContract]
    public class ConfirmRequestFriendItemDto
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public ConfirmRequestFriendItemDto()
        {

        }

        /// <summary>
        /// 申请成为自己好友的角色的Id。
        /// </summary>
        [DataMember]
        public string FriendId { get; set; }

        /// <summary>
        /// 是否拒绝好友申请。
        /// true拒绝申请，false确认申请。
        /// </summary>
        [DataMember]
        public bool IsRejected { get; set; }
    }

    /// <summary>
    /// ConfirmRequestFriend 接口参数封装类。
    /// </summary>
    [DataContract]
    public class ConfirmRequestFriendParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public ConfirmRequestFriendParamsDto()
        {

        }

        /// <summary>
        /// 参见 ConfirmRequestFriendItemDto。
        /// </summary>
        [DataMember]
        public List<ConfirmRequestFriendItemDto> Items { get; set; } = new List<ConfirmRequestFriendItemDto>();
    }

    [DataContract]
    public class ConfirmRequestFriendReturnItemDto
    {
        /// <summary>
        /// 朋友的角色Id。
        /// </summary>
        [DataMember]
        public string Id { get; set; }

        /// <summary>
        /// 申请的结果。参见 ConfirmFriendResult。
        /// </summary>
        /// <remarks></remarks>
        [DataMember]
        public ConfirmFriendResult Result { get; set; }
    }

    /// <summary>
    ///  ConfirmRequestFriend 接口返回值封装类。
    /// </summary>
    [DataContract]
    public class ConfirmRequestFriendReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public ConfirmRequestFriendReturnDto()
        {

        }

        /// <summary>
        /// 参见 ConfirmRequestFriendReturnItemDto
        /// </summary>
        [DataMember]
        public List<ConfirmRequestFriendReturnItemDto> Results { get; set; } = new List<ConfirmRequestFriendReturnItemDto>();
    }

    /// <summary>
    /// RemoveFriend 接口返回值封装类。
    /// </summary>
    [DataContract]
    public class ModifySrReturnDto : ReturnDtoBase
    {
        public ModifySrReturnDto()
        {

        }

        [DataMember]
        public string FriendId { get; set; }
    }

    /// <summary>
    /// RemoveFriend 接口参数封装类。
    /// </summary>
    [DataContract]
    public class ModifySrParamsDto : TokenDtoBase
    {
        [DataMember]
        public string FriendId { get; set; }
    }

    /// <summary>
    /// 互动Id封装常量类。
    /// Id会逐渐增加。
    /// </summary>
    public static class InteractActiveIds
    {
        /// <summary>
        /// 与家园主基地互动获得体力的Id。
        /// </summary>
        public static Guid PatForTili = new Guid("{910FC71A-3E1F-405B-8224-8182C4EC882E}");

        /// <summary>
        /// 与好友家园中的坐骑互动。
        /// </summary>
        public static Guid PatWithMounts = new Guid("{F9E4552F-9CD1-46E8-84E8-E71D946465CA}");
    }

    /// <summary>
    /// Interact 接口返回值封装类。
    /// </summary>
    [DataContract]
    public class PatForTiliReturnDto : ChangesReturnDtoBase
    {
        public PatForTiliReturnDto()
        {

        }

        /// <summary>
        /// 互动结果的详细信息，<seealso cref="PatForTiliResult"/>。
        /// </summary>
        [DataMember]
        public PatForTiliResult Code { get; set; }
    }

    /// <summary>
    /// Interact 接口参数封装类。
    /// </summary>
    [DataContract]
    public class PatForTiliParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 互动的对方角色Id。
        /// </summary>
        [DataMember]
        public string ObjectId { get; set; }

        /// <summary>
        /// 附属参数。
        /// </summary>
        [DataMember]
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// PatWithMounts 接口的参数封装类。
    /// </summary>
    [DataContract]
    public partial class PatWithMountsParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要互动的坐骑Id。
        /// </summary>
        [DataMember]
        public string MountsId { get; set; }

        /// <summary>
        /// 是否解约，true解约，false签约或增加互动。
        /// </summary>
        [DataMember]
        public bool IsRemove { get; set; }

    }

    /// <summary>
    /// PatWithMounts 接口的返回值封装类
    /// </summary>
    [DataContract]
    public partial class PatWithMountsReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 互动产生的野兽。如果条件不具备则是空集合。
        /// </summary>
        [DataMember]
        public List<ChangesItemDto> Changes { get; set; } = new List<ChangesItemDto>();

        /// <summary>
        /// 通过邮件发送了物品集合。如果没有发送邮件则是空集合。
        /// Changes属性都可能有数据（部分放入）。
        /// 其中<see cref="ChangesItemDto.ContainerId"/>是邮件对象的Id。
        /// </summary>
        [DataMember]
        public List<ChangesItemDto> MailItems { get; set; } = new List<ChangesItemDto>();

        /// <summary>
        /// 与互动坐骑的关系数据结构。
        /// </summary>
        [DataMember]
        public GameSocialRelationshipDto Relationship { get; set; }

    }

    /// <summary>
    /// GetHomelandData 接口使用的返回值封装类。
    /// </summary>
    [DataContract]
    public class GetHomelandDataReturnDto : ReturnDtoBase
    {
        public GetHomelandDataReturnDto()
        {
        }

        /// <summary>
        /// 相关坐骑的数据。
        /// </summary>
        [DataMember]
        public List<GameItemDto> Mounts { get; set; } = new List<GameItemDto>();

        /// <summary>
        /// 家园信息。顶层是家园对象，下面挂着所有家园的子对象。
        /// </summary>
        [DataMember]
        public GameItemDto Homeland { get; set; }

    }

    /// <summary>
    /// GetHomelandData 接口使用的参数封装类。
    /// </summary>
    [DataContract]
    public class GetHomelandDataParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要获取家园信息的角色Id。
        /// </summary>
        [DataMember]
        public string OtherCharId { get; set; }

    }

    /// <summary>
    /// GetPvpList 接口参数封装类。
    /// </summary>
    [DataContract]
    public class GetPvpListParamsDto : TokenDtoBase
    {
        public GetPvpListParamsDto()
        {

        }

        /// <summary>
        /// 是否强制使用钻石刷新。
        /// false,不刷新，获取当日已经刷的最后一次数据,如果今日未刷则自动刷一次。
        /// true，强制刷新，根据设计可能需要消耗资源。
        /// </summary>
        [DataMember]
        public bool IsRefresh { get; set; }
    }

    /// <summary>
    /// GetPvpList 接口返回数据封装类。
    /// </summary>
    [DataContract]
    public class GetPvpListReturnDto : ChangesReturnDtoBase
    {
        /// <summary>
        /// 可pvp角色Id列表。
        /// </summary>
        [DataMember]
        public List<string> CharIds { get; set; } = new List<string>();

        /// <summary>
        /// 相关角色的信息。
        /// </summary>
        [DataMember]
        public List<CharSummaryDto> CharSummary { get; set; } = new List<CharSummaryDto>();
    }

    public class RemoveBlackParamsDto : TokenDtoBase
    {
        public RemoveBlackParamsDto()
        {
        }

        public RemoveBlackParamsDto(string token) : base(token)
        {
        }

        /// <summary>
        /// 要移除的黑名单的角色Id。
        /// </summary>
        public string CharId { get; set; }
    }

    public class RemoveBlackReturnDto : ReturnDtoBase
    {
        public RemoveBlackReturnDto()
        {
        }
    }

    /// <summary>
    /// GetSocialRelationship接口使用的数据类。
    /// </summary>
    [DataContract]
    public class GetSocialRelationshipsParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 按此类型键值过滤，如果是空集合则返回所有类型键值的条目，这可能数据较多。
        /// 最好使用枚举 SocialKeyTypes 类型的值填充此集合。
        /// </summary>
        [DataMember]
        public List<int> KeyTypes { get; set; } = new List<int>();
    }

    #endregion 社交相关

    #region 任务成就相关
    [DataContract]
    public partial class GameMissionTemplateDto
    {
        /// <summary>
        /// 唯一Id。
        /// </summary>
        [DataMember(Name = nameof(Id))]
        public string Id { get; set; }

        /// <summary>
        /// 对属性字符串的解释。键是属性名，字符串类型。值有三种类型，decimal,string,decimal[]。
        /// 特别注意，如果需要频繁计算，则应把用于战斗的属性单独放在其他字典中。该字典因大量操作皆为读取，反装箱问题不大。
        /// </summary>
        [DataMember]
        public Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();

        /// <summary>
        /// 一个说明性文字，服务器不使用该属性。
        /// </summary>
        [DataMember]
        public string DisplayName { get; set; }

        /// <summary>
        /// 前置任务Id集合。
        /// </summary>
        [DataMember]
        public List<string> PreMissionIds { get; set; } = new List<string>();

        /// <summary>
        /// 分组号，这个字符串用于区分不同类型的任务，与数据策划约定好即可。
        /// </summary>
        [DataMember]
        public string GroupNumber { get; set; }
    }

    [DataContract]
    public class GetMissionTemplatesParamsDto : TokenDtoBase
    {
    }

    [DataContract]
    public class GetMissionTemplatesReturnDto : ReturnDtoBase
    {
        [DataMember]
        public List<GameMissionTemplateDto> Templates { get; set; } = new List<GameMissionTemplateDto>();
    }

    /// <summary>
    /// 完成任务接口的参数数据封装类。
    /// </summary>
    [DataContract]
    public class CompleteMissionParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要完成任务的模板Id。
        /// </summary>
        [DataMember]
        public string MissionTId { get; set; }
    }

    /// <summary>
    /// 完成任务接口的返回值数据封装类。
    /// </summary>
    [DataContract]
    public class CompleteMissionReturnDto : ChangesAndMailReturnDtoBase
    {
        /// <summary>
        /// 要完成任务的模板Id。
        /// </summary>
        [DataMember]
        public string MissionTId { get; set; }
    }

    /// <summary>
    /// 获取任务状态接口参数的数据封装类。
    /// </summary>
    [DataContract]
    public class GetMissionStateParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 任务模板Id的集合。空集合表示所有任务状态。
        /// </summary>
        [DataMember]
        public List<string> TIds { get; set; } = new List<string>();
    }

    /// <summary>
    /// 获取任务状态接口返回值数据封装类。
    /// </summary>
    [DataContract]
    public class GetMissionStateReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 任务模板Id的集合。
        /// </summary>
        [DataMember]
        public List<string> TIds { get; set; } = new List<string>();

        /// <summary>
        /// 任务状态，索引与TIds对应。当前=9就是完成，否则就是没完成。
        /// </summary>
        [DataMember]
        public List<int> State { get; set; } = new List<int>();
    }

    /// <summary>
    /// 获取成就奖励接口GetMissionReward返回值封装类。
    /// ChangesItems 包含变化数据。
    /// MailIds可能有邮件Id,若无法拾取的物品将发送邮件。
    /// </summary>
    [DataContract]
    public class GetMissionRewardReturnDto : ChangesAndMailReturnDtoBase
    {
    }

    /// <summary>
    /// 获取成就奖励接口GetMissionReward参数封装类。
    /// </summary>
    [DataContract]
    public class GetMissionRewardParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要获取奖励所属任务对象的Id。
        /// </summary>
        [DataMember]
        public List<string> ItemIds { get; set; } = new List<string>();
    }

    #endregion 任务成就相关

    #region 排行相关

    /// <summary>
    /// 排行的数据项。
    /// </summary>
    public partial class RankDataItemDto
    {
        /// <summary>
        /// 角色的Id。
        /// </summary>
        public string CharId { get; set; }

        /// <summary>
        /// 在全服中的排行号。从0开始，排行第一，1是排行第二，以此类推...。
        /// </summary>
        public int OrderNumber { get; set; }

        /// <summary>
        /// 战力。
        /// </summary>
        public decimal Metrics { get; set; }

        /// <summary>
        /// 角色的昵称。
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 用户头像号。
        /// </summary>
        [DataMember]
        public int IconIndex { get; set; }
    }

    /// <summary>
    /// 推关战力排行。
    /// </summary>
    [DataContract]
    public class GetRankOfTuiguanQueryReturnDto : ReturnDtoBase
    {
        [DataMember]
        public List<RankDataItemDto> Datas { get; set; } = new List<RankDataItemDto>();
    }

    /// <summary>
    /// GetRankOfTuiguanForMe接口的参数封装类。
    /// </summary>
    public class GetRankOfTuiguanForMeParamsDto : TokenDtoBase
    {
        public GetRankOfTuiguanForMeParamsDto()
        {

        }
    }

    /// <summary>
    /// GetRankOfTuiguanForMe接口的返回值封装类。
    /// </summary>
    [DataContract]
    public class GetRankOfTuiguanForMeReturnDto : ReturnDtoBase
    {
        public GetRankOfTuiguanForMeReturnDto()
        {

        }

        /// <summary>
        /// 在角色之前（排名更高）紧邻的角色，最多25个。
        /// </summary>
        [DataMember]
        public List<RankDataItemDto> Prv { get; set; } = new List<RankDataItemDto>();

        /// <summary>
        /// 在角色之后（排名更低）紧邻的角色，最多25个。
        /// </summary>
        [DataMember]
        public List<RankDataItemDto> Next { get; set; } = new List<RankDataItemDto>();

        /// <summary>
        /// 自己在服务器中的排名。0表示第一，1表示第二，以此类推。
        /// </summary>
        [DataMember]
        public int Rank { get; set; }

        /// <summary>
        /// 自己的战力积分。
        /// </summary>
        [DataMember]
        public decimal Scope { get; set; }
    }

    #endregion 排行相关

    #region 管理相关

    [DataContract]
    public class SetCombatScoreParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 前缀。
        /// </summary>
        [DataMember]
        public string Prefix { get; set; }

        /// <summary>
        /// 起始索引号。
        /// </summary>
        [DataMember]
        public int StartIndex { get; set; }

        /// <summary>
        /// 终止索引号。
        /// </summary>
        [DataMember]
        public int EndIndex { get; set; }

        /// <summary>
        /// 设置或获取pvp等级分。
        /// </summary>
        [DataMember]
        public int? PvpScore { get; set; }

        /// <summary>
        /// 保留未用，设置或获取pve等级分。
        /// </summary>
        [DataMember]
        public int? PveScore { get; set; }
    }

    [DataContract]
    public class SetCombatScoreReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public class AddPowersParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 前缀。
        /// </summary>
        [DataMember]
        public string Prefix { get; set; }

        /// <summary>
        /// 起始索引号。
        /// </summary>
        [DataMember]
        public int StartIndex { get; set; }

        /// <summary>
        /// 终止索引号。
        /// </summary>
        [DataMember]
        public int EndIndex { get; set; }

        /// <summary>
        /// 权限的按位组合。
        /// </summary>
        [DataMember]
        public CharType CharType { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public class AddPowersReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public class AccountSummery
    {
        /// <summary>
        /// 登录名。
        /// </summary>
        [DataMember]
        public string LoginName { get; set; }

        /// <summary>
        /// 密码。
        /// </summary>
        [DataMember]
        public string Pwd { get; set; }
    }

    /// <summary>
    /// 复制账号接口返回值的数据封装类。
    /// </summary>
    [DataContract]
    public class CloneAccountReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 返回账号密码集合。Item1=账号，Item2=密码。
        /// </summary>
        [DataMember]
        public List<AccountSummery> Account { get; set; } = new List<AccountSummery>();
    }

    /// <summary>
    /// 复制账号接口参数封装类。
    /// </summary>
    [DataContract]
    public class CloneAccountParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 复制多少个账号。
        /// </summary>
        [DataMember]
        public int Count { get; set; }

        /// <summary>
        /// 登录名的前缀。
        /// </summary>
        [DataMember]
        public string LoginNamePrefix { get; set; }

    }

    /// <summary>
    /// 设置角色经验接口使用的参数传输类。
    /// </summary>
    [DataContract]
    public class SetCharExpParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 指定的经验值。
        /// </summary>
        [DataMember]
        public decimal Exp { get; set; }
    }

    /// <summary>
    /// 设置角色经验接口使用的返回值传输类。
    /// </summary>
    [DataContract]
    public class SetCharExpReturnDto : ReturnDtoBase
    {
    }

    #endregion 管理相关

    #region 商城相关

    public class ConfirmPayT78ParamsDto :TokenDtoBase
    {
        /// <summary>
        /// 游戏方的订单ID。
        /// </summary>
        public string cpOrderId { get; set; }

        /// <summary>
        /// 金额。单位:分。
        /// </summary>
        public int money { get; set; }

        /// <summary>
        /// 币种。
        /// </summary>
        public string currency { get; set; }
    }

    public class ConfirmPayT78ResultDto :ReturnDtoBase
    {
    }

    /// <summary>
    /// 付费回调的返回类。
    /// </summary>
    [DataContract]
    public class PayCallbackFromT78ReturnDto
    {
        /// <summary>
        /// 0=成功，表示游戏服务器成功接收了该次充值结果通知,注意是0为成功
        /// 1=失败，表示游戏服务器无法接收或识别该次充值结果通知，如：签名检验不正确、游戏服务器接收失败
        /// </summary>
        [DataMember(Name ="ret")]
        public int Ret { get; set; }
    }

    public class GetAllShoppingTemplatesParamsDto : TokenDtoBase
    {
    }

    public class GetAllShoppingTemplatesResultDto : ReturnDtoBase
    {
        /// <summary>
        /// 所有商品模板的信息集合。
        /// </summary>
        [DataMember]
        public List<ShoppingItemDto> Templates { get; set; } = new List<ShoppingItemDto>();
    }

    /// <summary>
    /// 抽奖接口参数封装类。
    /// </summary>
    [DataContract]
    public class LotteryParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 10连抽的次数。这个属性是1，代表一次10连抽，要消耗10个抽奖卷。
        /// </summary>
        [DataMember]
        public int LotteryTypeCount10 { get; set; }

        /// <summary>
        /// 单抽次数。
        /// </summary>
        [DataMember]
        public int LotteryTypeCount1 { get; set; }

        /// <summary>
        /// 卡池号。要在哪一个卡池内抽奖。
        /// </summary>
        [DataMember]
        public string CardPoolId { get; set; }
    }

    /// <summary>
    /// 抽奖接口返回值封装类。
    /// </summary>
    [DataContract]
    public class LotteryReturnDto : ChangesReturnDtoBase
    {
        /// <summary>
        /// 此次抽奖命中的模板id集合,可能有重复。
        /// </summary>
        [DataMember]
        public List<string> TemplateIds { get; set; } = new List<string>();

        /// <summary>
        /// 此次抽奖获得的所有物品，包括已放入的和邮件的。
        /// </summary>
        [DataMember]
        public List<GameItemDto> ResultItems { get; set; } = new List<GameItemDto>();
    }

    /// <summary>
    /// GetCurrentCardPool接口参数数据封装类。
    /// </summary>
    [DataContract]
    public class GetCurrentCardPoolParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 可以指定一个时间点。省略则使用调用时刻的时间点。
        /// 时间要使用UTC时间。
        /// </summary>
        [DataMember(IsRequired = false)]
        public DateTime NowUtc { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// GetCurrentCardPool接口返回数据封装类。
    /// </summary>
    public class GetCurrentCardPoolReturnDto : ReturnDtoBase
    {
        [DataMember]
        public List<GameCardTemplateDto> Templates { get; set; } = new List<GameCardTemplateDto>();
    }

    /// <summary>
    /// 卡池配置项。
    /// </summary>
    [DataContract]
    public partial class GameCardTemplateDto
    {
        /// <summary>
        /// 唯一标识。
        /// </summary>
        [DataMember]
        public string Id { get; set; }

        /// <summary>
        /// 属性字符串。
        /// </summary>
        [DataMember]
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 注释。
        /// </summary>
        [DataMember]
        public string Remark { get; set; }

        /// <summary>
        /// 卡池标识。
        /// </summary>
        [DataMember]
        public string CardPoolGroupString { get; set; }

        /// <summary>
        /// 奖池标识。
        /// </summary>
        [DataMember]
        public string SubCardPoolString { get; set; }

        /// <summary>
        /// 是否自动使用获得的物品。
        /// </summary>
        [DataMember]
        public bool AutoUse { get; set; }

        /// <summary>
        /// 起始日期，此日期及之后此物品才会出现在卡池内。
        /// </summary>
        [DataMember]
        public DateTime StartDateTime { get; set; }

        /// <summary>
        /// 终止日期，此日期及之前此物品才会出现在卡池内
        /// </summary>
        [DataMember]
        public DateTime EndDateTime { get; set; }

        /// <summary>
        /// 周期。d天,w周,m月,y年。不填写则表示无周期(唯一周期)。
        /// </summary>
        [DataMember]
        public string SellPeriod { get; set; }

        /// <summary>
        /// 周期开始后持续有效时间,d天,w周,m月,y年。仅在有效期内才出售，不填则是永久有效（在起止期间和周期的约束下）
        /// </summary>
        [DataMember]
        public string ValidPeriod { get; set; }

        ///// <summary>
        ///// 销售周期的单位的标量数值。
        ///// </summary>
        ////[NotMapped]
        ////public decimal SellPeriodValue => !string.IsNullOrWhiteSpace(SellPeriod) && decimal.TryParse(SellPeriod[0..^1], out var val) ? val : -1;

        ///// <summary>
        ///// 销售周期的单位字符(小写)。n表示无限。
        ///// </summary>
        ////[NotMapped]
        ////public char SellPeriodUnit => string.IsNullOrWhiteSpace(SellPeriod) ? 'n' : char.ToLower(SellPeriod[^1]);

        ////[NotMapped]
        ////public char ValidPeriodUnit => string.IsNullOrWhiteSpace(ValidPeriod) ? 'n' : char.ToLower(ValidPeriod[^1]);

        ////[NotMapped]
        ////public decimal ValidPeriodValue => !string.IsNullOrWhiteSpace(ValidPeriod) && decimal.TryParse(ValidPeriod[0..^1], out var val) ? val : -1;
    }

    /// <summary>
    /// 刷新商城接口的参数封装类。
    /// </summary>
    [DataContract]
    public class RefreshShopParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要刷新的页签(属)名。前端与数据约定好使用什么页签名即可。
        /// </summary>
        [DataMember]
        public string Genus { get; set; }
    }

    /// <summary>
    /// 刷新商城接口的返回值封装类。
    /// </summary>
    [DataContract]
    public class RefreshShopReturnDto : ChangesReturnDtoBase
    {
    }

    /// <summary>
    /// 商城物品传输对象。
    /// </summary>
    [DataContract]
    public partial class ShoppingItemDto
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 最长64个字符的字符串，用于标志一组商品，服务器不理解其具体意义。
        /// </summary>
        [DataMember]
        public string Genus { get; set; }

        /// <summary>
        /// 同页签同组号的物品一同出现/消失。用于随机商店.刷新逻辑用代码实现。非随机刷商品可以不填写。
        /// </summary>
        [DataMember]
        public int? GroupNumber { get; set; }

        /// <summary>
        /// 物品模板Id。
        /// </summary>
        [DataMember]
        public string ItemTemplateId { get; set; }

        /// <summary>
        /// 是否自动使用。仅对可使用物品有效。
        /// </summary>
        [DataMember]
        public bool AutoUse { get; set; }

        /// <summary>
        /// 首次销售日期
        /// </summary>
        [DataMember]
        public DateTime StartDateTime { get; set; }

        /// <summary>
        /// 多长周期销售一次。d天,w周,m月,y年。不填写则表示无周期(唯一周期)。
        /// </summary>
        [DataMember]
        public string SellPeriod { get; set; }

        /// <summary>
        /// 销售周期的单位字符(小写)。n表示无限。
        /// </summary>
        [DataMember]
        public char SellPeriodUnit => string.IsNullOrWhiteSpace(SellPeriod) ? 'n' : char.ToLower(SellPeriod.Last());

        /// <summary>
        /// 销售周期的单位的标量数值。
        /// </summary>
        [DataMember]
        public decimal SellPeriodValue => !string.IsNullOrWhiteSpace(SellPeriod) && decimal.TryParse(SellPeriod.Substring(0, SellPeriod.Length - 1), out var val) ? val : -1;

        /// <summary>
        /// 销售的最大数量。-1表示不限制。
        /// </summary>
        [DataMember]
        public decimal MaxCount { get; set; }

        /// <summary>
        /// 销售一次持续时间,d天,w周,m月,y年。仅在有效期内才出售，不填则是永久有效
        /// </summary>
        [DataMember]
        public string ValidPeriod { get; set; }

        /// <summary>
        /// 购买开始的时间点。
        /// </summary>
        [DataMember]
        public DateTime Start { get; set; }

        /// <summary>
        /// 此次购买结束的时间点。
        /// </summary>
        [DataMember]
        public DateTime End { get; set; }

        /// <summary>
        /// 已经购买的数量。新周期此成员是0。
        /// </summary>
        [DataMember]
        public decimal CountOfBuyed { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public class GetListParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 页签的名字，前端与数据协商好即可。如果设置了则仅返回指定页签(属)的商品。null会返回所有商品。
        /// </summary>
        [DataMember]
        public List<string> Genus { get; set; } = new List<string>();
    }

    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public class GetListResultDto : ReturnDtoBase
    {
        /// <summary>
        /// 商品列表。
        /// </summary>
        [DataMember]
        public List<ShoppingItemDto> ShoppingItems { get; set; } = new List<ShoppingItemDto>();

        /// <summary>
        /// 刷新的金币数量。Item1=商品的属，Item2=刷新该属所需金币。
        /// </summary>
        [DataMember]
        public List<StringDecimalTuple> RefreshCost { get; set; } = new List<StringDecimalTuple>();
    }

    /// <summary>
    /// 购买商品接口的参数封装类。
    /// </summary>
    [DataContract]
    public class BuyParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 购买商品的模板Id。
        /// </summary>
        [DataMember]
        public string ShoppingId { get; set; }

        /// <summary>
        /// 购买的数量。
        /// </summary>
        [DataMember]
        public decimal Count { get; set; }
    }

    /// <summary>
    /// 购买商品的返回值封装类。
    /// </summary>
    [DataContract]
    public class BuyResultDto : ChangesReturnDtoBase
    {
        public BuyResultDto()
        {
        }
    }
    #endregion 商城相关

    #region 聊天及相关
    [DataContract]
    public partial class ChatMessageDto
    {
        public ChatMessageDto()
        {
        }

        /// <summary>
        /// 频道Id。
        /// </summary>
        [DataMember]
        public string ChannelId { get; set; }

        /// <summary>
        /// 发送者的Id。
        /// </summary>
        [DataMember]
        public string Sender { get; set; }

        /// <summary>
        /// 发送的内容。当前版本仅支持字符串。
        /// </summary>
        [DataMember]
        public string Message { get; set; }

        /// <summary>
        /// 发送该消息的时间点,使用utc时间。这也是一个不严格非唯一的时间戳，<see cref="DateTime.Ticks"/>可以被认为是一个时间戳。
        /// </summary>
        [DataMember]
        public DateTime SendDateTimeUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 用户昵称。
        /// </summary>
        [DataMember]
        public string DisplayName { get; set; }

        /// <summary>
        /// 用户头像号。
        /// </summary>
        [DataMember]
        public int IconIndex { get; set; }
    }

    /// <summary>
    /// 发送消息的数据封装类。
    /// </summary>
    [DataContract]
    public class SendMessageDto
    {
        /// <summary>
        /// 频道Id。
        /// 频道Id分为几种："70EEA684-4E1F-4C1E-B987-765BE2845538"是世界频道。
        /// "5E36F81C-00BD-446D-B781-E48F2B088591"是当前所处工会频道。
        /// "角色Id,对方角色Id"是私聊频道，如"0D1F3AA5-0A11-4C69-9DCF-B15992B0D01C,91B7295C-0B3B-4563-A14A-4BF739001F94"，
        /// 表示0D1F3AA5-0A11-4C69-9DCF-B15992B0D01C,91B7295C-0B3B-4563-A14A-4BF739001F94聊个角色的私聊频道，注意，这两个id会自动排序，永远都是升序排序。
        /// </summary>
        [DataMember]
        public string ChannelId { get; set; }

        /// <summary>
        /// 发送的内容。当前版本仅支持字符串。
        /// </summary>
        [DataMember]
        public string Message { get; set; }

    }

    /// <summary>
    /// 获取消息接口的参数封装类。
    /// </summary>
    [DataContract]
    public class GetMessagesParamsDto : TokenDtoBase
    {
    }

    /// <summary>
    /// 获取消息接口的返回数据封装类
    /// </summary>
    [DataContract]
    public class GetMessagesReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 获取的消息。如果没有，这个结合可能为空。
        /// </summary>
        [DataMember]
        public List<ChatMessageDto> Messages { get; set; } = new List<ChatMessageDto>();
    }

    /// <summary>
    /// 发送消息接口的参数封装类。
    /// </summary>
    [DataContract]
    public class SendMessagesParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 发送的消息。
        /// </summary>
        [DataMember]
        public List<SendMessageDto> Messages { get; set; } = new List<SendMessageDto>();
    }

    /// <summary>
    /// 发送消息接口的返回值封装类。
    /// </summary>
    [DataContract]
    public class SendMessagesReturnDto : ReturnDtoBase
    {
    }
    #endregion 聊天及相关

    #region 行会相关

    [DataContract]
    public class GetAllGuildParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 搜索工会的名字。省略或为null则不限定工会的名称。
        /// </summary>
        [DataMember]
        public string DisplayName { get; set; }

        /// <summary>
        /// 随机的获取多少工会。DisplayName 省略或为null则不限定工会的名称，使用此数字限定返回的最大数量。
        /// </summary>
        [DataMember]
        public int Top { get; set; }
    }

    [DataContract]
    public class GetAllGuildReturnDto
    {
        /// <summary>
        /// 所有工会信息集合。
        /// </summary>
        [DataMember]
        public List<GameGuildDto> Guilds { get; set; } = new List<GameGuildDto>();
    }

    [DataContract]
    public class ModifyPermissionsReturnDto : ReturnDtoBase
    {
    }

    [DataContract]
    public class ModifyPermissionsParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要修改成的权限。10=普通会员，14=管理。
        /// </summary>
        [DataMember]
        public int Division { get; set; }

        /// <summary>
        /// 要修改的角色id集合。
        /// </summary>
        [DataMember]
        public List<string> CharIds { get; set; } = new List<string>();
    }


    [DataContract]
    public class RemoveGuildMemberParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要除名成员。
        /// 只能移除比自己权限低的会员。
        /// 不能移除会长。
        /// 可以移除自己。
        /// </summary>
        [DataMember]
        public List<string> CharIds { get; set; } = new List<string>();
    }

    [DataContract]
    public class RemoveGuildMemberReturnDto : ReturnDtoBase
    {
    }

    [DataContract]
    public class AccepteGuildMemberParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要批准加入的角色id集合。
        /// </summary>
        [DataMember]
        public List<string> CharIds { get; set; } = new List<string>();

        /// <summary>
        /// 是否接受。true表示接受，false表示拒绝。
        /// </summary>
        [DataMember]
        public bool IsAccept { get; set; }
    }

    [DataContract]
    public class AccepteGuildMemberReturnDto : ReturnDtoBase
    {
    }

    [DataContract]
    public class RequestJoinGuildParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 申请加入的工会Id。
        /// </summary>
        [DataMember]
        public string GuildId { get; set; }
    }

    [DataContract]
    public class RequestJoinGuildReturnDto : ReturnDtoBase
    {
    }

    [DataContract]
    public class GetGuildParamsDto : TokenDtoBase
    {
    }

    [DataContract]
    public class GetGuildReturnDto : ReturnDtoBase
    {
        [DataMember]
        public GameGuildDto Guild { get; set; } = new GameGuildDto();

        /// <summary>
        /// 个人已经完成的工会任务模板id。
        /// </summary>
        [DataMember]
        public List<string> DoneGuildMissionTIds { get; set; } = new List<string>();

        /// <summary>
        /// 工会今天发布的任务模板id。
        /// </summary>
        [DataMember]
        public List<string> GuildMissionTIds { get; set; } = new List<string>();

    }

    [DataContract]
    public class SetGuildParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 行会名。
        /// </summary>
        [DataMember]
        public string DisplayName { get; set; }

        /// <summary>
        /// 工会图标。
        /// </summary>
        [DataMember]
        public int IconIndex { get; set; }

        /// <summary>
        /// 是否自动接受加入申请。
        /// </summary>
        [DataMember]
        public bool AutoAccept { get; set; }

        /// <summary>
        /// 设置行会公告。
        /// </summary>
        [DataMember]
        public string Bulletin { get; set; }
    }

    [DataContract]
    public class SetGuildReturnDto : ReturnDtoBase
    {
    }


    [DataContract]
    public class DeleteGuildParamsDto : TokenDtoBase
    {
    }

    [DataContract]
    public class DeleteGuildReturnDto : ReturnDtoBase
    {
    }

    [DataContract]
    public class SendGuildParamsDto : SocialDtoBase
    {
    }

    [DataContract]
    public class SendGuildReturnDto : ReturnDtoBase
    {
    }

    [DataContract]
    public partial class GameGuildDto
    {
        [DataMember]
        public string Id { get; set; }

        /// <summary>
        /// 行会自身属性。
        /// GuildIconIndex=图标索引。AutoAccept=是否自动接受申请加入的成员(true接受,false不接受)
        /// </summary>
        [DataMember]
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 角色显示用的名字。
        /// </summary>
        [DataMember]
        public string DisplayName { get; set; }

        /// <summary>
        /// 下属对象（当前主要是建筑物）集合。
        /// </summary>
        [DataMember]
        public List<GameItemDto> Items { get; set; } = new List<GameItemDto>();

        /// <summary>
        /// 成员信息集合。
        /// </summary>
        [DataMember]
        public List<GuildMemberDto> Members { get; set; } = new List<GuildMemberDto>();
    }

    /// <summary>
    /// 行会成员信息类。
    /// 根据需求变化极有可能会添加成员。
    /// </summary>
    [DataContract]
    public class GuildMemberDto
    {
        /// <summary>
        /// 成员的角色Id。
        /// </summary>
        [DataMember]
        public string Id { get; set; }

        /// <summary>
        /// 成员的分工。0=正在申请，10=普通会员，14=管理，20=会长。
        /// </summary>
        [DataMember]
        public int Title { get; set; }

        /// <summary>
        /// 角色等级。
        /// </summary>
        [DataMember]
        public int Level { get; set; }

        /// <summary>
        /// 角色昵称。
        /// </summary>
        [DataMember]
        public string DisplayName { get; set; }

        /// <summary>
        /// 人物图标。
        /// </summary>
        [DataMember]
        public int IconIndex { get; set; }

        /// <summary>
        /// 战力。
        /// </summary>
        [DataMember]
        public decimal Power { get; set; }
    }

    [DataContract]
    public class CreateGuildReturnDto : ChangesReturnDtoBaseV2
    {
        /// <summary>
        /// 返回行会信息。
        /// </summary>
        [DataMember]
        public GameGuildDto Guild { get; set; }
    }

    [DataContract]
    public class CreateGuildParamsDto : TokenDtoBase
    {
        public CreateGuildParamsDto()
        {

        }

        /// <summary>
        /// 行会名。
        /// </summary>
        [DataMember]
        public string DisplayName { get; set; }

        /// <summary>
        /// 工会图标。
        /// </summary>
        [DataMember]
        public int IconIndex { get; set; }

        /// <summary>
        /// 是否自动接受加入申请。
        /// </summary>
        [DataMember]
        public bool AutoAccept { get; set; }

    }

    [DataContract]
    public class GuildMissionDto
    {
        /// <summary>
        /// 工会任务模板Id。
        /// </summary>
        [DataMember]
        public string GuildTemplateId { get; set; }

    }

    [DataContract]
    public class GetGuildMissionReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 工会任务信息集合。这里进返回当前工会的任务。
        /// </summary>
        [DataMember]
        public List<GuildMissionDto> GuildMissions { get; set; } = new List<GuildMissionDto>();

        /// <summary>
        /// 角色完成的工会任务信息。这里是角色完成的任务信息。
        /// </summary>
        [DataMember]
        public List<GuildMissionDto> CharDones { get; set; } = new List<GuildMissionDto>();
    }

    [DataContract]
    public class GetGuildMissionParamsDto : TokenDtoBase
    {
    }
    #endregion 行会相关
}
#pragma warning restore IDE0074 // 使用复合分配
#pragma warning restore IDE0057 // 使用范围运算符
