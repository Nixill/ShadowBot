namespace Nixill.Utils.Temp;

public static class MoreEnumerableExtensions
{
  public static IEnumerable<TOut> SelectUnerrored<TIn, TOut>(this IEnumerable<TIn> items, Func<TIn, TOut> selector)
  {
    foreach (TIn item in items)
    {
      try
      {
        yield return selector(item);
      }
      finally { }
    }
  }
}