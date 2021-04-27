using GY2021001DAL;
using Gy2021001Template;
using Microsoft.Extensions.DependencyInjection;
using OwGame;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace GY2021001BLL
{
    public class VWorld
    {
        public readonly DateTime StartDateTimeUtc = DateTime.UtcNow;

        private readonly IServiceProvider _ServiceProvider;

        CancellationTokenSource _CancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// 该游戏世界因为种种原因已经请求卸载。
        /// </summary>
        public CancellationToken RequestShutdown;

        public VWorld()
        {
            Initialize();
        }

        public VWorld(IServiceProvider serviceProvider)
        {
            _ServiceProvider = serviceProvider;
            Initialize();
        }

        private void Initialize()
        {
            RequestShutdown = _CancellationTokenSource.Token;
        }

        public TimeSpan GetServiceTime()
        {
            return DateTime.UtcNow - StartDateTimeUtc;
        }

        /// <summary>
        /// 通知游戏世界开始下线。
        /// </summary>
        public void NotifyShutdown()
        {
            _CancellationTokenSource.Cancel();
        }

        /// <summary>
        /// 按照指定模板创建一个对象。
        /// </summary>
        /// <param name="template"></param>
        /// <param name="ownerId">指定一个父Id,如果不指定或为null则忽略。</param>
        /// <returns></returns>
        public GameItem CreateGameItem(GameItemTemplate template, Guid? ownerId = null)
        {
            var result = new GameItem()
            {
                TemplateId = template.Id,
                PropertiesString = template.PropertiesString,
                OwnerId = ownerId,
            };
            //初始化级别
            decimal lv;
            if (!result.Properties.TryGetValue(ProjectConstant.LevelPropertyName, out object lvObj))
            {
                lv = 0;
                result.Properties[ProjectConstant.LevelPropertyName] = lv;
            }
            else
                lv = (decimal)lvObj;
            foreach (var item in template.Properties)
            {
                var seq = item.Value as decimal[];
                if (null != seq)   //若是属性序列
                {
                    result.Properties[item.Key] = seq[(int)lv];
                }
                else
                    result.Properties[item.Key] = item.Value;
            }
            result.PropertiesString = OwHelper.ToPropertiesString(result.Properties);   //改写属性字符串
            return result;
        }

        /// <summary>
        /// 创建角色。
        /// </summary>
        /// <param name="user">角色所属对象。</param>
        /// <returns></returns>
        public GameChar CreateChar(GameUser user)
        {
            GameItemTemplateManager templateManager = _ServiceProvider.GetService<GameItemTemplateManager>();
            var result = new GameChar()
            {
            };
            //初始化槽
            var slot = CreateGameItem(templateManager.GetTemplateFromeId(new Guid(ProjectConstant.ZuojiTou)), result.Id);    //创建当前坐骑头的容器
            result.GameItems.Add(slot);
            var slotBody = CreateGameItem(templateManager.GetTemplateFromeId(new Guid(ProjectConstant.ZuojiShen)), result.Id);    //创建当前坐骑身的容器
            result.GameItems.Add(slotBody);

            var ts = templateManager.Id2Template.Values.Where(c => c.Properties.ContainsKey("nm") && "羊" == c.Properties["nm"] as string).ToList();   //获取羊
            var headTemplate = ts.FirstOrDefault(c => c.IsHead());
            var bodyTemplate = ts.FirstOrDefault(c => c.IsBody());
            if (null != headTemplate && null != bodyTemplate)    //若需要的数据完整
            {
                var head = CreateGameItem(headTemplate);
                slot.Children.Add(head);
                var body = CreateGameItem(bodyTemplate);
                slotBody.Children.Add(body);
            }

            user.DbContext.Set<GameItem>().AddRange(result.GameItems);
            //result.GameUserId = user.Id;
            //user.DbContext.Set<GameChar>().Add(result);
            result.InitialCreation();
            //累计属性
            var allProps = OwHelper.GetAllSubItemsOfTree(result.GameItems, c => c.Children).SelectMany(c => c.Properties);
            var coll = from tmp in allProps
                       where tmp.Value is decimal && tmp.Key != ProjectConstant.LevelPropertyName   //避免累加级别属性
                       group (decimal)tmp.Value by tmp.Key into g
                       select ValueTuple.Create(g.Key, g.Sum());
            //select ValueTuple.Create(tmp.Key, (decimal)tmp.Value);

            foreach (var item in coll)
            {
                result.Properties[item.Item1] = item.Item2;
            }
            result.PropertiesString = OwHelper.ToPropertiesString(result.Properties);   //改写属性字符串

            user.GameChars.Add(result);

            return result;
        }

    }


}
