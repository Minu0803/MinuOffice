using ClientMessenger.Common;
using ClientMessenger.DTO;
using ClientMessenger.Models;
using ClientMessenger.Network;
using ClientMessenger.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClientMessenger.ViewModels
{
    public class ChattingRoomViewModel : INotifyPropertyChanged
    {
        private RoomModel _selectedRoom;
        private SocketClientManager _socketClientManager;
        private int _offset;
        private int PageSize;
        private bool _isLoading;
        private string _messageText;
        private RelayCommand _sendMessageCommand;
        private RelayCommand _sendFileCommand;
        private RelayCommand _deleteMessageCommand;
        private ICommand _downloadFileCommand;

        public ObservableCollection<MessageModel> MessageList { get; set; }
        public string MessageText
        {
            get { return _messageText; }
            set
            {
                if (_messageText != value)
                {
                    _messageText = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public ICommand SendMessageCommand
        {
            get
            {
                if (_sendMessageCommand == null)
                {
                    _sendMessageCommand = new RelayCommand(
                        _ => ExecuteSendMessage(),
                        _ => !string.IsNullOrWhiteSpace(MessageText));
                }
                return _sendMessageCommand;
            }
        }

        public ICommand DeleteMessageCommand
        {
            get
            {
                if (_deleteMessageCommand == null)
                {
                    _deleteMessageCommand = new RelayCommand(
                        param =>
                        {
                            var message = param as MessageModel;
                            if (message == null)
                                return;

                            var result = MessageBox.Show("메시지를 삭제하시겠습니까?", "확인", MessageBoxButton.YesNo);
                            if (result == MessageBoxResult.Yes)
                            {
                                ExecuteDeleteMessage(message);
                            }
                        });
                }
                return _deleteMessageCommand;
            }
        }



        public ICommand SendFileCommand
        {
            get
            {
                if (_sendFileCommand == null)
                {
                    _sendFileCommand = new RelayCommand(
                        _ => ExecuteSendFile()); 
                }
                return _sendFileCommand;
            }
        }
        
        public ICommand DownloadFileCommand
        {
            get
            {
                if (_downloadFileCommand == null)
                {
                    _downloadFileCommand = new RelayCommand(param => ExecuteDownloadFile(param));
                }
                return _downloadFileCommand;
            }
        }


        public ChattingRoomViewModel(RoomModel selectedRoom, SocketClientManager socketClientManager)
        {
            _offset = 0;
            PageSize = 50; // 페이지당 메시지 수
            _isLoading = false; // 스크롤로 메시지를 불러올 때 중복 요청을 방지

            _selectedRoom = selectedRoom;
            _socketClientManager = socketClientManager;
             
            MessageList = new ObservableCollection<MessageModel>(_selectedRoom.Messages);
            _socketClientManager.CurrentSession.OnMessagesLoaded += OnLoadMessagesResponse;
            _socketClientManager.CurrentSession.OnTextMessageReceived += HandleIncomingTextMessage;
            _socketClientManager.CurrentSession.OnMessageDeleted += HandleMessageDeleted;
            _socketClientManager.CurrentSession.OnFileMessageReceived += OnFileMessageReceived;

        }

        /// <summary>
        /// 채팅내역 불러오기
        /// </summary>
        public void RequestMessagesForRoom(int roomId, int offset = 0, int limit = 50)
        {
            try
            {
                var request = new LoadMessagesRequestDto
                {
                    RoomId = roomId,
                    Offset = offset,
                    Limit = limit
                };

                string json = JsonConvert.SerializeObject(request);
                byte[] body = Encoding.UTF8.GetBytes(json);

                _socketClientManager.CurrentSession.Send(PacketType.LoadMessagesRequest, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[클라이언트 오류 - RequestMessagesForRoom] {ex.Message}");
            }
        }

        /// <summary>
        /// 가져온 채팅내역 뿌려주기
        /// </summary>        
        private void OnLoadMessagesResponse(List<MessageModel> messages)
        {
            if (messages == null || messages.Count == 0) return;

            if (messages[0].RoomId != _selectedRoom.RoomId) return; // 현재 내가 보고 있는 방에 대한 메시지 응답이 맞는지

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                for (int i = messages.Count - 1; i >= 0; i--)
                {
                    var message = messages[i];

                    // 메시지가 삭제 상태인 경우 삭제 문구로 표시
                    if (message.IsDeleted)
                    {
                        message.Content = "[삭제된 메시지입니다]";
                    }

                    MessageList.Insert(0, message);
                }

                _offset += messages.Count;
                _isLoading = false;
            }));
        }


        public void LoadMoreMessages()
        {
            if (_isLoading) return;
            _isLoading = true;

            RequestMessagesForRoom(_selectedRoom.RoomId, _offset, PageSize);
        }

        private void ExecuteSendMessage()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(MessageText)) return;

                var message = new MessageModel
                {
                    RoomId = _selectedRoom.RoomId,
                    UserId = _socketClientManager.CurrentSession.UserId,                    
                    Content = MessageText,
                    Type = "text",                    
                    IsMine = true
                };

                string json = JsonConvert.SerializeObject(message);
                byte[] body = Encoding.UTF8.GetBytes(json);

                _socketClientManager.CurrentSession.Send(PacketType.TextMessage, body);

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    MessageList.Add(message);
                    MessageText = string.Empty; // 입력창 클리어
                }));
            }
            catch (Exception ex)
            {                
                MessageBox.Show($"메시지 전송 중 오류 발생: {ex.Message}");
            }
        }

        private void ExecuteDeleteMessage(MessageModel message)
        {
            if (message == null || _selectedRoom == null)
                return;

            // 내가 보낸 메시지 여부 판단
            bool isMine = message.UserId == _socketClientManager.CurrentSession.UserId;

            var dto = new DeleteMessageRequestDto
            {
                RoomId = _selectedRoom.RoomId,
                MessageId = message.MessageId,
                RequesterId = _socketClientManager.CurrentSession.UserId
            };

            try
            {
                string json = JsonConvert.SerializeObject(dto);
                byte[] body = Encoding.UTF8.GetBytes(json);

                _socketClientManager.CurrentSession.Send(PacketType.DeleteMessageRequest, body);

                if (!isMine)
                {

                    // 상대방 메시지인 경우는 내 로컬에서만 삭제 처리
                    var original = _selectedRoom.Messages.FirstOrDefault(m => m.MessageId == message.MessageId);
                    if (original != null)
                    {
                        original.IsDeleted = true;
                        original.Content = "[삭제된 메시지입니다]";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ExecuteDeleteMessage 예외] {ex.Message}");
            }
        }

        private void ExecuteSendFile()
        {
            try
            {
                Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "파일 선택",
                    Filter = "모든 파일|*.*",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() != true)
                    return;

                string filePath = openFileDialog.FileName;
                string fileName = System.IO.Path.GetFileName(filePath);
                string extension = System.IO.Path.GetExtension(filePath).ToLower();

                bool isImage = extension == ".jpg" || extension == ".jpeg" || extension == ".png";
                PacketType packetType = isImage ? PacketType.ImageMessage : PacketType.FileMessage;

                byte[] fileBytes = File.ReadAllBytes(filePath);
                int chunkSize = 1024 * 1024; // 1MB
                int totalChunks = (int)Math.Ceiling((double)fileBytes.Length / chunkSize);

                for (int i = 0; i < totalChunks; i++)
                {
                    int currentChunkSize = Math.Min(chunkSize, fileBytes.Length - (i * chunkSize));
                    byte[] chunkBytes = new byte[currentChunkSize];
                    Buffer.BlockCopy(fileBytes, i * chunkSize, chunkBytes, 0, currentChunkSize);

                    var meta = new FileTransferChunkDto
                    {
                        RoomId = _selectedRoom.RoomId,
                        SenderId = _socketClientManager.CurrentSession.UserId,
                        FileName = fileName,
                        TotalChunks = totalChunks,
                        ChunkIndex = i,
                        IsFinal = (i == totalChunks - 1)
                    };

                    byte[] jsonBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(meta));

                    _socketClientManager.CurrentSession.Send(packetType, jsonBytes, chunkBytes);
                }
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    _selectedRoom.Messages.Add(new MessageModel
                    {
                        UserId = _socketClientManager.CurrentSession.UserId,
                        Type = isImage ? "image" : "file",
                        Content = fileName,
                        IsMine = true,
                        Timestamp = DateTime.Now
                    });
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"파일 전송 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        /// <summary>
        /// 상대방으로부터 온 텍스트 메시지 처리
        /// </summary>        
        private void HandleIncomingTextMessage(MessageModel message)
        {
            // 내가 현재 보고 있는 채팅방의 메시지만 처리
            if (_selectedRoom == null || message.RoomId != _selectedRoom.RoomId)
                return;
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                MessageList.Add(message);
            }));
        }

        /// <summary>
        /// 메시지 삭제 처리
        /// </summary>        
        private void HandleMessageDeleted(DeleteMessageResponseDto dto)
        {
            if (dto.RoomId != _selectedRoom.RoomId)
                return;

            var message = _selectedRoom.Messages.FirstOrDefault(m => m.MessageId == dto.MessageId);
            if (message != null)
            {
                message.IsDeleted = dto.IsDeleted;

                message.Content = "[삭제된 메시지입니다]";
            }
        }

        private void OnFileMessageReceived(MessageModel message)
        {
            MessageBox.Show("파일 메시지를 전송 완료했습니다: " + message.Content, "파일 수신", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExecuteDownloadFile(object parameter)
        {
            var message = parameter as MessageModel;
            if (message == null || string.IsNullOrEmpty(message.FileName) || message.Type != "file")
                return;

            try
            {
                var meta = new FileDownloadRequestDto
                {
                    RoomId = message.RoomId,
                    FileName = message.FileName
                };

                string json = JsonConvert.SerializeObject(meta);
                byte[] body = Encoding.UTF8.GetBytes(json);

                _socketClientManager.CurrentSession.Send(PacketType.FileSaveRequest, body);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"파일 다운로드 요청 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        public void Cleanup()
        {
            if (_socketClientManager?.CurrentSession != null)
            {
                _socketClientManager.CurrentSession.OnMessagesLoaded -= OnLoadMessagesResponse;
                _socketClientManager.CurrentSession.OnTextMessageReceived -= HandleIncomingTextMessage;
                _socketClientManager.CurrentSession.OnMessageDeleted -= HandleMessageDeleted;
                _socketClientManager.CurrentSession.OnFileMessageReceived -= OnFileMessageReceived;

            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
