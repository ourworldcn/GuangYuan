using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Collections.Generic;
using System.Linq;
using System.Text.Unicode;
using System.Text;

namespace OW.Script
{
    public class OwScriptHelper
    {
        public static void Test()
        {
            var options = ScriptOptions.Default.
                AddReferences(typeof(OwScriptHelper).Assembly).AddImports("OW.Game.Script");
            var script = CSharpScript.Create("using static OW.Game.Script.Demo; HelloWorld()", options, typeof(string));
            var dia = script.Compile();
            var result = script.RunAsync("using static OW.Game.Script.Demo; HelloWorld()").Result;
            //System.Diagnostics.Debug
            var tmp = result.ReturnValue;
        }

        public static void Load(string path)
        {
            // Load the assembly
#if NETCOREAPP
            Assembly asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
# else
            Assembly asm = AssemblyLoader.Default.LoadFromAssemblyPath(path);
#endif
            // Invoke the RoslynCore.Helper.CalculateCircleArea method passing an argument
            double radius = 10;
            object result =
              asm.GetType("RoslynCore.Helper").GetMethod("CalculateCircleArea").
              Invoke(null, new object[] { radius });
            Console.WriteLine($"Circle area with radius = {radius} is {result}");
        }

        //public static void Diagnostics(EmitResult compilationResult)
        //{
        //    foreach (Diagnostic codeIssue in compilationResult.Diagnostics)
        //    {
        //        string issue = $"ID: {codeIssue.Id}, Message: {codeIssue.GetMessage()}, Location: { codeIssue.Location.GetLineSpan()},Severity: { codeIssue.Severity}";
        //        Debug.WriteLine(issue);
        //    }

        //}

        public static void TestAssm()
        {
            const string code = @"using System;
                using System.IO;
                namespace RoslynCore
                {
                 public static class Helper
                 {
                  public static double CalculateCircleArea(double radius)
                  {
                    return radius * radius * Math.PI;
                  }
                  }
                }";

            using var tr = new StringReader(code);
            var name = Path.GetTempFileName();
            var s = File.Create(name);
            EmitResult compResult;
            using (s)
                compResult = GenerateAssembly(tr, s, new Assembly[] { typeof(object).Assembly });
            Load(name);
        }

        /// <summary>
        /// 生成一个程序集。
        /// </summary>
        /// <param name="codeReader">读取代码文本的流。</param>
        /// <param name="assmStream">写入程序集的流。</param>
        /// <param name="references">引用的程序集。</param>
        /// <returns></returns>
        public static EmitResult GenerateAssembly(TextReader codeReader, FileStream assmStream, IEnumerable<Assembly> references)
        {
            SourceText sourceText = SourceText.From(codeReader, int.MaxValue);
            var tree = SyntaxFactory.ParseSyntaxTree(sourceText);

            // Detect the file location for the library that defines the object type
            //var systemRefLocation = typeof(object).GetTypeInfo().Assembly.Location;
            // Create a reference to the library
            var systemReference = references.Select(c => MetadataReference.CreateFromFile(c.Location));
            // A single, immutable invocation to the compiler to produce a library

            string fileName = Path.GetFileName(assmStream.Name);
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var compilation = CSharpCompilation.Create(fileName, new SyntaxTree[] { tree }, systemReference, options);

            EmitResult compilationResult = compilation.Emit(assmStream);
            return compilationResult;
        }

        public static (EmitResult,Stream) GenerateAssembly(TextReader codeReader, string assemblyName, IEnumerable<Assembly> references)
        {
            SourceText sourceText = SourceText.From(codeReader, int.MaxValue);
            var tree = SyntaxFactory.ParseSyntaxTree(sourceText);
            MemoryStream ms = new MemoryStream();

            // Detect the file location for the library that defines the object type
            //var systemRefLocation = typeof(object).GetTypeInfo().Assembly.Location;
            // Create a reference to the library
            var systemReference = references.Select(c => MetadataReference.CreateFromFile(c.Location));
            // A single, immutable invocation to the compiler to produce a library

            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var compilation = CSharpCompilation.Create(assemblyName, new SyntaxTree[] { tree }, systemReference, options);

            EmitResult compilationResult = compilation.Emit(ms);
            ms.Seek(0, SeekOrigin.Begin);
            return (compilationResult,ms);
        }
    }

    public static class Demo
    {
        public static string HelloWorld()
        {
            return "hello world";
            //SourceText.From()
            //SyntaxFactory.ParseSyntaxTree()
            //CSharpCompilation.Create();
        }


    }

}
