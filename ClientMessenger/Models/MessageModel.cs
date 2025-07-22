using Newtonsoft.Json;
using System;
using System.ComponentModel;

namespace ClientMessenger.Models
{
    public class MessageModel : INotifyPropertyChanged
    {
        private int _messageId;
        private string _userId;
        private int _roomId;
        private string _senderNickname;
        private DateTime _timestamp;
        private string _type;
        private string _content;
        private string _fileName;
        private string _fileSize;
        private string _fileUrl;
        private string _thumbnailUrl;
        private bool _isDeleted;
        private bool _isMine;

        

        public int MessageId
        {
            get { return _messageId; }
            set
            {
                if (_messageId != value)
                {
                    _messageId = value;
                    OnPropertyChanged(nameof(MessageId));
                }
            }
        }

        public string UserId
        {
            get { return _userId; }
            set
            {
                if (_userId != value)
                {
                    _userId = value;
                    OnPropertyChanged(nameof(UserId));
                }
            }
        }

        public int RoomId
        {
            get { return _roomId; }
            set
            {
                if (_roomId != value)
                {
                    _roomId = value;
                    OnPropertyChanged(nameof(RoomId));
                }
            }
        }

        public string SenderNickname
        {
            get { return _senderNickname; }
            set
            {
                if (_senderNickname != value)
                {
                    _senderNickname = value;
                    OnPropertyChanged(nameof(SenderNickname));
                }
            }
        }

        public DateTime Timestamp
        {
            get { return _timestamp; }
            set
            {
                if (_timestamp != value)
                {
                    _timestamp = value;
                    OnPropertyChanged(nameof(Timestamp));
                }
            }
        }

        public string Type
        {
            get { return _type; }
            set
            {
                if (_type != value)
                {
                    _type = value;
                    OnPropertyChanged(nameof(Type));
                }
            }
        }

        public string Content
        {
            get { return _content; }
            set
            {
                if (_content != value)
                {
                    _content = value;
                    OnPropertyChanged(nameof(Content));
                }
            }
        }

        public string FileName
        {
            get { return _fileName; }
            set
            {
                if (_fileName != value)
                {
                    _fileName = value;
                    OnPropertyChanged(nameof(FileName));
                }
            }
        }

        public string FileSize
        {
            get { return _fileSize; }
            set
            {
                if (_fileSize != value)
                {
                    _fileSize = value;
                    OnPropertyChanged(nameof(FileSize));
                }
            }
        }

        public string FileUrl
        {
            get { return _fileUrl; }
            set
            {
                if (_fileUrl != value)
                {
                    _fileUrl = value;
                    OnPropertyChanged(nameof(FileUrl));
                }
            }
        }

        public string ThumbnailUrl
        {
            get { return _thumbnailUrl; }
            set
            {
                if (_thumbnailUrl != value)
                {
                    _thumbnailUrl = value;
                    OnPropertyChanged(nameof(ThumbnailUrl));
                }
            }
        }

        public bool IsDeleted
        {
            get { return _isDeleted; }
            set
            {
                if (_isDeleted != value)
                {
                    _isDeleted = value;
                    OnPropertyChanged(nameof(IsDeleted));
                }
            }
        }

        [JsonIgnore]
        public bool IsMine
        {
            get { return _isMine; }
            set
            {
                if (_isMine != value)
                {
                    _isMine = value;
                    OnPropertyChanged(nameof(IsMine));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
