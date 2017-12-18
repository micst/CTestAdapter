using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace CTestAdapter.Events
{
  public sealed class SolutionEventListener : IVsSolutionEvents //, IDisposable
  {
    private bool _loaded = false;
    private uint _solutionCookie = VSConstants.VSCOOKIE_NIL;
    private IVsSolution _solution;

    public event Action SolutionLoaded;
    public event Action SolutionUnloaded;

    public SolutionEventListener(IServiceProvider serviceProvider)
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
      // start listening for solution events
      var hr = this._solution.AdviseSolutionEvents(this, out this._solutionCookie);
      ErrorHandler.ThrowOnFailure(hr); // do nothing if this fails
    }

    #region IVsSolutionEvents Members

    /**
     * 
     * after methods
     * 
     */

    int IVsSolutionEvents.OnAfterCloseSolution(object pUnkReserved)
    {
      if (null == this.SolutionUnloaded || !this._loaded)
      {
        return VSConstants.S_OK;
      }
      this._loaded = false;
      this.SolutionUnloaded();
      return VSConstants.S_OK;
    }

    int IVsSolutionEvents.OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
    {
      if (null == this.SolutionLoaded || this._loaded)
      {
        return VSConstants.S_OK;
      }
      this._loaded = true;
      this.SolutionLoaded();
      return VSConstants.S_OK;
    }

    public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
    {
      return VSConstants.S_OK;
    }

    public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
    {
      return VSConstants.S_OK;
    }

    /**
     * 
     * before methods
     * 
     */

    int IVsSolutionEvents.OnBeforeCloseSolution(object pUnkReserved)
    {
      if (null == this.SolutionUnloaded || !this._loaded)
      {
        return VSConstants.S_OK;
      }
      this._loaded = false;
      this.SolutionUnloaded();
      return VSConstants.S_OK;
    }

    int IVsSolutionEvents.OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
    {
      return VSConstants.S_OK;
    }

    public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
    {
      return VSConstants.S_OK;
    }

    /**
     * 
     * query methods
     * 
     */

    int IVsSolutionEvents.OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
    {
      return VSConstants.S_OK;
    }

    int IVsSolutionEvents.OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
    {
      return VSConstants.S_OK;
    }

    public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
    {
      return VSConstants.S_OK;
    }

    #endregion
  }
}
