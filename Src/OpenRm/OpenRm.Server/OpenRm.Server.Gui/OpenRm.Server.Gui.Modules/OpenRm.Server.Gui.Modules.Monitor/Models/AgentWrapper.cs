﻿using System;
using System.Collections.Generic;
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


        private IntDateTimeObject _firstPerformanceCpu;
        private IntDateTimeObject _firstPerformanceRam;
        private IntDateTimeObject _firstPerformanceFreeDisc;
        private IntDateTimeObject _firstPerformanceDiscQueue;

        public void AppendPerformanceCpu(IntDateTimeObject item)
        {
            if (_firstPerformanceCpu == null)
            {
                _firstPerformanceCpu = item;
            }
            else if (PerformanceCpu == null)
            {
                var collection = new ObservableCollection<IntDateTimeObject>()
                                     {
                                         _firstPerformanceCpu,
                                         item
                                     };
                PerformanceCpu = collection;
            }
            else
            {
                //if (PerformanceCpu.Count > 10)
                //{
                //    PerformanceCpu.RemoveAt(0);
                //}

                PerformanceCpu.Add(item);
            }
        }

        public void AppendPerformanceRam(IntDateTimeObject item)
        {
            if (_firstPerformanceRam == null)
            {
                _firstPerformanceRam = item;
            }
            else if (PerformanceRam == null)
            {
                var collection = new ObservableCollection<IntDateTimeObject>()
                                     {
                                         _firstPerformanceRam,
                                         item
                                     };
                PerformanceRam = collection;
            }
            else
            {
                //if (PerformanceRam.Count > 10)
                //{
                //    PerformanceCpu.RemoveAt(0);
                //}
                PerformanceRam.Add(item);
            }
        }

        public void AppendPerformanceFreeDisc(IntDateTimeObject item)
        {
            if (_firstPerformanceFreeDisc == null)
            {
                _firstPerformanceFreeDisc = item;
            }
            else if (PerformanceFreeDisc == null)
            {
                var collection = new ObservableCollection<IntDateTimeObject>()
                                     {
                                         _firstPerformanceFreeDisc,
                                         item
                                     };
                PerformanceFreeDisc = collection;
            }
            else
            {
                //if (PerformanceFreeDisc.Count > 10)
                //{
                //    PerformanceCpu.RemoveAt(0);
                //}
                PerformanceFreeDisc.Add(item);
            }
        }

        public void AppendPerformanceDiscQueue(IntDateTimeObject item)
        {
            if (_firstPerformanceDiscQueue == null)
            {
                _firstPerformanceDiscQueue = item;
            }
            else if (PerformanceDiscQueue == null)
            {
                var collection = new ObservableCollection<IntDateTimeObject>()
                                     {
                                         _firstPerformanceDiscQueue,
                                         item
                                     };
                PerformanceDiscQueue = collection;
            }
            else
            {
                //if (PerformanceDiscQueue.Count > 10)
                //{
                //    PerformanceCpu.RemoveAt(0);
                //}
                PerformanceDiscQueue.Add(item);
            }
        }

        public AgentWrapper()
        {
            //TODO: maybe initialize all variables?
            Data = new ClientData();
            Log = new ObservableCollection<string>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private string _runProcessCommand;
        public string RunProcessCommand
        {
            get { return _runProcessCommand; }
            set
            {
                if (value != _runProcessCommand)
                {
                    _runProcessCommand = value;
                    NotifyPropertyChanged("RunProcessCommand");
                }
            }
        }

        private string _runProcessArgs;
        public string RunProcessArgs
        {
            get { return _runProcessArgs; }
            set
            {
                if (value != _runProcessArgs)
                {
                    _runProcessArgs = value;
                    NotifyPropertyChanged("RunProcessArgs");
                }
            }
        }

        private string _runProcessWorkingDir;
        public string RunProcessWorkingDir
        {
            get { return _runProcessWorkingDir; }
            set
            {
                if (value != _runProcessWorkingDir)
                {
                    _runProcessWorkingDir = value;
                    NotifyPropertyChanged("RunProcessWorkingDir");
                }
            }
        }

        private int _runProcessDelay;
        public int RunProcessDelay
        {
            get { return _runProcessDelay; }
            set
            {
                if (value != _runProcessDelay)
                {
                    _runProcessDelay = value;
                    NotifyPropertyChanged("RunProcessDelay");
                }
            }
        }

        private bool _runProcessIsHidden;
        public bool RunProcessIsHidden
        {
            get { return _runProcessIsHidden; }
            set
            {
                if (value != _runProcessIsHidden)
                {
                    _runProcessIsHidden = value;
                    NotifyPropertyChanged("RunProcessIsHidden");
                }
            }
        }

        private bool _runProcessIsWait;
        public bool RunProcessIsWait
        {
            get { return _runProcessIsWait; }
            set
            {
                if (value != _runProcessIsWait)
                {
                    _runProcessIsWait = value;
                    NotifyPropertyChanged("RunProcessIsWait");
                }
            }
        }
    }
}
