using GuangYuan.GY001.UserDb;
using Microsoft.Extensions.ObjectPool;
using OW.Game;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

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
        /// </summary>
        public TimeSpan ChannelTimeout { get; set; } = TimeSpan.FromMinutes(2);
    }

    public class ChatManager : GameManagerBase<ChatManagerOptions>
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

        private void Initialize() { }

        #endregion 构造函数及相关

        #region 属性及相关

        private ConcurrentDictionary<string, ChatChannel> _Id2Channel;
        /// <summary>
        /// 键是频道的id,值该频道信息。
        /// </summary>
        public ConcurrentDictionary<string, ChatChannel> Id2Channel { get => _Id2Channel ??= new ConcurrentDictionary<string, ChatChannel>(); }

        private readonly ConcurrentDictionary<string, List<ChatChannel>> _Char2Chat = new ConcurrentDictionary<string, List<ChatChannel>>();
        /// <summary>
        /// 键是用户Id，值是角色所属的频道。
        /// </summary>
        public ConcurrentDictionary<string, List<ChatChannel>> Char2Chat { get => _Char2Chat; }

        #endregion 属性及相关

        #region 功能相关

        public bool Lock(ChatChannel channel, TimeSpan timeout)
        {
            if (!Monitor.TryEnter(channel, timeout))
                return false;
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

        public void Unlock(ChatChannel channel)
        {
            Monitor.Exit(channel);
        }

        /// <summary>
        /// 获取或创建一个频道。
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="creator"></param>
        public ChatChannel GetOrCreateChannel(string channelId, Func<ChatChannel> creator)
        {
            return Id2Channel.GetOrAdd(channelId, c => creator());
        }

        /// <summary>
        /// 创建或加入一个频道。
        /// </summary>
        /// <remarks>如果创建一个私聊频道，需要连续加入两个用户即可。</remarks>
        /// <param name="charId">用户id。</param>
        /// <param name="channelId">频道id。</param>
        /// <param name="timeout">频道闲置超时。</param>
        /// <param name="creator">返回该频道对象。</param>
        /// <returns></returns>
        public bool JoinChannel(string charId, string channelId, TimeSpan timeout, Func<ChatChannel> creator)
        {
            var channel = GetOrCreateChannel(channelId, creator);
            lock (channel)
            {
                if (channel.Disposed)
                    return false;
                var list = Char2Chat.GetOrAdd(charId, c => new List<ChatChannel>());
                lock (list)
                    if (list.Contains(channel))
                        return false;
                    else
                        list.Add(channel);
            }
            return true;
        }

        /// <summary>
        /// 指定用户离开指定频道，
        /// </summary>
        /// <param name="charId"></param>
        /// <param name="channelId"></param>
        /// <returns>true成功离开，false指定用户原本就不在频道中，或指定频道不存在。</returns>
        public bool LeaveChannel(string charId, string channelId)
        {
            return true;
        }

        public void SendMessages(SendMessageContext datas)
        {

        }

        public void GetMessages(GetMessageContext datas)
        {

        }
        #endregion 功能相关
    }

    public static class ChatManagerExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Lock(this ChatManager manager, string channelId, TimeSpan timeout, out ChatChannel channel)
        {
            if (!manager.Id2Channel.TryGetValue(channelId, out channel))
                return false;
            return manager.Lock(channel, timeout);
        }

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
            obj.PindaoName = default;
            obj.Sender = default;
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
    public class ChatMessage : IDisposable
    {
        public ChatMessage()
        {
        }

        /// <summary>
        /// 频道Id。
        /// </summary>
        public string PindaoName { get; set; }

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

        #region IDispose接口相关

        private volatile bool _Disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                _Disposed = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
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

        private ConcurrentQueue<ChatMessage> _Messages;
        /// <summary>
        /// 消息队列。
        /// </summary>
        public ConcurrentQueue<ChatMessage> Messages { get => _Messages = new ConcurrentQueue<ChatMessage>(); }

        /// <summary>
        /// 该频道最长不用的超时时间。超过此时间后将被清理。
        /// </summary>
        public TimeSpan Timeout { get; set; }

        /// <summary>
        /// 该频道最后修改的时间。
        /// </summary>
        public DateTime LastModifyDatetimeUtc { get; set; } = DateTime.UtcNow;

        private ConcurrentDictionary<string, DateTime> _Char2Timestamp;
        /// <summary>
        /// 
        /// </summary>
        public ConcurrentDictionary<string, DateTime> Char2Timestamp
        {
            get
            {
                if (_Char2Timestamp is null)
                    Interlocked.CompareExchange(ref _Char2Timestamp, new ConcurrentDictionary<string, DateTime>(), null);
                return _Char2Timestamp;
            }
        }

        #region IDispose接口相关

        private volatile bool _Disposed;

        public bool Disposed => _Disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                _Messages = null;
                _Char2Timestamp = null;
                _Disposed = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
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

    #endregion 基础数据结构

    public class GetMessageContext : GameCharGameContext
    {
        public GetMessageContext([NotNull] IServiceProvider service, [NotNull] GameChar gameChar) : base(service, gameChar)
        {
        }

        public GetMessageContext([NotNull] VWorld world, [NotNull] GameChar gameChar) : base(world, gameChar)
        {
        }

        public GetMessageContext([NotNull] VWorld world, [NotNull] string token) : base(world, token)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
            }
            base.Dispose(disposing);
        }

    }

    public class SendMessageContext
    {
        public string ChannelName { get; set; }
    }
}
