﻿using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace CTestAdapter
{
  public class CTestTestCollector
  {
    private const string FieldNameNumber = "number";
    private const string FieldNameTestname = "testname";

    private static readonly Regex TestRegex =
        new Regex(@".*#(?<" + CTestTestCollector.FieldNameNumber + 
          ">[1-9][0-9]*): *(?<" + CTestTestCollector.FieldNameTestname + 
          @">[\w-\.:]+).*");

    public string CTestExecutable { get; set; }

    public string CTestWorkingDir { get; set; }

    public string CurrentActiveConfig { get; set; }

    public string CTestArguments { get; set; }

    public CTestTestCollector()
    {
      this.CTestArguments = " -N ";
    }

    public void CollectTestCases(CTestTestCollection testCollection)
    {
      testCollection.Tests.Clear();
      if (!File.Exists(this.CTestExecutable))
      {
        return;
      }
      if (!Directory.Exists(this.CTestWorkingDir))
      {
        return;
      }
      var args = this.CTestArguments;
      if (!string.IsNullOrWhiteSpace(this.CurrentActiveConfig))
      {
        args += " -C ";
        args += this.CurrentActiveConfig;
      }
      var proc = new Process
      {
        StartInfo = new ProcessStartInfo()
        {
          FileName = this.CTestExecutable,
          WorkingDirectory = this.CTestWorkingDir,
          Arguments = args,
          CreateNoWindow = true,
          RedirectStandardError = true,
          RedirectStandardOutput = true,
          WindowStyle = ProcessWindowStyle.Hidden,
          UseShellExecute = false
        }
      };
      proc.Start();
      var output = proc.StandardOutput.ReadToEnd();
      proc.Dispose();
      var matches = TestRegex.Matches(output);
      foreach (var match in matches)
      {
        var m = match as Match;
        if (m == null)
        {
          continue;
        }
        var name = m.Groups[FieldNameTestname].Value;
        var numberStr = m.Groups[FieldNameNumber].Value;
        int number;
        int.TryParse(numberStr, out number);
        var newinfo = new CTestTestCollection.TestInfo
        {
          Name = name,
          Number = number,
        };
        testCollection.Tests.Add(newinfo);
      }
    }
  }
}
