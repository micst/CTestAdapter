using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EnvDTE;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace CTestAdapter
{
  internal static class TestContainerHelper
  {
    public const string TestFileExtension = ".cmake";
    public const string TestFileName = "CTestTestfile";

    private const string FieldNameTestname = "testname";
    private const string FieldNameIsExe = "isexe";

    private static readonly Regex AddTestRegex =
      new Regex(@"^\s*add_test\s*\((?<" + FieldNameTestname + @">\S+)\s.*\).*$");

    private static readonly Regex IsExeRegex =
      new Regex("^\\s*add_test\\s*\\(\\S+\\s+\"(?<" + FieldNameIsExe + ">[^\"]+)\".*\\)");

    /**
     * @brief verifies the file extension is .cmake
     */
    public static bool IsTestContainerFile(string file)
    {
      try
      {
        return TestContainerHelper.TestFileExtension.Equals(
          Path.GetExtension(file),
          StringComparison.OrdinalIgnoreCase);
      }
      catch (Exception /*e*/)
      {
        // TODO do some messaging here or so ...
      }
      return false;
    }

    /**
     * @brief recursively collects all test container files that can be found
     */
    public static IEnumerable<string> CollectCTestTestfiles(string currentDir)
    {
      var file = new FileInfo(Path.Combine(currentDir, TestContainerHelper.TestFileName + TestContainerHelper.TestFileExtension));
      if (!file.Exists)
      {
        return Enumerable.Empty<string>();
      }
      var res = new List<string>();
      var content = file.OpenText().ReadToEnd();
      var matches = Regex.Matches(content, @".*[sS][uB][bB][dD][iI][rR][sS]\s*\((?<subdir>.*)\)");
      var subdirs = (from Match match in matches select match.Groups["subdir"].Value).ToList();
      if (content.Contains("add_test"))
      {
        res.Add(file.FullName);
      }
      foreach (var dir in subdirs)
      {
        var subpath = dir.Trim('\"');
        subpath = Path.Combine(currentDir, subpath);
        res.AddRange(TestContainerHelper.CollectCTestTestfiles(subpath));
      }
      return res;
    }

    public static CTestTestCollection FindAllTestsWithCtest(CTestAdapterConfig cfg)
    {
      if (null == cfg)
      {
        return null;
      }
      if (!Directory.Exists(cfg.CacheDir))
      {
        return null;
      }
      if (!File.Exists(cfg.CTestExecutable))
      {
        return null;
      }
      var collection = new CTestTestCollection();
      var collector = new CTestTestCollector
      {
        CTestExecutable = cfg.CTestExecutable,
        CTestWorkingDir = cfg.CacheDir,
        CurrentActiveConfig = cfg.ActiveConfiguration
      };
      collector.CollectTestCases(collection);
      return collection;
    }

    public static Dictionary<string, TestCase> ParseTestContainerFile(string source, IMessageLogger log,
      CTestTestCollection collection)
    {
      var cases = new Dictionary<string, TestCase>();
      var content = File.ReadLines(source);
      var lineNumber = 0;
      foreach (var line in content)
      {
        lineNumber++;
        var matches = TestContainerHelper.AddTestRegex.Matches(line);
        foreach (var match in matches)
        {
          var m = match as Match;
          if (m == null)
          {
            continue;
          }
          var testname = m.Groups[FieldNameTestname].Value;
          if (null != collection)
          {
            if (!collection.TestExists(testname))
            {
              log.SendMessage(TestMessageLevel.Warning,
                "CTestDiscoverer.ParseTestContainerFile: test not listed by ctest -N :" + testname);
            }
          }
          if (cases.ContainsKey(testname))
          {
            continue;
          }
          var testcase = new TestCase(testname, CTestExecutor.ExecutorUri, source)
          {
            CodeFilePath = source,
            DisplayName = testname,
            LineNumber = lineNumber,
          };
          if (null != collection)
          {
            if (collection.TestExists(testname))
            {
              testcase.DisplayName = collection[testname].Number.ToString().PadLeft(3, '0') + ": " + testname;
            }
          }
          var isExe = IsExeRegex.Match(line);
          cases.Add(testname, testcase);
        }
      }
      return cases;
    }

    public static string FindCMakeCacheDirectory(string fileOrDirectory)
    {
      // if a file is given, remove the filename before processing
      if (File.Exists(fileOrDirectory))
      {
        var finfo = new FileInfo(fileOrDirectory);
        if (null != finfo.DirectoryName)
        {
          fileOrDirectory = finfo.DirectoryName;
        }
      }
      if (!File.Exists(Path.Combine(fileOrDirectory, Constants.CTestTestFileName)))
      {
        // we are not within a testable cmake build tree
        return string.Empty;
      }
      while (true)
      {
        var info = new DirectoryInfo(fileOrDirectory);
        if (File.Exists(info.FullName + "\\" + Constants.CMakeCacheFilename))
        {
          return info.FullName;
        }
        if (!info.Exists)
        {
          return string.Empty;
        }
        if (info.Parent == null)
        {
          return string.Empty;
        }
        fileOrDirectory = info.Parent.FullName;
      }
    }
  }
}
