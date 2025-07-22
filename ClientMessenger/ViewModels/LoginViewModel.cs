using ClientMessenger.Common;
using ClientMessenger.DTO;
using ClientMessenger.Network;
using ClientMessenger.Util;
using ClientMessenger.Views;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace ClientMessenger.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {        
        private string _userId;
        private string _password;
        private SocketClientManager _socketClientManager;

        private RelayCommand _loginCommand;
        private RelayCommand _goToSignUpCommand;


        public string UserId
        {
            get => _userId;
            set
            {
                _userId = value;
                OnPropertyChanged();
            }
        }
        
        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
            }
        }             
        
        public ICommand LoginCommand
        {
            get
            {
                if (_loginCommand == null)
                {
                    _loginCommand = new RelayCommand(ExecuteLogin);
                }
                return _loginCommand;
            }
        }
        
        public ICommand GoToSignUpCommand
        {
            get
            {
                if (_goToSignUpCommand == null)
                {
                    _goToSignUpCommand = new RelayCommand(ExecuteGoToSignUp);
                }
                return _goToSignUpCommand;
            }
        }
        public LoginViewModel(SocketClientManager socketClientManager)
        {
            _socketClientManager = socketClientManager;
        }

        private void ExecuteLogin(object obj)
        {
            // 서버 연결 및 연결 확인
            if (!_socketClientManager.Connect())
            {
                MessageBox.Show("서버에 연결할 수 없습니다.\n네트워크 상태를 확인하세요.", "연결 실패", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(UserId) || string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show("아이디와 비밀번호를 모두 입력해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var dto = new LoginRequestDto
                {
                    UserId = UserId,
                    Password = HashHelper.ComputeSha256Hash(Password)
                };

                string json = JsonConvert.SerializeObject(dto);
                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

                _socketClientManager.CurrentSession.Send(PacketType.LoginRequest, jsonBytes);
            }
            catch (Exception ex)
            {
                MessageBox.Show("로그인 요청 중 오류가 발생했습니다.\n" + ex.Message, "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteGoToSignUp(object obj)
        {
            if (_socketClientManager.Connect())
            {
                var signUpWindow = new SignUpView(_socketClientManager);
                signUpWindow.Owner = Application.Current.MainWindow;
                signUpWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("서버에 연결할 수 없습니다.\n네트워크 상태를 확인하세요.", "연결 실패", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
