﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Windows.Input;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using OpenRm.Common.Entities;
using OpenRm.Common.Entities.Network;
using OpenRm.Common.Entities.Network.Messages;
using OpenRm.Server.Gui.Modules.Monitor.Api.Services;
using OpenRm.Server.Gui.Modules.Monitor.Api.ViewModels;
using OpenRm.Server.Gui.Modules.Monitor.Models;

namespace OpenRm.Server.Gui.Modules.Monitor.ViewModels
{
    public class AgentsRibbonTabViewModel : IAgentsRibbonTabViewModel, INotifyPropertyChanged
    {
        private readonly IAgentDataService _dataService;
        private readonly IUnityContainer _container;
        
        public AgentsRibbonTabViewModel(IUnityContainer container, IAgentDataService dataService)
        {
            _container = container;
            _dataService = dataService;

            ConnectCommand = new DelegateCommand(ConnectToServer);
            IsConnectEnabled = true;
            RefreshAgentsCommand = new DelegateCommand(RefreshAgentsList);
            InstalledProgramsCommand = new DelegateCommand(ListInstalledPrograms);
            LockSessionCommand = new DelegateCommand(LockSession);
            RestartCommand = new DelegateCommand(Restart);
            ShutDownCommand = new DelegateCommand(ShutDown);
            RemoteControlCommand = new DelegateCommand(RemoteControl);
        }

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
        
        public ICommand ConnectCommand { get; private set; }
        public ICommand RefreshAgentsCommand { get; private set; }
        public ICommand InstalledProgramsCommand { get; private set; }
        public ICommand LockSessionCommand { get; private set; }
        public ICommand RestartCommand { get; private set; }
        public ICommand ShutDownCommand { get; private set; }
        public ICommand RemoteControlCommand { get; private set; }

        private bool _isConnectEnabled;
        public bool IsConnectEnabled
        {
            get { return _isConnectEnabled; }
            private set
            {
                if (value != _isConnectEnabled)
                {
                    _isConnectEnabled = value;
                    NotifyPropertyChanged("IsConnectEnabled");
                }
            }
        }

        private void ConnectToServer()
        {
            var messageClient = _container.Resolve<IMessageClient>();
            messageClient.Connect(
                new IPEndPoint(IPAddress.Loopback, 3777),  OnConnectToServerCompleted);
        }

        private void ListInstalledPrograms()
        {
            var proxy = _container.Resolve<IMessageProxyInstance>(); 

            var installedProgramsMessage = new RequestMessage()
                                               {
                                                   Request = new InstalledProgramsRequest(),
                                                   AgentId = CurrentEntity.ID
                                               };
            proxy.Send(installedProgramsMessage, OnListInstalledProgramsCompleted);
        }

        private void OnListInstalledProgramsCompleted(CustomEventArgs args)
        {
            if(args.Status == SocketError.Success)
            {
                var message = (ResponseMessage) args.Result;
                var response = (InstalledProgramsResponse) message.Response;

                CurrentEntity.Log.Add(response.ToString());
            }
        }

        private void OnConnectToServerCompleted(CustomEventArgs args)
        {
            IsConnectEnabled = false;
        }

        private void RefreshAgentsList()
        {
            var listAgentsMessage = new RequestMessage()
                                     {
                                         Request = new ListAgentsRequest()
                                     };

            var proxy = _container.Resolve<IMessageProxyInstance>();
            proxy.Send(listAgentsMessage, OnRefreshAgentsListCompleted);
        }

        private void OnRefreshAgentsListCompleted(CustomEventArgs args)
        {
            var message = (ResponseMessage) args.Result;
            var response = (ListAgentsResponse) message.Response;

            _dataService.SetAgents(WrapAgents(response.Agents));
        }

        private void LockSession()
        {
            var lockSessionMessage = new RequestMessage()
                                         {
                                             Request = new LockSessionRequest(),
                                             AgentId = CurrentEntity.ID
                                         };

            var proxy = _container.Resolve<IMessageProxyInstance>();
            proxy.Send(lockSessionMessage, OnLockSessionCompleted);
        }

        private void OnLockSessionCompleted(CustomEventArgs args)
        {
            if (args.Status == SocketError.Success)
            {
                var message = (ResponseMessage)args.Result;
                var response = (LockSessionResponse)message.Response;
                
                CurrentEntity.Log.Add(response.ToString());
            }
        }

        private void Restart()
        {
            var restartMessage = new RequestMessage()
            {
                Request = new RestartRequest(),
                AgentId = CurrentEntity.ID
            };

            var proxy = _container.Resolve<IMessageProxyInstance>();
            proxy.Send(restartMessage, OnRestartCompleted);
        }

        private void OnRestartCompleted(CustomEventArgs args)
        {
            if (args.Status == SocketError.Success)
            {
                var message = (ResponseMessage)args.Result;
                var response = (RestartResponse)message.Response;

                CurrentEntity.Log.Add(response.ToString());
            }
        }

        private void ShutDown()
        {
            var shutdownMessage = new RequestMessage()
            {
                Request = new ShutdownRequest(),
                AgentId = CurrentEntity.ID
            };

            var proxy = _container.Resolve<IMessageProxyInstance>();
            proxy.Send(shutdownMessage, OnShutDownCompleted);
        }

        private void OnShutDownCompleted(CustomEventArgs args)
        {
            if (args.Status == SocketError.Success)
            {
                var message = (ResponseMessage)args.Result;
                var response = (ShutdownResponse)message.Response;

                CurrentEntity.Log.Add(response.ToString());
            }
        }


        private void RemoteControl()
        {
            string consoleIp = "";  //TODO:  !!!!!!
            var scRequest = new RemoteControlRequest(consoleIp, 5555);

            //launch VNC viewer on local computer
            scRequest.StartVncListerner();

            var remoteControlMessage = new RequestMessage()
            {
                Request = scRequest,
                AgentId = CurrentEntity.ID
            };

            var proxy = _container.Resolve<IMessageProxyInstance>();
            proxy.Send(remoteControlMessage, OnRestartCompleted);
        }

        private void OnRemoteControlCompleted(CustomEventArgs args)
        {
            if (args.Status == SocketError.Success)
            {
                var message = (ResponseMessage)args.Result;
                var response = (RemoteControlResponse)message.Response;
                if (response.ErrorMessage != "")
                    CurrentEntity.Log.Add(response.ErrorMessage);
                else
                    CurrentEntity.Log.Add("Successfully launched Remote Contol on remote computer.");
            }
        }


        private List<AgentWrapper> WrapAgents(IEnumerable<Agent> agents)
        {
            var agentWrappers = new List<AgentWrapper>();
            foreach (var agent in agents)
            {
                var wrapperAgent = new AgentWrapper()
                    {
                        Data = agent.Data,
                        ID = agent.ID,
                        Name = agent.Name,
                        Status = agent.Status
                    };
                agentWrappers.Add(wrapperAgent);
            }

            return agentWrappers;
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
