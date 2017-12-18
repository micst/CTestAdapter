using System.Collections.Generic;
using System.Linq;

namespace CTestAdapter
{
  public class CTestTestCollection
  {
    public struct TestInfo
    {
      public string Name;
      public int Number;
    }

    private readonly List<TestInfo> _tests;

    public CTestTestCollection()
    {
      this._tests = new List<TestInfo>();
    }

    public bool TestExists(string testname)
    {
      return this._tests.Any(v => v.Name == testname);
    }

    public int TestCount
    {
      get { return this._tests.Count; }
    }

    public TestInfo this[int number]
    {
      get { return this._tests.FirstOrDefault(item => item.Number == number); }
    }

    public TestInfo this[string name]
    {
      get { return this._tests.FirstOrDefault(item => string.Equals(item.Name, name)); }
    }

    public List<TestInfo> Tests
    {
      get { return this._tests; }
    }
  }
}
