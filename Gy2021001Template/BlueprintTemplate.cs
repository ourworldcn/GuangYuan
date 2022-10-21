using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace GuangYuan.GY001.TemplateDb
{
    /// <summary>
    /// 蓝图模板。
    /// </summary>
    [Table("蓝图")]
    public class BlueprintTemplate : GameThingTemplateBase
    {

        public BlueprintTemplate()
        {

        }

        #region 导航属性

        public virtual List<BpFormulaTemplate> FormulaTemplates { get; } = new List<BpFormulaTemplate>();
        #endregion 导航属性

        public int? GId { get; set; }
    }

    /// <summary>
    /// 公式模板。
    /// </summary>
    [Table("公式")]
    public class BpFormulaTemplate : GameThingTemplateBase
    {
        public BpFormulaTemplate()
        {
        }

        #region 导航属性

        [ForeignKey(nameof(BlueprintTemplate))]
        [Column("蓝图Id")]
        public Guid BlueprintTemplateId { get; set; }

        public virtual BlueprintTemplate BlueprintTemplate { get; set; }

        public virtual List<BpItemTemplate> BptfItemTemplates { get; set; }
        #endregion 导航属性

        /// <summary>
        /// 序号。
        /// </summary>
        [Column("序号")]
        public int OrderNumber { get; set; }

        /// <summary>
        /// 命中概率。
        /// </summary>
        [Column("命中概率")]
        public string Prob { get; set; }

        /// <summary>
        /// 命中并继续。
        /// </summary>
        [Column("命中并继续")]
        public bool IsContinue { get; set; }

        #region 废弃

    #endregion 废弃
    }

    /// <summary>
    /// 在PropertiesString中 TemplateId 限定此物料的模板Id,ContainerId限定此物料的容器Id,SamePN=body表示同一个公式下，所有具有该属性的物料其body属性必须相同。
    /// </summary>
    [Table("物料")]
    public class BpItemTemplate : GameThingTemplateBase
    {
        public BpItemTemplate()
        {
        }

        #region 导航属性

        [ForeignKey(nameof(FormulaTemplate)), Column("公式Id")]
        public Guid BlueprintTemplateId { get; set; }

        public virtual BpFormulaTemplate FormulaTemplate { get; set; }
        #endregion 导航属性

        [Column("变量声明")]
        public string VariableDeclaration { get; set; }

        [Column("条件属性")]
        public string Conditional { get; set; }

        [Column("增量上限")]
        public string CountUpperBound { get; set; }

        [Column("增量下限")]
        public string CountLowerBound { get; set; }

        [Column("增量概率")]
        public string CountProb { get; set; }

        /// <summary>
        /// 对数量进行取整运算。
        /// </summary>
        [Column("增量取整")]
        public bool IsCountRound { get; set; }

        [Column("属性更改")]
        public string PropertiesChanges { get; set; }

        /// <summary>
        /// 若是新建物品。
        /// </summary>
        [Column("新建物品否")]
        public bool IsNew { get; set; }

        /// <summary>
        /// 这个原料项可以没有对应的物品，如果有则尽量填入。
        /// </summary>
        [Column("允许空")]
        public bool AllowEmpty { get; set; }

        #region 废弃
        #endregion 废弃

    }
}
