using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace FoliCon.Modules
{
    public static class Extensions
    {
        public static string WithoutExt(this string str)
        {
            return Path.GetFileNameWithoutExtension(str);
        }
        /// <summary>
        /// https://stackoverflow.com/a/15275682/8076598
        /// </summary>
        public static IEnumerable<T> OrderBySequence<T, TId>(
                this IEnumerable<T> source,
                IEnumerable<TId> order,
                Func<T, TId> idSelector)
        {
            var lookup = source.ToLookup(idSelector, t => t);
            foreach (var id in order)
            {
                foreach (var t in lookup[id])
                {
                    yield return t;
                }
            }
        }
        /// <summary>
        /// https://stackoverflow.com/a/13257600/8076598
        /// </summary>
        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> col)
        {
            return new ObservableCollection<T>(col);
        }
    }
}