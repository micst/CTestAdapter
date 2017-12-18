using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;

namespace CTestAdapter
{
  internal sealed class CTestCommand
  {
    private readonly Package _package;

    private CTestCommand(Package package)
    {
      if (package == null)
      {
        throw new ArgumentNullException("package");
      }
      this._package = package;
      var commandService =
        this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
      if (commandService != null)
      {
        var menuCommandId = new CommandID(Constants.CommandSet, Constants.CTestCommandId);
        var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandId);
        commandService.AddCommand(menuItem);
      }
    }

    public static CTestCommand Instance { get; private set; }

    private IServiceProvider ServiceProvider
    {
      get { return this._package; }
    }

    public static void Initialize(Package package)
    {
      Instance = new CTestCommand(package);
    }

    private void MenuItemCallback(object sender, EventArgs e)
    {
      var ctestOptionsType = typeof(CTestAdapterOptionPage);
      this._package.ShowOptionPage(ctestOptionsType);
    }
  }
}
