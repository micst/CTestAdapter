using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Timers;
using CTestAdapter.Events;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using EnvDTE;

namespace CTestAdapter
{
  /**
   * @brief main Package class of the CTestAdapter
   * 
   * The attributes tell the pkgdef creation utility what data to put into .pkgdef file.
   */
  [PackageRegistration(UseManagedResourcesOnly = true)]
  // Info on this package for Help/About
  [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] 
  [Guid(Constants.PackageString)]
  [SuppressMessage("StyleCop.CSharp.DocumentationRules",
    "SA1650:ElementDocumentationMustBeSpelledCorrectly",
    Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
  [ProvideOptionPage(typeof(CTestAdapterOptionPage),
    Constants.OptionsPageCategory,
    Constants.OptionsPageGridPage,
    0, 0, true)]
  [ProvideMenuResource("Menus.ctmenu",1)]
  [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string)]
  [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string)]
  public sealed class CTestAdapterPackage : Package, ILog
  {
    private const string GitHubUrl = "https://github.com/micst/CTestAdapter";
    private const int ConfigurationTimerIntervalMs = 1000;

    private bool _ctestAdapterEnabled = false;
    private string _cMakeCacheDirectory = "";
    // private self-managed members
    private readonly CTestAdapterConfig _config;
    private readonly DTE _dte;
    private readonly SolutionEventListener _sol;
    private readonly CMakeCache _cmakeCache;
    private readonly CMakeCacheWatcher _cMakeCacheWatcher;
    private readonly TestContainerManager _containerManager;
    private readonly System.Timers.Timer _activeConfigurationTimer;
    private readonly ILogWriter _log;
    //
    private CTestContainerDiscoverer _discoverer = null;

    /**
     * @brief initialization WITHOUT Visual Studio services available.
     */
    public CTestAdapterPackage()
    {
      if (null != Instance)
      {
        /// @todo give some error here!
      }
      Instance = this;
      // main DTE object
      this._dte = (DTE)Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider.GetService(typeof(DTE));
      // basic solution events
      this._log = new CTestAdapterLog();
      this._sol = new SolutionEventListener(Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider);
      this._sol.SolutionLoaded += this.SolutionLoaded;
      this._sol.SolutionUnloaded += this.SolutionUnloaded;
      // cmake cache & cmake test file
      this._config = new CTestAdapterConfig();
      this._cmakeCache = new CMakeCache(this._log);
      this._cMakeCacheWatcher = new CMakeCacheWatcher(this._log);
      this._cMakeCacheWatcher.CacheFileChanged += this.OnCMakeCacheChanged;
      // test container discovery and management
      this._containerManager = new TestContainerManager(this, this._log);
      this._activeConfigurationTimer = new System.Timers.Timer(CTestAdapterPackage.ConfigurationTimerIntervalMs);
      this._activeConfigurationTimer.Elapsed += this.UpdateActiveConfiguration;
      this._containerManager.TestContainersChanged += this.OnTestContainersChanged;
    }

    /**
     * @brief initialization with Visual Studio services available.
     */
    protected override void Initialize()
    {
      this.SetOptions(null);
      CTestCommand.Initialize(this);
      this._log.Activate();
      base.Initialize();
    }

    public static CTestAdapterPackage Instance { get; private set; }

    public bool CTestAdapterEnabled
    {
      get { return this._ctestAdapterEnabled; }
      private set
      {
        var changed = this._ctestAdapterEnabled != value;
        this._ctestAdapterEnabled = value;
        if (!changed)
        {
          return;
        }
        this.Log(LogLevel.Info, value
          ? "++++ Enabled CTest Adapter ++++"
          : "++++ Disabled CTest Adapter ++++");
        if (value)
        {
          var name = Assembly.GetExecutingAssembly().GetName();
          this.Log(LogLevel.Debug, "assembly: " + name.Name);
          this.Log(LogLevel.Info, "version: " + name.Version.ToString());
          this.Log(LogLevel.Info, "If you find any issues or want to contribute to the project, checkout");
          this.Log(LogLevel.Info, "CTestAdapter on github: " + GitHubUrl);
          this.Log(LogLevel.Info, "-----------------------------------");
        }
        this._containerManager.CTestAdapterEnabled = value;
      }
    }

    public string CMakeCacheDirectory
    {
      get { return this._cMakeCacheDirectory; }
      private set
      {
        this._cMakeCacheDirectory = value;
        this._config.CacheDir = value;

        CTestAdapterConfig.WriteToDisk(this._config);
      }
    }

    public CTestContainerDiscoverer Discoverer
    {
      get { return this._discoverer; }
      set
      {
        var changed = this._discoverer != value;
        if (!changed)
        {
          return;
        }
        this.Log(LogLevel.Debug, "setting container discoverer");
        this._discoverer = value;
        this._discoverer.SetTestContainers(this._containerManager.TestContainers);
      }
    }

