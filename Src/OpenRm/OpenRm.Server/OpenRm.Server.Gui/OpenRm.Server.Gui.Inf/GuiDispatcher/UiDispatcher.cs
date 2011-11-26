using System;
using System.Windows.Threading;

namespace OpenRm.Server.Gui.Inf.GuiDispatcher
{
    public class UiDispatcher : IDispatcher
    {
        private readonly Dispatcher _dispatcher;

        public UiDispatcher()
            : this(Dispatcher.CurrentDispatcher) { }

        public UiDispatcher(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public void Dispatch(DispatcherPriority priority, Action actionToInvoke)
        {
            if (!DispatchRequired())
            {
                actionToInvoke();
            }
            else
            {
                _dispatcher.Invoke(priority, actionToInvoke);
            }
        }

        public void Dispatch(Action actionToInvoke)
        {
            Dispatch(DispatcherPriority.Normal, actionToInvoke);
        }

        public bool DispatchRequired()
        {
            return !_dispatcher.CheckAccess();
        }
    }
}
