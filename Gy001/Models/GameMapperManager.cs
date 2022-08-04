using OW.Game;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using OW.Game.Store;
using GuangYuan.GY001.UserDb.Combat;
using GY2021001WebApi.Models;
using GuangYuan.GY001.UserDb;
using OW.Extensions.Game.Store;
using GuangYuan.GY001.UserDb.Social;
using OW.Game.PropertyChange;
using System.Diagnostics;

namespace GuangYuan.GY001.BLL.Specific
{
    /// <summary>
    /// 转换服务。
    /// </summary>
    public class GameMapperManager : GameManagerBase<GameMapperManagerOptions>
    {
        public GameMapperManager()
        {
            Initialize();
        }

        public GameMapperManager(IServiceProvider service) : base(service)
        {
            Initialize();
        }

        public GameMapperManager(IServiceProvider service, GameMapperManagerOptions options) : base(service, options)
        {
            Initialize();
        }

        void Initialize()
        {
            _Mapper = Service.GetRequiredService<IMapper>();
        }

        IMapper _Mapper;

        public TDestination Map<TDestination>(object source)
        {
            return _Mapper.Map<TDestination>(source);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="dic">不会清理其中内容。</param>
        /// <param name="prefix"></param>
        public void Map(VirtualThing node, IDictionary<string, string> dic, string prefix = null)
        {
            if (node.Parent != null)
                dic[$"{prefix}ptid"] = node.Parent.ExtraDecimal.ToString();
            dic[$"{prefix}tid"] = node.ExtraGuid.ToString();
            dic[$"{prefix}Count"] = node.GetJsonObject<Dictionary<string, string>>().GetValueOrDefault("Count");
        }

        #region 特定类型映射

        public void Map(CombatReport src, CombatDto dest)
        {
            var mapper = Service.GetRequiredService<IMapper>();
            mapper.Map(src, dest);
            dest.Id = src.Thing.Base64IdString;

            //dest.AttackerIds.AddRange(src.AttackerIds.Select(c => c.ToBase64String()));
            //dest.DefenserIds.AddRange(src.DefenserIds.Select(c => c.ToBase64String()));
        }

        public void Map(GameItem src, GameItemDto dest)
        {
            dest.Id = src.Id.ToBase64String();
            dest.Count = src.Count;
            dest.ExtraGuid = src.ExtraGuid.ToBase64String();
            dest.OwnerId = src.OwnerId?.ToBase64String();
            dest.ParentId = src.ParentId?.ToBase64String();
            dest.ClientString = src.GetClientString();
            foreach (var item in src.Name2FastChangingProperty)
            {
                item.Value.GetCurrentValueWithUtc();
                FastChangingPropertyExtensions.ToDictionary(item.Value, src.Properties, item.Key);
            }
            OwHelper.Copy(src.Properties, dest.Properties);

            dest.Properties[nameof(GameItem.ExtraString)] = src.ExtraString;
            dest.Properties[nameof(GameItem.ExtraDecimal)] = src.ExtraDecimal;

            //特殊处理处理木材堆叠数
            if (ProjectConstant.MucaiId == src.ExtraGuid)
                dest.Properties[ProjectConstant.StackUpperLimit] = World.ItemManager.GetStcOrOne(src);
            dest.Children.AddRange(src.Children.Select(c => Map(c)));
        }

        public GameItemDto Map(GameItem src)
        {
            var result = new GameItemDto();
            Map(src, result);
            return result;
        }

        public void Map(GameChar obj, GameCharDto result)
        {
            result.Id = obj.Id.ToBase64String();
            result.ClientGutsString = obj.GetClientString();
            result.CreateUtc = obj.CreateUtc;
            result.DisplayName = obj.DisplayName;
            result.GameUserId = obj.GameUserId.ToBase64String();
            result.TemplateId = obj.ExtraGuid.ToBase64String();
            result.CurrentDungeonId = obj.CurrentDungeonId?.ToBase64String();
            result.CombatStartUtc = obj.CombatStartUtc;

            result.GameItems.AddRange(obj.GameItems.Select(c => Map(c)));
            foreach (var item in obj.Properties)
            {
                result.Properties[item.Key] = item.Value;
            }
            foreach (var item in obj.GetOrCreateBinaryObject<CharBinaryExProperties>().ClientProperties)  //初始化客户端扩展属性
            {
                result.ClientExtendProperties[item.Key] = item.Value;
            }
        }

        public GameCharDto Map(GameChar obj)
        {
            var result = new GameCharDto();
            Map(obj, result);
            return result;
        }

        public void Map(GameGuild obj, GameGuildDto result)
        {
            result.Items.AddRange(obj.Items.Select(c => Map(c)));
        }

        public GameGuildDto Map(GameGuild obj)
        {
            var result = new GameGuildDto();
            Map(obj, result);
            return result;
        }

        public void Map(CharSummary obj, CharSummaryDto result)
        {
            result.CombatCap = obj.CombatCap;
            result.DisplayName = obj.DisplayName;
            result.Id = obj.Id.ToBase64String();
            result.LastLogoutDatetime = obj.LastLogoutDatetime;
            result.Level = obj.Level;
            result.Gold = obj.Gold;
            result.GoldOfStore = obj.GoldOfStore;
            result.MainControlRoomLevel = obj.MainBaseLevel;
            result.PvpScores = obj.PvpScores;
            result.Wood = obj.Wood;
            result.WoodOfStore = obj.WoodOfStore;
            result.HomelandShows.AddRange(obj.HomelandShows.Select(c => Map(c)));
        }

        public CharSummaryDto Map(CharSummary obj)
        {
            var result = new CharSummaryDto();
            Map(obj, result);
            return result;
        }

        public void Map(GamePropertyChangeItem<object> obj, GamePropertyChangeItemDto result)
        {
            result.DateTimeUtc = obj.DateTimeUtc;
            result.HasNewValue = obj.HasNewValue;
            result.HasOldValue = obj.HasOldValue;
            result.NewValue = obj.NewValue;
            result.ObjectId = (obj.Object as GameThingBase)?.Base64IdString;
            result.OldValue = obj.OldValue;
            result.PropertyName = obj.PropertyName;
            result.TId = (obj.Object as GameThingBase)?.ExtraGuid.ToBase64String();

            if (obj.IsCollectionRemoved())  //若是集合删除元素
                if (obj.OldValue is GameThingBase gt)
                    result.OldValue = gt.Base64IdString;
            if (obj.IsCollectionAdded()) //若添加了元素
            {
                if (obj.NewValue is GameItem gi)
                    result.NewValue = Map(gi);
                else if (obj.NewValue is GameChar gc)
                    result.NewValue = Map(gc);
                else if (obj.NewValue is GameGuild gg)
                    result.NewValue = Map(gg);
                Debug.WriteLine($"不认识的对象类型{obj.NewValue.GetType()}");
                //TO DO 不认识的对象类型
            }
        }

        public GamePropertyChangeItemDto Map(GamePropertyChangeItem<object> obj)
        {
            var result = new GamePropertyChangeItemDto();
            Map(obj, result);
            return result;
        }

        public void Map(ChangeItem obj, ChangesItemDto result)
        {
            result.ContainerId = obj.ContainerId.ToBase64String();
            result.DateTimeUtc = obj.DateTimeUtc;
            result.Adds.AddRange(obj.Adds.Select(c => Map(c)));
            result.Changes.AddRange(obj.Changes.Select(c => Map(c)));
            result.Removes.AddRange(obj.Removes.Select(c => c.ToBase64String()));
        }

        public ChangesItemDto Map(ChangeItem obj)
        {
            var result = new ChangesItemDto();
            Map(obj, result);
            return result;
        }

        public void Map(ApplyBlueprintDatas obj, ApplyBlueprintReturnDto result)
        {
            result.HasError = obj.HasError;
            result.DebugMessage = obj.DebugMessage;
            result.SuccCount = obj.SuccCount;
            if (!result.HasError)
            {
                result.ChangesItems.AddRange(obj.ChangeItems.Select(c => Map(c)));
                result.FormulaIds.AddRange(obj.FormulaIds.Select(c => c.ToBase64String()));
                result.ErrorTIds.AddRange(obj.ErrorItemTIds.Select(c => c.ToBase64String()));
                result.MailIds.AddRange(obj.MailIds.Select(c => c.ToBase64String()));
            }
        }

        public ApplyBlueprintReturnDto Map(ApplyBlueprintDatas obj)
        {
            var result = new ApplyBlueprintReturnDto();
            Map(obj, result);
            return result;
        }

        public void Map(EndCombatData obj, CombatEndReturnDto result)
        {
            result.NextDungeonId = obj.NextTemplate?.Id.ToBase64String();
            result.HasError = obj.HasError;
            result.DebugMessage = obj.DebugMessage;
            result.ChangesItems.AddRange(obj.ChangesItems.Select(c => Map(c)));
        }

        public CombatEndReturnDto Map(EndCombatData obj)
        {
            var result = new CombatEndReturnDto();
            Map(obj, result);
            return result;
        }

        public void Map(GameBooty obj, GameBootyDto result)
        {
            result.CharId = obj.CharId.ToBase64String();
            result.Count = obj.StringDictionary.GetDecimalOrDefault("count");
            result.ParentId = obj.Thing.ParentId.Value.ToBase64String();
            result.TemplateId = obj.StringDictionary.GetGuidOrDefault("tid").ToBase64String();
            OwHelper.Copy(obj.StringDictionary, result.Properties);
        }

        public GameBootyDto Map(GameBooty obj)
        {
            var result = new GameBootyDto();
            Map(obj, result);
            return result;
        }
        #endregion 特定类型映射

        //public partial class CombatDto
        //{
        //    public static implicit operator CombatDto(CombatReport obj)
        //    {
        //        var result = new CombatDto()
        //        {
        //            EndUtc = obj.EndUtc,
        //            Id = obj.Thing.Base64IdString,
        //        };
        //        result.AttackerIds.AddRange(obj.AttackerIds.Select(c => c.ToBase64String()));
        //        result.DefenserIds.AddRange(obj.DefenserIds.Select(c => c.ToBase64String()));
        //        OwHelper.Copy(obj.StringDictionary, result.Properties);
        //        return result;
        //    }
        //}


    }

