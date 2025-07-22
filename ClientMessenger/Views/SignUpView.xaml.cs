using ClientMessenger.Network;
using ClientMessenger.ViewModels;
using System;
using System.Collections.Generic;
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
    /// <summary>
    /// SignUpView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SignUpView : Window
    {
        public SignUpView(SocketClientManager socketClientManager)
        {
            InitializeComponent();

            var viewModel = new SignUpViewModel(socketClientManager); 
            viewModel.CloseAction = new Action(this.Close);           
            DataContext = viewModel;                                  

            this.Unloaded += SignUpView_Unloaded; // UI 트리에서 제거되었을 때 발생(현재는 창 닫기로)
        }

        /// <summary>
        /// PasswordBox에 사용자가 입력한 비밀번호를 ViewModel의 Password 속성에 복사
        /// </summary>
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is SignUpViewModel vm)
            {
                vm.Password = ((PasswordBox)sender).Password; // sender는 object 타입이라 PasswordBox로 형변환
            }
        }
        /// <summary>
        /// 위와 동일하지만 비밀번호 확인용
        /// </summary>
        private void PasswordConfirmBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is SignUpViewModel vm)
            {
                vm.PasswordConfirm = ((PasswordBox)sender).Password; 
            }
        }

        private void SignUpView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is SignUpViewModel vm)
            {
                vm.Cleanup();
            }
        }

    }
}
