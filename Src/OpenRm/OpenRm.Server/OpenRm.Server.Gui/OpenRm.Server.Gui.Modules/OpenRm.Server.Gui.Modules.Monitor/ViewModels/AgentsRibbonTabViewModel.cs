using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Input;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using OpenRm.Common.Entities;
using OpenRm.Common.Entities.Network;
using OpenRm.Common.Entities.Network.Messages;
using OpenRm.Server.Gui.Modules.Monitor.Api.Services;
using OpenRm.Server.Gui.Modules.Monitor.Api.ViewModels;
using OpenRm.Server.Gui.Modules.Monitor.Models;
using OpenRm.Server.Gui.Modules.Monitor.Views;

namespace OpenRm.Server.Gui.Modules.Monitor.ViewModels
{
    public class AgentsRibbonTabViewModel : IAgentsRibbonTabViewModel, INotifyPropertyChanged
    {
        private readonly IAgentDataService _dataService;
        private readonly IUnityContainer _container;
        private string sourceIP = "";

        
        public AgentsRibbonTabViewModel(IUnityContainer container, IAgentDataService dataService)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += ExceptionHandler;

            _container = container;
            _dataService = dataService;

            ConnectCommand = new DelegateCommand(ConnectToServer);
            IsConnectEnabled = true;
            RefreshAgentsCommand = new DelegateCommand(RefreshAgentsList);
            InstalledProgramsCommand = new DelegateCommand(ListInstalledPrograms);
            LockSessionCommand = new DelegateCommand(LockSession);
            RestartCommand = new DelegateCommand(Restart);
            ShutDownCommand = new DelegateCommand(ShutDown);
            CommonCommand = new DelegateCommand(CommonExecute);
            SelectedComboBoxValue = "Ping";
            CommonCommandParameter = "gmail.com";
            RemoteControlCommand = new DelegateCommand(RemoteControl);
            WakeOnLanCommand = new DelegateCommand(WakeOnLan);
            RunProcessCommand = new DelegateCommand(RunProcess);

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
        public ICommand CommonCommand { get; private set; }
        public ICommand RemoteControlCommand { get; private set; }
        public ICommand WakeOnLanCommand { get; private set; }
        public ICommand RunProcessCommand { get; private set; }

        private string _selectedComboBoxValue;
        public string SelectedComboBoxValue
        {
            get { return _selectedComboBoxValue; }
            set
            {
                if (value != _selectedComboBoxValue)
                {
                    _selectedComboBoxValue = value;
                    NotifyPropertyChanged("SelectedComboBoxValue");
                }
            }
        }

        private string _commonCommandParameter;
        public string CommonCommandParameter
        {
            get { return _commonCommandParameter; }
            set
            {
                if (value != _commonCommandParameter)
                {
                    _commonCommandParameter = value;
                    NotifyPropertyChanged("CommonCommandParameter");
                }
            }
        }
        

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
            // Read configuration from file
            if (!SettingsManager.ReadConfigFile()) return;      //TODO: add some message box?

            // Initialize some global settings
            Logger.CreateLogFile("logs", SettingsManager.LogFilenamePattern);
            EncryptionAdapter.SetEncryption(SettingsManager.SecretKey);

