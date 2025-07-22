using ClientMessenger.Common;
using ClientMessenger.DTO;
using ClientMessenger.Models;
using ClientMessenger.Network;
using ClientMessenger.Util;
using ClientMessenger.Views;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClientMessenger.ViewModels
{
    public class MainChatViewModel : INotifyPropertyChanged, IRoomSender
    {
        private ObservableCollection<RoomModel> _chatRoomList;
        private RoomModel _selectedChatRoom;
        private ChattingRoomViewModel _roomVm;
        private UserListViewModel _userVm;
        private ICommand _openUserListCommand;
        private SocketClientManager _socketClientManager;
        public ObservableCollection<RoomModel> ChatRoomList
        {
            get => _chatRoomList;
            set
            {
                _chatRoomList = value;
                OnPropertyChanged();
            }
        }

        public RoomModel SelectedChatRoom
        {
            get => _selectedChatRoom;
            set
            {
                _selectedChatRoom = value;
                OnPropertyChanged();

                if (_selectedChatRoom != null)
                {
                    // 선택된 방에 맞는 ChattingRoomView와 ViewModel 생성 
                    var vm = new ChattingRoomViewModel(_selectedChatRoom, _socketClientManager);
                    vm.RequestMessagesForRoom(_selectedChatRoom.RoomId);
                }
            }
        }

        public ChattingRoomViewModel RoomVm
        {
            get { return _roomVm; }
            set
            {
                _roomVm = value;
                OnPropertyChanged();
            }
        }

        public ICommand OpenUserListCommand
        {
            get
            {
                if (_openUserListCommand == null)
                {
                    _openUserListCommand = new RelayCommand(ExecuteOpenUserList);
                }
                return _openUserListCommand;
            }
        }
        public MainChatViewModel(SocketClientManager socketClientManager, UserListViewModel userVm)
        {
            _socketClientManager = socketClientManager;
            ChatRoomList = new ObservableCollection<RoomModel>();
            _userVm = userVm;
        }

        private void ExecuteOpenUserList(object obj)
        {
            var window = new UserListView();
            window.DataContext = _userVm; // UserListViewModel을 DataContext로 설정
            window.Topmost = true; 
            window.ShowDialog();
        }        

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public void RoomCreated(RoomModel room)
        {
            // 방 추가
            this.ChatRoomList.Add(room);
            // 선택 방 지정
            this.SelectedChatRoom = room;
        }
    }
}
