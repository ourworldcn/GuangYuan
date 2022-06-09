using GuangYuan.GY001.UserDb;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace OW.Game.Operate
{
    /// <summary>
    /// 
    /// </summary>
    public class GameOperate
    {
        public GameItemsReference Reference { get; set; }

        public decimal Count { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class GameItemsReference
    {
        public static bool TryParse(IDictionary<string, object> dic, [AllowNull] string prefix, out GameItemsReference result)
        {
            var coll = ((IReadOnlyDictionary<string, object>)dic).GetValuesWithoutPrefix(prefix);
            result = new GameItemsReference();
            return true;
        }


    }
}
