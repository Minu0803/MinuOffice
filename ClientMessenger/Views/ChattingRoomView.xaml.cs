using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ClientMessenger.Views
{
    public partial class ChattingRoomView : UserControl
    {
        private bool _isLoadingOldMessages = false;

        public ChattingRoomView()
        {
            InitializeComponent();

            this.Loaded += ChattingRoomView_Loaded;
            this.Unloaded += ChattingRoomView_Unloaded;
        }

        private void ChattingRoomView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.ChattingRoomViewModel vm && vm.MessageList != null)
            {
                vm.MessageList.CollectionChanged += Messages_CollectionChanged;
            }
        }

        private void ChattingRoomView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.ChattingRoomViewModel vm && vm.MessageList != null)
            {
                vm.Cleanup();
                vm.MessageList.CollectionChanged -= Messages_CollectionChanged;
            }
        }

        private void MessagesListBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalOffset == 0 && e.ExtentHeightChange == 0)
            {
                // 스크롤이 맨 위에 도달했을 때 과거 메시지 불러오기
                if (DataContext is ViewModels.ChattingRoomViewModel vm)
                {
                    _isLoadingOldMessages = true;  // 이전 메시지 로딩 중임을 표시
                    vm.LoadMoreMessages();
                }
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

        // 메시지가 추가될 때마다 자동 스크롤, 단 예전 메시지 불러올 땐 제외
        private void Messages_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Reset)
            {
                // 과거 메시지 로딩 중이면 스크롤 내리지 않음
                if (_isLoadingOldMessages)
                {
                    _isLoadingOldMessages = false;
                    return;
                }

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var scrollViewer = FindScrollViewer(MessagesListBox);
                    scrollViewer?.ScrollToEnd();
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }
    }
}
