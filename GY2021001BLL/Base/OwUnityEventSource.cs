using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GuangYuan.GY001.BLL.Base
{
    public class UdpDataReceivedEventArgs : EventArgs
    {

        public UdpDataReceivedEventArgs(byte[] data)
        {
            _Data = data;
        }

        byte[] _Data;
        /// <summary>
        /// 获取数据。
        /// </summary>
        public byte[] Data { get => _Data; }

    }

    public class OwUnityEventSource : IDisposable
    {
        public OwUnityEventSource()
        {
            _SynchronizationContext = SynchronizationContext.Current;
            Debug.Assert(_SynchronizationContext != null, "CLR无法认知当前运行上下文。");
        }

        /// <summary>
        /// 设置udp侦听端口。
        /// </summary>
        /// <param name="remoteHostname">远程主机的Ip。</param>
        /// <param name="remotePort">远程端口号。</param>
        public void SetUdp(string remoteHostname, int remotePort)
        {
            _Udp?.Dispose();
            try
            {
                _Udp = new UdpClient(20079);
                _Udp.Connect(remoteHostname, remotePort);
                Task.Factory.StartNew(ReceiveFunc, TaskCreationOptions.LongRunning);
            }
            catch (Exception)
            {
                Debug.WriteLine("无法侦听本机端口20079。");
            }
        }

        private void ReceiveFunc()
        {
            try
            {
                while (true)
                {
                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                    var ary = _Udp.Receive(ref endPoint);
                    var eventArgus = new UdpDataReceivedEventArgs(ary);
                    _SynchronizationContext.Post(c => { OnUdpDataReceived((UdpDataReceivedEventArgs)c); }, eventArgus);
                }
            }
            catch (Exception)
            {
                Debug.WriteLine("网络出现异常。");
            }
        }

        SynchronizationContext _SynchronizationContext;
        UdpClient _Udp;

        public event EventHandler<UdpDataReceivedEventArgs> UdpDataReceived;

        protected virtual void OnUdpDataReceived(UdpDataReceivedEventArgs e) => UdpDataReceived?.Invoke(this, e);

        #region IDisposable接口及相关

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                    _Udp?.Dispose();
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _Udp = null;
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~OwUnityEventSource()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion  IDisposable接口及相关
    }
}
