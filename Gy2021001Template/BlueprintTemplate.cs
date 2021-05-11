using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Text;

namespace Gy2021001Template
{
    public class BlueprintTemplate : GameThingTemplateBase
    {
        public virtual List<BpInputItemTemplate> InputItems { get; } = new List<BpInputItemTemplate>();

        public BlueprintTemplate()
        {

        }

        public int? GId { get; set; }
    }

    public class BpInputItemTemplate : GameTemplateBase
    {
        public BpInputItemTemplate()
        {

        }

        [ForeignKey(nameof(BlueprintTemplate))]
        public Guid BlueprintTemplateId { get; set; }

        public virtual BlueprintTemplate BlueprintTemplate { get; set; }
    }

    public class BpInputItem
    {
        public BpInputItem(decimal probability)
        {
            Probability = probability;
        }

        private string GetDebuggerDisplay()
        {
            return ToString();
        }

        public decimal LossCount { get; set; }

        public decimal Probability { get; set; }

        /// <summary>
        /// 该原料必须属于哪种槽，即处于什么槽中。
        /// </summary>
        public string ContainerIdString { get; set; }

        /// <summary>
        /// 直接指定该原料的模板Id。
        /// </summary>
        public string TemplateIdString { get; set; }

        /// <summary>
        /// 属性条件。如mlv>lv,hp==mhp ,没有达到最大等级，且满血。
        /// </summary>
        public string Condition { get; set; }

        /// <summary>
        /// 该公式中原料的某个属性必须有相同值。
        /// </summary>
        public string SamePropertyName { get; set; }
    }

    public class BpOutItem
    {

    }

    public class PropertyChanges
    {
        public PropertyChanges()
        {
            
        }
        public string Name { get; set; }

        public decimal MinIncrement { get; set; }

        public decimal MaxIncrement { get; set; }

        public bool IsRound { get; set; }

        public decimal DefaultValue { get; set; }
    }
}
