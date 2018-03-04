using Prism.Commands;
using Prism.Mvvm;
using Quobject.SocketIoClientDotNet.Client;
using System;

namespace GoBoard.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _s = "Text here...";

        private string _h;
        private Socket _sk;
        private bool _c = false;
        private string _n1;
        public string S
        {
            get
            {
                return _s;
            }

            set
            {
                SetProperty(ref _s, value);
                //OnPropertyChanged(() => H);
            }
        }

        public string H
        {
            get
            {
                return _h;
            }

            set
            {
                SetProperty(ref _h, value);
                //ChatCommand.RaiseCanExecuteChanged();
            }
        }

        public DelegateCommand ChatCommand { get; private set; }

        public Socket socket
        {
            get
            {
                return _sk;
            }
            set
            {
                SetProperty(ref _sk, value);
            }
        }

        public bool C
        {
            get
            {
                return _c;
            }

            set
            {
                SetProperty(ref _c, value);
            }
        }

        public string N1
        {
            get
            {
                return _n1;
            }

            set
            {
                SetProperty(ref _n1, value);
            }
        }

        public MainWindowViewModel()
        {
            ChatCommand = new DelegateCommand(onChat);
        }

        public MainWindowViewModel(string chat)
        {
            H += chat;
        } 

        private void onChat()
        {
            if (S.Length != 0)
            {
                if (C)
                    socket.Emit("ChatMessage", S);
                else
                    H += N1 + "(" + DateTime.Now.ToShortTimeString() + "): "  + S + "\n";
                S = "";
            }
        }


    }
}
