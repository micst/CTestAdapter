using System;
using System.IO;
using System.Threading;
using System.Xml.Serialization;

namespace CTestAdapter
{
  [Serializable]
  public class CTestAdapterConfig
  {
    private string _cacheDir;
    private string _configFileName;

    private bool _dirty = false;

    private string _activeConfiguration;
    private string _ctestExecutable;
    private string _cmakeConfigurationTypes;

    public string CacheDir
    {
      get { return this._cacheDir;  }
      set
      {
        this._cacheDir = value;
        this._configFileName = 
          Path.Combine(this._cacheDir, Constants.CTestAdapterConfigFileName);
      }
    }

    public string ActiveConfiguration
    {
      get { return this._activeConfiguration; }
      set
      {
        if (this._activeConfiguration == value)
        {
          return;
        }
        this._activeConfiguration = value;
        this._dirty = true;
      }
    }

    public string CTestExecutable
    {
      get { return this._ctestExecutable; }
      set
      {
        if (this._ctestExecutable == value)
        {
          return;
        }
        this._ctestExecutable = value;
        this._dirty = true;
      }
    }

    public string CMakeConfigurationTypes
    {
      get { return this._cmakeConfigurationTypes; }
      set
      {
        if (this._cmakeConfigurationTypes == value)
        {
          return;
        }
        this._cmakeConfigurationTypes = value;
        this._dirty = true;
      }
    }

    public static void WriteToDisk(CTestAdapterConfig cfg)
    {
      if (!cfg._dirty)
      {
        return;
      }
      if (!Directory.Exists(cfg.CacheDir))
      {
        return;
      }
      var ser = new XmlSerializer(typeof(CTestAdapterConfig));
      while (CTestAdapterConfig.IsFileLocked(cfg._configFileName))
      {
        Thread.Sleep(50);
      }
      var str = new StreamWriter(cfg._configFileName);
      ser.Serialize(str, cfg);
      str.Close();
      cfg._dirty = false;
    }

    public static CTestAdapterConfig ReadFromDisk(string file)
    {
      if (!File.Exists(file))
      {
        return null;
      }
      var ser = new XmlSerializer(typeof(CTestAdapterConfig));
      while (CTestAdapterConfig.IsFileLocked(file))
      {
        Thread.Sleep(50);
      }
      var str = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
      var cfg = (CTestAdapterConfig)ser.Deserialize(str);
      str.Close();
      if (null == cfg)
      {
        return null;
      }
      if (cfg._configFileName != file)
      {
        // @todo give some message here?
      }
      return cfg;
    }

    public static CTestAdapterConfig ReadFromCache(string dir)
    {
      if (!Directory.Exists(dir))
      {
        return null;
      }
      ILog log = null;
      var pkg = CTestAdapterPackage.Instance;
      if (null != pkg)
      {
        log = pkg.Logger;
      }
      var cache = new CMakeCache(log);
      cache.LoadCMakeCache(Path.Combine(dir, Constants.CMakeCacheFilename));
      if (!cache.IsLoaded)
      {
        return null;
      }
      var cfg = new CTestAdapterConfig
      {
        // unfortunately we cannot set the active configuration here,
        // a fallback will be used when parsing
        CMakeConfigurationTypes = cache[Constants.CMakeCacheKey_CofigurationTypes],
        CTestExecutable = cache[Constants.CMakeCacheKey_CTestCommand],
        CacheDir = cache[Constants.CMakeCacheKey_CacheFileDir]
      };
      return cfg;
    }

    public static bool IsFileLocked(string filename)
    {
      if (!File.Exists(filename))
      {
        return false;
      }
      FileStream stream = null;
      try
      {
        var finfo = new FileInfo(filename);
        stream = finfo.Open(FileMode.Open, FileAccess.Read, FileShare.None);
      }
      catch (IOException)
      {
        return true;
      }
      finally
      {
        if (stream != null)
        {
          stream.Close();
        }
      }
      return false;
    }
  }
}