    public class GameMapperManagerOptions
    {
        public GameMapperManagerOptions()
        {

        }
    }

    public class GameMapperTypeDescriptorContext : ITypeDescriptorContext
    {
        public GameMapperTypeDescriptorContext(object instance)
        {
            _Instance = instance;
        }

        public GameMapperTypeDescriptorContext(object instance, IServiceProvider services)
        {
            _Services = services;
            _Instance = instance;
        }

        public IContainer Container { get; }

        object _Instance;

        public object Instance
        {
            get => _Instance;
            set => _Instance = value;
        }

        public PropertyDescriptor PropertyDescriptor { get; }

        readonly IServiceProvider _Services;

        public object GetService(Type serviceType) => _Services.GetService(serviceType);

        public void OnComponentChanged()
        {
        }

        public bool OnComponentChanging() => true;
    }

    /// <summary>
    /// AutoMapper 底层使用转换，如果用了 AutoMapper 最好就不用转换器以避免冲突。
    /// </summary>
    public abstract class GameMapperConverter : TypeConverter
    {
        public GameMapperConverter()
        {
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return false;
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var collSrc = TypeDescriptor.GetProperties(value).OfType<PropertyDescriptor>();
            var collDest = TypeDescriptor.GetProperties(context.Instance).OfType<PropertyDescriptor>();
            var coll = collDest.Join(collSrc, c => c.Name, c => c.Name, (l, r) => (src: r, dest: l));
            foreach (var item in coll)
            {
                var src = item.src.GetValue(value);
                switch (src)
                {
                    case IReadOnlyDictionary<string, string> dic:
                        OwHelper.Copy(dic, item.dest.GetValue(context.Instance) as IDictionary<string, string>);
                        break;
                    case IReadOnlyDictionary<string, object> dic:
                        OwHelper.Copy(dic, item.dest.GetValue(context.Instance) as IDictionary<string, object>);
                        break;
                    case ICollection collection when item.dest.GetValue(context.Instance) is IList destColl:
                        var type = item.dest.PropertyType.GenericTypeArguments[0];
                        var converter = TypeDescriptor.GetConverter(type);
                        foreach (var element in collection)
                        {
                            var destElement = TypeDescriptor.CreateInstance(null, type, default, default);
                            converter.ConvertFrom(new GameMapperTypeDescriptorContext(destElement), CultureInfo.InvariantCulture, element);
                            destColl.Add(destElement);
                        }
                        break;
                    case Guid id:
                        item.dest.SetValue(context.Instance, id.ToBase64String());
                        break;
                    case ValueType valueType:
                        item.dest.SetValue(context.Instance, valueType);
                        break;
                    case object obj:
                        break;
                    default:
                        break;
                }
            }
            return context.Instance;
        }

    }

    public static class GameMapperManagerExtensions
    {
        static GameMapperManager _Mapper;
        public static GameMapperManager GetMapper(this VWorld world) => _Mapper ??= world.Service.GetService<GameMapperManager>();
    }
}
