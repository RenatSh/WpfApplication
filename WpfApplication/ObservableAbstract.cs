using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WpfApplication
{
    public abstract class ObservableAbstract : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string callerProperty = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(callerProperty));
        }
    }
}
