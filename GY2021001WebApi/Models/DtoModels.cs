using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace GY2021001WebApi.Models
{
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
    /// 使用令牌的基类。
    /// </summary>
    [DataContract]
    public class TokenDtoBase
    {
        /// <summary>
        /// 令牌。
        /// </summary>
        [DataMember(Name = nameof(Token))]
        public string Token { get; set; }

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
    public class GameItemTemplateDto
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
        Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();
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

    }

}
