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

    public void Cancel()
    {
      this._cancelled = true;
    }

    public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
    {
      frameworkHandle.SendMessage(TestMessageLevel.Informational, "CTestExecutor: running tests (src) ...");
      var enumerable = sources as IList<string> ?? sources.ToList();
      if(!this.SetupEnvironment(enumerable.First(), frameworkHandle))
      {
        frameworkHandle.SendMessage(TestMessageLevel.Error, "CTestExecutor: could not initialize environment (src)");
        return;
      }
      this._runningFromSources = true;
      var logFileDir = this._config.CacheDir + "\\Testing\\Temporary";
      frameworkHandle.SendMessage(TestMessageLevel.Informational,
          "CTestExecutor: logs are written to (file://" + logFileDir + ")");
      foreach (var s in enumerable)
      {
        var cases = TestContainerHelper.ParseTestContainerFile(s, frameworkHandle, null);
        this.RunTests(cases.Values, runContext, frameworkHandle);
      }
      this._runningFromSources = false;
      frameworkHandle.SendMessage(TestMessageLevel.Informational, "CTestExecutor: running tests (src) done");
    }

    public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
    {
      frameworkHandle.SendMessage(TestMessageLevel.Informational, "CTestExecutor: running tests ...");
      var testCases = tests as IList<TestCase> ?? tests.ToList();
      if (!testCases.Any())
      {
        return;
      }
      if (!this._runningFromSources)
      {
        if(!this.SetupEnvironment(testCases.First().Source, frameworkHandle))
        {
          frameworkHandle.SendMessage(TestMessageLevel.Error, "CTestExecutor: could not initialize environment");
          return;
        }
      }
      if (!this._config.ActiveConfiguration.Any())
      {
        // get configuration name from cmake cache, pick first found
        var typeString = this._config.CMakeConfigurationTypes;
        var types = typeString.Split(';');
        if (types.Any())
        {
          this._config.ActiveConfiguration = types.First();
          frameworkHandle.SendMessage(TestMessageLevel.Warning,
              "CTestExecutor: Configuration fallback to: " + this._config.ActiveConfiguration);
        }
      }
      if (!this._config.ActiveConfiguration.Any())
      {
        frameworkHandle.SendMessage(TestMessageLevel.Warning,
            "CTestExecutor: no build configuration found");
      }
      if (!File.Exists(this._config.CTestExecutable))
      {
        frameworkHandle.SendMessage(TestMessageLevel.Error,
            "CTestExecutor: ctest not found: \"" + this._config.CTestExecutable + "\"");
        return;
      }
      if (!Directory.Exists(this._config.CacheDir))
      {
        frameworkHandle.SendMessage(TestMessageLevel.Error,
            "CTestExecutor: working directory not found: \"" + this._config.CacheDir + "\"");
        return;
      }
      frameworkHandle.SendMessage(TestMessageLevel.Informational,
          "CTestExecutor: working directory is \"" + this._config.CacheDir + "\"");
      var logFileDir = this._config.CacheDir + "\\Testing\\Temporary";
      if (!this._runningFromSources)
      {
        frameworkHandle.SendMessage(TestMessageLevel.Informational,
            "CTestExecutor: ctest (" + this._config.CTestExecutable + ")");
        frameworkHandle.SendMessage(TestMessageLevel.Informational,
            "CTestExecutor: logs are written to (file://" + logFileDir + ")");
      }
      // run test cases
      foreach (var test in testCases)
      {
        if (this._cancelled)
        {
          break;
        }
        // verify we have a run directory and a ctest executable
        var args = "-R \"^" + test.FullyQualifiedName + "$\"";
        if (this._config.ActiveConfiguration.Any())
        {
          args += " -C \"" + this._config.ActiveConfiguration + "\"";
        }
        var startInfo = new ProcessStartInfo
        {
          Arguments = args,
          FileName = this._config.CTestExecutable,
          WorkingDirectory = this._config.CacheDir,
          CreateNoWindow = true,
          RedirectStandardOutput = true,
          RedirectStandardError = true,
          UseShellExecute = false,
          WindowStyle = ProcessWindowStyle.Hidden
        };
        var process = new Process
        {
          StartInfo = startInfo
        };
        var logFileName = logFileDir + "\\LastTest.log";
        if (File.Exists(logFileName))
        {
          File.Delete(logFileName);
        }
        var logMsg = "CTestExecutor: ctest " + test.FullyQualifiedName;
        if (this._config.ActiveConfiguration.Any())
        {
          logMsg += " -C " + this._config.ActiveConfiguration;
        }
        frameworkHandle.SendMessage(TestMessageLevel.Informational, logMsg);
        if (runContext.IsBeingDebugged)
        {
          /// @todo check if child process debugging is available?!?
          process.Start();
        }
        else
        {
          process.Start();
        }
        process.WaitForExit();
        var output = process.StandardOutput.ReadToEnd();
        if (!File.Exists(logFileName))
        {
          frameworkHandle.SendMessage(TestMessageLevel.Warning, "logfile not found: "
                                                                + logFileName);
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
          frameworkHandle.SendMessage(TestMessageLevel.Warning,
              "CTestExecutor: could not get runtime of test " + test.FullyQualifiedName);
        }
        var testResult = new TestResult(test)
        {
          ComputerName = Environment.MachineName,
          Duration = timeSpan,
          Outcome = process.ExitCode == 0 ? TestOutcome.Passed : TestOutcome.Failed
        };
        if (process.ExitCode != 0)
        {
          var matchesOutput = CTestExecutor.RegexOutput.Match(content);
          testResult.ErrorMessage = matchesOutput.Groups[CTestExecutor.RegexFieldOutput].Value;
          frameworkHandle.SendMessage(TestMessageLevel.Error,
              "CTestExecutor: ERROR IN TEST " + test.FullyQualifiedName + ":");
          frameworkHandle.SendMessage(TestMessageLevel.Error, output);
          frameworkHandle.SendMessage(TestMessageLevel.Error,
              "CTestExecutor: END OF TEST OUTPUT FROM " + test.FullyQualifiedName);
        }
        frameworkHandle.SendMessage(TestMessageLevel.Informational,
            "CTestExecutor: Log saved to file://" + logFileBackup);
        frameworkHandle.RecordResult(testResult);
      }
      frameworkHandle.SendMessage(TestMessageLevel.Informational, "CTestExecutor: running tests done");
    }

    private bool SetupEnvironment(string source, IMessageLogger h)
    {
      var cacheDir = TestContainerHelper.FindCMakeCacheDirectory(source);
      this._config = CTestAdapterConfig.ReadFromDisk(Path.Combine(cacheDir, Constants.CTestAdapterConfigFileName)) ??
                     CTestAdapterConfig.ReadFromCache(cacheDir);
      return null != this._config;
    }
  }
}