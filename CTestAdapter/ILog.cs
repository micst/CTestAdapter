namespace CTestAdapter
{
  public enum LogLevel
  {
    Debug,
    Warning,
    Error,
    Info,
  }

  public interface ILog
  {
    void Log(LogLevel level, string message);
  }
}
