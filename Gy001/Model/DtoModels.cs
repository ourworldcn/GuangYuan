using Game.Social;
using GuangYuan.GY001.UserDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GY2021001WebApi.Models
{
#pragma warning disable IDE0074 // 使用复合分配

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

    #region 基础数据封装类
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
    [DataContract]
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
        /// 创建该对象的通用协调时间。
        /// </summary>
        [DataMember]
        public DateTime CreateUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 数量。
        /// </summary>
        [DataMember(Name = nameof(Count))]
        public decimal? Count { get; set; }

        Dictionary<string, object> _Properties;
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
    }

    /// <summary>
    /// 返回数据对象的基类。
    /// </summary>
    [DataContract]
    public class ReturnDtoBase
    {
        /// <summary>
        /// 返回时指示是否有错误。false表示正常完成，true表示有错误发生。
        /// </summary>
        [DataMember]
        public bool HasError { get; set; }

        /// <summary>
        /// 调试信息。调试状态下返回时填写。
        /// </summary>
        [DataMember]
        public string DebugMessage { get; set; }

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

    #endregion 基础数据封装类

    #region 接口特定数据封装类

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

    #region 战斗相关数据

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
    public partial class CombatStartReturnDto
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

        /// <summary>
        /// 返回时指示是否有错误。false表示正常计算完成，true表示规则校验认为有误。返回时填写。
        /// </summary>
        [DataMember]
        public bool HasError { get; set; }

        /// <summary>
        /// 调试信息。调试状态下返回时填写。
        /// </summary>
        [DataMember]
        public string DebugMessage { get; set; }
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
        /// 角色是否退出，true强制在结算后退出当前大关口，false试图继续(如果已经是最后一关则不起作用——必然退出)。
        /// </summary>
        [DataMember]
        public bool EndRequested { get; set; }

        /// <summary>
        /// 收益。
        /// </summary>
        [DataMember]
        public List<GameItemDto> GameItems { get; set; } = new List<GameItemDto>();
    }

    /// <summary>
    /// 结束战斗的返回数据传输类
    /// </summary>
    [DataContract]
    public partial class CombatEndReturnDto
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

        /// <summary>
        /// 返回时指示是否有错误。false表示正常计算完成，true表示规则校验认为有误。返回时填写。
        /// </summary>
        [DataMember]
        public bool HasError { get; set; }

        /// <summary>
        /// 调试信息。调试状态下返回时填写。
        /// </summary>
        [DataMember]
        public string DebugMessage { get; set; }

        /// <summary>
        /// 获取变化物品的数据。仅当结算大关卡时这里才有数据。
        /// </summary>
        [DataMember]
        public List<ChangesItemDto> ChangesItems { get; set; } = new List<ChangesItemDto>();

    }

    #endregion 战斗相关数据

    /// <summary>
    /// 设置客户端扩展属性接口的参数封装类。
    /// </summary>
    [DataContract]
    public class ModifyClientExtendPropertyParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 是否移除指定属性。true移除Name中指定的属性，false追加或修改属性。
        /// </summary>
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
        public List<GameItemDto> GameItemDtos { get; set; } = new List<GameItemDto>();
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
    public partial class ApplyBlueprintReturnDto
    {
        public ApplyBlueprintReturnDto()
        {

        }

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
        /// 获取变化物品的数据。仅当成功返回时有意义。
        /// </summary>
        [DataMember]
        public List<ChangesItemDto> ChangesItems { get; set; } = new List<ChangesItemDto>();

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
        public List<string> Ids { get; set; } = new List<string>();
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
        List<MoveItemsItemDto> _Items;

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
        /// 要获取信息的Id集合。
        /// </summary>
        [DataMember]
        public List<string> Ids { get; set; } = new List<string>();

        /// <summary>
        /// 是否返回每个对象完整的孩子集合。
        /// true 则返回所有孩子；false则Children属性返回空集合。
        /// </summary>
        [DataMember]
        public bool IncludeChildren { get; set; }
    }

    /// <summary>
    /// 获取对象信息接口返回的数据封装类。
    /// </summary>
    [DataContract]
    public class GetItemsReturnDto
    {
        public GetItemsReturnDto()
        {

        }

        /// <summary>
        /// 返回的物品信息。
        /// </summary>
        [DataMember]
        public List<GameItemDto> GameItems { get; set; } = new List<GameItemDto>();
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

    #region 家园建设方案

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
        /// 下属具体加载物品及其位置信息
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

    #endregion 接口特定数据封装类

    #region 社交相关

    /// <summary>
    /// 获取邮件接口参数封装类数据。
    /// </summary>
    [DataContract]
    public class GetMailsParamsDto : TokenDtoBase
    {
        public GetMailsParamsDto()
        {

        }
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
        public List<string> Ids { get; set; }
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
    }

    /// <summary>
    /// GetCharSummary 接口返回值封装类。
    /// </summary>
    [DataContract]
    public class GetCharSummaryReturnDto : ReturnDtoBase
    {
        [DataMember]
        public List<CharSummaryDto> CharSummaries { get; set; } = new List<CharSummaryDto>();
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
    }

    /// <summary>
    /// RequestFriend 接口返回值封装类。
    /// </summary>
    [DataContract]
    public class RequestFriendReturnDto : ReturnDtoBase
    {
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

        /// <summary>
        /// 左看右的友好度。
        /// 小于-5则是黑名单，大于5是好友。目前这个字段仅使用-6和6两个值。
        /// </summary>
        [DataMember]
        public sbyte Friendliness { get; set; } = 0;
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
    }

    /// <summary>
    /// RemoveFriend 接口返回值封装类。
    /// </summary>
    [DataContract]
    public class ModifySrReturnDto : ReturnDtoBase
    {
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
        static public Guid PatForTili = new Guid("{910FC71A-3E1F-405B-8224-8182C4EC882E}");
    }

    /// <summary>
    /// Interact 接口返回值封装类。
    /// </summary>
    [DataContract]
    public class InteractReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 变化的对象集合。这个集合仅包括属于自己的对象。社交行为可能影响对方的属性，不在此属性内返回。
        /// </summary>
        [DataMember]
        public List<ChangesItemDto> Changes { get; set; } = new List<ChangesItemDto>();
    }

    /// <summary>
    /// Interact 接口参数封装类。
    /// </summary>
    [DataContract]
    public class InteractParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 互动的Id。参见 InteractActiveIds 类的说明。
        /// </summary>
        [DataMember]
        public string ActiveId { get; set; }

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

    #endregion 社交相关
#pragma warning restore IDE0074 // 使用复合分配

}
