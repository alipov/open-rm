using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using OpenRm.Server.Gui.Modules.Monitor.Api.ViewModels;
using OpenRm.Server.Gui.Modules.Monitor.Api.Views;
using OpenRm.Server.Gui.Modules.Monitor.Models;

namespace OpenRm.Server.Gui.Modules.Monitor.Views
{
    public partial class AgentPerformanceView : UserControl, IAgentPerformanceView
    {
        public AgentPerformanceView(IAgentPerformanceViewModel viewModel)
        {
            InitializeComponent();

            this.DataContext = viewModel;

            // This view is displayed in a region with a region context.
            // The region context is defined as the currently selected agent
            // When the region context is changed, we need to propogate the
            // change to this view's view model.
            RegionContext.GetObservableContext(this).PropertyChanged += (s, e)
                                        =>
                                        viewModel.CurrentEntity =
                                        RegionContext.GetObservableContext(this).Value
                                        as AgentWrapper;
        }

        public IAgentPerformanceViewModel ViewModel
        {
            get { return DataContext as IAgentPerformanceViewModel; }
        }




        const int N = 1000;         //maximum length
        private double[] xData;
        private double[] yData;
        private LineGraph graph;
        EnumerableDataSource<double> animatedDataSource = null;

        public void AddCpuUageValue(int cpuValue)
        {
            double[] xDataPrev = xData;
            double[] yDataPrev = yData;
            xData = new double[xDataPrev.Length + 1];
            yData = new double[xDataPrev.Length + 1];
            Array.Copy(xDataPrev, xData, xDataPrev.Length);
            Array.Copy(yDataPrev, yData, yDataPrev.Length);

            //add new values
            xData[xData.Length - 1] = xData[xData.Length - 2] + 1;  //add 1 to last value
            yData[yData.Length - 1] = cpuValue;

            graph.Description = new PenDescription(String.Format("CPU - {0}%", cpuValue));


            // update graph
            animatedDataSource.RaiseDataChanged();
        }


        void AgentCpuUageView_Loaded(object sender, RoutedEventArgs e)
        {
            //xData = new double[1];
            //yData = new double[1];

            var xSrc = new EnumerableDataSource<double>(xData);
            xSrc.SetXMapping(x => x);
            animatedDataSource = new EnumerableDataSource<double>(yData);
            animatedDataSource.SetYMapping(y => y);

            //// Adding graph to plotter
            //graph = CpuPlotter.AddLineGraph(new CompositeDataSource(xSrc, animatedDataSource),
            //    new Pen(Brushes.Goldenrod, 2),
            //    new PenDescription("cpu %"));

            //// Force evertyhing plotted to be visible
            //CpuPlotter.FitToView();
        }
    }
}
