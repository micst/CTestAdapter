using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace CTestAdapter.Events
{
  internal class TestContainerWatcher : IDisposable
  {
    private class FileWatcherInfo
    {
      public FileWatcherInfo(FileSystemWatcher watcher)
      {
        Watcher = watcher;
        LastEventTime = DateTime.MinValue;
      }

      public FileSystemWatcher Watcher { get; set; }
      public DateTime LastEventTime { get; set; }
      public byte[] Hash { get; set; }
    }

    private bool _enabled = false;
    private IDictionary<string, FileWatcherInfo> _fileWatchers;

    public event EventHandler<TestContainerEventArgs> TestContainerChanged;
    public event EventHandler<TestContainerEventArgs> TestContainerDeleted;

    public TestContainerWatcher()
    {
      this._fileWatchers = new Dictionary<string, FileWatcherInfo>(StringComparer.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
      this.Clear();
      this._fileWatchers = null;
    }

    public bool Enabled
    {
      get { return this._enabled; }
      set
      {
        this._enabled = value;
        if (!this._enabled)
        {
          this.Clear();
        }
      }
    }

    public void AddWatch(string path)
    {
      ValidateArg.NotNullOrEmpty(path, "path");
      if (string.IsNullOrEmpty(path))
      {
        return;
      }
      var directoryName = Path.GetDirectoryName(path);
      var fileName = Path.GetFileName(path);
      FileWatcherInfo watcherInfo;
      if (this._fileWatchers.TryGetValue(path, out watcherInfo))
      {
        return;
      }
      Debug.Assert(directoryName != null, "directoryName != null");
      watcherInfo =
        new FileWatcherInfo(new FileSystemWatcher(directoryName, fileName))
        {
          Hash = ComputeHash(
            Path.Combine(directoryName, fileName))
        };
      this._fileWatchers.Add(path, watcherInfo);
      watcherInfo.Watcher.Changed += this.OnChanged;
      watcherInfo.Watcher.Deleted += this.OnDeleted;
      watcherInfo.Watcher.EnableRaisingEvents = true;
    }

    public void RemoveWatch(string path)
    {
      ValidateArg.NotNullOrEmpty(path, "path");
      if (string.IsNullOrEmpty(path))
      {
        return;
      }
      FileWatcherInfo watcherInfo;
      if (!this._fileWatchers.TryGetValue(path, out watcherInfo))
      {
        return;
      }
      watcherInfo.Watcher.EnableRaisingEvents = false;
      this._fileWatchers.Remove(path);
      watcherInfo.Watcher.Changed -= this.OnChanged;
      watcherInfo.Watcher.Deleted -= this.OnDeleted;
      watcherInfo.Watcher.Dispose();
      watcherInfo.Watcher = null;
    }

    public void Clear()
    {

      foreach (var fileWatcher in this._fileWatchers.Values)
      {
        if (fileWatcher.Watcher == null)
        {
          continue;
        }
        fileWatcher.Watcher.Dispose();
        fileWatcher.Watcher = null;
      }
      this._fileWatchers.Clear();
    }

    private static byte[] ComputeHash(string fileName)
    {
      var hash = new byte[0];
      if (!File.Exists(fileName))
      {
        return hash;
      }
      using (var md5 = MD5.Create())
      {
        using (var stream = File.OpenRead(fileName))
        {
          hash = md5.ComputeHash(stream);
        }
      }
      return hash;
    }


    private void OnChanged(object sender, FileSystemEventArgs e)
    {
      if (this.TestContainerChanged == null)
      {
        return;
      }
      FileWatcherInfo watcherInfo;
      if (!this._fileWatchers.TryGetValue(e.FullPath, out watcherInfo))
      {
        return;
      }
      var writeTime = File.GetLastWriteTime(e.FullPath);
      // Only fire update if enough time has passed since last update to prevent duplicate events
      if (!(writeTime.Subtract(watcherInfo.LastEventTime).TotalMilliseconds > 500))
      {
        return;
      }
      var newHash = ComputeHash(e.FullPath);
      if (watcherInfo.Hash == newHash)
      {
        return;
      }
      watcherInfo.LastEventTime = writeTime;
      if (null != this.TestContainerChanged)
      {
        this.TestContainerChanged(this, new TestContainerEventArgs(e.FullPath));
      }
    }

    private void OnDeleted(object sender, FileSystemEventArgs e)
    {
      if (this.TestContainerDeleted == null)
      {
        return;
      }
      FileWatcherInfo watcherInfo;
      if (!this._fileWatchers.TryGetValue(e.FullPath, out watcherInfo))
      {
        return;
      }
      var container = e.FullPath;
      this.RemoveWatch(e.FullPath);
      this.TestContainerDeleted(this, new TestContainerEventArgs(container));
    }
  }
}
