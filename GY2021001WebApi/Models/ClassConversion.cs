using GY2021001BLL;
using GY2021001DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;

namespace GY2021001WebApi.Models
{
    public class ClassConversion
    {
        public static GameItemDto ToDto(GameItem gameItem)
        {
            var result = new GameItemDto()
            {
                Id = gameItem.Id.ToBase64String(),
                Count = gameItem.Count,
                TemplateId = gameItem.TemplateId.ToBase64String(),
            };
            if (null != gameItem.UserId && gameItem.UserId.HasValue)
                result.OwnerId = gameItem.UserId.Value.ToBase64String();

            foreach (var item in gameItem.NumberProperties)
                result.NumberProperties.Add(item.Key, item.Value);
            foreach (var item in gameItem.StringProperties)
                result.StringProperties.Add(item.Key, item.Value);
            foreach (var item in gameItem.SequenceProperties)
                result.SequenceProperties.Add(item.Key, item.Value);

            result.Children.AddRange(gameItem.Children.Select(c => ToDto(c)));

            return result;
        }

        public static GameItem FromDto(GameItem gameItem)
        {
            var result = new GameItem();
            return result;
        }

        public static T ToDto<T>(object src)
            where T : new()
        {
            T result = new T();
            var destType = typeof(T);
            var srcType = src.GetType();
            var destProps = destType.GetProperties();
            var srcProps = srcType.GetProperties();
            var coll = destProps.Join(srcProps, c => c.Name, c => c.Name, (l, r) => ValueTuple.Create(r, l));
            foreach (var item in coll)
            {
                var srcPi = item.Item1;
                var destPi = item.Item2;
                if (destPi.PropertyType.IsAssignableFrom(srcPi.PropertyType))  //若可直接赋值
                {
                    if (destPi.CanWrite) //若可以直接写入
                        destPi.SetValue(result, srcPi.GetValue(src));
                    else //若不可直接写入
                    {
                    }
                }
                else //若不可以直接赋值
                {

                }
            }
            return result;
        }

        public static void Copy(IDictionary src, IDictionary dest)
        {
            var destType = dest.GetType();

        }
    }
}
