using ClientMessenger.Common;
using ClientMessenger.DTO;
using ClientMessenger.Models;
using ClientMessenger.Network;
using ClientMessenger.Util;
using ClientMessenger.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace ClientMessenger.ViewModels
{
    public class UserListViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<UserModel> _userList;
        private IRoomSender _roomSender;
        private ICommand _startChatCommand;
        private SocketClientManager _socketClientManager;
        public Action CloseAction { get; set; }

        public ObservableCollection<UserModel> UserList
        {
            get => _userList;
            set
            {
                _userList = value;
                OnPropertyChanged();
            }
        }        

        public IRoomSender RoomSender
        {
            get => _roomSender;
            set
            {
                _roomSender = value;
                OnPropertyChanged();
            }
        }

        public ICommand StartChatCommand
        {
            get
            {
                if (_startChatCommand == null)
                {
                    _startChatCommand = new RelayCommand(ExecuteStartChat);
                }
                return _startChatCommand;
            }
        }
        public UserListViewModel(SocketClientManager socketClientManager)
        {
            _socketClientManager = socketClientManager;
            UserList = new ObservableCollection<UserModel>();
        }
   
        private void ExecuteStartChat(object obj)
        {
            if (_socketClientManager.CurrentSession != null)
            {                
                _socketClientManager.CurrentSession.OnRoomCreated -= OnRoomCreated; // 중복 방지
                _socketClientManager.CurrentSession.OnRoomCreated += OnRoomCreated;
            }

            var selectedUsers = UserList.Where(u => u.IsSelected).ToList();

            if (selectedUsers.Count == 0)
            {
                MessageBox.Show("채팅할 유저를 선택해주세요.");
                return;
            }

            try
            {
                // 선택된 유저 ID 목록 생성
                var users = new
                {
                    UserIds = selectedUsers.Select(u => u.UserId).ToList()
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(users);
                byte[] body = Encoding.UTF8.GetBytes(json);
                
                _socketClientManager.CurrentSession.Send(PacketType.CreateRoomRequest, body);
                
                MessageBox.Show("채팅방 생성 요청을 서버로 전송했습니다.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"채팅방 생성 요청 중 오류가 발생했습니다: {ex.Message}");
            }
        }

        private void OnRoomCreated(CreateRoomResponseDto response)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (response.IsSuccess)
                {
                    // 새 방 정보 모델 생성
                    var newRoom = new RoomModel
                    {
                        RoomId = response.RoomId,
                        RoomName = response.RoomName,
                        Participants = response.ParticipantIds,
                        Messages = new ObservableCollection<MessageModel>(),
                        LastMessage = string.Empty,
                        Timestamp = DateTime.Now
                    };


                if (this._roomSender != null)
                    {
                        this._roomSender.RoomCreated(newRoom);
                    }

                    // 창 닫기
                    CloseAction?.Invoke();
                }
                else
                {
                    MessageBox.Show("채팅방 생성에 실패했습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }));
        }

        public void Cleanup()
        {
            if (_socketClientManager.CurrentSession != null)
            {
                _socketClientManager.CurrentSession.OnRoomCreated -= OnRoomCreated;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
