using GY2021001DAL;
using Gy2021001Template;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GY2021001BLL
{
    public static class GameHelper
    {
        public static string ToBase64String(this Guid guid)
        {
            return Convert.ToBase64String(guid.ToByteArray());
        }

        public static Guid FromBase64String(string str)
        {
            return new Guid(Convert.FromBase64String(str));
        }

        public static GameItemTemplate GetTemplate(this GameChar gameChar)
        {
            return null;
        }

    }
}