            // Process to connect
            var messageClient = _container.Resolve<IMessageClient>();
            messageClient.Connect(SettingsManager.ServerEndPoint,  OnConnectToServerCompleted);
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
            if (args.LocalEndPoint != null)
            {
                IsConnectEnabled = false;
                sourceIP = args.LocalEndPoint.Address.ToString();    
            }
            else
            {
                MessageBox.Show("Cannot connect server. Please make sure the server is running.", "Oops", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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


        private void CommonExecute()
        {
            var message = new RequestMessage()
                                  {
                                      AgentId = CurrentEntity.ID
                                  };
            
            if (SelectedComboBoxValue == "Ping")
                message.Request = new PingRequest(0, CommonCommandParameter);
            else if(SelectedComboBoxValue == "TraceRoute")
                message.Request = new TraceRouteRequest(0, CommonCommandParameter);
            else
            {
                MessageBox.Show("Please choose valid option from drop-down list", "Oops", MessageBoxButton.OK,
                                MessageBoxImage.Exclamation);
                return;
            }
              
            var proxy = _container.Resolve<IMessageProxyInstance>();
            proxy.Send(message, OnCommonExecuteCompleted);
        }

        private void OnCommonExecuteCompleted(CustomEventArgs args)
        {
            if (args.Status == SocketError.Success)
            {
                var message = (ResponseMessage)args.Result;
                var response = (RunCommonResponse)message.Response;

                CurrentEntity.Log.Add(response.ToString());
            }
        }


        private void WakeOnLan()
        {
            var wakeOnLanMessage = new RequestMessage()
            {
                Request = new WakeOnLanRequest(),
                AgentId = CurrentEntity.ID
            };

            var proxy = _container.Resolve<IMessageProxyInstance>();
            proxy.Send(wakeOnLanMessage, OnWakeOnLanCompleted);
        }

        private void OnWakeOnLanCompleted(CustomEventArgs args)
        {
            if (args.Status == SocketError.Success)
            {
                var message = (ResponseMessage)args.Result;
                var response = (WakeOnLanResponse)message.Response;

                CurrentEntity.Log.Add(response.ToString());
            }
        }



        private void RemoteControl()
        {
            string consoleIp = sourceIP;
            var scRequest = new RemoteControlRequest(consoleIp, 5555);

            //launch VNC viewer on local computer
            scRequest.StartVncListerner();

            var remoteControlMessage = new RequestMessage()
            {
                Request = scRequest,
                AgentId = CurrentEntity.ID
            };

            var proxy = _container.Resolve<IMessageProxyInstance>();
            proxy.Send(remoteControlMessage, OnRemoteControlCompleted);
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


        private void RunProcess()
        {
            var prompt = new RemoteCommandDialogView();
            prompt.DataContext = CurrentEntity;
            //prompt.Owner = this;
            //prompt.DocumentMargin = this.documentTextBox.Margin;
            // Open the dialog box
            prompt.ShowDialog();

            if (prompt.DialogResult.HasValue && prompt.DialogResult.Value)
            {
                var wakeOnLanMessage = new RequestMessage()
                {
                    Request = new RunProcessRequest()
                                  {
                                      Args = CurrentEntity.RunProcessArgs,
                                      Cmd = CurrentEntity.RunProcessCommand,
                                      Delay = CurrentEntity.RunProcessDelay,
                                      Hidden = CurrentEntity.RunProcessIsHidden,
                                      WaitForCompletion = CurrentEntity.RunProcessIsWait,
                                      WorkDir = CurrentEntity.RunProcessWorkingDir
                                  },
                    AgentId = CurrentEntity.ID
                };

                var proxy = _container.Resolve<IMessageProxyInstance>();
                proxy.Send(wakeOnLanMessage, OnRunProcessCompleted);
            }
        }


        private void OnRunProcessCompleted(CustomEventArgs args)
        {
            if (args.Status == SocketError.Success)
            {
                var message = (ResponseMessage)args.Result;
                var response = (RunProcessResponse)message.Response;
                if (response.ErrorMessage != "")
                    CurrentEntity.Log.Add(response.ErrorMessage);
                else
                    CurrentEntity.Log.Add("Remote process was successfully executed on remote computer.");
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


        private void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            var e = (Exception)args.ExceptionObject;
            string errMsg = "Error occured: " + e.Message +
                            "\nPlease review log and restart Console if nesessary.";
            Logger.WriteStr(errMsg);
            MessageBox.Show(errMsg, "Oops", MessageBoxButton.OK, MessageBoxImage.Error);
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
