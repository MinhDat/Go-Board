using System.Windows;
using GoBoard.ViewModels;
using System;
using Newtonsoft.Json.Linq;
using Quobject.SocketIoClientDotNet.Client;
using System.Configuration;

namespace GoBoard.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int check = -1;
        MainWindowViewModel mvvm;
        private bool onStep = true;
        private bool flat = true;
        private bool fn = true;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void goBoardPainter_MovePlayed(object sender, RoutedMovePlayedEventArgs e)
        {
            mvvm = this.DataContext as MainWindowViewModel;
            mvvm.N1 = txtName.Text;
            string[] m_Coordinates = new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "J", "K", "L", "M" };

            if (!goBoardPainter.StoneList.ContainsKey(e.Position) && !goBoardPainter.onWin() && (check != 2))
            {
                if (check == 1)
                {
                    mvvm.socket.Emit("MyStepIs", JObject.FromObject(new { row = e.Position.Y, col = e.Position.X }));
                    onStep = true;
                }
                if (flat)
                {
                    goBoardPainter.StoneList.Add(new GoBoardPoint(e.Position.X, e.Position.Y), goBoardPainter.ToPlay);
                    goBoardPainter.ToPlay = e.StoneColor ^ Stone.Red;
                    goBoardPainter.Redraw();
                    if (check == 1)
                        flat = false;
                }
            }

            if (!goBoardPainter.onWin() && check == 0)
            {
                GoBoardPoint gp = goBoardPainter.PlayerVsCOM();
                goBoardPainter.StoneList.Add(new GoBoardPoint(gp.X, gp.Y), goBoardPainter.ToPlay);
                goBoardPainter.ToPlay = goBoardPainter.ToPlay ^ Stone.Red;
                goBoardPainter.Redraw();
            }

            if (goBoardPainter.onWin() && (check == -1 || check == 0))
            {
                goBoardPainter.messageEnd();
            } 
        }

        private void rbtnPvP_Checked(object sender, RoutedEventArgs e)
        {
            if (goBoardPainter.StoneList.Count == 0)
                check = -1;
        }

        private void rbtnPvC_Checked(object sender, RoutedEventArgs e)
        {
            if (goBoardPainter.StoneList.Count == 0)
                check = 0;
        }

        private void rbtnPvO_Checked(object sender, RoutedEventArgs e)
        {
            if (goBoardPainter.StoneList.Count == 0)
            {
                check = 1;
                
            }
        }

        private void rbtnCvO_Checked(object sender, RoutedEventArgs e)
        {
            if (goBoardPainter.StoneList.Count == 0)
            {
                check = 2;
            }
        }

        private void btnNewgame_Click(object sender, RoutedEventArgs e)
        {
            mvvm = this.DataContext as MainWindowViewModel;
            mvvm.N1 = txtName.Text;
            fn = true;
            goBoardPainter.StoneList.Clear();
            goBoardPainter.ToPlay = Stone.Black;
            goBoardPainter.Redraw();
            if (rbtnPvC.IsChecked == true)
            {
                check = 0;
                mvvm.C = false;
            }
            if (rbtnPvP.IsChecked == true)
            {
                check = -1;
                mvvm.C = false;
            }
            if (rbtnPvO.IsChecked == true)
            {
                check = 1;
                mvvm.C = true;
                string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
                mvvm.socket = IO.Socket(connectionString);
                mvvm.socket.On(Socket.EVENT_CONNECT, () =>
                {
                    mvvm.H += "System(" + DateTime.Now.ToShortTimeString() + "): connected\n";
                });

                mvvm.socket.On(Socket.EVENT_MESSAGE, (data) =>
                {
                    mvvm.H += "System(" + DateTime.Now.ToShortTimeString() + "): " + data.ToString() + "\n";
                });

                mvvm.socket.On(Socket.EVENT_CONNECT_ERROR, (data) =>
                {
                    mvvm.H += "System(" + DateTime.Now.ToShortTimeString() + "): " + data.ToString() + "\n";
                });

                mvvm.socket.On("ChatMessage", (data) =>
                {
                    string[] delim = { "{", "  \"message\": \"", "\"", "}", "<br />", "\",", "  \"from\": \"" };
                    string[] s = data.ToString().Split(delim, System.StringSplitOptions.RemoveEmptyEntries);
                    if (fn)
                        mvvm.H += "System(" + DateTime.Now.ToShortTimeString() + "): " + s[1] + "\n";
                    else
                    {
                        if (s.Length > 3)
                            mvvm.H += s[3] + "(" + DateTime.Now.ToShortTimeString() + "): " + s[1] + "\n";
                        else
                            mvvm.H += "System(" + DateTime.Now.ToShortTimeString() + "): " + s[1] + "\n";
                    }
                    if (s[2] == "You are the first player!")
                    {
                        mvvm.H += "System(" + DateTime.Now.ToShortTimeString() + "): " + s[2] + "\n";
                        flat = true;
                        fn = false;
                    }
                    if (s[2] == "You are the second player!")
                    {
                        mvvm.H += "System(" + DateTime.Now.ToShortTimeString() + "): " + s[2] + "\n";
                        flat = false;
                        fn = false;
                        onStep = true;
                    }

                    if (((Newtonsoft.Json.Linq.JObject)data)["message"].ToString() == "Welcome!")
                    {
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            mvvm.socket.Emit("MyNameIs", txtName.Text);
                            mvvm.socket.Emit("ConnectToOtherPlayer");
                        }));
                    }

                });

                mvvm.socket.On(Socket.EVENT_ERROR, (data) =>
                {
                    mvvm.H += "System(" + DateTime.Now.ToShortTimeString() + "): " + data.ToString() + "\n";
                });
                
                gamePlayerOnline();
            }
            if (rbtnCvO.IsChecked == true)
            {
                check = 2;
                mvvm.C = true;
                string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
                mvvm.socket = IO.Socket(connectionString);
                mvvm.socket.On(Socket.EVENT_CONNECT, () =>
                {
                    mvvm.H += "System(" + DateTime.Now.ToShortTimeString() + "): connected\n";
                });

                mvvm.socket.On(Socket.EVENT_MESSAGE, (data) =>
                {
                    mvvm.H += "System(" + DateTime.Now.ToShortTimeString() + "): " + data.ToString() + "\n";
                });

                mvvm.socket.On(Socket.EVENT_CONNECT_ERROR, (data) =>
                {
                    mvvm.H += "System(" + DateTime.Now.ToShortTimeString() + "): " + data.ToString() + "\n";
                });
                
                mvvm.socket.On("ChatMessage", (data) =>
                {
                    
                    string[] delim = { "{", "  \"message\": \"", "\"", "}", "<br />", "\",", "  \"from\": \""};
                    string[] s = data.ToString().Split(delim, System.StringSplitOptions.RemoveEmptyEntries);
                    if (fn)
                        mvvm.H += "System(" + DateTime.Now.ToShortTimeString() + "): " + s[1] + "\n";
                    else
                    {
                        if (s.Length > 3)
                            mvvm.H += s[3] + "(" + DateTime.Now.ToShortTimeString() + "): " + s[1] + "\n";
                        else
                            mvvm.H += "System(" + DateTime.Now.ToShortTimeString() + "): " + s[1] + "\n";
                    }

                    if (s[2] == "You are the first player!")
                    {
                        mvvm.H += "System(" + DateTime.Now.ToShortTimeString() + "): " + s[2] + "\n";
                        flat = true;
                        ComputerPlayer();
                        fn = false;
                    }
                    if (s[2] == "You are the second player!")
                    {
                        mvvm.H += "System(" + DateTime.Now.ToShortTimeString() + "): " + s[2] + "\n";
                        flat = false;
                        fn = false;
                    }

                    if (((Newtonsoft.Json.Linq.JObject)data)["message"].ToString() == "Welcome!")
                    {
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            mvvm.socket.Emit("MyNameIs", txtName.Text);
                            mvvm.socket.Emit("ConnectToOtherPlayer");
                        }));
                    }

                });
                mvvm.socket.On(Socket.EVENT_ERROR, (data) =>
                {
                    mvvm.H += "System(" + DateTime.Now.ToShortTimeString() + "): " + data.ToString() + "\n";
                });
                onStep = true;
                gamePlayerOnline();
            }
        }

        public void ComputerPlayer()
        {
            if (flat && !goBoardPainter.onWin())
            {
                Dispatcher.Invoke((Action)(() =>
                {
                    GoBoardPoint gp = goBoardPainter.PlayerVsCOM();
                    mvvm.socket.Emit("MyStepIs", JObject.FromObject(new { row = gp.Y, col = gp.X }));
                    goBoardPainter.StoneList.Add(new GoBoardPoint(gp.X, gp.Y), goBoardPainter.ToPlay);
                    goBoardPainter.ToPlay = goBoardPainter.ToPlay ^ Stone.Red;
                    goBoardPainter.Redraw();
                }));
                flat = false;
            }
            if (goBoardPainter.onWin())
            {
                goBoardPainter.messageEnd();
            }
        }

        public void gamePlayerOnline()
        {
            mvvm = this.DataContext as MainWindowViewModel;
            string[] s;
            if (onStep)
            {
                mvvm.socket.On("NextStepIs", (data) =>
                {
                    string[] delim = { "{", "  \"player\": 1,", "  \"player\": 0,", "  \"row\": ", ",", "  \"col\": ", "}", " " };
                    s = data.ToString().Split(delim, System.StringSplitOptions.RemoveEmptyEntries);
                    int y = Convert.ToInt32(s[2]);
                    int x = Convert.ToInt32(s[4]);
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        if (!goBoardPainter.StoneList.ContainsKey(new GoBoardPoint(x, y)) && !goBoardPainter.onWin())
                        {
                            goBoardPainter.StoneList.Add(new GoBoardPoint(x, y), Stone.Red);
                            goBoardPainter.ToPlay = Stone.Black;
                            goBoardPainter.Redraw();
                            flat = true;
                            if (rbtnCvO.IsChecked == true)
                            {
                                ComputerPlayer();
                            }
                        }
                        if (goBoardPainter.onWin())
                        {
                            goBoardPainter.messageEnd();
                        }
                    }));
                });
                onStep = false;
            }
        }
    }
}
