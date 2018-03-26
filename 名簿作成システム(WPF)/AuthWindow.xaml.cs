using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
namespace 名簿作成システム_WPF_
{
    /// <summary>
    /// AuthWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class AuthWindow : Window
    {
        private string lockpass; //パスワード
        
        //パスワード受け渡し用プロパティ
        public string gotpass
        {
            get { return lockpass; }
            set { lockpass = value; }
        }
        private DispatcherTimer Timer;
        public AuthWindow()
        {
            InitializeComponent();
            Timer = new DispatcherTimer(DispatcherPriority.Normal, this.Dispatcher);
            Timer.Interval = TimeSpan.FromSeconds(0.5);
            Timer.Tick += (ss, ee) =>
            {
                passdiff.Visibility = System.Windows.Visibility.Hidden;
            };
        }

        private void PwKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    if (password.Password == lockpass)
                    { //パスワード一致
                        DialogResult = true;
                        this.Close();
                    }
                    else
                    {
                        password.Clear();
                        WarnPassDiff();

                    }
                    break;
                case Key.Pause: //このキーだけはOK
                    DialogResult = true;
                    this.Close();
                    break;
                case Key.Escape:
                    this.Close();
                    break;
            }
        }
        
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            password.Focus();
        }

        private void WarnPassDiff()
        {
            passdiff.Visibility = System.Windows.Visibility.Visible;
            if (Timer.IsEnabled)
            {
                Timer.Stop();
            }

            Timer.Start();
        }
    }
}
