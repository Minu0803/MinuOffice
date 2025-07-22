using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ServerMessenger
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var vm = new ViewModels.ServerViewModel();
            this.DataContext = vm;

            if (vm.ServerLogs is INotifyCollectionChanged observable)
            {
                observable.CollectionChanged += ServerLogs_CollectionChanged;
            }
        }

        private void ServerLogs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Reset)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var scrollViewer = FindScrollViewer(ServerLogsListBox);
                    scrollViewer?.ScrollToEnd();
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private ScrollViewer FindScrollViewer(DependencyObject d)
        {
            if (d is ScrollViewer sv) return sv;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++)
            {
                var child = VisualTreeHelper.GetChild(d, i);
                var result = FindScrollViewer(child);
                if (result != null) return result;
            }
            return null;
        }
    }
}
