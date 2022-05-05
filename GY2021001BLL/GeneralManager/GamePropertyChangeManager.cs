using GuangYuan.GY001.UserDb;
using OW.Game;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace OW.Game.PropertyChange
{
    /// <summary>
    /// 标记处理属性变化的方法。
    /// </summary>
    /// <remarks>
    ///  请务必使用后缀“Attribute”来命名自定义属性类。
    ///  ✔️ 请务必将 AttributeUsageAttribute 应用于自定义属性。
    ///  ✔️ 请务必提供可选参数的可设置属性。
    ///  ✔️ 请务必提供必需参数的仅限获取属性。
    ///  ✔️ 请务必提供构造函数参数来初始化对应于必需参数的属性。 每个参数都应具有与相应属性相同的名称（但大小写不同）。
    ///  ❌ 请避免提供构造函数参数来初始化对应于可选参数的属性。
    ///  换句话说，请勿包含可同时使用构造函数和 setter 设置的属性。 此准则非常明确地说明了哪些参数是可选的，哪些参数是必需的，并避免了用两种方法来执行相同的操作。
    ///  ❌ 请避免重载自定义特性构造函数。
    ///  只具有一个构造函数，这清楚地告诉了用户哪些参数是必需的，哪些参数是可选的。
    ///  ✔️ 如果可能，请务必密封自定义特性类。 这样可以更快地查找特性。</remarks>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class PropertyChangedCallbackAttribute : Attribute
    {
        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236

        // This is a positional argument
        public PropertyChangedCallbackAttribute(Type objectType, string propertyName)
        {
            _ObjectType = objectType;
            _PropertyName = propertyName;
        }

        readonly string _PropertyName;
        /// <summary>
        /// 属性名。
        /// </summary>
        public string PropertyName
        {
            get { return _PropertyName; }
        }

        readonly Type _ObjectType;
        /// <summary>
        /// 对象的实际类型。
        /// </summary>
        public Type ObjectType { get => _ObjectType; }
    }

    /// <summary>
    /// 属性变化管理器。
    /// </summary>
    public class GamePropertyChangeManager : GameManagerBase<GamePropertyChangeManagerOptions>
    {
        public GamePropertyChangeManager()
        {
            Initializer();
        }

        public GamePropertyChangeManager(IServiceProvider service) : base(service)
        {
            Initializer();
        }

        public GamePropertyChangeManager(IServiceProvider service, GamePropertyChangeManagerOptions options) : base(service, options)
        {
            Initializer();
        }

        void Initializer()
        {
            List<Type> types = new List<Type>() { typeof(GamePropertyChangeManager) };
            for (var type = GetType(); type != typeof(GamePropertyChangeManager); type = type.BaseType) //遍历子类
                types.Add(type);
            var coll = from tmp in types.SelectMany(c => c.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                       let attr = tmp.GetCustomAttribute<PropertyChangedCallbackAttribute>()
                       where attr != null
                       select (tmp, attr);
            _MemberInfos = coll.ToLookup(c => (c.attr.ObjectType, c.attr.PropertyName), c => c.Item1);
            //_MemberInfos[(typeof(GameChar), "s")].ToList().ForEach(c => c.Invoke(this, null));
        }

        /// <summary>
        /// 处理函数的集合。结构：(类型，属性名),处理者。
        /// </summary>
        ILookup<(Type, string), MethodInfo> _MemberInfos;

        /// <summary>
        /// 处理对象内暂存的属性变化数据。
        /// </summary>
        /// <param name="thing"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public bool Dispatch(GameThingBase thing)
        {
            List<Exception> excps = new List<Exception>();
            bool succ = false;
            var list = thing.GetOrCreatePropertyChangedList();
            var paras = new object[1];
            while (list.TryDequeue(out var item))    //若存在数据
            {
                succ = true;
                var key = (item.Object.GetType(), item.PropertyName);
                var methodes = _MemberInfos[key];
                paras[0] = item;
                foreach (var method in methodes) //逐个调用处理函数
                {
                    try
                    {
                        method.Invoke(this, paras);
                    }
                    catch (Exception err)
                    {
                        excps.Add(err);
                    }
                }
                GamePropertyChangeItemPool<object>.Shared.Return(item);  //放入池中备用
            }
            if (excps.Count > 0)    //若需要引发工程中堆积的异常
                throw new AggregateException(excps);
            return succ;
        }

        [PropertyChangedCallback(typeof(GameChar), "s")]
        void Test()
        {
        }
    }

    /// <summary>
    /// 属性变化管理器配置类。
    /// </summary>
    public class GamePropertyChangeManagerOptions
    {
    }

    /// <summary>
    /// 属性变化管理器扩展方法类。
    /// </summary>
    public static class GamePropertyChangeManagerExtensions
    {
        /// <summary>
        /// 获取或初始化事件数据对象的列表。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ConcurrentQueue<GamePropertyChangeItem<object>> GetOrCreatePropertyChangedList(this GameThingBase gameChar) =>
            (ConcurrentQueue<GamePropertyChangeItem<object>>)gameChar.RuntimeProperties.GetOrAdd("EventArgsList", c => new ConcurrentQueue<GamePropertyChangeItem<object>>());

    }
}
