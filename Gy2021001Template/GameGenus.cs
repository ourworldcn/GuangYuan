using OW.Game;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace GuangYuan.GY001.TemplateDb
{
    /// <summary>
    /// "属"对象。
    /// </summary>
    public class GameGenus
    {
        public GameGenus()
        {

        }

        /// <summary>
        /// 子属Name的字符串，以逗号分割。
        /// </summary>
        public string ChildrenIdString { get; set; }

        List<string> _ChildrenIds;
        [NotMapped]
        public List<string> ChildrenIds
        {
            get
            {
                lock (this)
                    if (null == _ChildrenIds)
                    {
                        if (!string.IsNullOrEmpty(ChildrenIdString))
                        {
                            _ChildrenIds = ChildrenIdString.Split(OwHelper.CommaArrayWithCN, StringSplitOptions.RemoveEmptyEntries).ToList();
                        }
                        else
                            _ChildrenIds = new List<string>();
                    }
                return _ChildrenIds;
            }
        }

        /// <summary>
        /// 父属Name的字符串，以逗号分割。
        /// </summary>
        public string ParentsIdString { get; set; }

        List<string> _ParentIds;

        [NotMapped]
        public List<string> ParentIds
        {
            get
            {
                lock (this)
                    if (null == _ParentIds)
                    {
                        if (!string.IsNullOrEmpty(ParentsIdString))
                        {
                            _ParentIds = ParentsIdString.Split(OwHelper.CommaArrayWithCN, StringSplitOptions.RemoveEmptyEntries).ToList();
                        }
                        else
                            _ParentIds = new List<string>();
                    }
                return _ParentIds;
            }
        }

        string _Name;
        /// <summary>
        /// 名字。这个是键值，必须唯一不能有中英文逗号。最大64字符(中文算1个字符)
        /// </summary>
        [Key, StringLength(64)]
        public string Name
        {
            get => _Name;
            set
            {
                if (value.Any(c => OwHelper.CommaArrayWithCN.Contains(c)))
                    throw new ArgumentException("不能有中英文逗号。", nameof(value));
                _Name = value;
            }
        }

        /// <summary>
        /// 助记名，服务器不使用。
        /// </summary>
        public string ShortName { get; set; }

        /// <summary>
        /// 显示名。
        /// </summary>
        public string DisplayName { get; set; }
    }
}
