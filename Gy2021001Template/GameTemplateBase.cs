/*物品对象
 */
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;

namespace GuangYuan.GY001.TemplateDb
{
    /// <summary>
    /// 虚拟事物对象的模板类。
    /// </summary>
    public abstract class GameThingTemplateBase : GameTemplateBase
    {
        /// <summary>
        /// 通用索引序列属性的名称或专用索引序列属性的前缀。
        /// </summary>
        public const string LevelPrefix = "lv";

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public GameThingTemplateBase()
        {
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="id"><inheritdoc/></param>
        public GameThingTemplateBase(Guid id) : base(id)
        {

        }

        /// <summary>
        /// 显示名称。
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 该模板创建对象应有的子模板Id字符串集合。用逗号分割。
        /// 可能存在多个相同Id。
        /// </summary>
        public string ChildrenTemplateIdString { get; set; }

        /// <summary>
        /// 脚本，内容根据使用情况具体定义。
        /// </summary>
        public string Script { get; set; }

        private List<Guid> _ChildrenTemplateIds;

        /// <summary>
        /// 该模板创建对象应有的子模板Id集合。
        /// 可能存在多个相同Id。
        /// </summary>
        [NotMapped]
        public List<Guid> ChildrenTemplateIds
        {
            get
            {
                if (_ChildrenTemplateIds is null)
                    lock (this)
                        if (_ChildrenTemplateIds is null)
                        {
                            if (string.IsNullOrWhiteSpace(ChildrenTemplateIdString))
                            {
                                _ChildrenTemplateIds = new List<Guid>();
                            }
                            else
                            {
                                _ChildrenTemplateIds = ChildrenTemplateIdString.Split(OwHelper.CommaArrayWithCN, StringSplitOptions.RemoveEmptyEntries).Select(c => Guid.Parse(c)).ToList();
                            }
                        }
                return _ChildrenTemplateIds;
            }
        }

        /// <summary>
        /// 获取属性值。
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetPropertyValue(string propertyName, object defaultVal = default) => TryGetPropertyValue(propertyName, out var result) ? result : defaultVal;

        /// <summary>
        /// 获取指定属性名的属性值。
        /// </summary>
        /// <param name="propertyName">属性名。</param>
        /// <param name="result">属性的值。</param>
        /// <returns>true成功返回属性值，false没有找到指定名称的属性。</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0066:将 switch 语句转换为表达式", Justification = "<挂起>")]
        public virtual bool TryGetPropertyValue(string propertyName, out object result)
        {
            bool succ;
            switch (propertyName)
            {
                default:
                    succ = Properties.TryGetValue(propertyName, out result);
                    break;
            }
            return succ;
        }

        #region 序列属性相关


        /// <summary>
        /// 获取指定名称的属性的最大等级（基于0）.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>指定名称序列属性最大等级，null指定名称不是序列属性。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int? GetMaxLevel(string name) => (Properties.GetValueOrDefault(name) as Array)?.Length;

        private string[] _SequencePropertyNames;
        /// <summary>
        /// 获取所有序列属性名的数组。
        /// </summary>
        [NotMapped]
        public string[] SequencePropertyNames
        {
            get
            {
                if (null == _SequencePropertyNames)
                {
                    lock (this)
                    {
                        if (null == _SequencePropertyNames)
                        {
                            _SequencePropertyNames = Properties.Where(c => c.Value is Array).Select(c => c.Key).ToArray();
                        }
                    }
                }

                return _SequencePropertyNames;
            }
        }

        /// <summary>
        /// 获取指定序列属性的索引属性名。
        /// 如果有lvXXX则返回，没有则一律返回 lv。
        /// </summary>
        /// <param name="sequencePropertyName"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetIndexPropertyName(string sequencePropertyName)
        {
            string specName = LevelPrefix + sequencePropertyName;
            return Properties.TryGetValue(specName, out var indexObj) && OwConvert.TryGetDecimal(indexObj, out _) ? specName : LevelPrefix;
        }
        #endregion 序列属性相关

        /// <summary>
        /// 
        /// </summary>
        /// <remarks></remarks>
        /// <returns></returns>
        public override string ToString()
        {
            var tmp = Id.ToString();
            return $"{DisplayName}(Properties.Count = {Properties.Count}, Id = {{{tmp[0..4]}...{tmp[^4..^0]}}})";
        }

    }

    public static class GameThingTemplateBaseExtensons
    {
        /// <summary>
        /// 试图获取指定的序列属性的值。
        /// </summary>
        /// <param name="name"></param>
        /// <returns>序列属性（数组表示），null表示不存在指定名称的属性或其不是指定元素类型的序列属性。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] GetSequenceProperty<T>(this GameThingTemplateBase obj, string name) => obj.Properties.GetValueOrDefault(name) as T[];

        /// <summary>
        /// 获取属性的值，若是序列属性则返回相应索引的值，如果不是序列属性则返回属性的值。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="name"></param>
        /// <param name="lv"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal GetSequenceValueOrValue(this GameThingTemplateBase obj, string name, int lv)
        {
            var seq = obj.GetSequenceProperty<decimal>(name);
            if (seq is null) //若非序列属性
                return obj.TryGetPropertyValue(name, out var resultObj) && OwConvert.TryGetDecimal(resultObj, out var result) ? result : default;
            else //是序列属性
                return seq[lv];
        }

        /// <summary>
        /// 获取指定名称和等级的序列属性的值。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="name"></param>
        /// <param name="lv"></param>
        /// <param name="defaultVal">若不是等级属性，使用此值。</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetSequencePropertyValueOrDefault<T>(this GameThingTemplateBase obj, string name, int lv, T defaultVal = default)
        {
            var tmp = obj.GetSequenceProperty<T>(name);
            return tmp is null ? defaultVal : tmp[lv];
        }

        /// <summary>
        /// 获取指定名称指定级别的序列属性的值。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="level"></param>
        /// <param name="result"></param>
        /// <returns>true返回值，false,指定名称的属性不是序列属性，或指定级别超出限制。</returns>
        public static bool TryGetValueWithLevel<T>(this GameThingTemplateBase obj, string name, int level, out T result)
        {
            var ary = obj.GetSequenceProperty<T>(name);
            if (level < 0 || null == ary || level >= ary.Length)
            {
                result = default;
                return false;
            }
            result = ary[level];
            return true;
        }

        /// <summary>
        /// 获取指定名字序列属性的索引属性名，如果没有找到则考虑使用lv。
        /// </summary>
        /// <param name="template"></param>
        /// <param name="seqPropName">序列属性的名称。</param>
        /// <returns></returns>
        public static string GetIndexPropName(this GameThingTemplateBase template, string seqPropName)
        {
            if (!template.Properties.TryGetValue(seqPropName, out object obj) || !(obj is decimal[]))
                return null;
            var pn = $"{GameThingTemplateBase.LevelPrefix}{seqPropName}";
            if (template.Properties.ContainsKey(pn))
                return pn;
            return GameThingTemplateBase.LevelPrefix;
        }


    }
}
