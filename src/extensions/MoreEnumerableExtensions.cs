namespace Nixill.Utils.Extra;

public static class MoreEnumerableExtensions
{
  public static IEnumerable<(T, T)> Pairs<T>(this IEnumerable<T> sequence)
  {
    bool first = true;
    T last = default(T);

    foreach (T item in sequence)
    {
      if (!first) yield return (last, item);
      last = item;
      first = false;
    }
  }
}
