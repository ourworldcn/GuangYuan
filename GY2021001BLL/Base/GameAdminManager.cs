﻿using Game.Social;
using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using Microsoft.EntityFrameworkCore;
using OW.Game;
using OW.Game.Item;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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
                //if (!datas.GameChar.CharType.HasFlag(CharType.Admin))    //若没有权限
                //{
                //    datas.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                //    return;
                //}
            }
            var gis = World.ItemManager.ToGameItems(datas.Propertyies);
            var coll = gis.Select(c => (c, c.Properties.GetGuidOrDefault("ptid")));
            var mail = new GameMail() { };
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
                //if (!datas.GameChar.CharType.HasFlag(CharType.Admin))    //若没有权限
                //{
                //    datas.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                //    return;
                //}
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

        public void CloneUser(CloneUserDatas datas)
        {
            string prefix = datas.LoginNamePrefix ?? "vip";
            var context = datas.UserContext;
            var loginNames = context.Set<GameUser>().Where(c => c.LoginName.StartsWith(prefix)).Select(c => (c.LoginName));    //获取已有登录名
            var listNames = loginNames.ToList();
            var lastIndex = listNames.Count == 0 ? -1 : loginNames.ToList().Select(c =>
                 {
                     var str = c[prefix.Length..];
                     return int.TryParse(str, out var number) ? number : 0;
                 }).Max();   //最大尾号
            List<(string, string, string)> list = new List<(string, string, string)>();
            for (int i = 0; i < datas.Count; i++)   //生成账号数据
            {
                list.Add(($"{prefix}{lastIndex + 1 + i}", NewPassword(4), CnNames.GetName(VWorld.IsHit(0.5))));
            }
            foreach (var item in list)  //生成账号
            {
                using var dwUser = World.CharManager.LockOrLoad(item.Item1, out var user);
                IDisposable disposable;
                if (dwUser is null) //若没有已经存在的用户
                {
                    if (VWorld.GetLastError() != ErrorCodes.ERROR_NO_SUCH_USER) //若未知错误
                        continue;
                    user = World.CharManager.CreateNewUserAndLock(item.Item1, item.Item2);
                    if (user is null) //若未知错误
                        continue;
                    user.CurrentChar.DisplayName = item.Item3;
                    user.Timeout = TimeSpan.FromSeconds(1);
                    disposable = DisposerWrapper.Create(c => World.CharManager.Unlock(c), user);
                }
                else
                    disposable = null;
                using var dwUser1 = disposable;
                CloneUser(datas.GameChar.GameUser, user);
                if (!World.CharManager.IsOnline(user.CurrentChar.Id))   //若不是登录账户
                    user.Timeout = TimeSpan.FromMinutes(1);
                World.CharManager.NotifyChange(user);
            }
            datas.Account.AddRange(list.Select(c => (c.Item1, c.Item2)));
        }

        /// <summary>
        /// 复制一个账号信息到另一个账号。
        /// </summary>
        /// <param name="srcUser"></param>
        /// <param name="destUser"></param>
        private void CloneUser(GameUser srcUser, GameUser destUser)
        {
            for (int i = 0; i < srcUser.GameChars.Count; i++)
            {
                var srcChar = srcUser.GameChars[i];
                GameChar destChar;
                if (i >= destUser.GameChars.Count)
                {
                    var charTemplate = World.ItemTemplateManager.GetTemplateFromeId(ProjectConstant.CharTemplateId);
                    destChar = new GameChar();
                    World.EventsManager.GameCharCreated(destChar, charTemplate, destUser, CnNames.GetName(World.IsHit(0.5)), null); //TO DO
                    destUser.GameChars.Add(destChar);
                }
                else
                    destChar = destUser.GameChars[i];
                GameItem srcSlot, destSlot;

                #region 坐骑
                srcSlot = srcChar.GetZuojiBag();
                destSlot = destChar.GetZuojiBag();
                //清理目标已有坐骑
                destSlot.Children.RemoveAll(c =>
                {
                    //destUser.DbContext.Remove(c);
                    return true;
                });
                foreach (var srcItem in srcSlot.Children)
                {
                    var destItem = World.ItemManager.CloneMounts(srcItem, srcItem.TemplateId);
                    World.ItemManager.ForcedAdd(destItem, destSlot);
                }
                #endregion 坐骑

                #region 动物
                srcSlot = srcChar.GetShoulanBag();
                destSlot = destChar.GetShoulanBag();
                foreach (var srcItem in srcSlot.Children)
                {
                    var destItem = World.ItemManager.CloneMounts(srcItem, srcItem.TemplateId);
                    World.ItemManager.ForcedAdd(destItem, destSlot);
                }
                #endregion 动物

                #region 货币
                srcSlot = srcChar.GetCurrencyBag();
                destSlot = destChar.GetCurrencyBag();
                foreach (var srcItem in srcSlot.Children)
                {
                    var destItem = destSlot.Children.FirstOrDefault(c => c.TemplateId == srcItem.TemplateId);
                    if (destItem is null)
                    {
                        destItem = new GameItem();
                        World.EventsManager.GameItemCreated(destItem, srcItem.TemplateId);
                        World.ItemManager.Clone(srcItem, destItem);
                        World.ItemManager.AddItem(destItem, destSlot);
                    }
                    else
                        World.ItemManager.Clone(srcItem, destItem);
                }
                #endregion 货币

                #region 道具
                srcSlot = srcChar.GetItemBag();
                destSlot = destChar.GetItemBag();
                foreach (var srcItem in srcSlot.Children)
                {
                    var destItem = destSlot.Children.FirstOrDefault(c => c.TemplateId == srcItem.TemplateId);
                    if (destItem is null)
                    {
                        destItem = new GameItem();
                        World.EventsManager.GameItemCreated(destItem, srcItem.TemplateId);
                        World.ItemManager.Clone(srcItem, destItem);
                        World.ItemManager.AddItem(destItem, destSlot);
                    }
                    else
                        World.ItemManager.Clone(srcItem, destItem);
                }
                #endregion 道具

                #region 时装
                srcSlot = srcChar.GetShizhuangBag();
                destSlot = destChar.GetShizhuangBag();
                foreach (var srcItem in srcSlot.Children)
                {
                    var destItem = destSlot.Children.FirstOrDefault(c => c.TemplateId == srcItem.TemplateId);
                    if (destItem is null)
                    {
                        destItem = new GameItem();
                        World.EventsManager.GameItemCreated(destItem, srcItem.TemplateId);
                        World.ItemManager.Clone(srcItem, destItem);
                        World.ItemManager.AddItem(destItem, destSlot);
                    }
                    else
                        World.ItemManager.Clone(srcItem, destItem);
                }
                #endregion 时装
                //背包容量
                {
                    GameItem srcItem, destItem;
                    srcItem = srcChar.GetShoulanBag();
                    destItem = destChar.GetShoulanBag();
                    destItem.Properties[ProjectConstant.ContainerCapacity] = srcItem.Properties.GetDecimalOrDefault(ProjectConstant.ContainerCapacity);
                }
                //等级
                World.CharManager.SetExp(destChar, srcChar.Properties.GetDecimalOrDefault("exp"));
            }
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
            StringBuilder sb = new StringBuilder();
            var length = PwdChars.Count;
            for (int i = 0; i < bits; i++)
                sb.Append(PwdChars[VWorld.WorldRandom.Next(length)]);
            return sb.ToString();
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
                //if (!datas.GameChar.CharType.HasFlag(CharType.Admin))    //若没有权限
                //{
                //    datas.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                //    return;
                //}
            }
            var db = datas.UserContext;
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
                //if (!datas.GameChar.CharType.HasFlag(CharType.Admin))    //若没有权限
                //{
                //    datas.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                //    return;
                //}
            }
            var ary = JsonSerializer.DeserializeAsync<GameUser[]>(datas.Store).Result;
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
                //if (!datas.GameChar.CharType.HasFlag(CharType.Admin))    //若没有权限
                //{
                //    datas.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                //    return;
                //}
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
                //if (!datas.GameChar.CharType.HasFlag(CharType.Admin))    //若没有权限
                //{
                //    datas.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                //    return;
                //}
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
                //if (!datas.GameChar.CharType.HasFlag(CharType.Admin))    //若没有权限
                //{
                //    datas.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
                //    return;
                //}
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
    /// 
    /// </summary>
    public class SendThingDatas : ComplexWorkDatasBase
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
    }

    /// <summary>
    /// 强制下线。
    /// </summary>
    public class LetOutDatas : ComplexWorkDatasBase
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

    public class BlockDatas : ComplexWorkDatasBase
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

    public class RebootDatas : ComplexWorkDatasBase
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


    public class GetInfosDatas : ComplexWorkDatasBase
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

    public class ImportUsersDatas : ComplexWorkDatasBase
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

    public class ExportUsersDatas : ComplexWorkDatasBase
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
    public class CloneUserDatas : ComplexWorkDatasBase
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
