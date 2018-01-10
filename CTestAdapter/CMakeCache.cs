using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace CTestAdapter
{
  public class CMakeCache : ILog
  {
    private static readonly Regex CacheEntryRegex = new Regex("^([\\w-\\.]+|\"[\\w-\\.:]+\"):([^=]+)=(.*)$");

    private enum CMakeCacheEntryType
    {
      // ReSharper disable InconsistentNaming
      // ReSharper disable UnusedMember.Local
      BOOL,
      PATH,
      FILEPATH,
      STRING,
      INTERNAL,
      STATIC,
      UNINITIALIZED
    }

    private struct CMakeCacheEntry
    {
      public string Name;
      public string Value;
    }

    private readonly ILog _log;
    private readonly Dictionary<string, CMakeCacheEntry> _cacheEntries;
    private string _cmakeCacheFile;
    private FileInfo _cmakeCacheInfo;
    private CMakeCacheEntry _tmpEntry;

    public CMakeCache(ILog log)
    {
      this._log = log;
      this._cacheEntries = new Dictionary<string, CMakeCacheEntry>();
    }

    public string CMakeCacheFile
    {
      get { return this._cmakeCacheFile; }
      set
      {
        this.LoadCMakeCache(value);
      }
    }

    public bool IsLoaded
    {
      get { return this._cacheEntries.Count > 0; }
    }

    public string this[string name]
    {
      get { return this._cacheEntries.TryGetValue(name, out this._tmpEntry) ?
          this._tmpEntry.Value : string.Empty; }
    }

    public void LoadCMakeCache(string fileName)
    {
      this._cmakeCacheFile = fileName;
      if (null == this._cmakeCacheFile || !File.Exists(this._cmakeCacheFile))
      {
        this.Log(LogLevel.Debug, "LoadCMakeCache: clearing cmake CMakeCache");
        this._cacheEntries.Clear();
        return;
      }
      var newInfo = new FileInfo(this._cmakeCacheFile);
      if (this._cmakeCacheInfo != null)
      {
        this.Log(LogLevel.Debug, "LoadCMakeCache: comparing already loaded CMakeCache");
        if (this._cmakeCacheInfo.FullName == newInfo.FullName &&
            this._cmakeCacheInfo.LastWriteTime == newInfo.LastWriteTime &&
            newInfo.Exists)
        {
          this.Log(LogLevel.Debug, "LoadCMakeCache: CMakeCache did not change, not reloading");
          return;
        }
      }
      this.Log(LogLevel.Debug, "LoadCMakeCache: loading CMakeCache from \"" + this._cmakeCacheFile + "\"");
      this._cmakeCacheInfo = newInfo;
      this._cacheEntries.Clear();
      if (!File.Exists(this._cmakeCacheFile))
      {
        this.Log(LogLevel.Error, "LoadCMakeCache: CMakeCache not found at:\"" + this._cmakeCacheFile + "\"");
        return;
      }
      while (CTestAdapterConfig.IsFileLocked(this._cmakeCacheFile))
      {
        Thread.Sleep(50);
      }
      var stream = new FileStream(this._cmakeCacheFile, FileMode.Open,
          FileAccess.Read, FileShare.ReadWrite);
      var r = new StreamReader(stream);
      while (!r.EndOfStream)
      {
        var line = r.ReadLine();
        if (null == line)
        {
          continue;
        }
        line = line.TrimStart(' ');
        if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("//"))
        {
          continue;
        }
        var c = CMakeCache.CacheEntryRegex.Split(line);
        if (c.Length != 5)
        {
          this.Log(LogLevel.Warning, "LoadCMakeCache: CMakeCache load: element count != 5: (" + c.Length + ")" + line);
          var count = 0;
          foreach (var asdf in c)
          {
            this.Log(LogLevel.Warning, "v" + count + ": " + asdf);
            count++;
          }
          continue;
        }
        CMakeCacheEntryType myType;
        if (!Enum.TryParse(c[2], out myType))
        {
          this.Log(LogLevel.Error, "LoadCMakeCache: cache load: error parsing enum Type: " + c[2]);
          continue;
        }
        var entry = new CMakeCacheEntry()
        {
          Name = c[1],
          Value = c[3]
        };
        if (entry.Name.StartsWith("\"") && entry.Name.Length > 2)
        {
          entry.Name = entry.Name.Substring(1, entry.Name.Length - 2);
        }
        this._cacheEntries.Add(entry.Name, entry);
      }
      r.Close();
      stream.Close();
      r.Dispose();
      stream.Dispose();
    }

    public void Log(LogLevel level, string message)
    {
      if (null == this._log)
      {
        return;
      }
      this._log.Log(level, Constants.LogPrefixCmCa + message);
    }
  }
}
