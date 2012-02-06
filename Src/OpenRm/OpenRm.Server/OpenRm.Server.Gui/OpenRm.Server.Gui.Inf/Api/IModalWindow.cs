namespace OpenRm.Server.Gui.Inf.Api
{
    interface IModalWindow
    {
        object DataContext { get; set; }
        bool? DialogResult { get; set; }
    }
}
