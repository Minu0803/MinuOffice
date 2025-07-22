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
    /// LoginView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();            
        }

        /// <summary>
        /// PasswordBox에 사용자가 입력한 비밀번호를 ViewModel의 Password 속성에 복사
        /// </summary>
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.Password = ((PasswordBox)sender).Password; // sender는 object 타입이라 PasswordBox로 형변환
            }
        }
    }
}
