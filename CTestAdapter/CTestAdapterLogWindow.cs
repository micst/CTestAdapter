using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CTestAdapter
{
  public class CTestAdapterLogWindow : ILogWriter
  {
    private bool _active = false;
    private LogWriterOptions _opts;
    private IVsOutputWindowPane _outWindowPane;
    private bool _paneActive = false;

    public CTestAdapterLogWindow()
    {
      var outWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
      if (null == outWindow)
      {
        return;
      }
      if (outWindow.CreatePane(Constants.OutputWindow, Constants.LogWindowTitle, 1, 1) != VSConstants.S_OK)
      {
        return;
      }
      if (outWindow.GetPane(Constants.OutputWindow, out this._outWindowPane) != VSConstants.S_OK)
      {
        return;
      }
    }

    public void Log(LogLevel level, string message)
    {
      if (!this._active)
      {
        return;
      }
      if (level < this._opts.CurrentLogLevel)
      {
        return;
      }
      if (null == message)
      {
        return;
      }
      if (null == this._outWindowPane)
      {
        return;
      }
      this._outWindowPane.OutputString(
        "[" + DateTime.Now.ToLongTimeString() + "] [" + 
        level.ToString() + "] " +
        message +
         "\n");
      // @todo maybe add task item?!?
    }

    public void Activate()
    {
      this._active = true;
      if (null != this._outWindowPane && !this._paneActive)
      {
        this._outWindowPane.Activate();
        this._paneActive = true;
      }
    }

    public void Deactivate()
    {
      if (null != this._outWindowPane && this._paneActive)
      {
        this._outWindowPane.Hide();
        this._paneActive = false;
      }
      this._active = false;
    }

    public void SetOptions(LogWriterOptions options)
    {
      this._opts = options;
    }

    public LogWriterOptions GetOptions()
    {
      return this._opts;
    }
  }
}
