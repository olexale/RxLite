using System;
using System.Collections.Generic;
using System.Linq;

namespace RxLite
{
    public static class CompatMixins
    {
        internal static IEnumerable<T> SkipLast<T>(this IEnumerable<T> This, int count)
        {
            return This.Take(This.Count() - count);
        }
    }

    // according to spouliot, this is just a string match, and will cause the
    // linker to be ok with everything.
    internal class PreserveAttribute : Attribute
    {
        public bool AllMembers { get; set; }
    }
}