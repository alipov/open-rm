using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Windows.Threading;
using Microsoft.Practices.Unity;
using OpenRm.Common.Entities;
using OpenRm.Common.Entities.Network;
using OpenRm.Common.Entities.Network.Messages;
using OpenRm.Server.Gui.Modules.Monitor.Api.ViewModels;
using OpenRm.Server.Gui.Modules.Monitor.Models;

namespace OpenRm.Server.Gui.Modules.Monitor.ViewModels
{
    public class AgentSummaryViewModel : IAgentSummaryViewModel, INotifyPropertyChanged
    {
        public AgentSummaryViewModel(IUnityContainer container)
        {
            _container = container;
        }

        private readonly IUnityContainer _container;

        private AgentWrapper _currentEntity;
        public AgentWrapper CurrentEntity
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

        private DispatcherTimer _timer;

        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get { return _selectedTabIndex; }
            set
            {
                _selectedTabIndex = value;
                if (value == 0)
                {
                    if (_timer != null && _timer.IsEnabled)
                    {
                        _timer.Stop();
                    }
                }
                else if (value == 1)
                {
                    if (_timer == null)
                    { 
                        _timer = new DispatcherTimer();
                        _timer.Interval = TimeSpan.FromSeconds(1);
                        _timer.Tick += GetPerformanceData;
                    }
                    _timer.Start();
                }
            }
        }


        private void GetPerformanceData(object sender, EventArgs args)
        {
            var proxy = _container.Resolve<IMessageProxyInstance>();

            var installedProgramsMessage = new RequestMessage()
            {
                Request = new PerfmonDataRequest(),
                AgentId = CurrentEntity.ID
            };
            proxy.Send(installedProgramsMessage, OnGetPerformanceDataCompleted);
        }

        private void OnGetPerformanceDataCompleted(CustomEventArgs args)
        {
            if(args.Status == SocketError.Success)
            {
                var message = (ResponseMessage)args.Result;
                var response = (PerfmonDataResponse)message.Response;

                CurrentEntity.PerformanceCpu.Add(new IntDateTimeObject()
                                            {
                                                Time = DateTime.Now,
                                                Value = response.CPUuse
                                            });
                CurrentEntity.PerformanceDiscQueue.Add(new IntDateTimeObject()
                                            {
                                                Time = DateTime.Now,
                                                Value = (int)response.DiskQueue
                                            });
                CurrentEntity.PerformanceFreeDisc.Add(new IntDateTimeObject()
                                            {
                                                Time = DateTime.Now,
                                                Value = response.DiskFree
                                            });
                CurrentEntity.PerformanceRam.Add(new IntDateTimeObject()
                                            {
                                                Time = DateTime.Now,
                                                Value = response.RAMfree
                                            });
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
