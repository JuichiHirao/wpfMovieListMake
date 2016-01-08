using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib;


namespace wpfMovieListMake
{
    // ファイル情報取得の別スレッドからスレッド終了時のデリゲート
    public delegate void dlgThreadEndGetFileInfo();

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        ObjectDataProvider provTargetFiles = null;
        TargetFilesParent parentTargetFiles = null;
        List<SiteStore> listSelectSite = null;
        List<AutoSelect> listAutoSelect = null;
        private bool IsMouseXButton1 = false;

        SiteStore dispinfoSiteStore = null;
        AutoSelect dispinfoAutoSelect = null;
        int dispinfoAutoSelectEditingNo = -1;

        DbConnection dbcon;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dbcon = new DbConnection();

            // チェックボックスの選択以外は読み取り専用にして編集不可にする
            foreach (DataGridColumn col in dgridMain.Columns)
            {
                if (!col.Header.Equals("選択"))
                    col.IsReadOnly = true;
            }

            listAutoSelect = AutoSelectXmlControl.GetList();
            lstAutoSetting.ItemsSource = listAutoSelect;

            // TAB-ITEM:DIR情報のリストを設定
            lstTargetDir.ItemsSource = GetDirInfo();

            // サイト情報を取得
            List<SiteStore> listSiteAll = CommonMethod.GetSiteStore(dbcon);
            SiteStoreNameComparer sitecomp = new SiteStoreNameComparer();
            lstTargetSite.ItemsSource = listSiteAll.Distinct(sitecomp);

