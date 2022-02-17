using GuangYuan.GY001.TemplateDb;
using GuangYuan.GY001.UserDb;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace OW.Extensions.Game.Store
{
    public static class GameThingBaseExtensions
    {
        /// <summary>
        /// 客户端要记录的一些属性，这个属性客户端可以随意更改，服务器不使用。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetClientString(this GameThingBase thing)
        {
            var tmp = thing.Properties.GetStringOrDefault("ClientString");
            return tmp is null ? null : Uri.UnescapeDataString(tmp);
        }

        /// <summary>
        /// 客户端要记录的一些属性，这个属性客户端可以随意更改，服务器不使用。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetClientString(this GameThingBase thing, string value)
        {
            if (value is null)
                thing.Properties.Remove("ClientString");
            else
                thing.Properties["ClientString"] = Uri.EscapeDataString(value);
        }

    }

    public static class GameItemBaseExtensions
    {
        /// <summary>
        /// 如果物品处于某个容器中，则这个成员指示其所处位置号，从0开始，但未必连续,序号相同则顺序随机。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetOrderNumber(this GameItem gameItem)
        {
            return (int)gameItem.Properties.GetDecimalOrDefault("OrderNumber");
        }

        /// <summary>
        /// 如果物品处于某个容器中，则这个成员指示其所处位置号，从0开始，但未必连续,序号相同则顺序随机。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetOrderNumber(this GameItem gameItem, int value)
        {
            gameItem.Properties["OrderNumber"] = (decimal)value;
        }

    }

    public static class GameCharBaseExtensions
    {

    }

    public static class GameUserBaseExtensions
    {

    }
}