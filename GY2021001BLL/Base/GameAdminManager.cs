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

        public void CloneUser(CloneUserDatas datas)
        {
            const string prefix = "vip";
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
                list.Add(($"vip{lastIndex + 1 + i}", NewPassword(4), CnNames.GetName(VWorld.IsHit(0.5))));
            }
            foreach (var item in list)  //生成账号
            {
                using var dwUser = World.CharManager.LockOrLoad(item.Item1, out var user);
                IDisposable disposable;
                if (dwUser is null)
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
            }
            //if (!datas.GameChar.CharType.HasFlag(CharType.Admin))    //若没有权限
            //{
            //    datas.ErrorCode = ErrorCodes.ERROR_IMPLEMENTATION_LIMIT;
            //    return;
            //}
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
