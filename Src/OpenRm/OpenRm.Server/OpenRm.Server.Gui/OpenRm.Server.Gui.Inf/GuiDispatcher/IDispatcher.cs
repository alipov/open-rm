using System;
using System.Windows.Threading;

namespace OpenRm.Server.Gui.Inf.GuiDispatcher
{
    public interface IDispatcher
    {
        /// <summary>
        /// Dispatches the specified action to the thread.
        /// </summary>
        /// <param name="actionToInvoke">The action to invoke.</param>
        void Dispatch(Action actionToInvoke);

        void Dispatch(DispatcherPriority priority, Action actionToInvoke);

        /// <summary>
        /// Checks whether the thread invoking the method
        /// </summary>
        /// <returns></returns>
        bool DispatchRequired();
    }
}
