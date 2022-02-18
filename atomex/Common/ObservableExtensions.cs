using System;
using System.Linq.Expressions;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace atomex.Common
{
    public static class ObservableExtensions
    {
        public static IObservable<(T1, T2)> WhereAllNotNull<T1, T2>(this IObservable<(T1, T2)> observable) =>
           observable.Where(t => t.Item1 != null && t.Item2 != null);

        public static IDisposable SubscribeInMainThread<T>(
            this IObservable<T> observable,
            Action<T> onNext)
        {
            return observable
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(onNext);
        }

        public static ObservableAsPropertyHelper<TRet> ToPropertyExInMainThread<TObj, TRet>(
            this IObservable<TRet> item,
            TObj source,
            Expression<Func<TObj, TRet>> property,
            bool deferSubscription = false) where TObj : ReactiveObject
        {
            return item.ToPropertyEx(source, property, deferSubscription, RxApp.MainThreadScheduler);
        }

        public static IDisposable InvokeCommandInMainThread<T, TResult>(
            this IObservable<T> item,
            ReactiveCommandBase<T, TResult>? command)
        {
            return item
                .ObserveOn(RxApp.MainThreadScheduler)
                .InvokeCommand(command);
        }
    }
}
