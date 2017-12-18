namespace CTestAdapter
{ 
  public interface ILogWriter : ILog
  {
    void Activate();

    void Deactivate();

    void SetOptions(LogWriterOptions options);

    LogWriterOptions GetOptions();
  }
}
