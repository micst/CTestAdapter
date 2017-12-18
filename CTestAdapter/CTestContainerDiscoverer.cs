using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace CTestAdapter
{
  [Export(typeof(ITestContainerDiscoverer))]
  public class CTestContainerDiscoverer : ITestContainerDiscoverer
  {
    private readonly List<ITestContainer> _cachedContainers;

    [ImportingConstructor]
    public CTestContainerDiscoverer(
      [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
    {
      ValidateArg.NotNull(serviceProvider, "serviceProvider");
      this.ExecutorUri = new Uri(Constants.ExecutorUriString);
      this._cachedContainers = new List<ITestContainer>();
      var pkg = CTestAdapterPackage.Instance;
      if (null != pkg)
      {
        pkg.Discoverer = this;
      }
    }

    #region ITestContainerDiscoverer

    public Uri ExecutorUri { get; private set; }
    public event EventHandler TestContainersUpdated;

    public IEnumerable<ITestContainer> TestContainers
    {
      get { return this._cachedContainers; }
    }

    #endregion

    public void SetTestContainers(List<string> testContainerFiles)
    {
      // @todo maybe check if something changed at all before firing events and so?!
      this._cachedContainers.Clear();
      foreach (var containerFile in testContainerFiles)
      {
        var index = this._cachedContainers
          .FindIndex(x => x.Source.Equals(containerFile, StringComparison.OrdinalIgnoreCase));
        if (index != -1)
        {
          return;
        }
        if (!TestContainerHelper.IsTestContainerFile(containerFile))
        {
          return;
        }
        var container = new CTestContainer(this, containerFile);
        this._cachedContainers.Add(container);
      }
      if (null == this.TestContainersUpdated)
      {
        return;
      }
      this.TestContainersUpdated(this, EventArgs.Empty);
    }
  }
}
