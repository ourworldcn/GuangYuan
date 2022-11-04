using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace OW.Game
{
    /// <summary>
    /// 被包装的类需要实现的接口。
    /// </summary>
    public interface IGameObject<TKey>
    {
        /// <summary>
        /// 唯一Id。
        /// </summary>
        public TKey Id { get; set; }

        /// <summary>
        /// 质点(圆心)的x轴坐标。
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// 质点(圆心)的y轴坐标。
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// 质点(圆心)的z轴坐标。
        /// </summary>
        public float Z { get; set; }

        /// <summary>
        /// 碰撞半径。
        /// </summary>
        public float Radius { get; set; }
    }

    public class SquareItem<T>
    {

        public SquareItem<T> Top { get; set; }

        public SquareItem<T> Bottom { get; set; }

        public SquareItem<T> Left { get; set; }

        public SquareItem<T> Right { get; set; }

        private List<T> _Datas;
        public List<T> Datas { get => _Datas ??= new List<T>(); }
    }

    /// <summary>
    /// 碰撞运算加速器。
    /// </summary>
    /// <typeparam name="T">数据包装类。</typeparam>
    public class CollisionAccelerator<T, TKey> where T : IGameObject<TKey>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="nodeCount">预期的节点最大数量(同事存在的)。</param>
        public CollisionAccelerator(int nodeCount)
        {
            _NodeCount = nodeCount;
            _Nodes = new ConcurrentDictionary<int, Dictionary<int, IGameObject<TKey>>>(Environment.ProcessorCount, nodeCount);
        }

        ConcurrentDictionary<(int, int), SquareItem<T>> _Grid = new ConcurrentDictionary<(int, int), SquareItem<T>>();

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        protected (int, int) GetIndex(IGameObject<TKey> obj)
        {
            var x = (int)MathF.Floor(obj.X / 2);
            var y = (int)MathF.Floor(obj.Y / 2);
            return (x, y);
        }

        int _NodeCount;
        ConcurrentDictionary<int, Dictionary<int, IGameObject<TKey>>> _Nodes;

        LinkedList<T> _List = new LinkedList<T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        protected void GetIndex(IGameObject<TKey> obj, out int x, out int y)
        {
            x = (int)MathF.Truncate(obj.X);
            y = (int)MathF.Truncate(obj.Y);
        }

        public void Add(IGameObject<TKey> obj)
        {
            GetIndex(obj, out var x, out var y);
            var dic = _Nodes.GetOrAdd(x, c => new Dictionary<int, IGameObject<TKey>>());
            dic[y] = obj;
        }

        public void Remove(IGameObject<TKey> obj)
        {
            GetIndex(obj, out var x, out var y);
            if (_Nodes.TryGetValue(x, out var dic))
                dic.Remove(y);
        }

        public void Rebuild()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="radius">半径，非负数</param>
        /// <returns></returns>
        public IEnumerable<IGameObject<TKey>> GetCollision(float x, float y, float radius)
        {
            for (int xIndex = (int)MathF.Truncate(x - radius); xIndex < (int)Math.Ceiling(x + radius); xIndex++)
            {

            }
            return null;
        }
    }
}
