using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace CTestAdapter
{
  [ExtensionUri(Constants.ExecutorUriString)]
  public class CTestExecutor : ITestExecutor
  {
    public static readonly Uri ExecutorUri = new Uri(Constants.ExecutorUriString);

    private const string LogPrefix = "CTestExecutor: ";

    private const string RegexFieldOutput = "output";
    private const string RegexFieldDuration = "duration";

    private static readonly Regex RegexOutput =
        new Regex(@"Output:\r\n-+\r\n(?<" + CTestExecutor.RegexFieldOutput + @">.*)\r\n<end of output>\r\n",
            RegexOptions.Singleline);

    private static readonly Regex RegexDuration =
        new Regex(@"<end of output>\r\nTest time =\s+(?<" + CTestExecutor.RegexFieldDuration + 
          @">[\d\.]+) sec\r\n",
            RegexOptions.Singleline);

    private bool _runningFromSources = false;
    private bool _cancelled = false;

    private CTestAdapterConfig _config;

    private Process _proc = null;
    private ProcessStartInfo _procParam;

    private IMessageLogger _log = null;

    private void Log(TestMessageLevel lvl, string message)
    {
      if (this._log == null)
      {
        return;
      }
      this._log.SendMessage(lvl, CTestExecutor.LogPrefix + " " + message);
    }

    public void Cancel()
    {
      this._cancelled = true;
      if (this._proc != null)
      {
        this._proc.Kill();
      }
    }

    public void RunTests(IEnumerable<string> sources, IRunContext runContext,
      IFrameworkHandle frameworkHandle)
    {
      this._log = frameworkHandle;
      this.Log(TestMessageLevel.Informational, "running tests (src) ...");
      var sourcesList = sources as IList<string> ?? sources.ToList();
      if(!this.SetupEnvironment(sourcesList.First()))
      {
        return;
      }
      this.Log(TestMessageLevel.Informational, "using configuration: " 
        + this._config.ActiveConfiguration);
      this._runningFromSources = true;
      var logFileDir = this._config.CacheDir + "\\Testing\\Temporary";
      this.Log(TestMessageLevel.Informational,
          "logs are written to (" + TestContainerHelper.ToLinkPath(logFileDir) + ")");
      foreach (var source in sourcesList)
      {
        var cases = TestContainerHelper.ParseTestContainerFile(
          source, frameworkHandle, null, this._config.ActiveConfiguration);
        this.RunTests(cases.Values, runContext, frameworkHandle);
      }
      this._runningFromSources = false;
      this.Log(TestMessageLevel.Informational, "running tests (src) done");
    }

    public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext,
      IFrameworkHandle frameworkHandle)
    {
      this._log = frameworkHandle;
      this.Log(TestMessageLevel.Informational, "running tests ...");
      var testCases = tests as IList<TestCase> ?? tests.ToList();
      if (!testCases.Any())
      {
        return;
      }
      if (!this._runningFromSources)
      {
        if(!this.SetupEnvironment(testCases.First().Source))
        {
          return;
        }
      }
      // make sure a configuration is set
      if (!this._config.ActiveConfiguration.Any())
      {
        if (this._config.TrySetActiveConfigFromConfigTypes())
        {
          this.Log(TestMessageLevel.Warning,
            "Configuration fallback to: " + this._config.ActiveConfiguration);
        }
        else
        {
          this.Log(TestMessageLevel.Error, "could not set Configuration");
          return;
        }
      }
      // make sure we have a ctest executable
      if (!File.Exists(this._config.CTestExecutable))
      {
        this._config.CTestExecutable = TestContainerHelper.FindCTestExe(this._config.CacheDir);
      }
      if (!File.Exists(this._config.CTestExecutable))
      {
        this.Log(TestMessageLevel.Error,
            "ctest not found, tried: \"" + this._config.CTestExecutable + "\"");
        return;
      }
      if (!Directory.Exists(this._config.CacheDir))
      {
        this.Log(TestMessageLevel.Error,
            "working directory not found: " + TestContainerHelper.ToLinkPath(this._config.CacheDir));
        return;
      }
      this.Log(TestMessageLevel.Informational,
          "working directory is " + TestContainerHelper.ToLinkPath(this._config.CacheDir));
      var logFileDir = this._config.CacheDir + "\\Testing\\Temporary";
      if (!this._runningFromSources)
      {
        this.Log(TestMessageLevel.Informational,
            "ctest (" + this._config.CTestExecutable + ")");
        this.Log(TestMessageLevel.Informational,
            "logs are written to (" + TestContainerHelper.ToLinkPath(logFileDir) + ")");
      }
      this._proc = new Process();
      if (this._procParam == null)
      {
        this._procParam = new ProcessStartInfo
        {
          CreateNoWindow = true,
          RedirectStandardOutput = true,
          RedirectStandardError = true,
          UseShellExecute = false,
          WindowStyle = ProcessWindowStyle.Hidden
        };
      }
      // run test cases
      foreach (var test in testCases)
      {
        var testResult = new TestResult(test)
        {
          ComputerName = Environment.MachineName,
          Outcome = TestOutcome.Skipped
        };
        // verify we have a run directory and a ctest executable
        var args = "-R \"^" + test.FullyQualifiedName + "$\"";
        if (this._config.ActiveConfiguration.Any())
        {
          args += " -C \"" + this._config.ActiveConfiguration + "\"";
        }
        this._procParam.Arguments = args;
        this._procParam.FileName = this._config.CTestExecutable;
        this._procParam.WorkingDirectory = this._config.CacheDir;
        this._proc.StartInfo = this._procParam;
        var logFileName = logFileDir + "\\LastTest.log";
        if (File.Exists(logFileName))
        {
          File.Delete(logFileName);
        }
        var logMsg = "ctest " + test.FullyQualifiedName;
        if (this._config.ActiveConfiguration.Any())
        {
          logMsg += " -C " + this._config.ActiveConfiguration;
        }
        this.Log(TestMessageLevel.Informational, logMsg);
        if (this._cancelled)
        {
          break;
        }
        if (runContext.IsBeingDebugged)
        {
          /// @todo check if child process debugging is available?!?
          this._proc.Start();
        }
        else
        {
          this._proc.Start();
        }
        this._proc.WaitForExit();
        if (this._cancelled)
        {
          break;
        }
        var output = this._proc.StandardOutput.ReadToEnd();
        if (!File.Exists(logFileName))
        {
          this.Log(TestMessageLevel.Warning, "logfile not found: " 
            + TestContainerHelper.ToLinkPath(logFileName));
        }
        var content = File.ReadAllText(logFileName);
        var logFileBackup = test.FullyQualifiedName + ".log";
        var invalidChars = new string(Path.GetInvalidFileNameChars()) +
          new string(Path.GetInvalidPathChars());
        foreach (char c in invalidChars)
        {
          logFileBackup = logFileBackup.Replace(c.ToString(), "_");
        }
        logFileBackup = logFileDir + "\\" + logFileBackup;
        File.Copy(logFileName, logFileBackup, true);
        var matchesDuration = CTestExecutor.RegexDuration.Match(content);
        var timeSpan = new TimeSpan();
        if (matchesDuration.Success)
        {
          timeSpan = TimeSpan.FromSeconds(
              double.Parse(matchesDuration.Groups[CTestExecutor.RegexFieldDuration].Value,
                  System.Globalization.CultureInfo.InvariantCulture.NumberFormat));
        }
        else
        {
          this.Log(TestMessageLevel.Warning,
              "could not get runtime of test " + test.FullyQualifiedName);
        }
        testResult.Duration = timeSpan;
        testResult.Outcome = this._proc.ExitCode == 0 ? TestOutcome.Passed : TestOutcome.Failed;
        if (this._proc.ExitCode != 0)
        {
          var matchesOutput = CTestExecutor.RegexOutput.Match(content);
          testResult.ErrorMessage = matchesOutput.Groups[CTestExecutor.RegexFieldOutput].Value;
          this.Log(TestMessageLevel.Error,
              "ERROR IN TEST " + test.FullyQualifiedName + ":");
          this.Log(TestMessageLevel.Error, output);
          this.Log(TestMessageLevel.Error,
              "END OF TEST OUTPUT FROM " + test.FullyQualifiedName);
        }
        this.Log(TestMessageLevel.Informational,
            "Log saved to " + TestContainerHelper.ToLinkPath(logFileBackup));
        frameworkHandle.RecordResult(testResult);
      }
      this._proc.Dispose();
      this._proc = null;
      this.Log(TestMessageLevel.Informational, "running tests done");
    }

    private bool SetupEnvironment(string source)
    {
      var cacheDir = TestContainerHelper.FindCMakeCacheDirectory(source);
      this._config = CTestAdapterConfig.ReadFromDisk(Path.Combine(cacheDir, Constants.CTestAdapterConfigFileName)) ??
                     CTestAdapterConfig.ReadFromCache(cacheDir);
      if (this._config == null)
      {
        this.Log(TestMessageLevel.Error, "could not initialize environment");
        return false;
      }
      return true;
    }
  }
}
