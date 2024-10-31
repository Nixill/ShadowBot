namespace Nixill.Discord.ShadowBot;

public class UserInputException : Exception
{
  public UserInputException(string message) : base(message) { }
  public UserInputException(string message, Exception innerException) : base(message, innerException) { }
}