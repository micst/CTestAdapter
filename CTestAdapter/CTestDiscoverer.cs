using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace CTestAdapter
{
  [FileExtension(".cmake")]
  [DefaultExecutorUri(Constants.ExecutorUriString)]
  [Export(typeof(ITestDiscoverer))]
  public class CTestDiscoverer : ITestDiscoverer
  {
    // @todo CHECK HOW OFTEN TEST DISCOVERERS ARE INSTANTIATED!!!
    //       IS IT WORTH TO STORE INFORMATION?!?!?!?

    private const string LogPrefix = "CTestDiscoverer: ";

    private IMessageLogger _log = null;

    private void Log(TestMessageLevel lvl, string message)
    {
      if (this._log == null)
      {
        return;
      }
      this._log.SendMessage(lvl, CTestDiscoverer.LogPrefix + message);
    }

    public void DiscoverTests(IEnumerable<string> sources,
        IDiscoveryContext discoveryContext,
        IMessageLogger logger,
        ITestCaseDiscoverySink discoverySink)
    {
      this._log = logger;
      this.Log(TestMessageLevel.Informational, "discovering ...");
      var v = sources as IList<string> ?? sources.ToList();
      // verify we have a CMakeCache.txt directory
      var cacheDir = TestContainerHelper.FindCMakeCacheDirectory(v.First());
      if (!cacheDir.Any())
      {
        this.Log(TestMessageLevel.Informational, "cmake cache not found");
        return;
      }
      // read parameters
      var cfg = CTestAdapterConfig.ReadFromDisk(Path.Combine(cacheDir, Constants.CTestAdapterConfigFileName)) ??
                CTestAdapterConfig.ReadFromCache(cacheDir);
      if (null == cfg)
      {
        this.Log(TestMessageLevel.Error, "could not create CTestAdapterConfig");
        return;
      }
      // make sure a configuration is set
      if (!cfg.ActiveConfiguration.Any())
      {
        if (cfg.TrySetActiveConfigFromConfigTypes())
        {
          this.Log(TestMessageLevel.Warning,
            "Configuration fallback to: " + cfg.ActiveConfiguration);
        }
        else
        {
          this.Log(TestMessageLevel.Error, "could not set Configuration");
          return;
        }
      }
      this.Log(TestMessageLevel.Informational, "using configuration: " + cfg.ActiveConfiguration);
      // make sure we have a ctest executable
      if (!File.Exists(cfg.CTestExecutable))
      {
        cfg.CTestExecutable = TestContainerHelper.FindCTestExe(cfg.CacheDir);
      }
      if (!File.Exists(cfg.CTestExecutable))
      {
        this.Log(TestMessageLevel.Error,
          "ctest not found, tried: \"" + cfg.CTestExecutable + "\"");
        return;
      }
      this.Log(TestMessageLevel.Informational, "using ctest binary: " + cfg.CTestExecutable);
      // collect all existing tests by executing ctest
      var collection = TestContainerHelper.FindAllTestsWithCtest(cfg);
      foreach (var source in v)
      {
        var cases = TestContainerHelper.ParseTestContainerFile(source, this._log, collection, cfg.ActiveConfiguration);
        foreach (var c in cases)
        {
          discoverySink.SendTestCase(c.Value);
        }
      }
      this.Log(TestMessageLevel.Informational, "discovering done");
    }
  }
}
