using System;
using System.Windows.Input;

namespace ServerMessenger.Util
{
    public class RelayCommand : ICommand
    {
        private Action<object> execute;
        private Func<object, bool> canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged; // add
        public bool CanExecute(object parameter)
        {
            return this.canExecute == null || this.canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            this.execute(parameter);
        }

        //public void RaiseCanExecuteChanged() // CanExecute 조건 재 확인용
        //{
        //    CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        //}
    }
}
