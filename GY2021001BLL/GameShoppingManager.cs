using GuangYuan.GY001.TemplateDb;
using OW.Game;
using System;

namespace GuangYuan.GY001.BLL
{
    public class GameShoppingManagerOptions
    {

    }


    public class GameShoppingManager : GameManagerBase<GameShoppingManagerOptions>
    {
        public GameShoppingManager()
        {
        }

        public GameShoppingManager(IServiceProvider service) : base(service)
        {
        }

        public GameShoppingManager(IServiceProvider service, GameShoppingManagerOptions options) : base(service, options)
        {
        }

        public void GetList()
        {

        }

        public void Buy()
        {

        }

        public void Refresh()
        {

        }

        /// <summary>
        /// 指定商品在指定时间是否可以销售。
        /// </summary>
        /// <param name="template"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public bool IsValid(GameShoppingTemplate template, DateTime dateTime)
        {
            DateTime start; //最近一个周期的开始时间
            DateTime templateStart = template.StartDateTime;
            switch (template.SellPeriodUnit)
            {
                case 'n':   //无限
                    start = templateStart;
                    break;
                case 'd':   //日周期
                    var times = (dateTime - templateStart).Ticks / TimeSpan.FromDays((double)template.SellPeriodValue).Ticks;  //相隔日数
                    start = dateTime.AddTicks(times * TimeSpan.FromDays((double)template.SellPeriodValue).Ticks);
                    break;
                case 'w':   //周周期
                    times = (dateTime - templateStart).Ticks / TimeSpan.FromDays(7 * (double)template.SellPeriodValue).Ticks;  //相隔周数
                    start = dateTime.AddTicks(TimeSpan.FromDays(7 * (double)template.SellPeriodValue).Ticks * times);
                    break;
                case 'm':   //月周期
                    start = new DateTime(dateTime.Year, dateTime.Month, templateStart.Day) + templateStart.TimeOfDay;
                    if (start > dateTime)
                        start = start.AddMonths(-1);
                    break;
                case 'y':   //年周期
                    start = new DateTime(dateTime.Year, templateStart.Month, templateStart.Day) + templateStart.TimeOfDay;
                    if (start > dateTime)
                        start = new DateTime(dateTime.Year - 1, templateStart.Month, templateStart.Day) + templateStart.TimeOfDay; ;
                    break;
                default:
                    throw new InvalidOperationException("无效的周期表示符。");
            }
            return true;
        }

        /// <summary>
        /// 获取最近开始周期第一天。
        /// </summary>
        /// <param name="template"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public DateTime GetStart(GameShoppingTemplate template, DateTime dateTime)
        {
            DateTime start; //最近一个周期的开始时间
            DateTime templateStart = template.StartDateTime;
            switch (template.SellPeriodUnit)
            {
                case 'n':   //无限
                    start = templateStart;
                    break;
                case 's':
                    var times = (dateTime - templateStart).Ticks / TimeSpan.FromSeconds((double)template.SellPeriodValue).Ticks;  //相隔秒数
                    start = dateTime.AddTicks(times * TimeSpan.FromSeconds((double)template.SellPeriodValue).Ticks);
                    break;
                case 'd':   //日周期
                    times = (dateTime - templateStart).Ticks / TimeSpan.FromDays((double)template.SellPeriodValue).Ticks;  //相隔日数
                    start = dateTime.AddTicks(times * TimeSpan.FromDays((double)template.SellPeriodValue).Ticks);
                    break;
                case 'w':   //周周期
                    times = (dateTime - templateStart).Ticks / TimeSpan.FromDays(7 * (double)template.SellPeriodValue).Ticks;  //相隔周数
                    start = dateTime.AddTicks(TimeSpan.FromDays(7 * (double)template.SellPeriodValue).Ticks * times);
                    break;
                case 'm':   //月周期
                    DateTime tmp;
                    for (tmp = templateStart; tmp <= dateTime; tmp = tmp.AddMonths(((int)template.SellPeriodValue)))
                    {
                    }
                    start = tmp.AddMonths(-((int)template.SellPeriodValue));
                    break;
                case 'y':   //年周期
                    for (tmp = templateStart; tmp <= dateTime; tmp = tmp.AddYears(((int)template.SellPeriodValue)))
                    {
                    }
                    start = tmp.AddYears(-(int)template.SellPeriodValue);
                    break;
                default:
                    throw new InvalidOperationException("无效的周期表示符。");
            }
            return start;
        }

        /// <summary>
        /// 获取周期。
        /// </summary>
        /// <param name="template"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        protected DateTime GetEnd(GameShoppingTemplate template, DateTime start)
        {
            return start;
        }
    }

    public static class DateTimeExtensions
    {

    }
}
