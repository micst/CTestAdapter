using System;

namespace CTestAdapter
{
  internal class Constants
  {
    public const string ExecutorUriString = "executor://CTestExecutor/v1";

    public const string PackageString = "14796bd1-9ea5-4eff-b4c0-bee11efbb735";
    public static readonly Guid Package = new Guid(PackageString);

    public static readonly Guid CommandSet = new Guid("14796bd1-9ea5-4eff-b4c0-bee11efbb736");

    public const int CTestCommandId = 0x0100;

    public static readonly Guid OutputWindow = new Guid("231F0144-E723-4FD5-A62B-DADCFF615067");

    public const string CMakeCacheFilename = "CMakeCache.txt";
    public const string CTestTestFileName = "CTestTestfile.cmake";
    public const string CTestAdapterConfigFileName = "CTestAdapter.config";

    public const string CMakeCacheKey_CacheFileDir = "CMAKE_CACHEFILE_DIR";
    public const string CMakeCacheKey_CTestCommand = "CMAKE_CTEST_COMMAND";
    public const string CMakeCacheKey_CofigurationTypes = "CMAKE_CONFIGURATION_TYPES";

    public const string LogWindowTitle = "CTestAdapter";
    public const string AdapterLogFileName = "CTestAdapter.log";
    public const string AdapterLogFileNameInvalid = "no solution loaded";

    public const string LogPrefixPckg = "Package:      ";
    public const string LogPrefixCmCa = "Cache:        ";
    public const string LogPrefixCaWt = "WatcherCache: ";
    public const string LogPrefixCMgr = "WatcherCont:  ";

    public const string OptionsPageCategory = "CTestAdapter";
    public const string OptionsPageGridPage = "Logging";
  }
}
