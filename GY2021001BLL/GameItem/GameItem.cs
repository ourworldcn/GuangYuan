using GuangYuan.GY001.BLL.DDD;
using GuangYuan.GY001.UserDb;
using Microsoft.Extensions.DependencyInjection;
using OW.DDD;
using OW.Game;
using OW.Game.PropertyChange;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace GuangYuan.GY001.BLL
{
    public class SetLevelCommand : GameCharCommand<SetLevelCommand>
    {
        public SetLevelCommand()
        {
        }

        public SetLevelCommand(GameChar gameChar, ICollection<GamePropertyChangeItem<object>> changes = null) : base(gameChar, changes)
        {
        }

        public GameThingBase Item { get; set; }

        public int NewLevel { get; set; }

    }

    public class SetLevelCommandResult : WithChangesCommandResult<SetLevelCommandResult>
    {
        public SetLevelCommandResult()
        {
        }

        public SetLevelCommandResult([NotNull] ICollection<GamePropertyChangeItem<object>> changes) : base(changes)
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class SetLevelCommandHandler : GameCharCommandHandler<SetLevelCommand, SetLevelCommandResult>
    {
        public SetLevelCommandHandler()
        {
        }

        public SetLevelCommandHandler(VWorld world, OwEventBus owEventBus) : base(world)
        {
            _OwEventBus = owEventBus;
        }

        OwEventBus _OwEventBus;

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// 变换物品等级。会对比原等级的属性增减属性数值。如模板中原等级mhp=100,而物品mhp=120，则会用新等级mhp+20。
        /// 序列属性的名字。如果对象中没有索引必须的属性，则视同初始化属性。若无序列属性的值，但找到索引属性的话，则视同此属性值是模板中指定的值。
        /// </remarks>
        /// <param name="command"></param>
        /// <returns></returns>
        public override SetLevelCommandResult Handle(SetLevelCommand command)
        {
            var result = new SetLevelCommandResult(command.Changes);
            var notification = new LevelSettedNotification();

            var thing = command.Item;
            var olv = (int)thing.GetDecimalWithFcpOrDefault(World.PropertyManager.LevelPropertyName); //当前等级
            if (olv == command.NewLevel)    //若等级没有变化
                return result;
            var tt = thing.GetTemplate();
            thing.FcpToProperties();    //刷新所有fcp属性
            foreach (var kvp in thing.Properties.ToArray()) //遍历每个属性
            {
                if (!(tt.Properties.GetValueOrDefault(kvp.Key) is decimal[] ary) || ary.Length < 1)  //若没有随级别变化的可能
                    continue;
                if (!OwConvert.TryToDecimal(kvp.Value, out var ov)) //若不是数值
                    continue;

                var newValue = ov - GetOrDefault(ary, olv) + GetOrDefault(ary, command.NewLevel);   //升级后的值
                GamePropertyChangeItem<object>.ModifyAndAddChanged(result.Changes, thing, kvp.Key, newValue);
            }
            notification.ChangeItem = GamePropertyChangeItem<object>.Create(thing, World.PropertyManager.LevelPropertyName, command.NewLevel);
            //设置等级属性
            GamePropertyChangeItem<object>.ModifyAndAddChanged(result.Changes, thing, World.PropertyManager.LevelPropertyName, command.NewLevel);
            //刷新fcp属性
            thing.RefreshFcp();
            //引发通告
            _OwEventBus.Add(notification);
            _OwEventBus.Raise();
            return result;
        }

        /// <summary>
        /// 获取数组指定索引处的值，若索引超出范围则返回默认值。
        /// </summary>
        /// <param name="ary"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        T GetOrDefault<T>(T[] ary, int index, T defaultValue = default) =>
            index < ary.GetLowerBound(0) || index > ary.GetUpperBound(0) ? defaultValue : ary[index];

    }

    /// <summary>
    /// 等级变化后的通知事件。
    /// </summary>
    public class LevelSettedNotification : WithChangesNotification
    {
        public LevelSettedNotification()
        {
        }

        public LevelSettedNotification(GamePropertyChangeItem<object> changeItem)
        {
            ChangeItem = changeItem;
        }

        public LevelSettedNotification([NotNull] List<GamePropertyChangeItem<object>> changes) : base(changes)
        {
        }

        public GamePropertyChangeItem<object> ChangeItem { get; set; }

    }
}