            DisplayGrid();
        }

        public void DisplayGrid()
        {
            // dgridSiteInfo：XMLからサイトパス情報を取得して、サイト情報のみをDistinctして表示
            listSelectSite = new List<SiteStore>();
            listSelectSite = GetSelectSiteFromXmlFile();

            SiteStoreNameComparer sitecomp = new SiteStoreNameComparer();
            dgridSiteInfo.ItemsSource = listSelectSite.Distinct(sitecomp);

            // dgridDisplaySelectSite：DBからサイト情報を取得して設定と一致する行にチェックを付けて表示
            List<SiteStore> listSiteStore = CommonMethod.GetSiteStore(dbcon);
            foreach (SiteStore site in listSiteStore)
            {
                var data = from matchdata in listSelectSite
                           where matchdata.Name == site.Name && matchdata.Path == site.Path
                           select matchdata;

                if (data.Count() >= 1)
                    site.IsSelected = true;
            }

            dgridDisplaySelectSite.ItemsSource = listSiteStore;
        }

        private void lstAutoSetting_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstAutoSetting.SelectedItem == null)
                return;

            dispinfoAutoSelect = lstAutoSetting.SelectedItem as AutoSelect;

            // テキストボックスのWPFにDataContextとTextにBindingで設定される値を取得、コンボボックスに設定
            cmbMakeListKind.SelectedValue = dispinfoAutoSelect.Kind;
            cmbMakeListExt.SelectedValue = dispinfoAutoSelect.Extention;
            cmbMakeListMovieFolder.SelectedValue = dispinfoAutoSelect.Folder;
        }

        private void btnGetFileInfo_Click(object sender, RoutedEventArgs e)
        {
            // タブが「」の場合はテーブル「MOVIE_FILES」と実ファイルのマッチングを行い、未登録のみを抽出する
            if (tabitemDirExport.IsSelected)
            {
                MovieFileContentsParent parent = new MovieFileContentsParent();

                string dir = lstTargetDir.SelectedItem.ToString();
                List<MovieFileContents> listDirFiles = parent.GetDirFiles(dir);
                List <MovieFileContents> listDbFiles = parent.GetDbContents(dir);

                List<MovieFileContents> listTargetFiles = new List<MovieFileContents>();

                // 実ファイルで登録されていない場合
                foreach (MovieFileContents dirfile in listDirFiles)
                {
                    var data = from matchdata in listDbFiles
                               where matchdata.Name == dirfile.Name
                               select matchdata;

                    if (data.Count() <= 0)
                    {
                        dirfile.Remark = "DB追加";
                        listTargetFiles.Add(dirfile);
                    }
                }

                // DBにのみ存在して、実ファイルが無い場合
                foreach (MovieFileContents dbfile in listDbFiles)
                {
                    var data = from matchdata in listDirFiles
                               where matchdata.Name == dbfile.Name
                               select matchdata;

                    if (data.Count() <= 0)
                    {
                        dbfile.Remark = "DB削除";
                        listTargetFiles.Add(dbfile);
                    }
                }

                dgridSiteContents.ItemsSource = listTargetFiles;

                dgridMain.Visibility = System.Windows.Visibility.Hidden;
                dgridSiteContents.Visibility = System.Windows.Visibility.Visible;
                //dgridSiteContents.ItemsSource = listSiteData;
            }
            else if (tabitemSiteExport.IsSelected)
            {
                using (new WaitCursor())
                {
                    TargetFilesParent parent = new TargetFilesParent();

                    DetailInfoSetting setting = new DetailInfoSetting();
                    setting.SiteName = lstTargetSite.SelectedValue.ToString();
                    setting.MakeListKind = ConvertSelectedItem(cmbMakeListKind);
                    setting.MakeListExt = ConvertSelectedItem(cmbMakeListExt);
                    setting.MakeListMovieFolder = ConvertSelectedItem(cmbMakeListMovieFolder);
                    parent.setting = setting;

                    string SiteName = lstTargetSite.SelectedValue.ToString();

                    //dbcon.execSqlCommand("DELETE FROM MOVIE_SITECONTENTS WHERE SITE_NAME = '" + SiteName + "'");

                    List<string> listSiteAllPath = GetSiteAllPath(setting.SiteName, dbcon);

                    List<MovieSiteContents> listSiteData = new List<MovieSiteContents>();
                    List<TargetFile> listFiles = new List<TargetFile>();
                    foreach (string path in listSiteAllPath)
                    {
                        parent.GetDirectorysInfo(path);

                        List<TargetFile> files = parent.GetDetailInfo();

                        foreach (TargetFile file in files)
                        {
                            // TargetFileをMovieSiteContentsへコンバートして生成
                            MovieSiteContents dataReal = new MovieSiteContents(SiteName, file);

                            string strDir = Regex.Match(file.DirInfo.Parent.FullName, ".*" + SiteName + "\\\\(?<abc>.*)").Groups["abc"].Value;
                            // サイト名とNameが一致する情報をDBから取得、無い場合はnull
                            MovieSiteContents dataDb = MovieSiteContents.GetDbDataByName(SiteName, strDir, file.Name, dbcon);

                            if (SiteName.Equals("舞ワイフ"))
                            {
                                if (dataDb == null)
                                {
                                    dataDb = MovieSiteContents.GetDbDataByName(SiteName, strDir, Regex.Replace(file.Name, "MP4$", ""), dbcon);

                                    if (dataDb != null)
                                        dataDb.Remark = "DB名前変更「" + Regex.Replace(file.Name, "MP4$", "") + "->" + file.Name + "」、";
                                }
                            }

                            // MovieCount等の各種情報を設定する
                            dataReal.SetContentsInfo();

                            if (dataDb == null)
                            {
                                dataReal.Remark = "DB追加";
                                listSiteData.Add(dataReal);
                                //dataReal.DbExport(dbcon);
                            }
                            else
                            {
                                string updatecolname = dataDb.IsDbUpdate(dataReal);
                                if (updatecolname.Length > 0)
                                {
                                    dataDb.MovieNewDate = dataReal.MovieNewDate;
                                    dataDb.MovieCount = dataReal.MovieCount;
                                    dataDb.PhotoCount = dataReal.PhotoCount;
                                    dataDb.ParentPath = dataReal.ParentPath;
                                    dataDb.Extension = dataReal.Extension;

                                    dataDb.Remark = dataDb.Remark + "DB更新「" + updatecolname + "」";
                                    file.Message = "DB更新";
                                    listSiteData.Add(dataDb);
                                    //dataDb.DbUpdate(dbcon);
                                }
                            }
                            listFiles.Add(file);
                        }
                    }

                    // 削除のチェック（DBを基準にファイルに存在しないデータを削除対象とする）
                    List<MovieSiteContents> listDbSite = MovieSiteContentsParent.GetDbContents(SiteName);

                    foreach (MovieSiteContents dataSite in listDbSite)
                    {
                        var data = from file in listFiles
                                   where file.Name == dataSite.Name
                                   select file;

                        if (data.Count() <= 0)
                        {
                            dataSite.Remark = "DB削除";
                            listSiteData.Add(dataSite);
                        }
                    }


                    dgridMain.Visibility = System.Windows.Visibility.Hidden;
                    dgridSiteContents.Visibility = System.Windows.Visibility.Visible;
                    dgridSiteContents.ItemsSource = listSiteData;
                    //dgridMain.ItemsSource = listTargetFile;
                }
            }
            else
            {
                dgridMain.Visibility = System.Windows.Visibility.Visible;

                string sitepath = cmbMovieContentsPath.Text;

                // ファイルの情報をDataGridへ行追加する
                provTargetFiles = (ObjectDataProvider)this.Resources["TargetDirectoryProvider"];
                provTargetFiles.MethodParameters.Clear();
                provTargetFiles.DeferRefresh(); // 複数のMethodParametersにAddする場合には同メソッドで遅延させた後にRefreshメソッドで明示的に更新を行う
                provTargetFiles.MethodParameters.Add(sitepath);
                provTargetFiles.Refresh();

                parentTargetFiles = (TargetFilesParent)provTargetFiles.ObjectInstance;
                parentTargetFiles.mainWindow = this;

                // 画面の設定項目をThread実行からの参照用にTargetFilesParentに設定する
                DetailInfoSetting setting = new DetailInfoSetting();
                setting.SiteName = dispinfoSiteStore.Name;
                setting.MakeListKind = ConvertSelectedItem(cmbMakeListKind);
                setting.MakeListExt = ConvertSelectedItem(cmbMakeListExt);
                setting.MakeListMovieFolder = ConvertSelectedItem(cmbMakeListMovieFolder);
                parentTargetFiles.setting = setting;

                // 写真数、動画数などの取得はスレッドで実行する
                Thread thread = new Thread(parentTargetFiles.SetDetailInfo);
                thread.Start();
            }
        }

        /// <summary>
        /// XmlDataProviderの値を設定している画面コントールからXMLタグ内の値のみを取得する
        /// </summary>
        /// <param name="myCombo"></param>
        /// <returns></returns>
        private string ConvertSelectedItem(ComboBox myCombo)
        {
            XmlElement ele = (XmlElement)myCombo.SelectedItem;

            // 未選択状態の場合
            if (ele == null)
                return "";

            XmlNode node = ele.LastChild;
            string name = node.InnerText;

            return name;
        }

        /// <summary>
        /// 別スレッドで取得した詳細情報から選択するための項目をグリッド内の詳細の項目から生成する
        /// </summary>
        public void ThreadEndGetFileInfo()
        {
            Debug.Print("ThreadEnd");
            List<TargetFile> listTargetFiles = (List<TargetFile>)dgridMain.ItemsSource;

            if (listTargetFiles == null)
                return;

            List<string> listMessage = new List<string>();
            foreach (TargetFile file in listTargetFiles)
            {
                if (file.Message == null)
                    continue;

                bool IsExist = false;
                foreach(string Message in listMessage)
                {
                    if (file.Message.Equals(Message))
                    {
                        IsExist = true;
                        break;
                    }
                }

                if (IsExist == false)
                    listMessage.Add(file.Message);
            }

            if (listMessage.Count > 0)
            {
                cmbSelectTarget.ItemsSource = listMessage;
                cmbSelectTarget.SelectedIndex = 0;
            }

            SetGridMainCount();
        }

        private void dgridSiteInfo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dispinfoSiteStore = dgridSiteInfo.SelectedItem as SiteStore;

            if (dispinfoSiteStore == null)
                return;

            List<SiteStore> listSiteStore = CommonMethod.GetSiteStore(dbcon);

            var pathdata = from matchdata in listSiteStore
                           where matchdata.Name == dispinfoSiteStore.Name
                           orderby matchdata.Path descending
                           select matchdata;

            cmbMovieContentsPath.ItemsSource = pathdata;

            cmbMovieContentsPath.SelectedIndex = 0;
        }

        class FindSearchSiteName
        {
            public string sitename;
            public bool Match(string myData)
            {
                if (myData.IndexOf(sitename) >= 0)
                    return true;

                return false;
            }
        }

        private void OnDataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 「wpf datagrid checkbox single click」で検索
            // 参考：http://social.msdn.microsoft.com/Forums/ja-JP/wpfja/thread/8a9a0654-1aff-4144-9167-232b2a91fafe/
            //       http://wpf.codeplex.com/wikipage?title=Single-Click Editing&ProjectName=wpf
            DataGridCell cell = sender as DataGridCell;

            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                TargetFile selStartFile = (TargetFile)dgridMain.SelectedItem;

                if (selStartFile != null)
                {
                    DataGridRow row = FindVisualParent<DataGridRow>(cell);
                    TargetFile selEndFile = row.Item as TargetFile;
                    //Debug.Print("Shiftキーが押されたよ name [" + selStartFile.Name + "] ～ [" + selEndFile.Name + "]");

                    bool selStart = false;
                    foreach (TargetFile file in dgridMain.ItemsSource)
                    {
                        if (file.Name.Equals(selStartFile.Name))
                            selStart = true;

                        if (selStart)
                            file.IsSelected = true;

                        if (file.Name.Equals(selEndFile.Name))
                            break;
                    }

                    SetGridMainCount();
                    return;
                }
            }

            // 編集可能なセルの場合のみ実行
            if (cell != null && !cell.IsEditing && !cell.IsReadOnly)
            {
                // フォーカスが無い場合はフォーカスを取得
                if (!cell.IsFocused)
                    cell.Focus();

                DataGrid dataGrid = FindVisualParent<DataGrid>(cell);
                if (dataGrid != null)
                {
                    if (dataGrid.SelectionUnit != DataGridSelectionUnit.FullRow)
                    {
                        if (!cell.IsSelected)
                            cell.IsSelected = true;
                    }
                    else
                    {
                        DataGridRow row = FindVisualParent<DataGridRow>(cell);

                        TargetFile selFile = row.Item as TargetFile;

                        if (selFile != null)
                        {
                            if (row != null && !row.IsSelected)
                            {
                                if (selFile.IsSelected)
                                    selFile.IsSelected = false;
                                else
                                    //row.IsSelected = true;
                                    selFile.IsSelected = true;
                            }
                            else
                            {
                                if (row.IsSelected && selFile.IsSelected)
                                    row.IsSelected = false;

                                selFile.IsSelected = false;
                            }
                        }
                        else
                        {
                            MovieFileContents selMFile = row.Item as MovieFileContents;
                        }
                    }
                }
            }

            SetGridMainCount();

        }

        private void OnDataGridSiteContents_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 「wpf datagrid checkbox single click」で検索
            // 参考：http://social.msdn.microsoft.com/Forums/ja-JP/wpfja/thread/8a9a0654-1aff-4144-9167-232b2a91fafe/
            //       http://wpf.codeplex.com/wikipage?title=Single-Click Editing&ProjectName=wpf
            DataGridCell cell = sender as DataGridCell;

            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                MovieSiteContents selStartFile = (MovieSiteContents)dgridSiteContents.SelectedItem;

                if (selStartFile != null)
                {
                    DataGridRow row = FindVisualParent<DataGridRow>(cell);
                    MovieSiteContents selEndFile = row.Item as MovieSiteContents;
                    //Debug.Print("Shiftキーが押されたよ name [" + selStartFile.Name + "] ～ [" + selEndFile.Name + "]");

                    bool selStart = false;
                    foreach (MovieSiteContents data in dgridSiteContents.ItemsSource)
                    {
                        if (data.Name.Equals(selStartFile.Name))
                            selStart = true;

                        if (selStart)
                            data.IsSelected = true;

                        if (data.Name.Equals(selEndFile.Name))
                            break;
                    }

                    SetGridMainCount();
                    return;
                }
            }

            // 編集可能なセルの場合のみ実行
            if (cell != null && !cell.IsEditing && !cell.IsReadOnly)
            {
                // フォーカスが無い場合はフォーカスを取得
                if (!cell.IsFocused)
                    cell.Focus();

                DataGrid dataGrid = FindVisualParent<DataGrid>(cell);
                if (dataGrid != null)
                {
                    if (dataGrid.SelectionUnit != DataGridSelectionUnit.FullRow)
                    {
                        if (!cell.IsSelected)
                            cell.IsSelected = true;
                    }
                    else
                    {
                        DataGridRow row = FindVisualParent<DataGridRow>(cell);

                        MovieSiteContents selFile = row.Item as MovieSiteContents;

                        if (selFile != null)
                        {
                            if (row != null && !row.IsSelected)
                            {
                                if (selFile.IsSelected)
                                    selFile.IsSelected = false;
                                else
                                    //row.IsSelected = true;
                                    selFile.IsSelected = true;
                            }
                            else
                            {
                                if (row.IsSelected && selFile.IsSelected)
                                    row.IsSelected = false;

                                selFile.IsSelected = false;
                            }
                        }
                    }
                }
            }

            SetGridMainCount();

        }

        /// <summary>
        /// ファイル情報取得の右のファイル数のラベル情報を更新する
        /// </summary>
        public void SetGridMainCount()
        {
            int SelectCount = 0;

            if (dgridMain.Visibility == System.Windows.Visibility.Visible)
            {
                foreach (TargetFile file in dgridMain.ItemsSource)
                {
                    if (file.IsSelected)
                        SelectCount++;
                }
                List<TargetFile> list = (List<TargetFile>)dgridMain.ItemsSource;
                int Count = list.Count();

                lblCountValue.Content = SelectCount + "/" + Count;
            }
            else
            {
                int Count = 0;
                var listData = dgridSiteContents.ItemsSource;
                foreach (var data in dgridSiteContents.ItemsSource)
                {
                    string type = data.GetType().ToString();
                    if (type.IndexOf("MovieSiteContents") >= 0)
                    {
                        MovieSiteContents site = data as MovieSiteContents;
                        if (site.IsSelected)
                            SelectCount++;
                    }
                    if (type.IndexOf("MovieFileContents") >= 0)
                    {
                        MovieFileContents file = data as MovieFileContents;
                        if (file.IsSelected)
                            SelectCount++;
                    }
                    Count++;
                }
/*
                foreach (MovieSiteContents data in dgridSiteContents.ItemsSource)
                {
                    if (data.IsSelected)
                        SelectCount++;
                }
 */
   //             List<MovieSiteContents> list = (List<MovieSiteContents>)dgridSiteContents.ItemsSource;
                //int Count = listData.Count();

                lblCountValue.Content = SelectCount + "/" + Count;

            }
            //return SelectCount + "/" + Count;
        }
        static T FindVisualParent<T>(UIElement element) where T : UIElement
        {
            UIElement parent = element;
            while (parent != null)
            {
                T correctlyTyped = parent as T;
                if (correctlyTyped != null)
                {
                    return correctlyTyped;
                }

                parent = VisualTreeHelper.GetParent(parent) as UIElement;
            }
            return null;
        }

        private void btnExecute_Click(object sender, RoutedEventArgs e)
        {
            // タブが「」の場合はテーブル「MOVIE_FILES」と実ファイルのマッチングを行い、未登録のみを抽出する
            if (tabitemDirExport.IsSelected)
            {
                dbcon.openConnection();
                foreach (MovieFileContents data in dgridSiteContents.ItemsSource)
                {
                    if (data.IsSelected)
                    {
                        if (data.Remark.IndexOf("DB追加") >= 0)
                            // 取得したDIR情報に対してファイル情報をMOVIE_FILESテーブルへ格納する
                            data.DbExport(dbcon);
                        else if (data.Remark.IndexOf("DB削除") >= 0)
                            data.DbDelete(dbcon);
                    }
                }
                dbcon.closeConnection();

                return;
            }
            else if (tabitemSiteExport.IsSelected)
            {
                dbcon.openConnection();
                foreach (MovieSiteContents data in dgridSiteContents.ItemsSource)
                {
                    if (data.IsSelected)
                    {
                        if (data.Remark.IndexOf("DB更新") >= 0)
                            data.DbUpdate(dbcon);
                        else if (data.Remark.IndexOf("DB追加") >= 0)
                            data.DbExport(dbcon);
                    }
                }
                dbcon.closeConnection();

                return;
            }
            string ErrorFilename = "";
            try
            {
                foreach (TargetFile file in dgridMain.ItemsSource)
                {
                    if (file.IsSelected)
                    {
                        string MoviePath = System.IO.Path.Combine(file.DirInfo.FullName, parentTargetFiles.setting.MakeListMovieFolder);

                        if (file.Message == null)
                            MakeList(file, MoviePath);
                        else
                        {
                            if (file.Message.IndexOf("動画ZIPファイルあり") >= 0)
                            {
                                string[] filesZip = Directory.GetFiles(MoviePath, "*.zip", SearchOption.TopDirectoryOnly);

                                string[] TargetRegex = { ".*" };
                                for (int IndexSubArr = 0; IndexSubArr < filesZip.Length; IndexSubArr++)
                                {
                                    ErrorFilename = System.IO.Path.Combine(file.DirInfo.FullName, filesZip[IndexSubArr]);
                                    ZipArchiveFile.ExtractPattern(filesZip[IndexSubArr], null, TargetRegex);
                                }
                                //    ZIPファイルの削除
                                for (int IndexSubArr = 0; IndexSubArr < filesZip.Length; IndexSubArr++)
                                    File.Delete(filesZip[IndexSubArr]);

                                MakeList(file, MoviePath);
                            }
                            if (file.Message.IndexOf("動画ファイル更新あり") >= 0)
                                MakeList(file, MoviePath);
                            if (file.Message.IndexOf("画像ZIPファイルあり") >= 0)
                            {
                                string ExtractPattern = ConvertSelectedItem(cmbAutoSettingExtractPatterns);
                                string[] TargetRegexs = ExtractPattern.Split(',');
                                OrganizeZipFile(file.DirInfo, TargetRegexs);
                            }
                            if (file.Message.IndexOf("動画フォルダなし") >= 0)
                            {
                                // 動画フォルダの作成
                                OrganizeMakeFolder(file.DirInfo, parentTargetFiles.setting.MakeListMovieFolder);

                                // 動画ファイルの移動
                                string MoveMovieExt = ConvertSelectedItem(cmbAutoSettingExts);
                                string[] TargetRegexs = MoveMovieExt.Split(',');
                                OrganizeMoveFile(file.DirInfo, parentTargetFiles.setting.MakeListMovieFolder, TargetRegexs);

                                MakeList(file, MoviePath);
                            }
                        }
                    }
                }
            }
            catch (SharpZipBaseException exp)
            {
                MessageBox.Show(exp.Message);
                Debug.Write(exp);

                // ファイルの解凍に失敗したファイルは削除する
            }
        }

        private void OrganizeMakeFolder(DirectoryInfo myOneActressDir, string myMakeMoviePath)
        {
            string MoviePathname = System.IO.Path.Combine(myOneActressDir.FullName, myMakeMoviePath);

            DirectoryInfo MovieDir = new DirectoryInfo(MoviePathname);
            if (!MovieDir.Exists)
                Directory.CreateDirectory(MoviePathname);

            return;
        }

        private void OrganizeMoveFile(DirectoryInfo myOneActressDir, string myMakeMoviePath, string[] myTargetPattern)
        {
            for (int IndexArr = 0; IndexArr < myTargetPattern.Length; IndexArr++)
            {
                string[] MovieFiles = Directory.GetFiles(myOneActressDir.FullName, myTargetPattern[IndexArr].Trim(), SearchOption.TopDirectoryOnly);
                for (int IndexSubArr = 0; IndexSubArr < MovieFiles.Length; IndexSubArr++)
                {
                    FileInfo MovieFile = new FileInfo(MovieFiles[IndexSubArr]);
                    string TempFilename = System.IO.Path.Combine(myOneActressDir.FullName, myMakeMoviePath);
                    string DestFilename = System.IO.Path.Combine(TempFilename, MovieFile.Name);
                    File.Move(MovieFiles[IndexSubArr], DestFilename);
                }
            }
        }
        private void OrganizeZipFile(DirectoryInfo myOneActressDir, string[] myTargetRegex)
        {
            string[] filesZip = Directory.GetFiles(myOneActressDir.FullName, "*.zip", SearchOption.TopDirectoryOnly);

            for (int IndexSubArr = 0; IndexSubArr < filesZip.Length; IndexSubArr++)
                ZipArchiveFile.ExtractPattern(filesZip[IndexSubArr], null, myTargetRegex);

            // 4) ZIPファイルの削除
            for (int IndexSubArr = 0; IndexSubArr < filesZip.Length; IndexSubArr++)
                File.Delete(filesZip[IndexSubArr]);
        }

        private void MakeList(TargetFile myTargetFile, string myMoviePath)
        {
            // 動画リストの再作成
            string kind = parentTargetFiles.setting.MakeListKind;
            string ext = parentTargetFiles.setting.MakeListExt;
            if (kind.Equals("WPL"))
            {
                string[] arrTargetExt = null;
                if (dispinfoSiteStore.Name.Equals("舞ワイフ"))
                {
                    arrTargetExt = new string[3];
                    arrTargetExt[0] = "w_*";
                    arrTargetExt[1] = "31_*";
                    arrTargetExt[2] = "Bw_*";
                    //arrTargetExt[0] = "w_*." + ext;
                    //arrTargetExt[1] = "Bw_*." + ext;
                    //arrTargetExt[2] = "31_*." + ext;
                }
                else
                {
                    arrTargetExt = new string[1];
                    arrTargetExt[0] = "*." + ext;
                }

                MoviePlayList.MakeWplFile("." + parentTargetFiles.setting.MakeListKind
                    , myTargetFile.DirInfo.FullName, arrTargetExt, parentTargetFiles.setting.MakeListMovieFolder);
            }
            else
            {
                string[] arrTargetExt = null;
                if (dispinfoSiteStore.Name.Equals("舞ワイフ"))
                {
                    arrTargetExt = new string[3];
                    arrTargetExt[0] = "w_*";
                    arrTargetExt[1] = "31_*";
                    arrTargetExt[2] = "Bw_*";
                    //arrTargetExt[0] = "w_*." + ext;
                    //arrTargetExt[1] = "Bw_*." + ext;
                    //arrTargetExt[2] = "31_*." + ext;
                }
                else
                {
                    arrTargetExt = new string[1];
                    arrTargetExt[0] = "*." + ext;
                }

                MoviePlayList.MakeAsxFile("." + parentTargetFiles.setting.MakeListKind
                    , myTargetFile.DirInfo.FullName, arrTargetExt, parentTargetFiles.setting.MakeListMovieFolder);
            }
        }

        private void menuitemMakeFolder_Click(object sender, RoutedEventArgs e)
        {
            winMakeFolder win = new winMakeFolder(cmbMovieContentsPath.Text);

            win.Show();
        }

        private void menuitemCopyFilePath_Click(object sender, RoutedEventArgs e)
        {
            TargetFile selStartFile = (TargetFile)dgridMain.SelectedItem;

            DirectoryInfo dirinfo = selStartFile.DirInfo;

            if (dirinfo.Exists)
            {
                //コピーするファイルのパスをStringCollectionに追加する
                System.Collections.Specialized.StringCollection files =
                    new System.Collections.Specialized.StringCollection();
                files.Add(dirinfo.FullName);

                Clipboard.SetFileDropList(files);
            }
        }

        private void menuitemCopyFilePathText_Click(object sender, RoutedEventArgs e)
        {
            TargetFile selStartFile = (TargetFile)dgridMain.SelectedItem;

            DirectoryInfo dirinfo = selStartFile.DirInfo;

            if (dirinfo.Exists)
                Clipboard.SetText(dirinfo.FullName);
        }

        private void dgridMain_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TargetFile selStartFile = (TargetFile)dgridMain.SelectedItem;
            Debug.Print(selStartFile.DirInfo.FullName);

            // ファイルの情報をDataGridへ行追加する
            provTargetFiles = (ObjectDataProvider)this.Resources["TargetFilesProvider"];
            provTargetFiles.MethodParameters.Clear();
            provTargetFiles.DeferRefresh(); // 複数のMethodParametersにAddする場合には同メソッドで遅延させた後にRefreshメソッドで明示的に更新を行う
            provTargetFiles.MethodParameters.Add(selStartFile.DirInfo.FullName);
            provTargetFiles.Refresh();

            dgridMain.Visibility = System.Windows.Visibility.Hidden;
            dgridFileList.Visibility = System.Windows.Visibility.Visible;

        }

        private void DockPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // MouseDownでファイルリスト非表示の処理を行うと画面がチラつくので実処理はMouseUpで行う
            if (e.XButton1 == MouseButtonState.Pressed)
                IsMouseXButton1 = true;
        }

        private void DockPanel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // 戻るボタンが押下された場合はファイルリストを非表示にする
            if (IsMouseXButton1)
            {
                dgridMain.Visibility = System.Windows.Visibility.Visible;
                dgridFileList.Visibility = System.Windows.Visibility.Hidden;
                IsMouseXButton1 = false;
            }
        }

        public List<string> GetSiteAllPath(string mySiteName, DbConnection myDbCon)
        {
            try
            {
                string queryString = "";

                queryString = "SELECT PATH FROM MOVIE_SITESTORE WHERE NAME = '" + mySiteName + "'";

                SqlDataAdapter adapter = new SqlDataAdapter(queryString, myDbCon.getSqlConnection());

                DataSet dtsetSitePath = new DataSet();
                adapter.Fill(dtsetSitePath, "SitePath");

                // 参考： wpfMovieManager::MainWIndow.xaml GridvGroupDatabaseFill
                var rows = dtsetSitePath.Tables["SitePath"].AsEnumerable();
                var query = rows.Select(el => el["PATH"].ToString());

                return query.ToList<string>();
            }
            catch (Exception exp)
            {
                Debug.Write(exp);
            }

            return null;
        }

        private List<SiteStore> GetSelectSiteFromXmlFile()
        {
            string ListPathname = "TARGET_SITESTORE.xml";

            XElement root = null;

            // 未実行の行をキューへ登録する
            // XMLファイルを読み込む、存在しない場合は作成
            try
            {
                root = XElement.Load(ListPathname);
            }
            catch (FileNotFoundException)
            {
//                _logger.Debug("FileNotFoundException [" + ListPathname + "] GetDownloadListFromXmlFile");
                root = new XElement("SelectSiteList");
            }

            var listAll = from element in root.Elements("SiteInfo")
                          select element;

            List<SiteStore> listSelectSiteStore = new List<SiteStore>();

            foreach (XContainer xcon in listAll)
            {
                SiteStore sitestore = new SiteStore();

                try
                {
                    sitestore.Name = xcon.Element("NAME").Value;
                    sitestore.Path = xcon.Element("PATH").Value;
                }
                catch (NullReferenceException)
                {
                    //_logger.Error("項目取得エラー");
                    // XML内にElementが存在しない場合に発生、無視する
                }

                listSelectSiteStore.Add(sitestore);
            }

            return listSelectSiteStore;
        }

        private void btnSelectExecute_Click(object sender, RoutedEventArgs e)
        {
            string ListPathname = "TARGET_SITESTORE.xml";

            XElement root = null;

            root = new XElement("SelectSiteList");

            List<SiteStore> listSiteStore = (List<SiteStore>)dgridDisplaySelectSite.ItemsSource;

            foreach (SiteStore site in listSiteStore)
            {
                if (site.IsSelected)
                {
                    root.Add(new XElement("SiteInfo"
                                        , new XElement("NAME", site.Name)
                                        , new XElement("PATH", site.Path)
                                ));
                }
            }

            root.Save(ListPathname);

            DisplayGrid();

            btnSelectCancel_Click(null, null);
        }

        private void menuitemSelectSiteInfo_Click(object sender, RoutedEventArgs e)
        {
            lgridSelectSiteInfo.Visibility = System.Windows.Visibility.Visible;
            lgridMain.Visibility = System.Windows.Visibility.Hidden;
        }

        private void btnSelectCancel_Click(object sender, RoutedEventArgs e)
        {
            lgridSelectSiteInfo.Visibility = System.Windows.Visibility.Collapsed;
            lgridMain.Visibility = System.Windows.Visibility.Visible;
        }

        private void menuitemAutoSelectEdit_Click(object sender, RoutedEventArgs e)
        {
            AutoSelect autosel = lstAutoSetting.SelectedItem as AutoSelect;

            cmbAutoSelectMakeListKind.SelectedValue = autosel.Kind;
            cmbAutoSelectMakeListExt.SelectedValue = autosel.Extention;
            cmbAutoSelectMakeListMovieFolder.SelectedValue = autosel.Folder;

            txtAutoSelectName.Text = autosel.Name;

            dispinfoAutoSelectEditingNo = autosel.No;

            tabitemAutoSelect.IsSelected = true;
        }

        private void menuitemAutoSelectAdd_Click(object sender, RoutedEventArgs e)
        {
            dispinfoAutoSelectEditingNo = -1;

            tabitemAutoSelect.IsSelected = true;
        }

        private void btnSaveCancelAutoSelect_Click(object sender, RoutedEventArgs e)
        {
            tabitemMakeList.IsSelected = true;
        }

        private void btnSaveAutoSelect_Click(object sender, RoutedEventArgs e)
        {

            AutoSelect data = new AutoSelect();

            data.No = dispinfoAutoSelectEditingNo;
            data.Name = txtAutoSelectName.Text;
            data.Kind = cmbAutoSelectMakeListKind.Text;
            data.Extention = cmbAutoSelectMakeListExt.Text;
            data.Folder = cmbAutoSelectMakeListMovieFolder.Text;

            AutoSelectXmlControl.Save(listAutoSelect, data);

            listAutoSelect = AutoSelectXmlControl.GetList();
            lstAutoSetting.ItemsSource = listAutoSelect;

            lstAutoSetting_SelectionChanged(null, null);

            tabitemMakeList.IsSelected = true;
        }

        private void menuitemAutoSelectDelete_Click(object sender, RoutedEventArgs e)
        {
            AutoSelectXmlControl.Delete(listAutoSelect, dispinfoAutoSelect);

            listAutoSelect = AutoSelectXmlControl.GetList();
            lstAutoSetting.ItemsSource = listAutoSelect;

            lstAutoSetting_SelectionChanged(null, null);

            //tabitemMakeList.IsSelected = true;
        }

        private List<string> GetDirInfo()
        {
            DbConnection dbcon = new DbConnection();

            string queryString = "SELECT ID, NAME, REMARK, ISNULL(ACTIVITY_DATE, '1900/1/1') AS ACTIVITY_DATE FROM MOVIE_ACTRESS WHERE REMARK LIKE 'DIR情報%' ORDER BY ACTIVITY_DATE";

            dbcon.openConnection();

            SqlCommand command = new SqlCommand(queryString, dbcon.getSqlConnection());
            SqlDataReader reader = command.ExecuteReader();

            List<string> listData = new List<string>();
            do
            {
                while (reader.Read())
                {
                    string ContentsPath = DbExportCommon.GetDbString(reader, 2);

                    string strDir = Regex.Match(ContentsPath, "【(?<abc>.*)】").Groups["abc"].Value;

                    if (strDir != null)
                        listData.Add(strDir);
                }
            } while (reader.NextResult());
            reader.Close();

            return listData;
        }
        private List<string> GetSiteInfo()
        {
            DbConnection dbcon = new DbConnection();

            string queryString = "SELECT ID, NAME, REMARK, ISNULL(ACTIVITY_DATE, '1900/1/1') AS ACTIVITY_DATE FROM MOVIE_ACTRESS WHERE REMARK LIKE 'DIR情報%' ORDER BY ACTIVITY_DATE";

            dbcon.openConnection();

            SqlCommand command = new SqlCommand(queryString, dbcon.getSqlConnection());
            SqlDataReader reader = command.ExecuteReader();

            List<string> listData = new List<string>();
            do
            {
                while (reader.Read())
                {
                    string ContentsPath = DbExportCommon.GetDbString(reader, 2);

                    string strDir = Regex.Match(ContentsPath, "【(?<abc>.*)】").Groups["abc"].Value;

                    if (strDir != null)
                        listData.Add(strDir);
                }
            } while (reader.NextResult());
            reader.Close();

            return listData;
        }
    }
}
