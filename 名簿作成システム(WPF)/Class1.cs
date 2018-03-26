using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//↓TSV出力用↓
using System.IO;
//↓Excel出力用↓
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace 名簿作成システム_WPF_
{

    class SaveDataObject
    {
        //ファイル名
        public string FileName { protected set; get; }
        //データの項目
        public List<DataItems> Items { set; get; }
        //ラベル→列挙体　への対応を表す連想配列
        protected static Dictionary<string, DataItems> LabelToEnum = new Dictionary<string, DataItems>() { { "名前", DataItems.Name }, { "メールアドレス", DataItems.Address }, { "学部", DataItems.Dept } };
        //上の逆
        protected static string[] EnumToLabel = new string[3] { "学部", "名前", "メールアドレス" };
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="FN">ファイル名</param>
        public SaveDataObject(string FN)
        {
            FileName = FN;
            Items = null;
        }
        //データを1件書き込む
        //仕様
        //  項目数が合わない→例外「データの形式がおかしいです」
        //  ファイルが開けない→例外「データファイルが開けませんでした」
        //  書き込み中にエラー→例外「書き込みエラー」
        public virtual void AddRecord(List<string> Record) { }
        //見出しを書き込む
        //仕様
        //  上に同じ
        public virtual void WriteHeader(List<DataItems> Header) { }
        public virtual List<DataItems> ReadHeader() { return null; }
        //見出しを読み込む
        //仕様
        //  ファイルがない→nullを返す
        //  ファイルが開けない→例外
        //  読み込み失敗→例外
        //  変な見出し・4つ以上→打ち切り(既知の見出しがなければ例外)
        //  順番がおかしい→例外
    }
    //***********************************************************************************//
    //***********************************TSV用のクラス***********************************//
    //***********************************************************************************//
    class SaveDataObjectTSV : SaveDataObject
    {
        private static Encoding utf16 = Encoding.GetEncoding("UTF-16LE");
        public SaveDataObjectTSV(string FN) : base(FN) { }
        //データを1件書き込む
        public override void AddRecord(List<string> Record)
        {
            if (Items == null) return;
            if (Items.Count != Record.Count)
                throw new Exception("データの形式がおかしいです");
            FileStream fst = null;
            StreamWriter swr = null;
            try
            {
                try
                {
                    OpenFile(ref fst, ref swr);
                }
                catch
                {
                    throw new Exception("データファイル「" + FileName + "」が開けませんでした");
                }
                try
                { //以下書き込み作業
                    int MAX = Record.Count - 1;
                    for (int i = 0; i < MAX; i++)
                    {
                        swr.Write(Record[i] + '\t');
                    }
                    swr.Write(Record[MAX] + '\n');
                }
                catch
                {
                    throw new Exception("データファイル「" + FileName + "」の書き込み中にエラーが発生しました");
                }
            }
            finally
            {
                if (fst != null) CloseFile(ref fst, ref swr);
            }

        }
        //見出しを書き込む
        public override void WriteHeader(List<DataItems> Header)
        {
            if (Items != null) return;
            FileStream fst = null;
            StreamWriter swr = null;
            try
            {
                try
                {
                    OpenFile(ref fst, ref swr);
                }
                catch
                {
                    throw new Exception("データファイル「" + FileName + "」が開けませんでした");
                }
                try
                { //以下書き込み作業
                    int MAX = Header.Count - 1;
                    for (int i = 0; i < MAX; i++)
                    {
                        swr.Write(EnumToLabel[(int)Header[i]] + '\t');
                    }
                    swr.Write(EnumToLabel[(int)Header[MAX]] + '\n');
                }
                catch
                {
                    throw new Exception("データファイル「" + FileName + "」の書き込み中にエラーが発生しました");
                }
                Items = Header;
            }
            finally
            {
                if (fst != null) CloseFile(ref fst, ref swr);
            }

        }
        //見出しを読み込む(try内部から呼び出し)
        public override List<DataItems> ReadHeader()
        {
            if (Items == null)
            {
                FileStream fst = null;
                StreamReader srd = null;
                try
                {
                    if (!File.Exists(FileName)) return null; //新規作成
                    string[] Labels;
                    try
                    {
                        OpenFile(ref fst, ref srd);
                        Labels = srd.ReadLine().Split(new char[1] { '\t' }, 3); //最初の行を読み込んでタブ文字で分割
                    }
                    catch (Exception)
                    {
                        throw new Exception("データファイル「" + FileName + "」を開く、もしくはデータを読み込むことができませんでした");
                    }
                    Items = new List<DataItems>();
                    foreach (string Label in Labels) //Label: 1マスの文字列（見出しが来るはず）
                    {
                        if (Items.Count >= 3) break; //項目は3つまで
                        if (!LabelToEnum.ContainsKey(Label))
                        { //変な見出しがあれば打ち切り
                            if (Items.Count == 0) throw new Exception("見出しがおかしいです");
                            break;
                        }
                        DataItems HDer = LabelToEnum[Label];
                        if (Items.Count > 0 && HDer == Items[Items.Count - 1])
                        { //見出しがおかしい
                            Items.Clear();
                            throw new Exception("同じ見出しを含んでいます");
                        }
                        Items.Add(HDer);

                    }
                    if (Items.Count == 3 && Items[0] == Items[2])
                    {
                        Items.Clear();
                        throw new Exception("同じ見出しを含んでいます");
                    }
                }
                finally
                {
                    if (fst != null) CloseFile(ref fst, ref srd);
                    if (Items != null && Items.Count == 0) Items = null;
                }

            }
            return Items;
        }
        //ファイルを開く関数(書き込み用、try内部に書くこと推奨)
        private void OpenFile(ref FileStream FS, ref StreamWriter SW)
        {
            FS = new FileStream(@FileName, FileMode.Append, FileAccess.Write);
            SW = new StreamWriter(FS, utf16);
        }
        //ファイルを開く関数(読み込み用、try内部に書くこと推奨)
        private void OpenFile(ref FileStream FS, ref StreamReader SR)
        {
            FS = new FileStream(@FileName, FileMode.Open, FileAccess.Read);
            SR = new StreamReader(FS, utf16);
        }
        //ファイルを閉じる関数(書き込み用、try内部推奨)
        private void CloseFile(ref FileStream FS, ref StreamWriter SW)
        {
            SW.Close();
            FS.Close();
        }
        //ファイルを閉じる関数(読み込み用、try内部推奨)
        private void CloseFile(ref FileStream FS, ref StreamReader SR)
        {
            SR.Close();
            FS.Close();
        }

    }
    //*********************************************************************//
    //*****************************XLSX用のクラス**************************//
    //*********************************************************************//
    class SaveDataObjectXLS : SaveDataObject
    {
        public SaveDataObjectXLS(string FN) : base(FN) { }
        //1～16384
        private string GetColIndex(int index)
        {
            string ret = "";
            if (index > 16384)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            for (; index > 0; index = (index - 1) / 26)
            {
                ret.Insert(0, new string((char)((int)'A' + (index - 1) % 26), 1));
            }

            return ret;
        }
        //データを1件書き込む
        public override void AddRecord(List<string> Record)
        {
            if (Items == null) return;
            SpreadsheetDocument Doc = null;
            if (File.Exists(FileName))
            {
                try
                {
                    Doc = SpreadsheetDocument.Open(FileName, true);
                }
                catch (Exception)
                {
                    throw new Exception("データファイル「" + FileName + "」を開けませんでした");
                }
            }
            else
            {
                throw new Exception("データファイル「" + FileName + "」が見つかりません");
            }
            try
            {
                WorkbookPart BPrt = Doc.WorkbookPart;
                Sheet MeiboSheet = null;
                if (BPrt == null)
                {
                    throw new Exception("データファイル「" + FileName + "」が空です");
                }
                else
                {
                    //シート「名簿作成システム」を探す
                    MeiboSheet = BPrt.Workbook.Descendants<Sheet>().Where(s => s.Name == "名簿作成システム").FirstOrDefault();
                }


                WorksheetPart SPrt;
                if (MeiboSheet == null)
                {

                    throw new Exception("データファイル「" + FileName + "」に本プログラム用のシートがありません");
                }
                SPrt = (WorksheetPart)(BPrt.GetPartById(MeiboSheet.Id));
                SheetData SData = SPrt.Worksheet.GetFirstChild<SheetData>();
                if (SData == null) throw new Exception("シートデータを取得できませんでした");
                var LastRow = SData.Descendants<Row>().LastOrDefault();
                if (LastRow == null) throw new Exception("シートが空です");
                var RecordRow = new Row();
                string LastIndexStr = (int.Parse(LastRow.GetFirstChild<Cell>().CellReference.InnerText.Substring(1)) + 1).ToString();
                for (int i = 0; i < Record.Count; i++) //項目を追加
                {
                    RecordRow.Append(new Cell()
                    {
                        DataType = CellValues.String,
                        CellReference = (char)((int)'A' + i) + LastIndexStr,
                        CellValue = new CellValue(Record[i])
                    });

                }
                SData.Append(RecordRow);
            }
            finally
            {
                if (Doc != null) Doc.Close();
            }
        }
        //見出しを書き込む
        public override void WriteHeader(List<DataItems> Header)
        {
            if (Items != null) return;
            SpreadsheetDocument Doc = null;
            if (File.Exists(FileName))
            {
                try
                {
                    Doc = SpreadsheetDocument.Open(FileName, true);
                }
                catch (Exception)
                {
                    throw new Exception("データファイル「" + FileName + "」を開けませんでした");
                }
            }
            else
            {
                Doc = SpreadsheetDocument.Create(FileName, SpreadsheetDocumentType.Workbook);
            }
            try
            {
                WorkbookPart BPrt = Doc.WorkbookPart;
                Sheet MeiboSheet = null;
                if (BPrt == null)
                {
                    BPrt = Doc.AddWorkbookPart();
                    BPrt.Workbook = new Workbook();
                }
                else
                {
                    //シート「名簿作成システム」を探す
                    MeiboSheet = BPrt.Workbook.Descendants<Sheet>().Where(s => s.Name == "名簿作成システム").FirstOrDefault();
                }

                WorksheetPart SPrt;
                var SData = new SheetData();
                if (MeiboSheet != null)
                {

                    throw new Exception("すでに本プログラム用のシートがあります");
                }
                SPrt = BPrt.AddNewPart<WorksheetPart>(); //新規作成
                SPrt.Worksheet = new Worksheet(SData);
                Sheets sheets = BPrt.Workbook.Descendants<Sheets>().FirstOrDefault();
                bool FirstSheetFlg = false;
                if (sheets == null)
                {//新規でシート群(Sheets)作成
                    sheets = BPrt.Workbook.
                    AppendChild<Sheets>(new Sheets());
                    FirstSheetFlg = true;
                }
                MeiboSheet = new Sheet()
                {
                    Id = Doc.WorkbookPart.GetIdOfPart(SPrt),
                    SheetId = FirstSheetFlg ? 1 : sheets.Descendants<Sheet>().Last<Sheet>().SheetId + 1,
                    Name = "名簿作成システム"
                };
                sheets.Append(MeiboSheet);

                var FirstRow = new Row();
                for (int i = 0; i < Header.Count; i++) //項目を追加
                {
                    FirstRow.Append(new Cell()
                    {
                        DataType = CellValues.String,
                        CellReference = (char)((int)'A' + i) + "1",
                        CellValue = new CellValue(EnumToLabel[(int)Header[i]])
                    });

                }
                SData.Append(FirstRow);
            }
            finally
            {
                if (Doc != null) Doc.Close();
            }
            Items = Header;

        }
        //見出しを読み込む
        //シート名は「名簿作成システム」
        public override List<DataItems> ReadHeader()
        {
            if (Items != null) return Items;
            if (!File.Exists(FileName)) return null;
            SpreadsheetDocument Doc = null;

            try
            {
                Doc = SpreadsheetDocument.Open(FileName, false);
            }
            catch (Exception)
            {
                throw new Exception("データファイル「" + FileName + "」を開けませんでした");
            }
            try
            {
                WorkbookPart BPrt = Doc.WorkbookPart;
                //シート「名簿作成システム」を探す
                Sheet MeiboSheet = BPrt.Workbook.Descendants<Sheet>().Where(s => s.Name == "名簿作成システム").FirstOrDefault();
                if (MeiboSheet == null) return null; //見つからない場合はnull
                WorksheetPart SPrt = (WorksheetPart)(BPrt.GetPartById(MeiboSheet.Id));
                Row FirstRow = SPrt.Worksheet.Descendants<Row>().First();
                if (FirstRow == null) return null;
                Items = new List<DataItems>();
                var stringTable =
                    BPrt.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
                for (int i = 0; i < 3; i++)
                {
                    Cell cl = FirstRow.Descendants<Cell>().Where(c => c.CellReference == (char)((int)'A' + i) + "1").FirstOrDefault();
                    if (cl == null)
                    {
                        if (Items.Count == 0) throw new Exception("見出しがおかしいです");
                        break; //途中で切れてる
                    }
                    string OneLabel;
                    //データ型によって処理を振り分け
                    switch (cl.DataType.Value)
                    {
                        case CellValues.String:
                            OneLabel = cl.InnerText;
                            break;
                        case CellValues.SharedString:
                            if (stringTable != null)
                            {
                                OneLabel = stringTable.SharedStringTable.
                                  ElementAt(int.Parse(cl.InnerText)).InnerText;
                            }
                            else
                            {
                                if (Items.Count == 0) throw new Exception("見出しのデータがおかしいです");
                                goto breakloop; //データ型がおかしい
                            }
                            break;
                        default:
                            if (Items.Count == 0) throw new Exception("見出しのデータ型がおかしいです");
                            goto breakloop; //データ型がおかしい
                    }
                    if (!LabelToEnum.ContainsKey(OneLabel))
                    { //変な見出しがあれば打ち切り
                        if (Items.Count == 0) throw new Exception("見出しがおかしいです");
                        break;
                    }
                    DataItems HDer = LabelToEnum[OneLabel];
                    if (Items.Count > 0 && HDer == Items[Items.Count - 1])
                    { //見出しの順番が正しくない
                        Items.Clear();
                        throw new Exception("同じ見出しを含んでいます");
                    }
                    Items.Add(HDer);

                }
            breakloop:
                if (Items.Count == 3 && Items[0] == Items[2])
                {
                    Items.Clear();
                    throw new Exception("同じ見出しを含んでいます");
                }
            }
            finally
            {
                if (Doc != null) Doc.Close();
                if (Items != null && Items.Count == 0) Items = null;
            }
            return Items;
        }
    }
}
