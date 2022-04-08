using Microsoft.Extensions.ObjectPool;
using OW.Game;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GuangYuan.GY001.BLL.GeneralManager
{
    public class ChatManagerOptions
    {
        /// <summary>
        /// 消息超时。超过这个时间未被读取的消息将被丢弃。
        /// 默认值1分钟。
        /// </summary>
        public TimeSpan MessageTimeout { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// 频道超时。超过此时长未使用的频道将被回收。
        /// 默认值2分钟。
        /// </summary>
        public TimeSpan ChannelTimeout { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// 用户超时。用户在指定时间内没有动作则被认为已经离开，并自动清理资源。
        /// </summary>
        public TimeSpan UserTimeOut { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>
        /// 锁定频道对象的默认超时时间。
        /// 默认值1秒。
        /// </summary>
        public TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(1);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>同一个线程中持有任何<see cref="ChatUser"/>对象的锁后可以试图锁定任意<see cref="ChatChannel"/>对象，但反之不行。</remarks>
    public class ChatManager : GameManagerBase<ChatManagerOptions>, IDisposable
    {
        #region 构造函数及相关

        public ChatManager()
        {
            Initialize();
        }

        public ChatManager(IServiceProvider service) : base(service)
        {
            Initialize();
        }

        public ChatManager(IServiceProvider service, ChatManagerOptions options) : base(service, options)
        {
            Initialize();
        }

        private void Initialize()
        {
            var task = Task.Factory.StartNew(() =>  //后台清理函数
            {
                while (true)
                {
                    foreach (var channel in Id2Channel.Values)
                    {
                        if (!Lock(channel, TimeSpan.Zero))
                            continue;
                        using var dwChannel = DisposerWrapper.Create(Unlock, channel);
                        //if (0 == channel.UserIds.Count && !channel.SuppressDispose)    //若已空且可以卸载
                        //{
                        //    Id2Channel.TryRemove(channel.Id, out var tmp);
                        //    channel.Dispose();
                        //}
                        var now = DateTime.UtcNow;
                        while (channel.Messages.TryPeek(out var msg) && now - msg.SendDateTimeUtc > Options.MessageTimeout)
                        {
                            channel.Messages.TryDequeue(out _);
                        }
                        //var stamp = Id2Users.Join(channel.UserIds, c => c.Key, c => c, (l, r) => l.Value.Timestamp).Max();
                    }
                    Thread.Sleep(1);
                    if (Environment.HasShutdownStarted)
                        break;
                }
            }, TaskCreationOptions.LongRunning);
        }

        #endregion 构造函数及相关

        #region 属性及相关

        private readonly ReaderWriterLockSlim _ReaderWriterLockSlim = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        private ConcurrentDictionary<string, ChatChannel> _Id2Channel;
        /// <summary>
        /// 键是频道的id,值该频道信息。
        /// </summary>
        public ConcurrentDictionary<string, ChatChannel> Id2Channel
        {
            get
            {
                if (_Id2Channel is null)
                    Interlocked.CompareExchange(ref _Id2Channel, new ConcurrentDictionary<string, ChatChannel>(), null);
                return _Id2Channel;
            }
        }

        private ConcurrentDictionary<string, ChatUser> _UserInfos;
        private bool disposedValue;

        /// <summary>
        /// 用户的信息字典，键是用户的Id，值用户信息类。
        /// </summary>
        public ConcurrentDictionary<string, ChatUser> Id2Users
        {
            get
            {
                if (_UserInfos is null)
                    Interlocked.CompareExchange(ref _UserInfos, new ConcurrentDictionary<string, ChatUser> { }, null);
                return _UserInfos;
            }
        }

        #endregion 属性及相关

        #region 功能相关

        /// <summary>
        /// 锁定用户。
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        protected bool LockUser(string userId)
        {
            if (!Id2Users.TryGetValue(userId, out var user))
                return false;
            return Lock(user, Options.LockTimeout);
        }

        /// <summary>
        /// 锁定指定聊天用户对象。与<see cref="Unlock(ChatUser)"/>配对使用。
        /// </summary>
        /// <param name="user"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        protected bool Lock(ChatUser user, TimeSpan timeout)
        {
            if (!Monitor.TryEnter(user, timeout))
                return false;
            if (user.Disposed)
            {
                Monitor.Exit(user);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 锁定指定频道。
        /// </summary>
        /// <param name="channelId"></param>
        /// <returns></returns>
        protected bool LockChannel(string channelId)
        {
            if (!Id2Channel.TryGetValue(channelId, out var channel))
                return false;
            return Lock(channel, Options.LockTimeout);
        }

        /// <summary>
        /// 锁定指定频道对象。与<see cref="Unlock(ChatChannel)"/>配对使用。
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="timeout"></param>
        /// <returns>true成功锁定，false超时或锁定到了无效对象。</returns>
        public bool Lock(ChatChannel channel, TimeSpan timeout)
        {
            if (!Monitor.TryEnter(channel, timeout))
            {
                return false;
            }
            try
            {
                if (channel.Disposed)
                {
                    Monitor.Exit(channel);
                    return false;
                }
            }
            catch (Exception)
            {
                Monitor.Exit(channel);
                throw;
            }
            return true;
        }

        /// <summary>
        /// 解锁指定频道对象，与<see cref="Lock(ChatChannel, TimeSpan)"/>配对使用。
        /// </summary>
        /// <param name="channel"></param>
        public void Unlock(ChatChannel channel)
        {
            Monitor.Exit(channel);
        }

        /// <summary>
        /// 解锁指定聊天用户对象，与<see cref="Lock(ChatUser, TimeSpan)"/>配对使用。
        /// </summary>
        /// <param name="user"></param>
        public void Unlock(ChatUser user)
        {
            Monitor.Exit(user);
        }

        /// <summary>
        /// 锁定指定id的频道对象，如果没有则首先创建。
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="timeout"></param>
        /// <param name="channel"></param>
        /// <returns>false无法锁定，true成功锁定对象，此后需要使用<see cref="Unlock(ChatChannel)"/>解锁。</returns>
        public bool GetOrCreateAndLockChannel(string channelId, TimeSpan timeout, out ChatChannel channel)
        {
            channel = Id2Channel.GetOrAdd(channelId, c => new ChatChannel()
            {
                Id = channelId,
                Timeout = Options.ChannelTimeout
            });
            return Lock(channel, timeout);
            //DateTime now = DateTime.UtcNow;
            //if (!Monitor.TryEnter(channel, timeout))  //若超时
            //    return false;

            //if (channel.Disposed)    //若并发争用导致锁定了无效对象
            //{
            //    do
            //    {
            //        Monitor.Exit(channel);  //释放旧对象
            //        channel = GetOrCreateChannel(channelId, creator);   //再次获取新对象
            //        if (Lock(channel, OwHelper.ComputeTimeout(now, timeout))) //若锁定成功
            //            break;
            //        if (OwHelper.ComputeTimeout(now, timeout) == TimeSpan.Zero)
            //            return false;
            //    } while (true);
            //}
            //return !channel.Disposed;
        }

        /// <summary>
        /// 创建或锁定用户信息对象。
        /// </summary>
        /// <param name="charId"></param>
        /// <param name="timeout"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool GetOrCreateAndLockUser(string charId, TimeSpan timeout, out ChatUser user)
        {
            user = Id2Users.GetOrAdd(charId, c => new ChatUser()
            {
                LastWrite = DateTime.MinValue,
                Timestamp = DateTime.MinValue,
                Id = charId,
            });
            return Lock(user, timeout);
        }

        /// <summary>
        /// 创建或加入一个频道。
        /// </summary>
        /// <remarks>如果创建一个私聊频道，需要连续加入两个用户即可。</remarks>
        /// <param name="charId">用户id。</param>
        /// <param name="channelId">频道id。</param>
        /// <param name="timeout">频道闲置超时。</param>
        /// <param name="creator">返回该频道对象。</param>
        /// <returns>true成功加入频道，否则返回false。</returns>
        public bool JoinOrCreateChannel(string charId, string channelId, TimeSpan timeout, Func<ChatChannel> creator)
        {
            if (!GetOrCreateAndLockUser(charId, timeout, out var user))    //若无法创建用户
                return false;
            using var dhUser = DisposeHelper.Create(Monitor.Exit, user);

            if (!GetOrCreateAndLockChannel(channelId, timeout, out var channel))   //若无法创建频道
                return false;
            using var dhChannel = DisposeHelper.Create(Unlock, channel);
            if (user.Channels.Contains(channel))
                return false;
            else
            {
                channel.UserIds.Add(charId);
                user.Channels.Add(channel);
            }
            return true;
        }

        /// <summary>
        /// 指定用户离开指定频道，
        /// </summary>
        /// <param name="charId"></param>
        /// <param name="channelId"></param>
        /// <param name="timeout">超时。</param>
        /// <returns>true成功离开，false指定用户原本就不在频道中，或指定频道不存在。</returns>
        public bool LeaveChannel(string charId, string channelId, TimeSpan timeout)
        {
            if (Id2Users.TryGetValue(charId, out var user))    //若指定用户不存在
                return false;
            if (!GetOrCreateAndLockUser(charId, timeout, out user))    //若无法锁定用户
                return false;
            using var dh1 = DisposeHelper.Create(Monitor.Exit, user);
            if (!Id2Channel.TryGetValue(channelId, out var channel))    //若无此频道
                return false;
            if (!Lock(channel, Options.LockTimeout))    //若锁定超时
                return false;
            using var dh = DisposeHelper.Create(c => Unlock(c), channel);
            return user.Channels.Remove(channel);
        }

        #endregion 功能相关

        #region 用户操作

        /// <summary>
        /// 发送消息。
        /// </summary>
        /// <param name="datas"></param>
        public void SendMessages(SendMessageContext datas)
        {
            if (!GetOrCreateAndLockUser(datas.CharId, Options.LockTimeout, out var user))   //若无法锁定用户的列表
                return;
            using var dhUser = DisposeHelper.Create(Unlock, user);
            if (!GetOrCreateAndLockChannel(datas.ChannelId, Options.LockTimeout, out var channel))  //若无法锁定频道。
                return;
            using var dhChannel = DisposeHelper.Create(Unlock, channel);
            if (!user.Channels.Contains(channel)) //若不在指定频道中
            {
                JoinOrCreateChannel(datas.CharId, datas.ChannelId, Options.LockTimeout, null);
            }
            var message = ChatMessagePool.Shard.Get();
            message.ChannelName = datas.ChannelId;
            message.Message = datas.Message;
            message.ChannelName = datas.ChannelId;
            message.Sender = datas.CharId;
            message.ExString = datas.ExString;
            channel.Messages.Enqueue(message);
            channel.UserIds.Add(datas.CharId);
        }

        /// <summary>
        /// 收消息。
        /// </summary>
        /// <param name="datas"></param>
        public void GetMessages(GetMessageContext datas)
        {
            var charId = datas.CharId;
            if (!GetOrCreateAndLockUser(datas.CharId, Options.LockTimeout, out var user))   //若无法锁定用户
                return;
            using var dhUser = DisposeHelper.Create(Unlock, user);
            var coll = Id2Channel.Values.Where(c =>
            {
                if (string.IsNullOrWhiteSpace(c.Id))
                    return false;
                var ary = c.Id.Split(OwHelper.CommaArrayWithCN);
                if (ary.Length != 2)
                    return false;
                var ids = ary.Select(c => OwConvert.ToGuid(c)).ToArray();
                var id = OwConvert.ToGuid(charId);
                return ids.Contains(id);
            });   //私聊频道
            foreach (var channel in user.Channels.Concat(coll).Distinct())  //频道信息
            {
                if (!Lock(channel, Options.LockTimeout))
                    continue;
                using var dh = DisposeHelper.Create(Unlock, channel);

                datas.Messages.AddRange(channel.Messages.Where(c => c.SendDateTimeUtc > user.Timestamp && c.Sender != charId)); //在上次获取信息之后的信息
            }
            user.Timestamp = datas.NowUtc;
        }

        /// <summary>
        /// 创建用户。
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public bool CreateUser(string userId)
        {
            if (Id2Channel.ContainsKey(userId))
                return false;
            var user = new ChatUser()
            {
                Id = userId,
            };
            var result = Id2Users.TryAdd(userId, user);
            if (!result) //若成功添加
                user.Dispose();
            return result;
        }

        /// <summary>
        /// 移除一个用户。
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>true成功移除用户，false指定用户不存在。</returns>
        /// <exception cref="Timeout">锁定用户超时。</exception>
        public bool RemoveUser(string userId)
        {
            if (!Id2Users.TryGetValue(userId, out var user))
                return false;
            if (!Lock(user, Options.LockTimeout))
                throw new TimeoutException();
            using var dhUser = DisposeHelper.Create(c => Unlock(c), user);
            foreach (var channel in user.Channels)
            {
                Lock(channel, Timeout.InfiniteTimeSpan);
                using var dwChannel = DisposerWrapper.Create(c => Unlock(c), channel);
                channel.UserIds.Remove(userId);
                if (0 == channel.UserIds.Count && !channel.SuppressUnload)  //若已无用户且允许卸载
                    if (Id2Channel.Remove(channel.Id, out _))
                        channel.Dispose();
            }
            user.Dispose();
            return true;
        }

        /// <summary>
        /// 创建频道。
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="suppressUnload">true永不自动卸载，false(省略)会在超时无动作或没有参与者时自动卸载。</param>
        /// <returns>true成功创建频道，false指定id的频道已经存在。</returns>
        public bool CreateChannel(string channelId, bool suppressUnload = false)
        {
            if (Id2Channel.ContainsKey(channelId))
                return false;
            var channel = new ChatChannel()
            {
                Id = channelId,
                SuppressUnload = suppressUnload,
            };
            var result = Id2Channel.TryAdd(channelId, channel);
            if (!result)    //若没有成功添加
                channel.Dispose();
            return result;
        }

        /// <summary>
        /// 移除指定频道。
        /// </summary>
        /// <param name="channelId"></param>
        /// <returns></returns>
        public bool RemoveChannel(string channelId)
        {
            if (!Id2Channel.TryGetValue(channelId, out var channel))
                return false;
            if (!Lock(channel, Options.LockTimeout))
                return false;
            using var dh = DisposeHelper.Create(c => Unlock(c), channel);
            foreach (var userId in channel.UserIds)
            {

            }

            return true;
        }

        #endregion 用户操作

        #region IDisposable接口相关

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                    _ReaderWriterLockSlim?.Dispose();
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _Id2Channel = null;
                _UserInfos = null;
                disposedValue = true;
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~ChatManager()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable接口相关
    }

    public static class ChatManagerExtensions
    {
    }

    #region 基础数据结构

    public class ChatMessagePooledObjectPolicy : DefaultPooledObjectPolicy<ChatMessage>
    {
        public ChatMessagePooledObjectPolicy()
        {
        }
    }

    public class ChatMessagePool : DefaultObjectPool<ChatMessage>
    {
        private static readonly ChatMessagePool _Shard;

        public static ChatMessagePool Shard => _Shard;

        static ChatMessagePool()
        {
            Interlocked.CompareExchange(ref _Shard, new ChatMessagePool(new ChatMessagePooledObjectPolicy()), null);
        }

        public ChatMessagePool(IPooledObjectPolicy<ChatMessage> policy) : base(policy)
        {
        }

        public ChatMessagePool(IPooledObjectPolicy<ChatMessage> policy, int maximumRetained) : base(policy, maximumRetained)
        {
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="obj"></param>
        public override void Return(ChatMessage obj)
        {
            obj.Message = default;
            obj.ChannelName = default;
            obj.Sender = default;
            obj.ExString = default;
            base.Return(obj);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns></returns>
        public override ChatMessage Get()
        {
            var result = base.Get();
            result.SendDateTimeUtc = DateTime.UtcNow;
            return result;
        }
    }

    /// <summary>
    /// 聊天消息记录的类。
    /// </summary>
    public class ChatMessage : IDisposable, ICloneable
    {
        public ChatMessage()
        {
        }

        /// <summary>
        /// 频道Id。
        /// </summary>
        public string ChannelName { get; set; }

        /// <summary>
        /// 发送者的Id。
        /// </summary>
        public string Sender { get; set; }

        /// <summary>
        /// 发送的内容。当前版本仅支持字符串。
        /// </summary>
        public object Message { get; set; }

        /// <summary>
        /// 发送该消息的时间点,使用utc时间。这也是一个不严格非唯一的时间戳，<see cref="DateTime.Ticks"/>可以被认为是一个时间戳。
        /// </summary>
        public DateTime SendDateTimeUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 扩展追加的属性。
        /// </summary>
        public string ExString { get; set; }

        #region IDispose接口相关

        private volatile bool _Disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _Disposed = true;
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~ChatMessage()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDispose接口相关

        #region IClone接口相关

        public object Clone()
        {
            var result = ChatMessagePool.Shard.Get();
            result.Message = Message;
            result.ChannelName = ChannelName;
            result.SendDateTimeUtc = SendDateTimeUtc;
            result.Sender = Sender;
            result.ExString = ExString;
            return result;
        }

        #endregion IClone接口相关
    }

    /// <summary>
    /// 聊天的频道类。
    /// </summary>
    public class ChatChannel : IDisposable
    {
        /// <summary>
        /// 获取或设置频道的唯一Id。
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 消息队列。
        /// </summary>
        public ConcurrentQueue<ChatMessage> Messages { get; private set; } = new ConcurrentQueue<ChatMessage>();

        /// <summary>
        /// 该频道包含的用户id。
        /// </summary>
        public HashSet<string> UserIds { get; private set; } = new HashSet<string>();

        /// <summary>
        /// 该频道最长不用的超时时间。超过此时间后将被清理。
        /// 设置为<see cref="Timeout.InfiniteTimeSpan"/>导致不会清理。
        /// </summary>
        public TimeSpan Timeout { get; set; }

        /// <summary>
        /// 是否阻止该频道被卸载。true阻止该频道卸载，false该频道可以正常卸载。
        /// </summary>
        public bool SuppressUnload { get; set; }

        #region IDispose接口相关

        private volatile bool _Disposed;

        public bool Disposed => _Disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                Messages = null;
                UserIds = null;
                _Disposed = true;
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~ChatChannel()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion IDispose接口相关
    }

    /// <summary>
    /// 角色用户的信息。
    /// </summary>
    public class ChatUser : IDisposable
    {
        public ChatUser()
        {
            Id = Guid.NewGuid().ToString();
        }

        public ChatUser(string id)
        {
            Id = id;
        }

        /// <summary>
        /// 该用户的唯一Id。使用<see cref="ChatUser"/>默认构造函数构造新对象时，这个Id初始化为新Guid的字符串形式。
        /// </summary>
        public string Id { get; set; }

        public List<ChatChannel> Channels { get; private set; } = new List<ChatChannel>();

        /// <summary>
        /// 最后读取信息的时间。
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 最后写入信息的时间。
        /// </summary>
        public DateTime LastWrite { get; set; }

        private volatile bool _Disposed;
        /// <summary>
        /// 获取该对象是否已经被处置。
        /// </summary>
        public bool Disposed { get => _Disposed; }

        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                Channels = null;
                _Disposed = true;
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~ChatCharInfo()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    #endregion 基础数据结构

    public class GetMessageContext : IResultWorkData, IDisposable
    {
        /// <summary>
        /// 获取或设置用户id。
        /// </summary>
        public string CharId { get; set; }

        /// <summary>
        /// 获取该时间之前发送的信息。默认使用当前时间(UTC)。
        /// </summary>
        public DateTime NowUtc { get; set; } = DateTime.UtcNow;

        public List<ChatMessage> Messages { get; private set; } = new List<ChatMessage>();

        public bool HasError { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                Messages = null;
                disposedValue = true;
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~GetMessageContext()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// 发送信息的上下文。
    /// </summary>
    public class SendMessageContext : IResultWorkData, IDisposable
    {
        /// <summary>
        /// 发言者的id。
        /// </summary>
        public string CharId { get; set; }

        /// <summary>
        /// 要发送的频道名。
        /// </summary>
        public string ChannelId { get; set; }

        /// <summary>
        /// 发送的内容信息。
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 扩展追加的属性。
        /// </summary>
        public string ExString { get; set; }

        public bool HasError { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~SendMessageContext()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
