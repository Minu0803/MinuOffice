using ServerMessenger.Network;
using ServerMessenger.Util;
using ServerMessenger.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using ServerMessenger.Managers;

namespace ServerMessenger.ViewModels
{
    public class ServerViewModel : INotifyPropertyChanged
    {
        private SocketServerManager _serverManager;       

        private RelayCommand _startCommand;
        private RelayCommand _stopCommand;

        private ObservableCollection<UserModel> _clientsList;
        private ObservableCollection<string> _serverLogs;
        public ObservableCollection<UserModel> ClientsList
        {
            get => _clientsList;
        }
        
        public ObservableCollection<string> ServerLogs
        {
            get => _serverLogs;
        }
       
        public ICommand StartServerCommand
        {
            get
            {
                if (_startCommand == null)
                {
                    _startCommand = new RelayCommand(x => StartServer());
                }
                return _startCommand;
            }
        }

        public ICommand StopServerCommand
        {
            get
            {
                if (_stopCommand == null)
                {
                    _stopCommand = new RelayCommand(x => StopServer());
                }
                return _stopCommand;
            }
        }
        public ServerViewModel()
        {
            _serverManager = new SocketServerManager(AddLog);
            _clientsList = new ObservableCollection<UserModel>();
            _serverLogs = new ObservableCollection<string>();
        }

        private void StartServer()
        {
            _serverManager.StartServer(OnClientConnected);
        }

        private void StopServer()
        {
            _serverManager.StopServer();
            ClientsList.Clear();
        }

        private void OnClientConnected(ClientSession session)
        {
            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                ClientsList.Add(new UserModel
                {
                    UserId = session.UserId,
                    Nickname = session.Nickname,
                    Email = session.Email
                });
            }));
        }

        private void AddLog(string message)
        {
            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                ServerLogs.Add($"{DateTime.Now:HH:mm:ss} - {message}");
            }));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
