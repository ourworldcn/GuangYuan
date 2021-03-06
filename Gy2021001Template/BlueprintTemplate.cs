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

        //private GameExpressionBase _ProbExpression;
        ///// <summary>
        ///// 命中概率的表达式。
        ///// </summary>
        //[NotMapped]
        //public GameExpressionBase ProbExpression
        //{
        //    get
        //    {
        //        lock (this)
        //            if (null == _ProbExpression)
        //            {
        //                var env = CompileEnvironment;
        //                _ProbExpression = GameExpressionBase.CompileExpression(env, Prob);
        //            }
        //        return _ProbExpression;
        //    }
        //}

        //private GameExpressionCompileEnvironment _CompileEnvironment;
        //[NotMapped]
        //public GameExpressionCompileEnvironment CompileEnvironment
        //{
        //    get
        //    {
        //        lock (this)
        //            if (null == _CompileEnvironment)
        //            {
        //                var result = new GameExpressionCompileEnvironment() { Services = Service ?? BlueprintTemplate?.Service, };
        //                foreach (var item in BptfItemTemplates) //将变量声明编译
        //                {
        //                    result.StartCurrentObject(item.Id.ToString());
        //                    try
        //                    {
        //                        GameExpressionBase.CompileVariableDeclare(result, item.VariableDeclaration);
        //                    }
        //                    finally
        //                    {
        //                        result.RestoreCurrentObject(out _);
        //                    }
        //                }
        //                _CompileEnvironment = result;
        //            }
        //        return _CompileEnvironment;
        //    }
        //}
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
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //private GameExpressionCompileEnvironment StartCurrentObject()
        //{
        //    var _ = FormulaTemplate.CompileEnvironment;
        //    _.StartCurrentObject(Id.ToString());
        //    return _;
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //private bool EndCurrentObject(out string result)
        //{
        //    var _ = FormulaTemplate.CompileEnvironment;
        //    return _.RestoreCurrentObject(out result);
        //}

        //private GameExpressionBase _ConditionalExpression;
        ///// <summary>
        ///// 获取条件表达式。
        ///// </summary>
        //[NotMapped]
        //public GameExpressionBase ConditionalExpression
        //{
        //    get
        //    {
        //        lock (this)
        //            if (null == _ConditionalExpression)
        //            {
        //                var env = StartCurrentObject();
        //                try
        //                {
        //                    _ConditionalExpression = GameExpressionBase.CompileExpression(FormulaTemplate.CompileEnvironment, Conditional);
        //                }
        //                finally
        //                {
        //                    EndCurrentObject(out _);
        //                }
        //            }
        //        return _ConditionalExpression;
        //    }
        //}

        //private GameExpressionBase _CountUpperBoundExpression;
        ///// <summary>
        ///// 获取增量上限表达式。
        ///// </summary>
        //[NotMapped]
        //public GameExpressionBase CountUpperBoundExpression
        //{
        //    get
        //    {

        //        lock (this)
        //            if (null == _CountUpperBoundExpression)
        //            {
        //                var env = StartCurrentObject();
        //                GameExpressionBase result;
        //                try
        //                {
        //                    result = GameExpressionBase.CompileExpression(env, CountUpperBound);
        //                }
        //                finally
        //                {
        //                    EndCurrentObject(out _);
        //                }
        //                _CountUpperBoundExpression = result;
        //                //if (IsCountRound)    //若需要取整
        //                //{
        //                //    result = new FunctionCallGExpression("round", result);
        //                //}
        //            }
        //        return _CountUpperBoundExpression;
        //    }
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public bool TryGetUpperBound(GameExpressionRuntimeEnvironment env, out decimal result)
        //{
        //    if (!CountUpperBoundExpression.TryGetValue(env, out var obj) || !OwConvert.TryToDecimal(obj, out result))
        //    {
        //        result = decimal.Zero;
        //        return false;
        //    }
        //    return true;
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public bool TryGetLowerBound(GameExpressionRuntimeEnvironment env, out decimal result)
        //{
        //    if (!CountLowerBoundExpression.TryGetValue(env, out var obj) || !OwConvert.TryToDecimal(obj, out result))
        //    {
        //        result = decimal.Zero;
        //        return false;
        //    }
        //    return true;
        //}

        //private GameExpressionBase _CountLowerBoundExpression;

        ///// <summary>
        ///// 获取增量下限表达式。
        ///// </summary>
        //[NotMapped]
        //public GameExpressionBase CountLowerBoundExpression
        //{
        //    get
        //    {

        //        lock (this)
        //            if (null == _CountLowerBoundExpression)
        //            {
        //                var env = StartCurrentObject();
        //                GameExpressionBase result;
        //                try
        //                {
        //                    result = GameExpressionBase.CompileExpression(env, CountLowerBound);
        //                }
        //                finally
        //                {
        //                    EndCurrentObject(out _);
        //                }
        //                //if (IsCountRound)    //若需要取整
        //                //{
        //                //    result = new FunctionCallGExpression("round", result);
        //                //}
        //                _CountLowerBoundExpression = result;
        //            }
        //        return _CountLowerBoundExpression;
        //    }
        //}

        //private GameExpressionBase _CountProbExpression;

        ///// <summary>
        ///// 获取增量发生概率表达式。
        ///// </summary>
        //[NotMapped]
        //public GameExpressionBase CountProbExpression
        //{
        //    get
        //    {

        //        lock (this)
        //            if (null == _CountProbExpression)
        //            {
        //                var env = StartCurrentObject();
        //                try
        //                {
        //                    _CountProbExpression = GameExpressionBase.CompileExpression(env, CountProb);
        //                }
        //                finally
        //                {
        //                    EndCurrentObject(out _);
        //                }
        //            }
        //        return _CountProbExpression;
        //    }
        //}

        //private GameExpressionBase _PropertiesChangesExpression;

        ///// <summary>
        ///// 获取属性变化表达式。
        ///// </summary>
        //[NotMapped]
        //public GameExpressionBase PropertiesChangesExpression
        //{
        //    get
        //    {
        //        lock (this)
        //            if (null == _PropertiesChangesExpression)
        //            {
        //                var env = StartCurrentObject();
        //                try
        //                {
        //                    _PropertiesChangesExpression = GameExpressionBase.CompileBlockExpression(env, PropertiesChanges);
        //                }
        //                finally
        //                {
        //                    EndCurrentObject(out _);
        //                }
        //            }
        //        return _PropertiesChangesExpression;
        //    }
        //}
        #endregion 废弃

    }
}
