using ClientMessenger.ViewModels;
using System.Windows;

namespace ClientMessenger.Views
{
    /// <summary>
    /// UserListView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class UserListView : Window
    {
        public UserListView()
        {
            InitializeComponent();

            // ViewModel의 CloseAction 설정
            if (DataContext is UserListViewModel vm)
            {
                vm.CloseAction = Close;
            }

            this.Unloaded += UserListView_Unloaded; // 언로드 이벤트 등록
        }

        private void UserListView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is UserListViewModel vm)
            {
                vm.Cleanup(); // ViewModel의 이벤트 해제 처리
            }
        }
    }
}
