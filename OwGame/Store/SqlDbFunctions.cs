using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Text;

namespace OW.Game.Store
{
    public static class SqlDbFunctions
    {
        [DbFunction("JSON_VALUE")]
        public static string JsonValue(string column, [NotParameterized] string path)
        {
            throw new NotSupportedException();
        }

    }
}
