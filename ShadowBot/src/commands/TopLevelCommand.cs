using System.Reflection;

namespace Nixill.Discord.ShadowBot;

[AttributeUsage(AttributeTargets.Class)]
public class TopLevelCommandAttribute : Attribute
{
  public static IEnumerable<Type> GetTypesWith(Assembly asm)
    => asm.GetTypes()
      .Where(t => t.GetCustomAttribute(typeof(TopLevelCommandAttribute)) != null);
}