using OW.DDD;
using OW.Game.PropertyChange;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace OW.Game
{
    public class GameNotificationBase : NotificationBase
    {

    }

    public abstract class GameNotificationHandlerBase<T> : NotificationHandlerBase<T> where T : INotification
    {
    }

    public class WithChangesNotification : GameNotificationBase
    {
        #region 构造函数

        /// <summary>
        /// 构造函数。
        /// </summary>
        public WithChangesNotification()
        {

        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="changes">指定该对象的<see cref="Changes"/>属性使用此集合。</param>
        public WithChangesNotification([NotNull] List<GamePropertyChangeItem<object>> changes)
        {
            _Changes = changes ?? throw new ArgumentNullException(nameof(changes));
        }

        #endregion 构造函数

        List<GamePropertyChangeItem<object>> _Changes;
        /// <summary>
        /// 变换数据。事件处理程序可以向此集合添加数据，且应假设该集合存在了0..n条数据。没有极其充分的理由不可删除或更改已存在的数据。
        /// </summary>
        public List<GamePropertyChangeItem<object>> Changes => _Changes ??= new List<GamePropertyChangeItem<object>>();

    }
}
