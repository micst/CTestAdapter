using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace CTestAdapter.Events
{
  public class FolderEventArgs : EventArgs
  {
    private readonly string _folderPath;

    public FolderEventArgs(string path)
    {
      this._folderPath = path;
    }

    public string FolderPath
    {
      get { return this._folderPath; }
    }
  }

  [ComVisible(true)]
  public sealed class FolderEventListener : IVsSolutionEvents, IVsSolutionEvents7
  {
    public delegate void FolderEventArgsHandler(object sender,
      FolderEventArgs args);

    private uint _solutionCookie = VSConstants.VSCOOKIE_NIL;
    private IVsSolution _solution;

    public event FolderEventArgsHandler SolutionLoaded;
    public event Action SolutionUnloaded;

    public FolderEventListener(IServiceProvider serviceProvider)
    {
      if (serviceProvider == null)
      {
        serviceProvider = Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider;
      }
      if (serviceProvider == null)
      {
        return;
      }
      this._solution = (IVsSolution)serviceProvider.GetService(typeof(IVsSolution));
      // start listening for events
      var hr = this._solution.AdviseSolutionEvents(this, out this._solutionCookie);
      ErrorHandler.ThrowOnFailure(hr); // do nothing if this fails
    }

    #region IVsSolutionEvents7

    public void OnAfterOpenFolder(string folderPath)
    {
      if (this.SolutionLoaded == null)
      {
        return;
      }
      this.SolutionLoaded(this, new FolderEventArgs(folderPath));
    }

    public void OnAfterCloseFolder(string folderPath)
    {
      if (this.SolutionUnloaded == null)
      {
        return;
      }
      this.SolutionUnloaded();
    }

    public void OnBeforeCloseFolder(string folderPath)
    {}

    public void OnQueryCloseFolder(string folderPath, ref int pfCancel)
    {}

    public void OnAfterLoadAllDeferredProjects()
    {}

    #endregion

    public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
    {
      return VSConstants.S_OK;
    }

    public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
    {
      return VSConstants.S_OK;
    }

    public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
    {
      return VSConstants.S_OK;
    }

    public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
    {
      return VSConstants.S_OK;
    }

    public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
    {
      return VSConstants.S_OK;
    }

    public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
    {
      return VSConstants.S_OK;
    }

    public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
    {
      return VSConstants.S_OK;
    }

    public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
    {
      return VSConstants.S_OK;
    }

    public int OnBeforeCloseSolution(object pUnkReserved)
    {
      return VSConstants.S_OK;
    }

    public int OnAfterCloseSolution(object pUnkReserved)
    {
      return VSConstants.S_OK;
    }
  }
}
