using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gy2021001Template
{
    /// <summary>
    /// 
    /// </summary>
    public enum GamePropertyType
    {
        /// <summary>
        /// 数值类型。
        /// </summary>
        Number = 1,

        /// <summary>
        /// 字符串类型。
        /// </summary>
        String = 2,

        /// <summary>
        /// 数值序列类型。
        /// </summary>
        Sequence = 3,

        /// <summary>
        /// Guid类型。
        /// </summary>
        Id = 4,
    }

    /// <summary>
    /// 游戏内属性定义的对象。键是字符串类型。就是属性的名字。
    /// </summary>
    [Table("游戏内属性")]
    public class GamePropertyTemplate
    {
        public GamePropertyTemplate()
        {

        }

        public GamePropertyTemplate(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 属性的名，这个字符串要唯一。
        /// </summary>
        [StringLength(64)]
        [Key]
        [Required(AllowEmptyStrings = false)]
        public string Name { get; set; }

        /// <summary>
        /// 属性的类型。
        /// </summary>
        public GamePropertyType Kind { get; set; }

        /// <summary>
        /// 默认值。未找到该属性的明确值，则使用此值。如果没有定义则使用类型的default值。
        /// </summary>
        public string DefaultValue { get; set; }

        /// <summary>
        /// 序列属性的公式。如 lv*10+10，当前版本未实现
        /// </summary>
        public string SequenceFormula { get; set; }

        /// <summary>
        /// 仅当<see cref="Kind"/>是<see cref="GamePropertyType.Sequence"/>此成员才有效。
        /// 成员值是另一个游戏属性的名字，表示当前游戏属性的具体值选择是另一个序列属性作为索引。如: {Name=="atk" IndexBy=="lvatk"}表示表示lvatk的属性值是序列属性atk的选择索引。
        /// </summary>
        public string IndexBy { get; set; }
    }
}
