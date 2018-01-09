using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace CTestAdapter
{
  public class CTestAdapterLogFile : ILogWriter
  {
    private bool _active = false;
    private StreamWriter _writer = null;
    private LogWriterOptions _opts;

    public void Log(LogLevel level, string message)
    {
      if (!this._active)
      {
        return;
      }
      if (null == this._writer)
      {
        return;
      }
      this._writer.WriteLine(
        "[" + DateTime.Now.ToLongTimeString() + "] [" +
        level.ToString() + "] " +
        message);
      this._writer.Flush();
    }

    public void Activate()
    {
      this._active = true;
      this.TryInitializeLogFile(this._opts);
    }

    public void Deactivate()
    {
      this.CloseLogFile();
      this._active = false;
    }

    public void SetOptions(LogWriterOptions options)
    {
      if (this._opts == options)
      {
        return;
      }
      if (!options.EnableLogFile)
      {
        this.CloseLogFile();
      }
      else if (options.LogFileName != this._opts.LogFileName ||
        options.EnableLogFile != this._opts.EnableLogFile)
      {
        this.TryInitializeLogFile(options);
      }
      this._opts = options;
    }

    public LogWriterOptions GetOptions()
    {
      return this._opts;
    }

    private void CloseLogFile()
    {
      if (null == this._writer)
      {
        return;
      }
      this.Log(LogLevel.Info, "--------------------------------------");
      this.Log(LogLevel.Info, "closing logfile");
      this._writer.Close();
      this._writer.Dispose();
      this._writer = null;
    }

    private void TryInitializeLogFile(LogWriterOptions newopts)
    {
      if (null != this._writer)
      {
        return;
      }
      if (!newopts.EnableLogFile)
      {
        return;
      }
      if (newopts.LogFileName.Length == 0)
      {
        return;
      }
      var info = new FileInfo(newopts.LogFileName);
      if (!Directory.Exists(info.DirectoryName))
      {
        return;
      }
      if (newopts.LogFileName == Constants.AdapterLogFileNameInvalid)
      {
        return;
      }
      this._writer = new StreamWriter(
        newopts.LogFileName, 
        newopts.AppendToLogFile,
        Encoding.UTF8)
      {
        AutoFlush = true
      };
      if (null != this._writer)
      {
        this.Log(LogLevel.Info, "opened logfile: " + newopts.LogFileName);
        var name = Assembly.GetExecutingAssembly().GetName();
        this.Log(LogLevel.Info, "assembly: " + name.Name);
        this.Log(LogLevel.Info, "assembly version: " + name.Version.ToString());
        this.Log(LogLevel.Info, "--------------------------------------");
      }
    }
  }
}
