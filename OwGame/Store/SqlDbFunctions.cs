using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OW.Game.Store
{
    /// <summary>
    /// 注册一些数据库的内置函数。
    /// </summary>
    public static class SqlDbFunctions
    {
        public static void Register(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDbFunction(() => JsonValue(default, default)).HasTranslation(
                args =>
                {
                    var result = SqlFunctionExpression.Create("JSON_VALUE", args, args.Last().Type, args.Last().TypeMapping);
                    return result;
                });
            modelBuilder.HasDbFunction(() => JsonQuery(default, default)).HasTranslation(
                args =>
                {
                    var result = SqlFunctionExpression.Create("JSON_QUERY", args, args.Last().Type, args.Last().TypeMapping);
                    return result;
                });
        }

        [DbFunction("JSON_VALUE")]
        public static string JsonValue(string column, [NotParameterized] string path)
        {
            throw new NotSupportedException();
        }

        [DbFunction("JSON_QUERY")]
        public static string JsonQuery(string column, [NotParameterized] string path)
        {
            throw new NotSupportedException();
        }

    }
}
