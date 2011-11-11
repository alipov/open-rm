using System.ComponentModel;
using OpenRm.Common.Entities;
using OpenRm.Server.Gui.Modules.Monitor.Api.ViewModels;

namespace OpenRm.Server.Gui.Modules.Monitor.ViewModels
{
    public class AgentSummaryViewModel : IAgentSummaryViewModel, INotifyPropertyChanged
    {
        private Agent _currentEntity;
        public Agent CurrentEntity
        {
            get { return _currentEntity; }
            set
            {
                if (_currentEntity != value)
                {
                    _currentEntity = value;
                    NotifyPropertyChanged("CurrentEntity");
                }
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
