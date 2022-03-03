using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GuangYuan.GY001.TemplateDb
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
    [Table("动态属性元数据")]
    public class GamePropertyTemplate
    {
        public GamePropertyTemplate()
        {

        }

        public GamePropertyTemplate(string name)
        {
            PName = name;
        }

        /// <summary>
        /// 属性的名，这个字符串要唯一。
        /// </summary>
        [StringLength(64)]
        [Key]
        [Required(AllowEmptyStrings = false)]
        public string PName { get; set; }

        /// <summary>
        /// 实际使用的名称。
        /// </summary>
        public string FName { get; set; }

        /// <summary>
        /// 是否是前缀。
        /// 游戏使用的实际名称。仅针对固有属性才有意义。固有属性是游戏服务器必须理解到的固定属性如lv等，但可以重新命名。
        /// </summary>
        [Column("前缀")]
        public bool IsPrefix { get; set; }

        /// <summary>
        /// 对象不会改变该属性的值，所以不必记录在对象中。
        /// </summary>
        [Column("不变")]
        public bool IsFix { get; set; }

        /// <summary>
        /// 不在对象中向客户端报告该属性和其值。客户端仍可通过模板对象查询此属性和值。这个属性是true时，对象也可能记录该属性值，仅仅是不报告给客户端。
        /// </summary>
        [Column("隐藏")]
        public bool HideToClient { get; set; }

        /// <summary>
        /// 属性的类型。
        /// </summary>
        //public GamePropertyType Kind { get; set; }

        [Column("默认值")]
        /// <summary>
        /// 默认值。未找到该属性的明确值，则使用此值。如果没有定义则使用类型的default值。
        /// </summary>
        public string DefaultValue { get; set; }

        /// <summary>
        /// 仅当<see cref="Kind"/>是<see cref="GamePropertyType.Sequence"/>此成员才有效。
        /// 成员值是另一个游戏属性的名字，表示当前游戏属性的具体值选择是另一个序列属性作为索引。如: {Tag=="atk" IndexBy=="lvatk"}表示表示lvatk的属性值是序列属性atk的选择索引。
        /// </summary>
        [Column("索引属性名")]
        public string IndexBy { get; set; }

        /// <summary>
        /// 系统不使用。
        /// </summary>
        [Column("备注")]
        public string Remark { get; set; }
    }

    /// <summary>
    /// 属性管理器。
    /// </summary>
    public interface IGamePropertyManager
    {
        /// <summary>
        /// 获取表示级别的属性名，或其前缀。
        /// 通常就是lv,这个也是前缀，如lvatk存在，则atk属性使用lvatk标记的级别，而不使用lv标记的级别。
        /// </summary>
        abstract string LevelPropertyName { get; }

        /// <summary>
        /// 堆叠属性的名称。默认stc。
        /// </summary>
        string StackUpperLimitPropertyName { get; }

        /// <summary>
        /// 过滤掉不必复制的属性名。
        /// </summary>
        /// <param name="pNmaes"></param>
        /// <returns></returns>
        public IEnumerable<string> Filter(IEnumerable<string> pNmaes);

        /// <summary>
        /// 过滤掉不必复制的属性名。
        /// </summary>
        /// <param name="dic"></param>
        /// <returns></returns>
        public IEnumerable<KeyValuePair<string, object>> Filter(IReadOnlyDictionary<string, object> dic);
    }
}
