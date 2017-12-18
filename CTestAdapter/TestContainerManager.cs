using System;
using System.Collections.Generic;
using System.Linq;
using CTestAdapter.Events;

namespace CTestAdapter
{
  public class TestContainerManager : ILog
  {
    private readonly CTestAdapterPackage _package;
    private readonly ILog _log;
    private bool _ctestAdapterEnabled = false;
    private readonly TestContainerWatcher _testContainerWatcher;
    private readonly List<string> _testContainers;

    public event EventHandler<TestContainerListArgs> TestContainersChanged;

    public TestContainerManager(CTestAdapterPackage package, ILog log)
    {
      this._package = package;
      this._log = log;
      this._testContainerWatcher = new TestContainerWatcher();
      this._testContainers = new List<string>();
      // connect listeners with local events
      this._testContainerWatcher.TestContainerDeleted += this.OnTestContainerRemoved;
      this._testContainerWatcher.TestContainerChanged += this.OnTestContainerChanged;
    }

    public bool CTestAdapterEnabled
    {
      get { return this._ctestAdapterEnabled; }
      set
      {
        this.Log(LogLevel.Debug, "set enabled to \"" + value + "\"");
        var changed = this._ctestAdapterEnabled != value;
        if (!changed)
        {
          return;
        }
        this._ctestAdapterEnabled = value;
        this._testContainerWatcher.Enabled = value;
        if (value)
        {
          this.FindTestContainers();
        }
      }
    }

    public List<string> TestContainers
    {
      get { return this._testContainers; }
    }

    public void FindTestContainers()
    {
      this.Log(LogLevel.Debug, "FindTestContainers (clear)");
      this._testContainerWatcher.Clear();
      this._testContainers.Clear();
      if (!this.CTestAdapterEnabled)
      {
        return;
      }
      this.Log(LogLevel.Debug, "FindTestContainers (search)");
      // @todo verify something changed actually?!?
      var files = TestContainerHelper.CollectCTestTestfiles(this._package.CMakeCacheDirectory).ToList();
      foreach (var file in files)
      {
        this._testContainerWatcher.AddWatch(file);
        this._testContainers.Add(file);
      }
      if (null != this.TestContainersChanged)
      {
        this.TestContainersChanged(this, new TestContainerListArgs(this._testContainers));
      }
    }

    private void OnTestContainerRemoved(object sender, Events.TestContainerEventArgs e)
    {
      if (e == null)
      {
        return;
      }
      if (!this._testContainers.Contains(e.File))
      {
        return;
      }
      if (!TestContainerHelper.IsTestContainerFile(e.File))
      {
        return;
      }
      this._testContainerWatcher.RemoveWatch(e.File);
      this._testContainers.Remove(e.File);
      if (null != this.TestContainersChanged)
      {
        this.TestContainersChanged(this, new TestContainerListArgs(this._testContainers));
      }
    }

    /**
     * @brief handles changes in test container files
     */
    private void OnTestContainerChanged(object sender, Events.TestContainerEventArgs e)
    {
      if (e == null)
      {
        return;
      }
      if (null != this.TestContainersChanged)
      {
        this.TestContainersChanged(this, new TestContainerListArgs(this._testContainers));
      }
    }

    public void Log(LogLevel level, string message)
    {
      if (null == this._log)
      {
        return;
      }
      this._log.Log(level, Constants.LogPrefixCMgr + message);
    }
  }
}
