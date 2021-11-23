/*
 * 商城相关的辅助代码文件
 */

using OW.Game;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GuangYuan.GY001.BLL
{

    public class StringsFromDictionary : IDisposable
    {
        private readonly string _Prefix;
        private readonly char _Separator;
        private Dictionary<string, object> _Dictionary;

        public const char Separator = '`';

        public StringsFromDictionary(Dictionary<string, object> dictionary, string prefix, char separator = Separator)
        {
            _Prefix = prefix;
            _Separator = separator;
            _Dictionary = dictionary;
        }

        public Guid Key { get; set; }

        public ObservableCollection<string> _Datas;
        private bool disposedValue;

        public ObservableCollection<string> Datas
        {
            get
            {
                if (_Datas is null)
                {
                    var str = _Dictionary.GetStringOrDefault($"{_Prefix}{Key}");
                    var ary = string.IsNullOrWhiteSpace(str) ? Array.Empty<string>() : str.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
                    _Datas = new ObservableCollection<string>(ary);
                }
                return _Datas;
            }
        }

        public void Save()
        {
            if (_Datas is null) //若没有改变内容
                return;
            _Dictionary[$"{_Prefix}{Key}"] = string.Join(Separator, Datas);

        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                _Datas = null;
                _Dictionary = null;
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~StringsFromDictionary()
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
    }

}