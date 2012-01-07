using System.Collections.ObjectModel;
using System.ComponentModel;
using OpenRm.Common.Entities;

namespace OpenRm.Server.Gui.Modules.Monitor.Models
{
    public class AgentWrapper : INotifyPropertyChanged
    {
        private int _id;
        public int ID
        {
            get { return _id; }
            set
            {
                if (value != _id)
                {
                    _id = value;
                    NotifyPropertyChanged("ID");
                }
            }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                if (value != _name)
                {
                    _name = value;
                    NotifyPropertyChanged("Name");
                }
            }
        }

        private int _status;
        public int Status
        {
            get { return _status; }
            set
            {
                if (value != _status)
                {
                    _status = value;
                    NotifyPropertyChanged("Status");
                }
            }
        }

        private ClientData _data;
        public ClientData Data
        {
            get { return _data; }
            set
            {
                if (value != _data)
                {
                    _data = value;
                    NotifyPropertyChanged("Data");
                }
            }
        }

        private ObservableCollection<string> _log;
        public ObservableCollection<string> Log
        {
            get { return _log; }
            set
            {
                if (value != _log)
                {
                    _log = value;
                    NotifyPropertyChanged("Log");
                }
            }
        }

        private ObservableCollection<IntDateTimeObject> _performanceCpu;
        public ObservableCollection<IntDateTimeObject> PerformanceCpu
        {
            get { return _performanceCpu; }
            set
            {
                if (value != _performanceCpu)
                {
                    _performanceCpu = value;
                    NotifyPropertyChanged("PerformanceCpu");
                }
            }
        }

        private ObservableCollection<IntDateTimeObject> _performanceRam;
        public ObservableCollection<IntDateTimeObject> PerformanceRam
        {
            get { return _performanceRam; }
            set
            {
                if (value != _performanceRam)
                {
                    _performanceRam = value;
                    NotifyPropertyChanged("PerformanceRam");
                }
            }
        }

        private ObservableCollection<IntDateTimeObject> _performanceFreeDisc;
        public ObservableCollection<IntDateTimeObject> PerformanceFreeDisc
        {
            get { return _performanceFreeDisc; }
            set
            {
                if (value != _performanceFreeDisc)
                {
                    _performanceFreeDisc = value;
                    NotifyPropertyChanged("PerformanceFreeDisc");
                }
            }
        }

        private ObservableCollection<IntDateTimeObject> _performanceDiscQueue;
        public ObservableCollection<IntDateTimeObject> PerformanceDiscQueue
        {
            get { return _performanceDiscQueue; }
            set
            {
                if (value != _performanceDiscQueue)
                {
                    _performanceDiscQueue = value;
                    NotifyPropertyChanged("PerformanceDiscQueue");
                }
            }
        }

        public AgentWrapper()
        {
            //TODO: maybe initialize all variables?
            Data = new ClientData();
            Log = new ObservableCollection<string>();
            PerformanceCpu = new ObservableCollection<IntDateTimeObject>();
            PerformanceDiscQueue = new ObservableCollection<IntDateTimeObject>();
            PerformanceFreeDisc = new ObservableCollection<IntDateTimeObject>();
            PerformanceRam = new ObservableCollection<IntDateTimeObject>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
