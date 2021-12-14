using GuangYuan.GY001.TemplateDb;
using OW.Game;
using OW.Script;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GuangYuan.GY001.BLL.Script
{
    public class GameScriptManagerOptions
    {

    }

    /// <summary>
    /// 脚本管理器。
    /// </summary>
    public class GameScriptManager : GameManagerBase<GameScriptManagerOptions>
    {
        public GameScriptManager()
        {
            Initialize();
        }

        public GameScriptManager(IServiceProvider service) : base(service)
        {
            Initialize();
        }

        public GameScriptManager(IServiceProvider service, GameScriptManagerOptions options) : base(service, options)
        {
            Initialize();
        }

        void Initialize()
        {
            _LazyItemTemplateScript = new Lazy<ConcurrentDictionary<Guid, ItemTemplateScriptBase>>(
                () =>
                {
                    using IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly, null, null);
                    using var streamCode = new IsolatedStorageFileStream("ItemTemplateScript.cs", FileMode.Create, FileAccess.ReadWrite, isoStore);    //文件流
                    FillTemplateScript(streamCode);
                    streamCode.Seek(0, SeekOrigin.Begin);
                    MemoryStream dynAssmStream = new MemoryStream();
                    using (var tr = new StreamReader(streamCode, Encoding.UTF8, true, 4096, true))
                    {
                        var ss = OwScriptHelper.GenerateAssembly(tr, "ItemTemplateScript.dll", GetAssemblies());
                    }
                    ConcurrentDictionary<Guid, ItemTemplateScriptBase> result = new ConcurrentDictionary<Guid, ItemTemplateScriptBase>();
                    return result;
                }, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
            _ = _LazyItemTemplateScript.Value;
        }

        IEnumerable<Assembly> GetAssemblies()
        {
            List<Assembly> result = new List<Assembly>(AppDomain.CurrentDomain.GetAssemblies().Where(c => !c.IsDynamic));
            return result;
        }

        void FillTemplateScript(Stream stream)
        {
            using var tw = new StreamWriter(stream, Encoding.UTF8, -1, true);
            tw.WriteLine("using System;using GuangYuan.GY001.TemplateDb;using GuangYuan.GY001.UserDb;using System.Collections.Generic;using OW.Game;");   //引用的命名空间
            tw.WriteLine("using GuangYuan.GY001.BLL.Script;");   //引用的命名空间
            tw.WriteLine("namespace GuangYuan.GY001.BLL{");
            string str;
            foreach (var item in World.ItemTemplateManager.Id2Template.Values)
            {
                str = GetClassDefine(item);
                if (!string.IsNullOrWhiteSpace(str))
                    tw.WriteLine(str);
            }
            tw.WriteLine("}");
        }

        string GetClassDefine(GameItemTemplate template)
        {
            return $"public class ItemTemplateScript{template.Id:N} : ItemTemplateScriptBase {{" + Environment.NewLine +
                $"public ItemTemplateScript{template.Id:N}(GameItemTemplate template) : base(template){{}}" + Environment.NewLine +
                $"{template.Script}}}";
        }

        Lazy<ConcurrentDictionary<Guid, ItemTemplateScriptBase>> _LazyItemTemplateScript;
    }

    public class ItemTemplateScriptDemo : ItemTemplateScriptBase
    {
        public ItemTemplateScriptDemo(GameItemTemplate template) : base(template) { }
    }

}
