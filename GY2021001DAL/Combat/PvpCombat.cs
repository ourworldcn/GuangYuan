using Microsoft.EntityFrameworkCore;
using OW.Game.Store;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace GuangYuan.GY001.UserDb.Combat
{
    public class PvpCombat : GameObjectBase
    {
        public const string Separator = "`";

        public PvpCombat()
        {
        }

        public PvpCombat(Guid id) : base(id)
        {
        }

        /// <summary>
        /// 攻击方Id集合字符串。
        /// </summary>
        [MaxLength(320)]    //保留能索引的能力
        public string AttackerIdString { get; set; }

        List<Guid> _AttackerIds;
        /// <summary>
        /// 攻击方Id集合。
        /// </summary>
        [NotMapped]
        public List<Guid> AttackerIds
        {
            get
            {
                if (_AttackerIds is null)
                {
                    _AttackerIds = new List<Guid>(AttackerIdString.Split(Separator, StringSplitOptions.RemoveEmptyEntries).Select(c => Guid.Parse(c)));
                }
                return _AttackerIds;
            }
        }

        /// <summary>
        /// 防御方Id集合字符串。
        /// </summary>
        [MaxLength(320)]    //保留能索引的能力
        public string DefenserIdString { get; set; }

        List<Guid> _DefenserIds;
        /// <summary>
        /// 防御方Id集合。
        /// </summary>
        [NotMapped]
        public List<Guid> DefenserIds
        {
            get
            {
                if (_DefenserIds is null)
                {
                    _DefenserIds = new List<Guid>(AttackerIdString.Split(Separator, StringSplitOptions.RemoveEmptyEntries).Select(c => Guid.Parse(c)));
                }
                return _DefenserIds;
            }
        }

        List<GameBooty> _BootyOfAttacker;
        public List<GameBooty> BootyOfAttacker(DbContext context)
        {
            if (_BootyOfAttacker is null)
            {
                _BootyOfAttacker = context.Set<GameBooty>().Where(c => c.ParentId == Id && AttackerIds.Contains(c.CharId)).ToList();
            }
            return _BootyOfAttacker;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="db"></param>
        public override void PrepareSaving(DbContext db)
        {
            if (null != _AttackerIds)
            {
                AttackerIdString = string.Join(Separator, _AttackerIds.Select(c => c.ToString()));
            }
            if (null != _DefenserIds)
            {
                DefenserIdString = string.Join(Separator, _DefenserIds.Select(c => c.ToString()));
            }
            base.PrepareSaving(db);
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                }
                _AttackerIds = null;
                _DefenserIds = null;
                AttackerIdString = null;
                DefenserIdString = null;
                base.Dispose(disposing);
            }
        }
    }
}
