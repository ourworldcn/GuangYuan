using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace GY2021001WebApi.Models
{
    #region 基础数据

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
        public static readonly Guid ZuojiTou = new Guid("{A06B7496-F631-4D51-9872-A2CC84A56EAB}");

        /// <summary>
        /// 当前装备的坐骑身体容器模板Id。已废弃
        /// </summary>
        public static readonly Guid ZuojiShen = new Guid("{7D191539-11E1-49CD-8D0C-82E3E5B04D31}");
        #endregion 废弃模板Id

        #region 坐骑相关Id

        /// <summary>
        /// 坐骑头和身体需要一个容器组合起来。此类容器的模板Id就是这个。
        /// </summary>
        public static readonly Guid ZuojiZuheRongqi = new Guid("{6E179D54-5836-4E0B-B30D-756BD07FF196}");

        /// <summary>
        /// 坐骑组合中的头容器。
        /// </summary>
        public static readonly Guid ZuojiZuheTou = new Guid("{740FEBF3-7472-43CB-8A10-798F6C61335B}");

        /// <summary>
        /// 坐骑组合中的身体容器。
        /// </summary>
        public static readonly Guid ZuojiZuheShenti = new Guid("{F8B1987D-FDF3-4090-9E9B-EBAF1DB2DCCD}");
        #endregion 坐骑相关Id

        /// <summary>
        /// 当前坐骑的容器Id。
        /// </summary>
        public static readonly Guid DangqianZuoqiCao = new Guid("{B19EE5AB-57E3-4513-8228-9F2A8364358E}");

        /// <summary>
        /// 角色模板Id。当前只有一个模板。
        /// </summary>
        public static readonly Guid CharTemplateId = new Guid("{0CF39269-6301-470B-8527-07AF29C5EEEC}");

        #endregion 固定模板Id

        public const string LevelPropertyName = "lv";
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
        /// 令牌。
        /// </summary>
        [DataMember]
        public string Token { get; set; }
    }

    #endregion 基础数据

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
        public List<GameCharDto> GameChars { get; } = new List<GameCharDto>();

    }

    /// <summary>
    /// 登录接口参数类。
    /// </summary>
    public class LoginParamsDto
    {
        public LoginParamsDto()
        {

        }

        /// <summary>
        /// 登录名。
        /// </summary>
        public string LoginName { get; set; }

        /// <summary>
        /// 密码。
        /// </summary>
        public string Pwd { get; set; }

        /// <summary>
        /// 登录客户端类型。目前可能值是IOS或Android。
        /// </summary>
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
    public class NopParamsDto : TokenDtoBase
    {
        public NopParamsDto()
        {

        }
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
        public DateTime CreateUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 数量。
        /// </summary>
        [DataMember(Name = nameof(Count))]
        public decimal? Count { get; set; }

        /// <summary>
        /// 对属性字符串的解释。键是属性名，字符串类型。值有三种类型，decimal,string,decimal[]。
        /// 特别注意，如果需要频繁计算，则应把用于战斗的属性单独放在其他字典中。该字典因大量操作皆为读取，反装箱问题不大。
        /// 属性集合。"Properties":{"atk":102,"qult":500,"catalogId":"shfdkjshfkjskfh=="}
        /// </summary>
        [DataMember(Name = nameof(Properties))]
        public Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();

        /// <summary>
        /// 所属Id。
        /// </summary>
        [DataMember]
        public string OwnerId { get; set; }

        /// <summary>
        /// 下属物品对象。
        /// </summary>
        [DataMember]
        public List<GameItemDto> Children { get; } = new List<GameItemDto>();

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

        }

        /// <summary>
        /// 为该坐骑设置身体。注意这将导致强制改写该对象的模板和Children以适应坐骑的结构。
        /// </summary>
        /// <param name="gameCharDto"></param>
        public void SetBody(GameCharDto gameCharDto)
        {

        }
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
        [DataMember(Name = nameof(Id))]
        public string Id { get; set; }

        /// <summary>
        /// 角色自身属性。
        /// </summary>
        [DataMember(Name = nameof(Properties))]
        public Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();

        /// <summary>
        /// 角色显示用的名字。
        /// </summary>
        [DataMember(Name = nameof(DisplayName))]
        public string DisplayName { get; set; }

        /// <summary>
        /// 客户端要记录的一些属性，这个属性客户端可以随意更改，服务器不使用。
        /// </summary>
        [DataMember(Name = nameof(ClientGutsString))]
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
        [IgnoreDataMember]
        public GameItemDto CurrentMounts
        {
            get
            {
                var id = DtoHelper.ToBase64String(DtoConstant.DangqianZuoqiCao);
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
    /// 客户端生成虚拟物品给服务器发送信息时，使用该类封装数据。
    /// </summary>
    [DataContract]
    public class ClientGameItemDto
    {
        public ClientGameItemDto()
        {

        }

        /// <summary>
        /// 简化模板Id。
        /// </summary>
        [DataMember]
        public int GId { get; set; }

        /// <summary>
        /// 属性集合。
        /// atkne是攻击资质,nhpne是最大血量资质，qltne是质量资质。
        /// </summary>
        [DataMember]
        public Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();

        /// <summary>
        /// 数量。对于堆叠物品是其一堆的数量，比如金币，堆叠且无堆叠上线(GId是指明为金币的)。
        /// </summary>
        [DataMember]
        public decimal? Count { get; set; }
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
        /// 新的客户端字符串。
        /// </summary>
        [DataMember]
        public string ClientString { get; set; }
    }

    #region 战斗相关数据

    /// <summary>
    /// 开始战斗的参数传输类。
    /// </summary>
    [DataContract]
    public class CombatStartParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public CombatStartParamsDto()
        {

        }

        /// <summary>
        /// 关卡Id。如果是小关Id表示该小关，如果是大关Id则表示整个大关通关。
        /// </summary>
        [DataMember]
        public string DungeonId { get; set; }
    }

    /// <summary>
    /// 开始战斗的返回数据传输类
    /// </summary>
    [DataContract]
    public class CombatStartReturnDto : TokenDtoBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public CombatStartReturnDto()
        {

        }
    }

    /// <summary>
    /// 结束战斗的参数传输类。
    /// </summary>
    [DataContract]
    public class CombatEndParamsDto : TokenDtoBase
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
        /// 角色是否退出，true强制推出当前大关口，false试图继续(如果已经是最后一关则不起作用)。
        /// </summary>
        [DataMember]
        public bool IsBreak { get; set; }

        /// <summary>
        /// 收益。
        /// </summary>
        [DataMember]
        public List<GameItemDto> Gameitems { get; set; }
    }

    /// <summary>
    /// 结束战斗的返回数据传输类
    /// </summary>
    [DataContract]
    public class CombatEndReturnDto : TokenDtoBase
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
    #endregion 战斗相关数据
}
