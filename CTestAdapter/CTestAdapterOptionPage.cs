using System.ComponentModel;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Shell;

namespace CTestAdapter
{
  public class CTestAdapterOptionPage : DialogPage
  {
    private bool _changed = false;
    private readonly CTestAdapterPackage _package;

    private LogLevel _logLevel = LogLevel.Error;
    private bool _enableLogFile = false;
    private bool _appendToLogFile = false;
    private string _logFileName;

    public CTestAdapterOptionPage()
    {
      this._package = CTestAdapterPackage.Instance;
      this.UpdateLogFileName();
    }

    [Category("General")]
    [DisplayName("Logger level")]
    [Description("Only log messages with level equal or higher will be reported.")]
    public LogLevel CurrentLogLevel
    {
      get { return this._logLevel; }
      set
      {
        if (value == this._logLevel)
        {
          return;
        }
        this._logLevel = value;
        this._changed = true;
      }
    }

    [Category("General")]
    [DisplayName("Enable Logging to File")]
    [Description("Enable logging to file in solution directory.")]
    public bool EnableLogFile
    {
      get { return this._enableLogFile; }
      set
      {
        if (value == this._enableLogFile)
        {
          return;
        }
        this._enableLogFile = value;
        this._changed = true;
      }
    }

    [Category("General")]
    [DisplayName("Append to log file")]
    [Description("Do not delete existing Logfile but append to it.")]
    public bool AppendToLogFile
    {
      get { return this._appendToLogFile; }
      set
      {
        if (value == this._appendToLogFile)
        {
          return;
        }
        this._appendToLogFile = value;
        this._changed = true;
      }
    }

    [Category("General")]
    [DisplayName("Logfile Name")]
    [Description("Name of the generated logfile (cannot be set).")]
    [ReadOnly(true)]
    public string LogFileName
    {
      get { return this._logFileName; }
      set
      {
        if (value == this._logFileName)
        {
          return;
        }
        this._logFileName = value;
        this._changed = true;
      }
    }

    protected override void OnApply(PageApplyEventArgs e)
    {
      if (!this._changed || null == this._package)
      {
        return;
      }
      this._package.SetOptions(this);
      this._changed = false;
      base.OnApply(e);
    }

    protected override void OnActivate(CancelEventArgs e)
    {
      base.OnActivate(e);
      this.LoadSettingsFromStorage();
      this.UpdateLogFileName();
    }

    private void UpdateLogFileName()
    {
      if (null == this._package)
      {
        return;
      }
      if (this._package.CMakeCacheDirectory.Any())
      {
        this.LogFileName = Path.Combine(this._package.CMakeCacheDirectory, Constants.AdapterLogFileName);
      }
      else
      {
        this.LogFileName = Constants.AdapterLogFileNameInvalid;
      }
    }
  }
}
