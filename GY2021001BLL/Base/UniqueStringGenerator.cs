using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace OW.Game
{
    public interface IUniqueStringGenerator
    {
        public abstract string GetUniqueString();
    }

    public abstract class LoginNameGenerator : IUniqueStringGenerator
    {
        public abstract string GetUniqueString();
    }

    public class DisplayNameGenerator : IUniqueStringGenerator
    {
        public DisplayNameGenerator(string firstNameFile, string secNameFile)
        {
            _FirstNames = new Lazy<string[]>(() =>
            {
                string str = File.ReadAllText(firstNameFile);
                return str.Split(Environment.NewLine).Where(c => !string.IsNullOrWhiteSpace(c)).ToArray();
            }, LazyThreadSafetyMode.ExecutionAndPublication);
            _SecNames = new Lazy<string[]>(() =>
            {
                var str = File.ReadAllText(secNameFile);
                return str.Split(Environment.NewLine).Where(c => !string.IsNullOrWhiteSpace(c)).ToArray();
            }, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        /// 前字词组。
        /// </summary>
        private readonly Lazy<string[]> _FirstNames;

        protected string[] FirstNames => _FirstNames.Value;

        /// <summary>
        /// 后字词组。
        /// </summary>
        private readonly Lazy<string[]> _SecNames;

        protected string[] SecNames => _SecNames.Value;

        private readonly Random _Random = new Random();

        public string GetUniqueString()
        {
            lock (_Random)
                return $"{_FirstNames.Value[_Random.Next(_FirstNames.Value.Length)]} {_SecNames.Value[_Random.Next(_SecNames.Value.Length)]}";
        }
    }

    public class GyLoginNameGenerator : LoginNameGenerator
    {
        public GyLoginNameGenerator(int start = 0)
        {
            Current = _Start = start % 1000000;
        }

        private readonly int _Start;
        public int Start
        {
            get => _Start;
        }

        public volatile int Current;

        public override string GetUniqueString()
        {
            return $"gy{DateTime.UtcNow:yyMMdd}{Interlocked.Increment(ref Current) % 1000000:000000}";
        }
    }

    public class EnDisplayNameGenerator : DisplayNameGenerator
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="firstNameFile">英文名文件。</param>
        /// <param name="secNameFile">英文姓文件。</param>
        public EnDisplayNameGenerator(string firstNameFile, string secNameFile) : base(firstNameFile, secNameFile)
        {
        }

    }
}
