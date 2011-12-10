using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.DataSources;

namespace OpenRm.Server.Gui.Modules.Monitor.Views
{

    public partial class AgentCpuUageView : Window
    {
        const int N = 1000;         //maximum length
        private double[] xData;
        private double[] yData;
        private LineGraph graph;

        EnumerableDataSource<double> animatedDataSource = null;


        public AgentCpuUageView()
        {
            InitializeComponent();
        }

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


                
            EnumerableDataSource<double> xSrc = new EnumerableDataSource<double>(xData);
            xSrc.SetXMapping(x => x);
            animatedDataSource = new EnumerableDataSource<double>(yData);
            animatedDataSource.SetYMapping(y => y);

            // Adding graph to plotter
            graph = CpuPlotter.AddLineGraph(new CompositeDataSource(xSrc, animatedDataSource),
                new Pen(Brushes.Goldenrod, 2),
                new PenDescription("cpu %"));

            // Force evertyhing plotted to be visible
            CpuPlotter.FitToView();
        }

    }
}