    public ILog Logger
    {
      get { return this._log; }
    }

    public void SetOptions(CTestAdapterOptionPage options)
    {
      var logOpts = this.GetLogWriterOptions(options);
      this._log.SetOptions(logOpts);
    }

    private void SolutionLoaded()
    {
      if (this._dte != null)
      {
        this.CMakeCacheDirectory = Path.GetDirectoryName(this._dte.Solution.FileName);
      }
      if (null == this.CMakeCacheDirectory)
      {
        this.Log(LogLevel.Error, "could not set CMakeCache directory");
        return;
      }
      // update logfile name
      this._log.Activate();
      var cfg = this._log.GetOptions();
      cfg.LogFileName = Path.Combine(this.CMakeCacheDirectory, Constants.AdapterLogFileName);
      this._log.SetOptions(cfg);
      // setting cache dir implicitly starts watching
      // @todo maybe change this for better consistency?
      this._cMakeCacheWatcher.CMakeCacheDirectory = this.CMakeCacheDirectory;
      this._activeConfigurationTimer.Start();
    }

    private void SolutionUnloaded()
    {
      this._cMakeCacheWatcher.StopWatching();
      this.CTestAdapterEnabled = false;
      this.CMakeCacheDirectory = "";
      this._activeConfigurationTimer.Stop();
      this._log.Deactivate();
    }

    private void OnCMakeCacheChanged()
    {
      var cacheFileName = 
        Path.Combine(this._cMakeCacheWatcher.CMakeCacheDirectory, this._cMakeCacheWatcher.CMakeCacheFile);
      var testFileName =
        Path.Combine(this._cMakeCacheWatcher.CMakeCacheDirectory, Constants.CTestTestFileName);
      this._cmakeCache.LoadCMakeCache(cacheFileName);
      var enabled = true;
      if (!this._cmakeCache.IsLoaded)
      {
        this.Log(LogLevel.Warning, "OnCMakeCacheChanged (cache not loaded)");
        enabled = false;
      }
      if (!File.Exists(testFileName))
      {
        this.Log(LogLevel.Warning, "OnCMakeCacheChanged file not found: " + testFileName);
        enabled = false;
      }
      if (this._cmakeCache[Constants.CMakeCacheKey_CTestCommand] == string.Empty)
      {
        this.Log(LogLevel.Warning, "OnCMakeCacheChanged ctest not found in cache");
        enabled = false;
      }
      var ctest = this._cmakeCache[Constants.CMakeCacheKey_CTestCommand];
      if (!File.Exists(ctest))
      {
        this.Log(LogLevel.Warning, "OnCMakeCacheChanged ctest executable not found: " + ctest);
        enabled = false;
      }
      this._config.CTestExecutable = ctest;
      this._config.CMakeConfigurationTypes = this._cmakeCache[Constants.CMakeCacheKey_CofigurationTypes];
      CTestAdapterConfig.WriteToDisk(this._config);
      if (this._ctestAdapterEnabled != enabled)
      {
        this.CTestAdapterEnabled = enabled;
      }
      this._containerManager.FindTestContainers();
    }

    private void OnTestContainersChanged(object sender, TestContainerListArgs e)
    {
      this.Log(LogLevel.Debug, "OnTestContainersChanged count:" + e.TestContainers.Count);
      if (null != this._discoverer)
      {
        this._discoverer.SetTestContainers(e.TestContainers);
      }
    }

    private void UpdateActiveConfiguration(object sender, ElapsedEventArgs elapsedEventArgs)
    {
      if (!this._ctestAdapterEnabled)
      {
        return;
      }
      // set current active configuration
      var p = this._dte.Solution.SolutionBuild;
      if (null == p)
      {
        return;
      }
      var sc = p.ActiveConfiguration;
      if (null == sc)
      {
        return;
      }
      this._config.ActiveConfiguration = sc.Name;
      CTestAdapterConfig.WriteToDisk(this._config);
    }

    private LogWriterOptions GetLogWriterOptions(CTestAdapterOptionPage options)
    {
      if (null == options)
      {
        options = (CTestAdapterOptionPage)this.GetDialogPage(typeof(CTestAdapterOptionPage));
        options.LoadSettingsFromStorage();
      }
      var logOpts = new LogWriterOptions()
      {
        CurrentLogLevel = options.CurrentLogLevel,
        EnableLogFile = options.EnableLogFile,
        LogFileName = options.LogFileName,
        AppendToLogFile = options.AppendToLogFile
      };
      return logOpts;
    }

    public void Log(LogLevel level, string message)
    {
      if (null == this._log)
      {
        return;
      }
      this._log.Log(level, Constants.LogPrefixPckg + message);
    }
  }
}
