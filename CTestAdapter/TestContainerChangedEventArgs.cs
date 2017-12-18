using System.Collections.Generic;

namespace CTestAdapter.Events
{
  public class TestContainerEventArgs : System.EventArgs
  {
    public string File { get; private set; }

    public TestContainerEventArgs(string file)
    {
      this.File = file;
    }
  }

  public class TestContainerListArgs : System.EventArgs
  {
    public List<string> TestContainers { get; private set; }

    public TestContainerListArgs(List<string> containers)
    {
      this.TestContainers = containers;
    }
  }
}
