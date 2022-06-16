using Game.Social;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ObjectPool;
using OW.Game;
using OW.Game.Item;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GuangYuan.GY001.BLL
{
    public class AdminManagerOptions
    {
    }

    public class GameAdminManager : GameManagerBase<AdminManagerOptions>
    {
        public GameAdminManager()
        {
        }

        public GameAdminManager(IServiceProvider service) : base(service)
        {
        }

        public GameAdminManager(IServiceProvider service, AdminManagerOptions options) : base(service, options)
        {
        }

        /// <summary>
        /// 设置战斗积分。
        /// </summary>
        /// <param name="datas"></param>
        public void SetCombatScore(SetCombatScoreDatas datas)
        {
            if (datas.EndIndex < datas.StartIndex)
            {
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.ErrorMessage = "StartIndex 要小于或等于 EndIndex。";
                return;
            }
            if (!datas.GameChar.CharType.HasFlag(CharType.SuperAdmin) && !datas.GameChar.CharType.HasFlag(CharType.Admin))   //若权限不够
            {
                datas.ErrorCode = ErrorCodes.ERROR_NO_SUCH_PRIVILEGE;
                datas.ErrorMessage = "权限不够";
                return;
            }
            var db = datas.UserDbContext;
            var loginNames = new List<string>();  //登录名数组
            for (int i = datas.StartIndex; i <= datas.EndIndex; i++)
            {
                loginNames.Add($"{datas.Prefix}{i}");
            }
            var userIds = db.Set<GameChar>().Where(c => loginNames.Contains(c.GameUser.LoginName)).Select(c => c.Id).ToArray();
            using var dwUsers = World.CharManager.LockOrLoadWithCharIds(userIds, userIds.Length * World.CharManager.Options.DefaultLockTimeout);    //顺序连锁
            foreach (var item in loginNames)
            {
                var gu = World.CharManager.GetUserFromLoginName(item);
                if (gu is null)
                    continue;
                if (!(datas.PvpScore is null))
                {
                    gu.CurrentChar.GetPvpObject().ExtraDecimal = datas.PvpScore;
                    //gu.CurrentChar.GetPveT().Count = datas.PveScore;
                    World.CharManager.NotifyChange(gu);
                }
            }

        }

        /// <summary>
        /// 是否有超管或运营的权限。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsSuperAdminOrAdmin(GameChar gameChar) =>
            gameChar.CharType.HasFlag(CharType.SuperAdmin) || gameChar.CharType.HasFlag(CharType.Admin);

        /// <summary>
        /// 给指定的一组账号增加权限。
        /// </summary>
        /// <param name="datas"></param>
        public void AddPowers(AddPowersDatas datas)
        {
            if (datas.EndIndex < datas.StartIndex)
            {
                datas.ErrorCode = ErrorCodes.ERROR_BAD_ARGUMENTS;
                datas.ErrorMessage = "StartIndex 要小于或等于 EndIndex。";
                return;
            }
            if (!datas.GameChar.CharType.HasFlag(CharType.SuperAdmin))   //若不是超管
            {
                datas.ErrorCode = ErrorCodes.ERROR_NO_SUCH_PRIVILEGE;
                datas.ErrorMessage = "需要超管权限";
                return;
            }
            var loginNames = new List<string>();  //登录名数组
            for (int i = datas.StartIndex; i <= datas.EndIndex; i++)
            {
                loginNames.Add($"{datas.Prefix}{i}");
            }
            var db = datas.UserDbContext;
            var userIds = db.Set<GameChar>().Where(c => loginNames.Contains(c.GameUser.LoginName)).Select(c => c.Id).ToArray();
            using var dwUsers = World.CharManager.LockOrLoadWithCharIds(userIds, userIds.Length * World.CharManager.Options.DefaultLockTimeout);    //顺序连锁
            foreach (var item in loginNames)
            {
                var gu = World.CharManager.GetUserFromLoginName(item);
                if (gu is null)
                    continue;
                gu.CurrentChar.CharType |= datas.CharType;
                World.CharManager.NotifyChange(gu);
            }
        }

        /// <summary>
        /// 发送邮件，寄送物品。
        /// </summary>
        /// <param name="datas"></param>
        public void SendThing(SendThingDatas datas)
        {
            using (var dw = datas.LockUser())
            {
                if (dw is null)
                    return;
                World.CharManager.Nope(datas.GameChar.GameUser);    //延迟登出时间
                if (!IsSuperAdminOrAdmin(datas.GameChar))    //若没有权限
                {
                    datas.ErrorCode = ErrorCodes.ERROR_NO_SUCH_PRIVILEGE;
                    return;
                }
            }
            var gis = World.ItemManager.ToGameItems(datas.Propertyies);
            var coll = gis.Select(c => (c, c.Properties.GetGuidOrDefault("ptid")));
            var mail = new GameMail()
            {
                Body = datas.Mail.Body,
                Subject = datas.Mail.Subject,
                CreateUtc = datas.Mail.CreateUtc,
            };
            OwHelper.Copy(datas.Mail.Properties, mail.Properties);
            World.SocialManager.SendMail(mail, datas.Tos.Select(c => OwConvert.ToGuid(c)), SocialConstant.FromSystemId, coll);
        }

        /// <summary>
        /// 强制下线。
        /// </summary>
        /// <param name="datas"></param>
        public void LetOut(LetOutDatas datas)
        {
            using (var dw = datas.LockUser())
            {
                if (dw is null)
                    return;
                World.CharManager.Nope(datas.GameChar.GameUser);    //延迟登出时间
                if (!IsSuperAdminOrAdmin(datas.GameChar))    //若没有权限
                {
                    datas.ErrorCode = ErrorCodes.ERROR_NO_SUCH_PRIVILEGE;
                    return;
                }
            }
            var gu = World.CharManager.GetUserFromLoginName(datas.LoginName);
            if (gu is null)
            {
                datas.ErrorCode = ErrorCodes.ERROR_NO_SUCH_USER;
                datas.ErrorMessage = "无此用户或不在线";
                return;
            }
            var succ = World.CharManager.Logout(gu, LogoutReason.Force);
            if (!succ)
            {
                datas.ErrorCode = ErrorCodes.WAIT_TIMEOUT;
                return;
            }
            return;
        }

        /// <summary>
        /// 复制当前账号。
        /// </summary>
        /// <param name="datas"></param>
        public void CloneUser(CloneUserDatas datas)
        {
            if (!IsSuperAdminOrAdmin(datas.GameChar))    //若没有权限
            {
                datas.ErrorCode = ErrorCodes.ERROR_NO_SUCH_PRIVILEGE;
                return;
            }
            string prefix = datas.LoginNamePrefix ?? "vip";
            var context = datas.UserDbContext;
            var loginNames = context.Set<GameUser>().Where(c => c.LoginName.StartsWith(prefix)).Select(c => (c.LoginName));    //获取已有登录名
            var listNames = loginNames.ToList();
            var lastIndex = listNames.Count == 0 ? -1 : loginNames.ToList().Select(c =>
                 {
                     var str = c[prefix.Length..];
                     return int.TryParse(str, out var number) ? number : 0;
                 }).Max();   //最大尾号
            List<(string, string, string)> list = new List<(string, string, string)>(); //(登录名，密码，角色名)
            List<GameUser> users = new List<GameUser>();
            for (int i = 0; i < datas.Count; i++)   //生成账号登录数据
            {
                list.Add(($"{prefix}{lastIndex + 1 + i}", NewPassword(4), CnNames.GetName(VWorld.IsHit(0.5))));
            }
            using var dwLns = OwHelper.LockWithOrder(list.Select(c => c.Item1).OrderBy(c => c),
                (ln, timeout) => World.LockStringAndReturnDisposer(ref ln, timeout), TimeSpan.FromSeconds(5));   //锁定所有登录名。
            if (dwLns is null)  //若锁定超时
            {
                datas.ErrorCode = ErrorCodes.WAIT_TIMEOUT;
                return;
            }
            list.ForEach(c =>   //生成账号
            {
                var gu = new GameUser()
                {
                    LoginName = c.Item1,
                };
                World.CharManager.SetPwd(gu, c.Item2);
                World.EventsManager.Clone(datas.GameChar.GameUser, gu);
                users.Add(gu);
            });
            if (!World.CharManager.Delete(list.Select(c => c.Item1)))
            {
                datas.HasError = true;
                datas.ErrorMessage = "至少有一个账户无法覆盖信息。";
                return;
            }
            users.ForEach(c => c.DbContext.SaveChanges());
            datas.Account.AddRange(list.Select(c => (c.Item1, c.Item2)));
            Task.Run(() => users.ForEach(c => c.Dispose()));
        }

        private List<char> _PwdChars;
        public List<char> PwdChars
        {
            get
            {
                if (_PwdChars is null)
                {
                    _PwdChars = new List<char>();
                    var start = Convert.ToByte('a');
                    for (int i = 0; i < 26; i++)
                        _PwdChars.Add(Convert.ToChar(start + i));
                    start = Convert.ToByte('A');
                    for (int i = 0; i < 26; i++)
                        _PwdChars.Add(Convert.ToChar(start + i));
                    start = Convert.ToByte('0');
                    for (int i = 0; i < 10; i++)
                        _PwdChars.Add(Convert.ToChar(start + i));

                }
                return _PwdChars;
            }
        }

        /// <summary>
        /// 生成新密码。
        /// </summary>
        /// <param name="bits">位数。</param>
        /// <returns>生成新密码。</returns>
        public string NewPassword(int bits)
        {
            StringBuilder sb = StringBuilderPool.Shared.Get();
            try
            {
                var length = PwdChars.Count;
                for (int i = 0; i < bits; i++)
                    sb.Append(PwdChars[VWorld.WorldRandom.Next(length)]);
                return sb.ToString();
            }
            finally
            {
                StringBuilderPool.Shared.Return(sb);
            }
        }

        /// <summary>
        /// 导出用户存储在指定流对象中。
        /// </summary>
        /// <param name="datas"></param>
        public void ExportUsers(ExportUsersDatas datas)
        {
            using (var dw = datas.LockUser())   //尽早解锁避免连锁死锁问题
            {
                if (dw is null)
                    return;
                World.CharManager.Nope(datas.GameChar.GameUser);    //延迟登出时间
                if (!IsSuperAdminOrAdmin(datas.GameChar))    //若没有权限
                {
                    datas.ErrorCode = ErrorCodes.ERROR_NO_SUCH_PRIVILEGE;
                    return;
                }
            }
            var db = datas.UserDbContext;
            var lns = db.Set<GameUser>().Where(c => EF.Functions.Like(c.LoginName, $"{datas.LoginNamePrefix}%")).Select(c => c.LoginName).
                 AsEnumerable().Where(c =>
                 {
                     var suffix = c[datas.LoginNamePrefix.Length..];
                     return int.TryParse(suffix, out var index) && index >= datas.StartIndex && index <= datas.EndIndex;
                 });
            JsonSerializerOptions options = new JsonSerializerOptions();
            using var writer = new Utf8JsonWriter(datas.Store);
            writer.WriteStartArray();
            foreach (var ln in lns)
            {
                using var dwu = World.CharManager.LockOrLoad(ln, out var gu);
                if (dwu is null)    //忽略错误
                    continue;
                JsonSerializer.Serialize(writer, gu, typeof(GameUser), options);
            }
            writer.WriteEndArray();
        }

        /// <summary>
        /// 导入用户数据。
        /// </summary>
        /// <param name="datas"></param>
        public void ImportUsers(ImportUsersDatas datas)
        {
            using (var dw = datas.LockUser())   //尽早解锁避免连锁死锁问题
            {
                if (dw is null)
                    return;
                World.CharManager.Nope(datas.GameChar.GameUser);    //延迟登出时间
                if (!IsSuperAdminOrAdmin(datas.GameChar))    //若没有权限
                {
                    datas.ErrorCode = ErrorCodes.ERROR_NO_SUCH_PRIVILEGE;
                    return;
                }
            }
            JsonSerializerOptions options = new JsonSerializerOptions() { };
#if DEBUG
            var buff = ArrayPool<byte>.Shared.Rent(16 * 1024 * 1024);
            var s = datas.Store.Read(buff, 0, buff.Length);
            var str = Encoding.UTF8.GetString(buff, 0, s);
            GameUser[] ary = JsonSerializer.Deserialize<GameUser[]>(str);
            ArrayPool<byte>.Shared.Return(buff);
#else
            //var buff = ArrayPool<byte>.Shared.Rent(102400);
            //var s = datas.Store.Read(buff, 0, buff.Length);
            //var str = Encoding.UTF8.GetString(buff,0,s);
            GameUser[] ary = JsonSerializer.DeserializeAsync<GameUser[]>(datas.Store, options).Result;
#endif
            var eve = World.EventsManager;
            Array.ForEach(ary, c => eve.JsonDeserialized(c));
            var lns = ary.Select(c => c.LoginName);
            if (!World.CharManager.Delete(lns))
            {
                datas.HasError = true;
                datas.ErrorMessage = "至少有一个用户不能正常添加";
                return;
            }
            Array.ForEach(ary, c => { c.DbContext.SaveChanges(); });

            Array.ForEach(ary, c => { c.Dispose(); });
        }

        /// <summary>
        /// 获取服务器的一些统计信息。
        /// </summary>
        /// <param name="datas"></param>
        public void GetInfos(GetInfosDatas datas)
        {
            using (var dw = datas.LockUser())
            {
                if (dw is null)
                    return;
                World.CharManager.Nope(datas.GameChar.GameUser);    //延迟登出时间
                if (!IsSuperAdminOrAdmin(datas.GameChar))    //若没有权限
                {
                    datas.ErrorCode = ErrorCodes.ERROR_NO_SUCH_PRIVILEGE;
                    return;
                }
            }
            datas.OnlineCount = World.CharManager.Id2OnlineChar.Count;
            datas.TotalCount = World.CharManager.Id2GameChar.Count;
            datas.LoadRate = (decimal)World.CharManager.Id2GameChar.Count / Environment.ProcessorCount / 10000;
        }

        /// <summary>
        /// 重启服务器。
        /// </summary>
        /// <param name="datas"></param>
        public void Reboot(RebootDatas datas)
        {
            using (var dw = datas.LockUser())
            {
                if (dw is null)
                    return;
                World.CharManager.Nope(datas.GameChar.GameUser);    //延迟登出时间
                if (!IsSuperAdminOrAdmin(datas.GameChar))    //若没有权限
                {
                    datas.ErrorCode = ErrorCodes.ERROR_NO_SUCH_PRIVILEGE;
                    return;
                }
            }
            foreach (var gc in World.CharManager.Id2GameChar.Values)
            {
                World.CharManager.Logout(gc.GameUser, LogoutReason.SystemShutdown);
            }
        }

        /// <summary>
        /// 封停账号。
        /// </summary>
        /// <param name="datas"></param>
        public void Block(BlockDatas datas)
        {
            using (var dw = datas.LockUser())
            {
                if (dw is null)
                    return;
                World.CharManager.Nope(datas.GameChar.GameUser);    //延迟登出时间
                if (!IsSuperAdminOrAdmin(datas.GameChar))    //若没有权限
                {
                    datas.ErrorCode = ErrorCodes.ERROR_NO_SUCH_PRIVILEGE;
                    return;
                }
            }
            using var dwUser = World.CharManager.LockOrLoad(datas.LoginName, out var gu);
            if (dwUser is null)
            {
                datas.ErrorCode = ErrorCodes.ERROR_NO_SUCH_USER;
                return;
            }
            gu.BlockUtc = datas.BlockUtc;
            World.CharManager.NotifyChange(gu);
            return;
        }
    }

    /// <summary>
    /// 设置战斗积分接口的数据封装类。
    /// </summary>
    public class SetCombatScoreDatas : ComplexWorkGameContext
    {
        public SetCombatScoreDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public SetCombatScoreDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public SetCombatScoreDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 前缀。
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// 起始索引号。
        /// </summary>
        public int StartIndex { get; set; }

        /// <summary>
        /// 终止索引号。
        /// </summary>
        public int EndIndex { get; set; }

        /// <summary>
        /// 设置或获取pvp等级分。
        /// </summary>
        public int? PvpScore { get; set; }

        /// <summary>
        /// 设置或获取pve等级分。
        /// </summary>
        public int? PveScore { get; set; }
    }

    /// <summary>
    /// 追加权限接口的数据封装类。
    /// </summary>
    public class AddPowersDatas : ComplexWorkGameContext
    {
        public AddPowersDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public AddPowersDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public AddPowersDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 前缀。
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// 起始索引号。
        /// </summary>
        public int StartIndex { get; set; }

        /// <summary>
        /// 终止索引号。
        /// </summary>
        public int EndIndex { get; set; }

        /// <summary>
        /// 权限的按位组合。
        /// </summary>
        public CharType CharType { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class SendThingDatas : ComplexWorkGameContext
    {
        public SendThingDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public SendThingDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public SendThingDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

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
        public Dictionary<string, object> Propertyies { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 发送给角色的Id。"{DA5B83F6-BB73-4961-A431-96177DE82BFF}"表示发送给所有角色。
        /// </summary>
        public List<string> Tos { get; set; } = new List<string>();

        public GameMail Mail { get; set; }
    }

    /// <summary>
    /// 强制下线。
    /// </summary>
    public class LetOutDatas : ComplexWorkGameContext
    {
        public LetOutDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public LetOutDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public LetOutDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 强制下线的用户。
        /// </summary>
        public string LoginName { get; set; }
    }

    public class BlockDatas : ComplexWorkGameContext
    {
        public BlockDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public BlockDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public BlockDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 封停账号的登录名。
        /// </summary>
        public string LoginName { get; set; }

        /// <summary>
        /// 封停的截止时间点，使用Utc时间。
        /// </summary>
        public DateTime BlockUtc { get; set; }
    }

    public class RebootDatas : ComplexWorkGameContext
    {
        public RebootDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public RebootDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public RebootDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }
    }


    public class GetInfosDatas : ComplexWorkGameContext
    {
        public GetInfosDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public GetInfosDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public GetInfosDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 在线用户数。
        /// </summary>
        public int OnlineCount { get; set; }

        /// <summary>
        /// 内存中总计用户数。
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 负载率。[0,1]之间的一个数。
        /// </summary>
        public decimal LoadRate { get; set; }
    }

    public class ImportUsersDatas : ComplexWorkGameContext
    {
        public ImportUsersDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public ImportUsersDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public ImportUsersDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        public Stream Store { get; set; }
    }

    public class ExportUsersDatas : ComplexWorkGameContext
    {
        public ExportUsersDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public ExportUsersDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public ExportUsersDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 登录名的前缀部分。
        /// </summary>
        public string LoginNamePrefix { get; set; }

        /// <summary>
        /// 后缀数字开始索引。
        /// </summary>
        public int StartIndex { get; set; }

        /// <summary>
        /// 后缀数字截至索引。
        /// </summary>
        public int EndIndex { get; set; }

        /// <summary>
        /// 存储的流对象。会向其追加数据。
        /// </summary>
        public Stream Store { get; set; }
    }

    /// <summary>
    /// 复制账号接口使用的参数封装类。
    /// </summary>
    public class CloneUserDatas : ComplexWorkGameContext
    {
        public CloneUserDatas([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public CloneUserDatas([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public CloneUserDatas([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        /// <summary>
        /// 复制的次数。
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// 登录名的前缀。
        /// </summary>
        public string LoginNamePrefix { get; set; }

        /// <summary>
        /// 返回账号密码集合。Item1=账号，Item2=密码。
        /// </summary>
        public List<(string, string)> Account { get; } = new List<(string, string)>();
    }
}
