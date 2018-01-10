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

    public void DiscoverTests(IEnumerable<string> sources,
        IDiscoveryContext discoveryContext,
        IMessageLogger log,
        ITestCaseDiscoverySink discoverySink)
    {
      log.SendMessage(TestMessageLevel.Informational, "CTestDiscoverer discovering ...");
      var v = sources as IList<string> ?? sources.ToList();
      // verify we have a CMakeCache.txt directory
      var cacheDir = TestContainerHelper.FindCMakeCacheDirectory(v.First());
      if (!cacheDir.Any())
      {
        log.SendMessage(TestMessageLevel.Informational, "cmake cache not found");
        return;
      }
      // read parameters
      var cfg = CTestAdapterConfig.ReadFromDisk(Path.Combine(cacheDir, Constants.CTestAdapterConfigFileName)) ??
                CTestAdapterConfig.ReadFromCache(cacheDir);
      if (null == cfg)
      {
        return;
      }
      log.SendMessage(TestMessageLevel.Informational, "using configuration: " + cfg.ActiveConfiguration);
      var collection = TestContainerHelper.FindAllTestsWithCtest(cfg);
      foreach (var source in v)
      {
        var cases = TestContainerHelper.ParseTestContainerFile(source, log, collection, cfg.ActiveConfiguration);
        foreach (var c in cases)
        {
          discoverySink.SendTestCase(c.Value);
        }
      }
      log.SendMessage(TestMessageLevel.Informational, "CTestDiscoverer discovering done");
    }
  }
}
