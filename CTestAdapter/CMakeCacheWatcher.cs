using System;
using System.IO;

namespace CTestAdapter
{
  public class CMakeCacheWatcher : ILog
  {
    private readonly ILog _log;
    private FileSystemWatcher _cacheWatcher;
    private string _cmakeCacheFile = Constants.CMakeCacheFilename;
    private string _cmakeCacheDirectory;

    public event Action CacheFileChanged;

    public CMakeCacheWatcher(ILog log)
    {
      this._log = log;
    }

    public string CMakeCacheFile
    {
      get { return this._cmakeCacheFile; }
      set
      {
        this._cmakeCacheFile = value;
        this.StartWatching();
      }
    }

    public string CMakeCacheDirectory
    {
      get { return this._cmakeCacheDirectory; }
      set
      {
        this._cmakeCacheDirectory = value;
        this.StartWatching();
      }
    }

    public void StartWatching()
    {
      if (!Directory.Exists(this._cmakeCacheDirectory))
      {
        this.Log(LogLevel.Warning, "cache dir does not exist: " + this._cmakeCacheDirectory);
        this.StopWatching();
        return;
      }
      this.Log(LogLevel.Debug, "start cache watching");
      if (this._cacheWatcher == null)
      {
        this._cacheWatcher = new FileSystemWatcher(this._cmakeCacheDirectory)
        {
          IncludeSubdirectories = false,
          EnableRaisingEvents = true,
          Filter = this._cmakeCacheFile
        };
        this._cacheWatcher.Changed += this.OnCMakeCacheChanged;
        this._cacheWatcher.Created += this.OnCMakeCacheChanged;
        this._cacheWatcher.Deleted += this.OnCMakeCacheChanged;
      }
      else
      {
        this._cacheWatcher.Path = this._cmakeCacheDirectory;
        this._cacheWatcher.Filter = this._cmakeCacheFile;
      }
      if (null != this.CacheFileChanged)
      {
        this.CacheFileChanged();
      }
    }

    public void StopWatching()
    {
      if (null == this._cacheWatcher)
      {
        return;
      }
      this.Log(LogLevel.Debug, "stop cache watching");
      this._cacheWatcher.Changed -= this.OnCMakeCacheChanged;
      this._cacheWatcher.Created -= this.OnCMakeCacheChanged;
      this._cacheWatcher.Deleted -= this.OnCMakeCacheChanged;
      this._cacheWatcher.Dispose();
      this._cacheWatcher = null;
    }

    private void OnCMakeCacheChanged(object source, FileSystemEventArgs e)
    {
      this.Log(LogLevel.Debug, "cache changed: " + e.FullPath);
      if (null == this.CacheFileChanged)
      {
        return;
      }
      this.CacheFileChanged();
    }

    public void Log(LogLevel level, string message)
    {
      if (null == this._log)
      {
        return;
      }
      this._log.Log(level, Constants.LogPrefixCaWt + message);
    }
  }
}
