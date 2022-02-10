using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace OW.EntityFramework
{
    public class EfHelper
    {
        static public IQueryable<TValue> GetNext<TValue, TKey>(IQueryable<TValue> from, Expression<Func<TValue, bool>> where, Expression<Func<TValue, TKey>> keySelector)
        {

            return from.Where(where).OrderBy(keySelector).Take(1);
        }

        //static public IQueryable<TValue> GetAdj<TValue, TKey>(IQueryable<TValue> from, Expression<Func<TValue, TKey>> keySelector)
        //{

        //}

    }
}
