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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace 名簿作成システム_WPF_
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private string[] cellphonemail = {"docomo.ne.jp", "mopera.net",
			 "softbank.ne.jp", "vodafone.ne.jp", "disney.ne.jp",
			 "i.softbank.jp",
			 "ezweb.ne.jp", "ido.ne.jp",
			 "emnet.ne.jp", "emobile.ne.jp", "emobile-s.ne.jp",
			 "pdx.ne.jp", "willcom.com", "wcm.ne.jp"}; //携帯アドレス一覧
        private string datafile; //データファイル名
        private string lockpass; //画面ロック用パスワード
        private bool nofullscr; //フルスクリーンにしないかどうか
        private bool settingok = false; //設定完了フラグ
        private List<DataItems> itemorder = new List<DataItems>(); //入力要求項目
        private SaveDataObject DataObj;
        private DataItems LastFocusedTextBox; //最後にフォーカスされた入力欄(Deptは使わない)
        public MainWindow()
        {
            InitializeComponent();
            var fd = new Microsoft.Win32.SaveFileDialog(); //ファイルダイアログ初期化
            fd.Filter = "全ての対応ファイル|*.xlsx; *.csv|Excelファイル|*.xlsx|CSVファイル|*.csv";
            fd.DefaultExt = "xlsx";
            fd.OverwritePrompt = false;
            fd.Title = "名簿ファイルを開く・新規作成";
            if (fd.ShowDialog() != true)
            {
                this.Close();
                return;
            }
            datafile = fd.FileName; //データファイル名取得
            switch (System.IO.Path.GetExtension(datafile).ToLower())
            { //拡張子判定
                case ".csv":
                    DataObj = new SaveDataObjectTSV(datafile);
                    break;
                case ".xlsx":
                    DataObj = new SaveDataObjectXLS(datafile);
                    break;
                default:
                    MessageBox.Show("未対応のファイル形式です。", "エラー", MessageBoxButton.OK,MessageBoxImage.Error);
                    this.Close();
                    return;
            }

            try
            {
                itemorder = DataObj.ReadHeader(); //ヘッダを読み込む
            }
            catch (Exception ex)
            {
                MessageBox.Show("エラー発生\r\n" + ex.Message, "読み込みエラー", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
                return;
            }
            //設定画面オブジェクト初期化
            var sw = new SettingWindow(itemorder);
            if (sw.ShowDialog() != true) //設定画面を開く
            {
                this.Close();
                return;
            }

            lockpass = sw.gotpass; //パスワード設定
            nofullscr = sw.NoFullScr; //フルスクリーンにするか(false)、ウインドウにするか(true)
            bool isnewdata = itemorder == null;
            if (isnewdata)
            {
                itemorder = sw.ItemOrder; //入力要求項目設定
                //見出しをデータファイルに書き込む
                try
                {
                    DataObj.WriteHeader(itemorder);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("エラー発生\r\n" + ex.Message, "書き込みエラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                    return;
                }
            }

            if (nofullscr)
            {
                //通常のウインドウにする
                this.WindowState = System.Windows.WindowState.Normal;
                this.ResizeMode = System.Windows.ResizeMode.CanMinimize;
                this.WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
                //タッチキーボード呼び出しボタンを隠す
                runtchky.Visibility = System.Windows.Visibility.Collapsed;
                TchKeyClm.Width = new GridLength(0.0);
            }

            //入力しない項目は隠す
            var visibleflg = new bool[] { false, false, false };
            foreach (int it in itemorder)
            {
                visibleflg[it] = true;
            }
            
            if (!visibleflg[(int)DataItems.Dept])
            {
                schoollabel.Visibility = dept.Visibility = _V(false);
                DeptRow.Height = new GridLength(0.0);
            }
            if (!visibleflg[(int)DataItems.Name])
            {
                namelabel.Visibility = name.Visibility = _V(false);
                NameRow.Height = new GridLength(0.0);
            }
            if (!visibleflg[(int)DataItems.Address])
            {
                maillabel.Visibility = address.Visibility = _V(false);
                AddrRow.Height = new GridLength(0.0);
            }

            //最初に入力されるであろう項目(名前、なければメアド)にフォーカスを移す
            if (visibleflg[(int)DataItems.Dept])
            {
                LastFocusedTextBox = DataItems.Name;
            }
            else
            {
                LastFocusedTextBox = DataItems.Address;
                /*if (visibleflg[(int)DataItems.Address]) //キーボードのないタブレットでの入力も一応想定しておく
                {
                    runtchky.IsEnabled = false; //メアド入力にタッチキーボードは不要？
                }*/
            }
            
            settingok = true; //設定完了
        }

        private System.Windows.Visibility _V(bool bl) { return bl ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden; }

        /// <summary>
        /// 登録を行う処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Register(object sender, RoutedEventArgs e)
        {
            //入力漏れのチェック
            string noinp = "";
            int check = 0;
            if (name.IsVisible && name.Text.Length == 0)
            {
                noinp += "\n・名前";
                name.Focus();
                check += 4;
            }
            if (dept.IsVisible && dept.SelectedIndex == -1)
            {
                if (noinp.Length == 0) dept.Focus();
                noinp += "\n・学部";
                check += 2;
            }
            if (address.IsVisible && address.Text.Length == 0)
            {
                if (noinp.Length == 0) address.Focus();
                noinp += "\n・メールアドレス";
                check += 1;
            }
            //何らかの入力漏れがあればメッセージ表示
            if (check != 0)
            {
                //warnform(check, "入力");
                MessageBox.Show("以下の項目が空欄になっています" + noinp, "入力漏れ", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            //タブのチェック
            //noinp使い回し
            if (name.IsVisible && name.Text.IndexOf('\t') != -1)
            {
                name.Focus();
                noinp += "\n・名前";
                check += 4;

            }
            if (address.IsVisible && address.Text.IndexOf('\t') != -1)
            {
                if (noinp.Length == 0) address.Focus();
                noinp += "\n・メールアドレス";
                check += 1;

            }
            if (check != 0)
            {
                //warnform(check, "修正");
                MessageBox.Show("以下の項目内にタブ文字が入っています。\n取り除いて下さい。" + noinp, "「\"」の混入", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            if (address.IsVisible)
            {
                //不正なメアドチェック
                int atpoint = address.Text.IndexOf('@');
                int dotpoint = address.Text.LastIndexOf('.');
                if (atpoint < 1 || dotpoint - atpoint < 3 || address.Text.Length - dotpoint < 3)
                {

                    address.Clear();
                    //warnform(1, "入力");
                    address.Focus();
                    MessageBox.Show("メールアドレスの形式がおかしいです。\nきちんとしたメールアドレスを入力してください。", "不正なメールアドレス", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                //携帯アドレスのチェック
                bool iscellmail = false;
                for (int i = 0; i < cellphonemail.Length; i++)
                {
                    if (address.Text.IndexOf(cellphonemail[i]) != -1)
                    {
                        iscellmail = true; //比較
                        break;
                    }
                }
                if (iscellmail)
                {
                    if (MessageBox.Show("携帯メールのアドレスが入力されました。\nメーリングリストからのメールは携帯メールのフィルタリングに引っかかる可能性があります。\nこのメールアドレスを登録してよろしいでしょうか？\nPCからのメールを受信する設定になっている場合のみ「はい」を押してください。", "携帯アドレスを登録しようとしています", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
                    {
                        address.Clear();
                        //warnform(1, "入力");
                        address.Focus();
                        return;
                    }
                }
            }
            string confmsg = "";
            if (dept.IsVisible)
            {
                confmsg += dept.Text;
                if (name.IsVisible) confmsg += "の";
            }
            if (name.IsVisible) confmsg += name.Text + "さん、";
            if (confmsg.Length != 0) confmsg += "\r\n";
            confmsg += "本当に";
            if (address.IsVisible) confmsg += "このメールアドレス(" + address.Text + ")\r\nを";
            if (MessageBox.Show(confmsg + "登録してもよろしいでしょうか？", "最終確認", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    var Record = new List<string>();
                    var strs = new Dictionary<DataItems, string>()
                    {
                        {DataItems.Dept, dept.Text},{DataItems.Name,name.Text},{DataItems.Address,address.Text}
                    }; //項目・データの組を一旦生成
                    foreach (var item in itemorder)
                    { //データを整列
                        Record.Add(strs[item]);
                    }
                    DataObj.AddRecord(Record); //書き込み
                }
                catch (Exception ex)
                {

                    MessageBox.Show("エラー発生\r\n\r\nエラー内容: " + ex.Message, "書き込みエラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    registerbutton.Focus();
                    return;
                }
                string accmsg = "メーリングリストの受付が完了しました。";
                if (address.IsVisible) accmsg += "(アドレス: " + address.Text + ")";
                MessageBox.Show(accmsg + "\r\n登録ありがとう！", "登録完了", MessageBoxButton.OK, MessageBoxImage.Information);
                formclear();
            }
        }
        /// <summary>
        /// リセットを行う処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Reset(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("入力内容をリセットしてもよろしいでしょうか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                formclear();
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (settingok)
            { //設定が終わっている時
                if (lockpass.Length > 0)
                { //パスワードが設定されている時
                    var passauth = new AuthWindow();
                    passauth.gotpass = lockpass;


                    if (passauth.ShowDialog() != true)  // [いいえ] の場合
                    {
                        e.Cancel = true;  // 終了処理を中止   
                        return;
                    }
                    
                }
                if (MessageBox.Show("名簿ファイルがあるフォルダを開きますか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    System.Diagnostics.Process.Start(System.IO.Path.GetDirectoryName(@datafile));
            }
        }
        /*private void warnform(int set, string mes)
        {
            if ((set & 4) == 4)
            {
                noname.Text = mes + "してください";
                noname.Visible = true;
                name.BackColor = System.Drawing.Color.Red   
            } if ((set & 2) == 2)
            {
                nodept.Text = "選択してください";
                nodept.Visible = true;
                //dept.BackColor = System.Drawing.Color.Red;
            } if ((set & 1) == 1)
            {
                nomail.Text = mes + "してください";
                nomail.Visible = true;
                address.BackColor = System.Drawing.Color.Red;
            }
        }*/

        private void formclear()
        {
            name.Clear();
            dept.SelectedIndex = -1;
            address.Clear();
            name.Focus();
        }

        private void WKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    Register(null,null);
                    break;
                case Key.Escape:
                    Reset(null,null);
                    break;
            }
        }

        private void RunTouchKeyboard(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(@"C:\Program Files\Common Files\microsoft shared\ink\TabTip.exe");
            }
            catch (Exception) { } //例外は握りつぶす
            if (LastFocusedTextBox == DataItems.Name)
                name.Focus();
            else address.Focus();
        }

        private void StartInputingName(object sender, RoutedEventArgs e)
        {
            LastFocusedTextBox = DataItems.Name;
        }

        private void StartInputtingAddress(object sender, RoutedEventArgs e)
        {
            LastFocusedTextBox = DataItems.Address;
        }

        private void WindowMouseDown(object sender, MouseButtonEventArgs e)
        {
            
        }

        private void WindowMouseMove(object sender, MouseEventArgs e)
        {
            if (nofullscr) return; //全画面でない場合は関係なし
            var PtrLoc = this.PointToScreen(e.GetPosition(this));
            if (PseudoTitleBar.IsVisible)
            {
                if (PtrLoc.Y > 96) //タイトルバーの外にカーソル
                {
                    PseudoTitleBar.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
            else
            {
                if (PtrLoc.Y < 8) //画面の上端にカーソル
                {
                    PseudoTitleBar.Visibility = System.Windows.Visibility.Visible;
                }
            }
        }

        private void CloseButtonClicked(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MinimizeButtonClick(object sender, RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }
    }
}
