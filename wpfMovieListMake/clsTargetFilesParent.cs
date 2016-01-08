using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace wpfMovieListMake
{
    class TargetFilesParent
    {
        public MainWindow mainWindow;
        DirectoryInfo dirinfoMain = null;
        List<TargetFile> listTargetFiles = null;
        public DetailInfoSetting setting = null;

        public TargetFilesParent()
        {
            listTargetFiles = new List<TargetFile>();
            //mainWindows = myMainWindow;
        }
        public List<TargetFile> GetDirectorysInfo(string myPath)
        {
            if (myPath == null)
                return null;

            dirinfoMain = new DirectoryInfo(myPath);

            listTargetFiles = new List<TargetFile>();
            string[] arrTargetDir = null;

            try
            {
                arrTargetDir = Directory.GetDirectories(myPath, "*", SearchOption.TopDirectoryOnly);
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                //Directory.CreateDirectory(myPath);
                return null;
            }
            catch (IOException)
            {
                // デバイスの準備が出来ていません
                return null;
            }

            for (int idxArr = 0; idxArr < arrTargetDir.Length; idxArr++)
            {
                DirectoryInfo dir = new DirectoryInfo(arrTargetDir[idxArr]);

                TargetFile file = new TargetFile();
                file.DirInfo = dir;

                listTargetFiles.Add(file);
            }
            Debug.Print("GetDirectorysInfo 件数 [" + listTargetFiles.Count + "]");

            return listTargetFiles;
        }
        public List<DisplayFile> GetFilesInfo(string myPath)
        {
            if (myPath == null)
                return null;

            dirinfoMain = new DirectoryInfo(myPath);

            List<DisplayFile> listDispFiles = new List<DisplayFile>();
            string[] arrTargetFiles = null;

            try
            {
                arrTargetFiles = Directory.GetFiles(myPath, "*", SearchOption.AllDirectories);
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                //Directory.CreateDirectory(myPath);
                return null;
            }

            for (int idxArr = 0; idxArr < arrTargetFiles.Length; idxArr++)
            {
                FileInfo file = new FileInfo(arrTargetFiles[idxArr]);

                DisplayFile dispfile = new DisplayFile();
                dispfile.DirInfo = file.Directory;

                if (dirinfoMain.Name != file.Directory.Name)
                    dispfile.DirName = file.Directory.Name;

                dispfile.MovieNewDate = file.LastWriteTime;
                dispfile.Size = file.Length;
                dispfile.Name = file.Name;

                listDispFiles.Add(dispfile);
            }
            Debug.Print("GetFilesInfo 件数 [" + listDispFiles.Count + "]");

            return listDispFiles;
        }
        /// <summary>
        /// ファイル情報取得ボタンが押下された後ファイル情報取得後に別スレッドで稼働
        /// 設定情報から各種の情報を取得、画面表示のグリッド用にTargetFileに設定する
        /// </summary>
        public void SetDetailInfo()
        {
            foreach (TargetFile file in listTargetFiles)
            {
                // 件数の取得：動画
                file.strMovieCount = GetMovieCount(file);

                // 件数の取得：画像
                string[] matchfile = Directory.GetFiles(file.DirInfo.FullName, "*jpg", SearchOption.TopDirectoryOnly);
                file.PhotoCount = matchfile.Count();

                // プレイリストの最終日付の取得
                string PlayListFile = GetPlayListFile(file);
                if (File.Exists(PlayListFile))
                {
                    DirectoryInfo dirinfo = new DirectoryInfo(PlayListFile);
                    file.ListUpdateDate = dirinfo.LastWriteTime;
                }

                // 各チェックの詳細情報の結果取得
                string MovieSearchPath = Path.Combine(file.DirInfo.FullName, setting.MakeListMovieFolder);

                // 1)
                if (IsZipFiles(MovieSearchPath, SearchOption.TopDirectoryOnly))
                    file.Message = "動画ZIPファイルあり";

                // 2)
                if (IsZipFiles(file.DirInfo.FullName, SearchOption.TopDirectoryOnly))
                    file.Message = "画像ZIPファイルあり";

                // 3)
                DateTime dtNew = GetMaxLastWriteDate(MovieSearchPath);
                file.MovieNewDate = dtNew;
                if (file.strMovieNewDate.Length > 0)
                {
                    if (file.MovieNewDate > file.ListUpdateDate)
                        file.Message = "動画ファイル更新あり";
                }

                // 4)
                if (!Directory.Exists(MovieSearchPath))
                    file.Message = "動画フォルダなし";

                if (setting.SiteName.Equals("舞ワイフ"))
                {
                    Regex regex = new Regex(MovieFileContents.REGEX_MOVIE_EXTENTION);
                    List<string> listFiles = new List<string>();

                    string mp4Path = Path.Combine(file.DirInfo.FullName, "MP4");
                    string moviePath = Path.Combine(file.DirInfo.FullName, "movie");

                    int mp4Count = -1;
                    if (Directory.Exists(mp4Path))
                    {
                        string[] mp4Files = Directory.GetFiles(mp4Path, "*" + setting.MakeListExt, SearchOption.TopDirectoryOnly);

                        foreach (var onefile in mp4Files)
                        {
                            FileInfo fileinfo = new FileInfo(onefile.ToString());
                            if (regex.IsMatch(fileinfo.Name.ToLower()))
                                mp4Count++;
                        }
                    }

                    int movieCount = -1;
                    if (Directory.Exists(moviePath))
                    {
                        string[] movieFiles = Directory.GetFiles(moviePath, "*", SearchOption.TopDirectoryOnly);

                        foreach (var onefile in movieFiles)
                        {
                            FileInfo fileinfo = new FileInfo(onefile.ToString());
                            if (regex.IsMatch(fileinfo.Name.ToLower()))
                                movieCount++;
                        }
                    }
                    if (mp4Count >= 0 && movieCount >= 0 && mp4Count < movieCount)
                        file.Message = "MOVIE(" + movieCount + ") ＞ MP4(" + mp4Count + ") ";
                    else if (mp4Count >= 0 && movieCount >= 0 && mp4Count > movieCount)
                        file.Message = "MOVIE(" + movieCount + ") ＜ MP4(" + mp4Count + ") ";
                }

            }
            // Window側でスレッド終了後の処理を行う
            //   ※ 選択用のComboBox項目の作成 etc...
            mainWindow.Dispatcher.Invoke(new dlgThreadEndGetFileInfo(mainWindow.ThreadEndGetFileInfo));
        }

        /// <summary>
        /// ファイル情報取得ボタンが押下された後ファイル情報取得（別スレッドでは稼働しない）
        /// 設定情報から各種の情報を取得、画面表示のグリッド用にTargetFileに設定する
        /// </summary>
        public List<TargetFile> GetDetailInfo()
        {
            foreach (TargetFile file in listTargetFiles)
            {
                // 件数の取得：動画
                file.strMovieCount = GetMovieCount(file);

                // 件数の取得：画像
                string[] matchfile = Directory.GetFiles(file.DirInfo.FullName, "*jpg", SearchOption.TopDirectoryOnly);
                file.PhotoCount = matchfile.Count();

                // プレイリストの最終日付の取得
                string PlayListFile = GetPlayListFile(file);
                if (File.Exists(PlayListFile))
                {
                    DirectoryInfo dirinfo = new DirectoryInfo(PlayListFile);
                    file.ListUpdateDate = dirinfo.LastWriteTime;
                }

                // 各チェックの詳細情報の結果取得
                string MovieSearchPath = Path.Combine(file.DirInfo.FullName, setting.MakeListMovieFolder);

                // 1)
                if (IsZipFiles(MovieSearchPath, SearchOption.TopDirectoryOnly))
                    file.Message = "動画ZIPファイルあり";

                // 2)
                if (IsZipFiles(file.DirInfo.FullName, SearchOption.TopDirectoryOnly))
                    file.Message = "画像ZIPファイルあり";

                // 3)
                DateTime dtNew = GetMaxLastWriteDate(MovieSearchPath);
                file.MovieNewDate = dtNew;
                if (file.strMovieNewDate.Length > 0)
                {
                    if (file.MovieNewDate > file.ListUpdateDate)
                        file.Message = "動画ファイル更新あり";
                }

                // 4)
                if (!Directory.Exists(MovieSearchPath))
                    file.Message = "動画フォルダなし";

                //   MakeMovieList：動画フォルダなし、動画ファイルなし、動画ファイル更新あり
                //   AutoOrganize ：MOVIEフォルダにZIPファイル、ZIPファイルは存在しません
            }

            return listTargetFiles;
        }

        private string GetPlayListFile(TargetFile myFile)
        {
            string[] arrSearchFilePattern = { "_MP4", "_DIRNAME*" };

            foreach (string FilePattern in arrSearchFilePattern)
            {
                string SearchFilename = "";

                if (FilePattern.IndexOf("DIRNAME") >= 0)
                    SearchFilename = FilePattern.Replace("DIRNAME", myFile.DirInfo.Name).Replace("MP4", "") + "." + setting.MakeListKind;
                else
                    SearchFilename = FilePattern + "." + setting.MakeListKind;
                string[] matchfile = Directory.GetFiles(myFile.DirInfo.FullName, SearchFilename, SearchOption.TopDirectoryOnly);

                if (matchfile.Count() > 0)
                    return matchfile[0];
            }

            return "";
        }
        /// <summary>
        /// 動画の件数をカウントする、通常はxx(zz)件だが舞ワイフはxx(yy)/xx(yy)の形式でリターン（xxはwmvファイル、yyはzip形式）zzはmovieフォルダ以外の動画ファイル
        /// </summary>
        /// <param name="myTargetFile"></param>
        /// <returns></returns>
        private string GetMovieCount(TargetFile myTargetFile)
        {
            int NormalCount = 0, BlueCount = 0;
            int NormalZipCount = 0, BlueZipCount = 0;
            string DisplayCount = "";

            string MovieSearchPath = null;
            if (setting == null)
                MovieSearchPath = myTargetFile.DirInfo.FullName;
            else
                MovieSearchPath = Path.Combine(myTargetFile.DirInfo.FullName, setting.MakeListMovieFolder);

            //if (!Directory.Exists(MovieSearchPath))
            //    return "";

            if (setting.SiteName.Equals("舞ワイフ"))
            {
                if (Directory.Exists(MovieSearchPath))
                {
                    NormalCount = Directory.GetFiles(MovieSearchPath, "w*" + setting.MakeListExt, SearchOption.TopDirectoryOnly).Count();
                    BlueCount = Directory.GetFiles(MovieSearchPath, "Bw*" + setting.MakeListExt, SearchOption.TopDirectoryOnly).Count();
                    NormalZipCount = Directory.GetFiles(MovieSearchPath, "w*zip", SearchOption.TopDirectoryOnly).Count();
                    BlueZipCount = Directory.GetFiles(MovieSearchPath, "Bw*zip", SearchOption.TopDirectoryOnly).Count();
                }

                if (NormalCount == 0 && BlueCount == 0 && NormalZipCount == 0 && BlueZipCount == 0)
                    DisplayCount = "";
                else
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(NormalCount);
                    if (NormalZipCount > 0)
                        sb.Append("(" + NormalZipCount + ")/");
                    else
                        sb.Append("/");

                    sb.Append(BlueCount);
                    if (BlueZipCount > 0)
                        sb.Append("(" + BlueZipCount + ")");

                    DisplayCount = sb.ToString();
                }
            }
            else
            {
                int TopCount = 0, MovieFolderCount = 0;

                TopCount = Directory.GetFiles(myTargetFile.DirInfo.FullName, "*" + setting.MakeListExt, SearchOption.TopDirectoryOnly).Count();

                if (Directory.Exists(MovieSearchPath))
                    MovieFolderCount = Directory.GetFiles(MovieSearchPath, "*" + setting.MakeListExt, SearchOption.TopDirectoryOnly).Count();

                StringBuilder sb = new StringBuilder();
                if (MovieFolderCount > 0)
                    sb.Append(MovieFolderCount);

                if (TopCount > 0)
                {
                    if (MovieFolderCount > 0)
                        sb.Append("/(");
                    else
                        sb.Append("(");

                    sb.Append(TopCount);
                    sb.Append(")");
                }

                DisplayCount = sb.ToString();
            }

            return DisplayCount;
        }
        
        /// <summary>
        /// ZIPファイルの数をカウントする
        /// </summary>
        /// <param name="myZipCheckPath"></param>
        /// <param name="mySearchOption"></param>
        /// <returns></returns>
        private bool IsZipFiles(string myZipCheckPath, SearchOption mySearchOption)
        {
            int NormalZipCount = 0;

            if (!Directory.Exists(myZipCheckPath))
                return false;

            NormalZipCount = Directory.GetFiles(myZipCheckPath, "*zip", mySearchOption).Count();

            if (NormalZipCount == 0)
                return false;

            return true;
        }

        /// <summary>
        /// 指定されたファイル内で一番最新のファイル日付を取得
        /// </summary>
        /// <param name="myArrFilePaths"></param>
        /// <returns></returns>
        private DateTime GetMaxLastWriteDate(string myDirName)
        {
            DateTime MaxDate = new DateTime(1900, 1, 1);

            if (!Directory.Exists(myDirName))
                return MaxDate;

            string[] MovieFiles = Directory.GetFiles(myDirName, "*" + setting.MakeListExt, SearchOption.TopDirectoryOnly);

            foreach (string file in MovieFiles)
            {
                // ファイルの更新日を取得
                FileInfo fileinfo = new FileInfo(file);
                DateTime FileLastWrite = fileinfo.LastWriteTime;

                // ファイル更新日の最新の日付を取得
                if (MaxDate < FileLastWrite)
                    MaxDate = FileLastWrite;
            }

            return MaxDate;
        }

    }
}
