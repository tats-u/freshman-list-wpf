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

namespace 名簿作成システム_WPF_
{
    /// <summary>
    /// SettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingWindow : Window
    {
        private string lockpass; //パスワード
        //データの形式(各ラジオボタンに対応)
        private enum DataFormat { All, WithoutDept, OnlyAddress, OnlyName, Other, Undefined, Error };
        private DataFormat formattype = DataFormat.All;
        private bool FormatChangeable; //既存ファイルを開くなどして形式が決まってるかどうかを示すフラグ
        //パスワード受け渡し用プロパティ
        public string gotpass
        {
            get { return lockpass; }
            set { lockpass = value; }
        }


        //データの形式(順番情報も含む)
        public List<DataItems> ItemOrder { get; set; }

        //入力画面を全画面で表示しないかどうかのフラグ
        public bool NoFullScr { get; set; }

        //フォーマットチェック
        private DataFormat CheckFormat(List<DataItems> Items)
        {
            if (Items == null || Items.Count == 0 || Items.Count > 4) return DataFormat.Error; //数のチェック
            int formatnum = 0;
            foreach (var eachitem in Items)
            {
                formatnum |= 1 << (int)eachitem;
            }
            switch (formatnum)
            {
                case (int)ItemFormat.All:
                    return DataFormat.All;
                case (int)ItemFormat.OnlyAddress:
                    return DataFormat.OnlyAddress;
                case (int)ItemFormat.OnlyName:
                    return DataFormat.OnlyName;
                case (int)ItemFormat.WithoutDept:
                    return DataFormat.WithoutDept;
                default:
                    return DataFormat.Other;
            }
        }
        //////////////////////////コンストラクタ//////////////////////////////
        public SettingWindow()
        {
            InitializeComponent();
            FormatChangeable = true;
        }

        

        public SettingWindow(List<DataItems> Items)
        {
            InitializeComponent();
            formattype = CheckFormat(Items);
            if (formattype != DataFormat.Error)
            { //既存データ
                RBWithoutDept.IsEnabled = RBAll.IsEnabled = RBName.IsEnabled = RBAddress.IsEnabled = false; //ラジオボタンロック
                var Alternative = new Dictionary<DataFormat, RadioButton>()
                {
                { DataFormat.All, RBAll },
                { DataFormat.OnlyAddress, RBAddress }, 
                { DataFormat.OnlyName, RBName }, 
                { DataFormat.WithoutDept, RBWithoutDept }, 
                { DataFormat.Other, RBOther }
                };
                Alternative[formattype].IsChecked = true;
                ItemOrder = Items;
                FormatChangeable = false;
            }
            else
            {
                formattype = DataFormat.All;
                RBAll.IsChecked = true;
                FormatChangeable = true;
            }
        }

        private void OKButtonClicked(object sender, RoutedEventArgs e)
        {
            lockpass = password.Password; //パスワードをメイン画面に返す準備
            NoFullScr = RBWindow.IsChecked == true; //フルスクリーンかどうかをメイン画面に返す
            if (FormatChangeable)
            {   //新規作成でフォーマットを選択できる(ユーザが選択した)とき
                //選択したフォーマットのアイテムをヘッダに追加
                ItemOrder = new List<DataItems>();
                switch (formattype)
                { //学部→名前→メアド　の順番が原則
                    case DataFormat.All:
                        ItemOrder.Add(DataItems.Dept);
                        goto case DataFormat.WithoutDept;
                    case DataFormat.WithoutDept:
                        ItemOrder.Add(DataItems.Name);
                        goto case DataFormat.OnlyAddress;
                    case DataFormat.OnlyAddress:
                        ItemOrder.Add(DataItems.Address);
                        break;
                    case DataFormat.OnlyName:
                        ItemOrder.Add(DataItems.Name);
                        break;

                }
            }
            this.DialogResult = true;
            this.Close();
        }

        private void RBAll_Checked(object sender, RoutedEventArgs e)
        {
            formattype = DataFormat.All;
        }

        private void RBWithoutDept_Checked(object sender, RoutedEventArgs e)
        {
            formattype = DataFormat.WithoutDept;
        }

        private void RBAddress_Checked(object sender, EventArgs e)
        {
            formattype = DataFormat.OnlyAddress;
        }

        private void RBName_Checked(object sender, EventArgs e)
        {
            formattype = DataFormat.OnlyName;
        }

        private void PwKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OKButtonClicked(null,null);
            }
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
    }
}
