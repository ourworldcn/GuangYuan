using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace OW.Game.Store
{
    public class SqlHelper
    {
        public static IQueryable<T> Abs<T>(IQueryable<T> baseColl, Expression<Func<T, decimal>> d, decimal value, decimal diff) where T : class
        {
            var result = baseColl.Where(c => 1 > diff);
            return result;
        }
    }
}