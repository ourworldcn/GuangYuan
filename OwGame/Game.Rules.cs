using System;

namespace OW.Game.Rules
{
    /// <summary>
    /// 游戏内常用的特定规则支持结构基类。
    /// </summary>
    public class GameRulesBase
    {

    }

    public class PeriodGameRules : GameRulesBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public PeriodGameRules()
        {

        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="period"></param>
        /// <param name="validPeriod"></param>
        public PeriodGameRules(DateTimePeriod period, TimeSpanEx validPeriod)
        {
            Period = period;
            ValidPeriod = validPeriod;
        }

        /// <summary>
        /// 周期。
        /// </summary>
        public DateTimePeriod Period { get; set; }

        /// <summary>
        /// 有效期。
        /// </summary>
        public TimeSpanEx ValidPeriod { get; set; }

        /// <summary>
        /// 指定时间点是否在有效期内。
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public bool IsValid(DateTime time)
        {
            var start = Period.GetPeriodStart(time);
            return start <= time && start + ValidPeriod >= time;
        }

    }

}