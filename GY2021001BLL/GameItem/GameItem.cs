using GuangYuan.GY001.BLL.DDD;
using GuangYuan.GY001.UserDb;
using Microsoft.Extensions.DependencyInjection;
using OW.DDD;
using OW.Game;
using OW.Game.PropertyChange;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace GuangYuan.GY001.BLL
{
    public class SetLevelCommand : GameCharCommand<SetLevelCommand>
    {
        public SetLevelCommand()
        {
        }

        public SetLevelCommand(GameChar gameChar, List<GamePropertyChangeItem<object>> changes = null) : base(gameChar, changes)
        {
        }

        public GameItem Item { get; set; }

        public int NewLevel { get; set; }

    }

    public class SetLevelCommandResult : WithChangesCommandResult<SetLevelCommandResult>
    {
        public SetLevelCommandResult()
        {
        }

        public SetLevelCommandResult([NotNull] List<GamePropertyChangeItem<object>> changes) : base(changes)
        {
        }
    }

    public class SetLevelCommandHandler : GameCharCommandHandler<SetLevelCommand, SetLevelCommandResult>
    {
        public SetLevelCommandHandler()
        {
        }

        public SetLevelCommandHandler(VWorld world) : base(world)
        {
        }

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
            return result;
        }
    }
}
