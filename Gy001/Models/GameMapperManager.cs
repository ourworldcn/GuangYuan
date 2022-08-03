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
}
