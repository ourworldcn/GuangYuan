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
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    sealed class PropertyChangeMethodAttribute : Attribute
    {
        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236
        readonly string _PropertyName;

        // This is a positional argument
        public PropertyChangeMethodAttribute(Type objectType, string propertyName)
        {
            ObjectType = objectType;
            _PropertyName = propertyName;

            // TODO: Implement code here

        }

        /// <summary>
        /// 属性名。
        /// </summary>
        public string PropertyName
        {
            get { return _PropertyName; }
        }

        /// <summary>
        /// 对象的实际类型。
        /// </summary>
        public Type ObjectType { get; set; }
    }

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
                       let attr = tmp.GetCustomAttribute<PropertyChangeMethodAttribute>()
                       where attr != null
                       select (tmp, attr);
            _MemberInfos = coll.ToLookup(c => (c.attr.ObjectType, c.attr.PropertyName), c => c.Item1);
            //_MemberInfos[(typeof(GameChar), "s")].ToList().ForEach(c => c.Invoke(this, null));

        }

        /// <summary>
        /// 处理函数的集合。
        /// </summary>
        ILookup<(Type, string), MethodInfo> _MemberInfos;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public bool Dispatch(GameChar gameChar)
        {
            List<Exception> excps = new List<Exception>();
            bool succ = false;
            var list = gameChar.GetOrCreatePropertyChangedList();
            GamePropertyChangeItem<object> item;
            while (!list.IsEmpty)    //若存在数据
            {
                for (var b = list.TryDequeue(out item); !b; b = list.TryDequeue(out item)) ;
                succ = true;
                var key = (item.Object.GetType(), item.PropertyName);
                if (!_MemberInfos.Contains(key)) //若没有找到指定的处理函数
                {
                }
                else //若找到处理函数
                {
                    var methods = _MemberInfos[key];
                    var para = new object[] { item };
                    foreach (var mi in methods) //逐个调用处理函数
                    {
                        try
                        {
                            mi.Invoke(this, para);
                        }
                        catch (Exception err)
                        {
                            excps.Add(err);
                        }
                    }
                }
                GamePropertyChangeItemPool<object>.Shared.Return(item);  //放入池中备用
            }
            if (excps.Count > 0)    //若需要引发工程中堆积的异常
                throw new AggregateException(excps);
            return succ;
        }

        [PropertyChangeMethod(typeof(GameChar), "s")]
        void Test()
        {
        }
    }

    public class GamePropertyChangeManagerOptions
    {
    }

    public static class GamePropertyChangeManagerExtensions
    {
        /// <summary>
        /// 获取或初始化事件数据对象的列表。
        /// </summary>
        /// <param name="gameChar"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ConcurrentQueue<GamePropertyChangeItem<object>> GetOrCreatePropertyChangedList(this GameChar gameChar) =>
            gameChar.RuntimeProperties.GetOrAdd("EventArgsList", c => new ConcurrentQueue<GamePropertyChangeItem<object>>()) as ConcurrentQueue<GamePropertyChangeItem<object>>;

    }
}
