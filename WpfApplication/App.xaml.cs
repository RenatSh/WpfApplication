using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApplication
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var model = new DealCaptureModel(equityNameSeed:1, bondNameSeed: 1);
            var viewModel = new DealCaptureViewModel(model, bondTolerance: 100*1000m, equityTolerance: 200*1000m);
            var window = new MainWindow { DataContext = viewModel };
            window.Show();
        }
    }
}
