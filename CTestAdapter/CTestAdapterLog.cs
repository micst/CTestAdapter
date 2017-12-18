using System.Collections.Generic;

namespace CTestAdapter
{
  public class CTestAdapterLog : ILogWriter
  {
    private bool _active = false;
    private LogWriterOptions _options;
    private readonly List<ILogWriter> _writers;

    public CTestAdapterLog()
    {
      this._writers = new List<ILogWriter>
      {
        new CTestAdapterLogWindow(),
        new CTestAdapterLogFile()
      };
    }

    public void Log(LogLevel level, string message)
    {
      if (!this._active)
      {
        return;
      }
      if (level < this._options.CurrentLogLevel)
      {
        return;
      }
      foreach (var w in this._writers)
      {
        w.Log(level, message);
      }
    }

    public void Activate()
    {
      this._active = true;
      foreach (var w in this._writers)
      {
        w.Activate();
      }
    }

    public void Deactivate()
    {
      this._active = false;
      foreach (var w in this._writers)
      {
        w.Deactivate();
      }
    }

    public void SetOptions(LogWriterOptions options)
    {
      if (this._options == options)
      {
        return;
      }
      this._options = options;
      foreach (var w in this._writers)
      {
        w.SetOptions(options);
      }
    }

    public LogWriterOptions GetOptions()
    {
      return this._options;
    }
  }
}
