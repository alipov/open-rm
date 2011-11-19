﻿using OpenRm.Server.Gui.Inf.Api.Mvvm;
using OpenRm.Server.Gui.Modules.Monitor.Api.ViewModels;

namespace OpenRm.Server.Gui.Modules.Monitor.Api.Views
{
    public interface IToolbarView : IView
    {
        IToolbarViewModel ViewModel { get; }
    }
}
