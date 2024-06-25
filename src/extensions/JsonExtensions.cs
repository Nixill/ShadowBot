using System.Text.Json.Nodes;

namespace Nixill.Utils.Extra;

public static class JsonExtensions
{
  public static JsonNode ReadPath(this JsonNode root, params object[] pathElements)
  {
    JsonNode node = root;

    foreach (object o in pathElements)
    {
      if (o is string s)
      {
        JsonNode nextNode = node[s];
        if (nextNode == null) return null;
        else node = nextNode;
      }

      else if (o is int i)
      {
        JsonNode nextNode = node[i];
        if (nextNode == null) return null;
        else node = nextNode;
      }

      else throw new JsonPathElementException($"The path element {o} is not a string or int.");
    }

    return node;
  }

  public static void WritePath(this JsonNode root, JsonNode value, params object[] pathElements)
  {
    JsonNode node = root;

    foreach ((object o, object next) in pathElements.Pairs())
    {
      if (o is string s)
      {
        JsonNode nextNode = node[s];

        if (nextNode == null)
        {
          if (next is string) nextNode = new JsonObject();
          else if (next is int) nextNode = new JsonArray();
          else throw new JsonPathElementException($"The path element {next} is not a string or int.");

          node[s] = nextNode;
        }

        node = nextNode;
      }

      else if (o is int i)
      {
        JsonNode nextNode = node[i];

        if (nextNode == null)
        {
          if (next is string) nextNode = new JsonObject();
          else if (next is int) nextNode = new JsonArray();
          else throw new JsonPathElementException($"The path element {next} is not a string or int.");

          node[i] = nextNode;
        }

        node = nextNode;
      }
    }

    {
      object last = pathElements.Last();
      if (last is string s) node[s] = value;
      else if (last is int i) node[i] = value;
      else throw new JsonPathElementException($"The path element {last} is not a string or int.");
    }
  }
}

public class JsonPathElementException : ArgumentException
{
  public JsonPathElementException(string message) : base(message) { }
  public JsonPathElementException() : base() { }
}