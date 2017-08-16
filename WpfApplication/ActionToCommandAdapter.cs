using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WpfApplication
{
    public class ActionToCommandAdapter : ICommand
    {
        private readonly Action action;

        public ActionToCommandAdapter(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            this.action = action;
        }

        public void Execute(object parameter)
        {
            this.action();
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}
