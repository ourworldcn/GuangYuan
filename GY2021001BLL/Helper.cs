using GY2021001DAL;
using Gy2021001Template;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GY2021001BLL
{
    public static class GameHelper
    {
        /// <summary>
        /// 用Base64编码Guid类型。
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static string ToBase64String(this Guid guid)
        {
            return Convert.ToBase64String(guid.ToByteArray());
        }

        /// <summary>
        /// 从Base64编码转换获取Guid值。
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Guid FromBase64String(string str)
        {
            return new Guid(Convert.FromBase64String(str));
        }

        public static GameItemTemplate GetTemplate(this GameChar gameChar)
        {
            return null;
        }

        public static int GetTemplateTypeCode(this GameItemTemplate template)
        {
            if (null == template.GId || !template.GId.HasValue)
                return -1;
            var result = template.GId.Value / 1000;
            return result;
        }

        /// <summary>
        /// 模板是不是一个头类型的模板。
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        public static bool IsHead(this GameItemTemplate template)
        {
            return GetTemplateTypeCode(template) == 3;
        }

        /// <summary>
        /// 模板是不是一个身体类型的模板。
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        public static bool IsBody(this GameItemTemplate template)
        {
            return GetTemplateTypeCode(template) == 4;
        }
    }
}

