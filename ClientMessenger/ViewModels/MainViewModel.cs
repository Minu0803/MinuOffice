using ClientMessenger.Common;
using ClientMessenger.Models;
using ClientMessenger.Network;
using ClientMessenger.Views;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ClientMessenger.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private object _currentVm;

        public SocketClientManager _socketClientManager;
        public MainChatViewModel MainChatViewModel { get; set; }
        public UserListViewModel UserListViewModel { get; set; }
        public SignUpViewModel SignUpViewModel;

        public object CurrentVm
        {
            get { return _currentVm; }
            set
            {
                _currentVm = value;
                OnPropertyChanged();
            }
        }

        public MainViewModel()
        {
            _socketClientManager = new SocketClientManager(); // 콜백 없이 먼저 생성

            SignUpViewModel = new SignUpViewModel(_socketClientManager);
            UserListViewModel = new UserListViewModel(_socketClientManager);
            MainChatViewModel = new MainChatViewModel(_socketClientManager, UserListViewModel);

            UserListViewModel.RoomSender = MainChatViewModel; // UserListViewModel에서 방을 선택할 때 MainChatViewModel로 전달

            _socketClientManager.OnLoginSuccess = (response) => // 로그인 성공 시 호출되는 콜백 등록
            {
                MainChatViewModel.ChatRoomList = new ObservableCollection<RoomModel>(response.ChatList);
                UserListViewModel.UserList = new ObservableCollection<UserModel>(response.UserList);

                NavigateToMainChatView();
            };            

            _currentVm = new LoginViewModel(_socketClientManager);
        }

        // 다른 뷰로 전환
        public void NavigateToMainChatView()
        {
            CurrentVm = MainChatViewModel;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
