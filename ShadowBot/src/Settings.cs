using System.Text.Json.Nodes;
using Nixill.Utils.Extensions;
using NodaTime;
using NodaTime.TimeZones;

namespace Nixill.Discord.ShadowBot;

public static class Settings
{
  static JsonObject GetSettingsObject()
  {
    var node = JsonNode.Parse(File.ReadAllText("cfg/settings.json")) as JsonObject;
    if (node == null)
    {
      File.WriteAllText("cfg/settings.json", "{}");
      node = new JsonObject();
    }
    return node;
  }

  static T? ParseEnum<T>(string value) where T : struct
  {
    if (Enum.TryParse<T>(value, out T val)) return val;
    else return null;
  }

  static readonly JsonObject SettingsObject = GetSettingsObject();

  public static ulong DebugGuildId => SettingsObject.ContainsKey("debugGuild") ? (ulong)SettingsObject["debugGuild"] : 0;

  public static DateTimeZone TimeZone
  {
    get => TzdbDateTimeZoneSource.Default.ForId((string)SettingsObject.ReadPath("time", "zone") ?? "America/Detroit");
    set => SettingsObject.WritePath(value.Id, "time", "zone");
  }

  public static void SaveObject() => File.WriteAllText("cfg/settings.json", SettingsObject.ToString());
}