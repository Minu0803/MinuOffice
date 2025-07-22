using ClientMessenger.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public class RoomModel : INotifyPropertyChanged
{
    private int _roomId;
    private string _roomName;
    private List<string> _participants = new List<string>();
    private ObservableCollection<MessageModel> _messages = new ObservableCollection<MessageModel>();
    private string _lastMessage;
    private DateTime _timestamp;

    public int RoomId
    {
        get => _roomId;
        set
        {
            if (_roomId != value)
            {
                _roomId = value;
                OnPropertyChanged();
            }
        }
    }

    public string RoomName
    {
        get => _roomName;
        set
        {
            if (_roomName != value)
            {
                _roomName = value;
                OnPropertyChanged();
            }
        }
    }

    public List<string> Participants
    {
        get => _participants;
        set
        {
            if (_participants != value)
            {
                _participants = value;
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<MessageModel> Messages
    {
        get => _messages;
        set
        {
            if (_messages != value)
            {
                _messages = value;
                OnPropertyChanged();
            }
        }
    }

    public string LastMessage
    {
        get => _lastMessage;
        set
        {
            if (_lastMessage != value)
            {
                _lastMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public DateTime Timestamp
    {
        get => _timestamp;
        set
        {
            if (_timestamp != value)
            {
                _timestamp = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}