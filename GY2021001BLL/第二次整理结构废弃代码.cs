using System;
using System.Collections.Generic;
using System.Text;

namespace GuangYuan.GY001.BLL
{/*
    /// <summary>
    /// 初始化挂接接口。
    /// </summary>
    public interface IGameObjectInitializer
    {
        /// <summary>
        /// 在游戏对象创建后调用，以帮助特定项目初始化自己独有的数据。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>true初始化了数据，false没有进行特定该类型对象的初始化。</returns>
        bool Created(object obj, IReadOnlyDictionary<string, object> parameters);

        /// <summary>
        /// 游戏对象从后被存储加载到内存后调用。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>true初始化了数据，false没有进行特定该类型对象的初始化。</returns>
        bool Loaded(object obj, DbContext context);
    }

        /// <summary>
    /// 项目特定的初始化。
    /// </summary>
    public class Gy001Initializer : GameManagerBase<Gy001InitializerOptions>, IGameObjectInitializer
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public Gy001Initializer()
        {

        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="service"><inheritdoc/></param>
        public Gy001Initializer(IServiceProvider service) : base(service)
        {
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="service"><inheritdoc/></param>
        /// <param name="options"><inheritdoc/></param>
        public Gy001Initializer(IServiceProvider service, Gy001InitializerOptions options) : base(service, options)
        {
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="parameters"></param>
        public bool Created(object obj, IReadOnlyDictionary<string, object> parameters)
        {
            if (obj is GameItem gi)
            {

            }
            else if (obj is GameChar gc)
            {
                return InitializerChar(gc, parameters);
            }
            else if (obj is GameUser gu)
            {
                InitializerUser(gu, parameters);
            }
            else
                return false;
            return true;
        }

        public bool Loaded(object obj, DbContext context)
        {
            if (obj is GameItem gi)
            {

            }
            else if (obj is GameChar gc)
            {
                //清除锁定属性槽内物品，放回道具背包中
                var gim = World.ItemManager;
                var daojuBag = gc.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.DaojuBagSlotId); //道具背包
                var slot = gc.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.LockAtkSlotId); //锁定槽
                gim.MoveItems(slot, c => true, daojuBag);
                slot = gc.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.LockMhpSlotId); //锁定槽
                gim.MoveItems(slot, c => true, daojuBag);
                slot = gc.GameItems.FirstOrDefault(c => c.TemplateId == ProjectConstant.LockQltSlotId); //锁定槽
                gim.MoveItems(slot, c => true, daojuBag);
                //挂接升级回调
                var hl = gc.GetHomeland();
                foreach (var item in hl.AllChildren)
                {
                    if (!item.Name2FastChangingProperty.TryGetValue(ProjectConstant.UpgradeTimeName, out var fcp))
                        continue;

                    var dt = fcp.GetComplateDateTime();
                    var now = DateTime.UtcNow;
                    TimeSpan ts;
                    if (now >= dt)   //若已经超时
                        ts = TimeSpan.Zero;
                    else
                        ts = dt - now;
                    var tm = new Timer(World.BlueprintManager.LevelUpCompleted, ValueTuple.Create(gc.Id, item.Id), ts, Timeout.InfiniteTimeSpan);
                }

            }
            else if (obj is GameUser gu)
            {
                gu.CurrentChar = gu.GameChars[0];   //项目特定:一个用户有且仅有一个角色
                gu.CurrentChar.Loaded(gu.Services, gu.DbContext);
            }
            else
                return false;
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public bool InitializerUser(GameUser user, IReadOnlyDictionary<string, object> parameters)
        {
            var gc = new GameChar();
            user.GameChars.Add(gc);
            user.CurrentChar = gc;
            gc.Initialize(Service, new Dictionary<string, object>()
                {
                    { "tid",ProjectConstant.CharTemplateId},
                    { "user",user},
                    {"DisplayName",parameters.GetValueOrDefault("charDisplayName") },
                });
            //生成缓存数据
            var sep = new CharSpecificExpandProperty
            {
                CharLevel = (int)gc.Properties.GetDecimalOrDefault(ProjectConstant.LevelPropertyName),
                LastPvpScore = 1000,
                PvpScore = 1000,
                Id = gc.Id,
                GameChar = gc,
                FrinedCount = 0,
                FrinedMaxCount = 10,
                LastLogoutUtc = DateTime.UtcNow,
            };
            gc.SpecificExpandProperties = sep;
            return true;
        }
        /// <summary>
        /// 角色创建后被调用。
        /// </summary>
        /// <param name="gameChar">已经创建的对象。</param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public bool InitializerChar(GameChar gameChar, IReadOnlyDictionary<string, object> parameters)
        {
            var world = World;
            var gitm = world.ItemTemplateManager;
            var result = false;
            //增加坐骑
            var mountsBagSlot = gameChar.GameItems.First(c => c.TemplateId == ProjectConstant.ZuojiBagSlotId);   //坐骑背包槽
            for (int i = 3001; i < 3002; i++)   //仅增加羊坐骑
            {
                var headTemplate = gitm.Id2Template.Values.FirstOrDefault(c => c.GId.GetValueOrDefault() == i);
                var bodyTemplate = gitm.Id2Template.Values.FirstOrDefault(c => c.GId.GetValueOrDefault() == 1000 + i);
                var mounts = world.ItemManager.CreateMounts(headTemplate, bodyTemplate);
                world.ItemManager.ForcedAdd(mounts, mountsBagSlot);
            }
            //增加神纹
            var runseSlot = gameChar.GameItems.First(c => c.TemplateId == ProjectConstant.ShenWenSlotId);   //神纹装备槽
            var templates = world.ItemTemplateManager.Id2Template.Values.Where(c => c.GenusCode == 10);
            var shenwens = templates.Select(c =>
            {
                var r = new GameItem();
                r.Initialize(Service, c.Id);
                return r;
            });
            foreach (var item in shenwens)
            {
                world.ItemManager.ForcedAdd(item, runseSlot);
            }
            var db = gameChar.GameUser.DbContext;
            if (string.IsNullOrWhiteSpace(gameChar.DisplayName))    //若没有指定昵称
            {
                string displayName;
                for (displayName = CnNames.GetName(VWorld.IsHit(0.5)); db.Set<GameChar>().Any(c => c.DisplayName == displayName); displayName = CnNames.GetName(VWorld.IsHit(0.5)))
                    ;
                gameChar.DisplayName = displayName;
            }
            result = true;
            //修正木材存贮最大量
            //var mucai = gameChar.GameItems.First(c => c.TemplateId == ProjectConstant.MucaiId);
            //var stcMucai = mucai.GetStc();
            //if (stcMucai < decimal.MaxValue)
            //{
            //    var mucaiStore = gameChar.GetHomeland().Children.Where(c => c.TemplateId == ProjectConstant.MucaiStoreTId);
            //    var stcs = mucaiStore.Select(c => c.GetStc());
            //    if (stcs.Any(c => c == decimal.MaxValue))   //若有任何仓库是最大堆叠
            //        mucai.SetPropertyValue(ProjectConstant.StackUpperLimit, -1);
            //    else
            //        mucai.SetPropertyValue(ProjectConstant.StackUpperLimit, stcs.Sum() + stcMucai);
            //}
            //将坐骑入展示宠物
            var sheepBodyTId = new Guid("BBC9FE07-29BD-486D-8AD6-B99DB0BD07D6");
            var gim = world.ItemManager;
            var showMount = gameChar.GetZuojiBag().Children.FirstOrDefault();
            var dic = showMount?.Properties;
            if (dic != null)
                dic["for10"] = 0;
            GameSocialRelationship gsr = new GameSocialRelationship()
            {
                Id = gameChar.Id,
                Id2 = gim.GetBody(showMount).TemplateId,
                KeyType = SocialConstant.HomelandShowKeyType,
            };
            db.Add(gsr);
            return result;
        }
    }

    public class Gy001InitializerOptions
    {
        public Gy001InitializerOptions()
        {

        }
    }

            /// <summary>
        /// 在新建一个对象后调用此方法初始化其内容。
        /// </summary>
        /// <param name="service"></param>
        /// <param name="parameters">初始化用到的附属参数。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(IServiceProvider service, IReadOnlyDictionary<string, object> parameters)
        {
            InitializeCore(service, parameters);
            //如果有初始化器则调用项目初始化函数
            var init = service.GetService(typeof(IGameObjectInitializer)) as IGameObjectInitializer;
            init?.Created(this, parameters);
        }

        /// <summary>
        /// 新建对象后此方法被<see cref="Initialize(IServiceProvider, IReadOnlyDictionary{string, object})"/>调用以实际初始化本对象。
        /// 此实现立即返回。
        /// </summary>
        /// <param name="service">服务容器。</param>
        /// <param name="parameters">初始化用到的附属参数。</param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected virtual void InitializeCore(IServiceProvider service, IReadOnlyDictionary<string, object> parameters)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="service"></param>
        /// <param name="context"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Loaded([NotNull] IServiceProvider service, [NotNull] DbContext context)
        {
            Loaded(service, new Dictionary<string, object>()
            {
                { "db",context},
            });
        }

        /// <summary>
        /// 在将一个对象从数据库加载到内存后调用此函数初始化。
        /// </summary>
        /// <param name="service"></param>
        /// <param name="parameters">各个类可能需要不同的参数以指导初始化工作。目前仅有"db"是必须的，是加载此对象的数据库上下文对象。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Loaded([NotNull] IServiceProvider service, [NotNull] IReadOnlyDictionary<string, object> parameters)
        {
            LoadedCore(service, parameters);
            //调用项目特定的加载函数。
            var init = service.GetService(typeof(IGameObjectInitializer)) as IGameObjectInitializer;
            var db = parameters["db"] as DbContext;
            init?.Loaded(this, db);
        }

        /// <summary>
        /// 对象从内存加载到数据库后调用此函数初始化。派生类应重载此方法。
        /// 此实现立即返回。
        /// </summary>
        /// <param name="service"></param>
        /// <param name="parameters"></param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected virtual void LoadedCore([NotNull] IServiceProvider service, [NotNull] IReadOnlyDictionary<string, object> parameters)
        {

        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="service"></param>
        /// <param name="parameters"><inheritdoc/>。另外额外需要user指定其所属的用户对象。</param>
        /// <exception cref="InvalidOperationException">缺少键值指定该对象所属的<see cref="GameUser"/>对象。</exception>
        protected override void InitializeCore(IServiceProvider service, IReadOnlyDictionary<string, object> parameters)
        {
            base.InitializeCore(service, parameters);
            //初始化本类型特殊数据
            if (parameters.TryGetValue("user", out var obj) && obj is GameUser gu)
            {
                GameUserId = gu.Id;
                GameUser = gu;
            }
            else
                throw new InvalidOperationException($"缺少键值指定该对象所属的{nameof(GameUser)}对象。");
            DisplayName = parameters.GetValueOrDefault(nameof(DisplayName)) as string;
            var db = DbContext;
            ExtendProperties.Add(new GameExtendProperty()   //增加推关战力
            {
                Id = Id,
                Name = "推关战力",
                StringValue = DisplayName,
                DecimalValue = 0,
            });
            //追加子对象
            if (Template.ChildrenTemplateIds.Count > 0)
            {
                Dictionary<string, object> dic = new Dictionary<string, object>()
                {
                    {"owner",this },
                };
                _GameItems ??= new List<GameItem>();
                _GameItems.AddRange(Template.ChildrenTemplateIds.Select(c =>
                {
                    dic["tid"] = c;
                    GameItem gameItem = new GameItem();
                    gameItem.Initialize(service, dic);
                    return gameItem;
                }));
                db.AddRange(_GameItems); //将直接孩子加入数据库
            }
            db.Add(new GameActionRecord
            {
                ParentId = Id,
                ActionId = "Created",
                PropertiesString = $"CreateBy=CreateChar",
            });
        }

        protected override void LoadedCore(IServiceProvider service, IReadOnlyDictionary<string, object> parameters)
        {
            base.LoadedCore(service, parameters);
            Debug.Assert(GameUser != null && GameUser.DbContext != null);
            //未发送给客户端的数据
            var exProp = ExtendProperties.FirstOrDefault(c => c.Name == ChangesItemExPropertyName);
            if (null != exProp)    //若有需要反序列化的对象
            {
                var tmp = JsonSerializer.Deserialize<List<ChangesItemSummary>>(exProp.Text);
                _ChangesItems = ChangesItemSummary.ToChangesItem(tmp, this);
            }

            //加载扩展属性
            SpecificExpandProperties = DbContext.Set<CharSpecificExpandProperty>().Find(Id);
            ////补足角色的槽
            //var tt = World.ItemTemplateManager.GetTemplateFromeId(e.GameChar.TemplateId);
            //List<Guid> ids = new List<Guid>();
            //tt.ChildrenTemplateIds.ApartWithWithRepeated(e.GameChar.GameItems, c => c, c => c.TemplateId, ids, null, null);
            //foreach (var item in ids.Select(c => World.ItemTemplateManager.GetTemplateFromeId(c)))
            //{
            //    var gameItem = World.ItemManager.CreateGameItem(item, e.GameChar.Id);
            //    e.GameChar.GameUser.DbContext.Set<GameItem>().Add(gameItem);
            //    e.GameChar.GameItems.Add(gameItem);
            //}
            ////补足所属物品的槽
            //World.ItemManager.Normalize(e.GameChar.GameItems);
            //通知直接所属物品加载完毕
            var list = GameItems.ToList();
            list.ForEach(c => c.Loaded(service, DbContext));
        }

        /// <summary>
        /// 新建对象后此方法被<see cref="Initialize(IServiceProvider, IReadOnlyDictionary{string, object})"/>调用以实际初始化本对象。
        /// </summary>
        /// <param name="service">服务容器。</param>
        /// <param name="parameters"><inheritdoc/> <see cref="GameThingBase"/>需要键为tid，值为Guid类型的参数指定使用的模板Id </param>
        /// <exception cref="InvalidOperationException">没有指定有效的模板Id。</exception>
        protected override void InitializeCore(IServiceProvider service, IReadOnlyDictionary<string, object> parameters)
        {
            base.InitializeCore(service, parameters);
            TemplateId = parameters.GetGuidOrDefault("tid");
            if (Guid.Empty == TemplateId) throw new InvalidOperationException("没有指定有效的模板Id。");
            var helper = service.GetService(typeof(IGameThingHelper)) as IGameThingHelper;
            Template = helper.GetTemplateFromeId(TemplateId);
            var gpm = service.GetService(typeof(IGamePropertyManager)) as IGamePropertyManager;
            var coll = gpm is null ? Template.Properties : gpm.Filter(Template.Properties);
            //初始化自身属性
            foreach (var item in coll)   //复制属性
            {
                if (item.Value is IList seq)   //若是属性序列
                {
                    var indexPn = Template.GetIndexPropName(item.Key);
                    var lv = Convert.ToInt32(Template.Properties.GetValueOrDefault(indexPn, 0m));
                    Properties[item.Key] = seq[Math.Clamp(lv, 0, seq.Count - 1)];
                }
                else
                    Properties[item.Key] = item.Value;
            }
            if (Template.SequencePropertyNames.Length > 0 && !Properties.Keys.Any(c => c.StartsWith(GameThingTemplateBase.LevelPrefix))) //若需追加等级属性
                Properties[GameThingTemplateBase.LevelPrefix] = 0m;
#if DEBUG
            Properties["tname"] = Template.DisplayName.Replace('，', '-').Replace(',', '-').Replace('=', '-');
#endif

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="service"></param>
        /// <param name="parameters"></param>
        protected override void LoadedCore(IServiceProvider service, IReadOnlyDictionary<string, object> parameters)
        {
            base.LoadedCore(service, parameters);
            var helper = service.GetService(typeof(IGameThingHelper)) as IGameThingHelper;
            Template = helper?.GetTemplateFromeId(TemplateId);

        }

            public void Initialize(IServiceProvider service, string loginName, string pwd, DbContext db, string charDisplayName = null)
        {
            var dic = new Dictionary<string, object>
            {
                {"uid",loginName },
                {"pwd",pwd },
                {"db",db },
                {nameof(GameChar.DisplayName),charDisplayName },
            };
            Initialize(service, dic);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="service"></param>
        /// <param name="parameters"><inheritdoc/>,额外需要以下参数<code>
        /// {
        ///     {"uid",loginName }, //登录名，字符串
        ///     {"pwd",pwd},    //密码，字符串明文。
        ///     {"db",db}, //数据库上下文对象。
        /// }
        /// </code>。</param>
        protected override void InitializeCore(IServiceProvider service, IReadOnlyDictionary<string, object> parameters)
        {
            base.InitializeCore(service, parameters);
            //初始化本类型的数据
            Services = service;
            DbContext = parameters["db"] as DbContext;

            CurrentToken = Guid.NewGuid();
            LoginName = (string)parameters["uid"];
            var pwd = (string)parameters["pwd"];
            using var hash = (HashAlgorithm)service.GetService(typeof(HashAlgorithm));
            var pwdHash = hash.ComputeHash(Encoding.UTF8.GetBytes(pwd));
            PwdHash = pwdHash;

            DbContext.Add(this);
        }

        protected override void LoadedCore(IServiceProvider service, IReadOnlyDictionary<string, object> parameters)
        {
            base.LoadedCore(service, parameters);
            //初始化本类型的数据
            Services = service;
            DbContext = parameters["db"] as DbContext;
            CurrentToken = Guid.NewGuid();
        }

            /// <summary>
        /// 
        /// </summary>
        /// <param name="service"></param>
        /// <param name="tid"></param>
        /// <param name="parent">省略则不指定父对象。</param>
        public void Initialize(IServiceProvider service, Guid tid, GameThingBase parent = null)
        {
            var dic = new Dictionary<string, object>
            {
                {"tid",tid },
            };
            if (parent is GameItem gi)
                dic["parent"] = gi;
            else if (null != parent)
                dic["owner"] = parent;
            Initialize(service, dic);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="service"><inheritdoc/></param>
        /// <param name="parameters"><inheritdoc/>。另外可以用parent或owner键指定其父对象。若都没有则不设置父的导航关系。
        /// owner可以是<see cref="GuidKeyObjectBase"/>的派生类，也可以是一个<see cref="Guid"/>对象</param>
        protected override void InitializeCore(IServiceProvider service, IReadOnlyDictionary<string, object> parameters)
        {
            base.InitializeCore(service, parameters);
            //设置本类型特有属性
            if (Template.Properties.TryGetValue("Count", out var countObj)) //若指定了初始数量
                Count = Convert.ToDecimal(countObj);
            else
                Count ??= Template.Properties.ContainsKey(StackUpperLimit) ? 0 : 1;
            if (parameters.TryGetValue("parent", out var obj) && obj is GameItem gi)
            {
                ParentId = gi.Id;
                Parent = gi;
            }
            else if (parameters.TryGetValue("owner", out obj))
            {
                OwnerId = obj switch
                {
                    _ when obj is GuidKeyObjectBase guidObj => guidObj.Id,
                    _ when obj is Guid id => id,
                    _ => throw new ArgumentException($"键值owner的对象只能是{typeof(Guid)}或{typeof(GuidKeyObjectBase)}的派生类。", nameof(parameters)),
                };
            }
            //追加子对象
            if (Template.ChildrenTemplateIds.Count > 0)
            {
                Dictionary<string, object> dic = new Dictionary<string, object>()
                {
                    { "parent", this},
                };
                Children.AddRange(Template.ChildrenTemplateIds.Select(c =>
                {
                    dic["tid"] = c;
                    var r = new GameItem();
                    r.Initialize(service, dic);
                    return r;
                }));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="service"></param>
        /// <param name="parameters"></param>
        protected override void LoadedCore(IServiceProvider service, IReadOnlyDictionary<string, object> parameters)
        {
            base.LoadedCore(service, parameters);
            //通知直接所属物品加载完毕
            var list = Children.ToList();
            list.ForEach(c => c.Loaded(service, DbContext));
        }


  */
}
