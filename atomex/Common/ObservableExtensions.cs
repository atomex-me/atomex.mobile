using System;
using System.Reactive.Linq;

namespace atomex.Common
{
    public static class ObservableExtensions
    {
        public static IObservable<(T1, T2)> WhereAllNotNull<T1, T2>(this IObservable<(T1, T2)> observable) =>
           observable.Where(t => t.Item1 != null && t.Item2 != null);
    }
}
