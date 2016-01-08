using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace wpfMovieListMake
{
    class MoviePlayList
    {
        public static void MakeWplFile(string myListType, string myOnePersonDir, string[] myFilePattern, string myMovieFolderName)
        {
            string WplFilename = "";

            // ディレクトリオブジェクトを生成
            DirectoryInfo dirinfoTargetBase = new DirectoryInfo(myOnePersonDir);    // 一人分の基本フォルダ（movieフォルダの上位フォルダ）
            DirectoryInfo dirinfoMovie = new DirectoryInfo(myOnePersonDir + "\\" + myMovieFolderName);  // リスト作成対象の動画があるフォルダ

            // 基本フォルダが存在しない場合は処理はしない
            if (dirinfoTargetBase.Exists == false
                    || dirinfoMovie.Exists == false)
                return;

            // リストファイル名の生成（動画フォルダがMP4の場合はファイル名は「_MP4.asx」にする）
            if (myMovieFolderName.Equals("movie"))
                WplFilename = dirinfoTargetBase.FullName + "\\_" + dirinfoTargetBase.Name + myListType;
            else
                WplFilename = dirinfoTargetBase.FullName + "\\_" + myMovieFolderName + myListType;
            /// ※ ASXファイルは文字コードがUTF-16でないと日本語を認識してくれないのでUTF-16を使用
            StreamWriter utf16Writer = new StreamWriter(WplFilename, false, Encoding.GetEncoding("UTF-16"));

            // 先頭部分を出力
            utf16Writer.WriteLine("<?wpl version=\"1.0\"?>");
            utf16Writer.WriteLine("<smil>");
            utf16Writer.WriteLine("\t<body>");
            utf16Writer.WriteLine("\t\t<seq>");

            try
            {
                for (int IndexArr = 0; IndexArr < myFilePattern.Length; IndexArr++)
                {
                    // リスト作成対象の動画ファイル情報を取得
                    string[] filesMovie = System.IO.Directory.GetFiles(dirinfoMovie.FullName, myFilePattern[IndexArr], System.IO.SearchOption.TopDirectoryOnly);

                    // リスト作成対象の動画ファイルが存在しない場合は処理しない
                    if (filesMovie.Length <= 0)
                        continue;

                    // 取得した動画ファイルの出力を行う
                    for (int IndexMovieArr = 0; IndexMovieArr < filesMovie.Length; IndexMovieArr++)
                    {
                        // ファイル名のみを取得
                        FileInfo file = new FileInfo(filesMovie[IndexMovieArr]);

                        // １動画分のリスト生成
                        utf16Writer.WriteLine("\t\t\t<media src=\"" + myMovieFolderName + "\\" + file.Name + "\" />");
                    }
                }
                utf16Writer.WriteLine("\t\t</seq>");
                utf16Writer.WriteLine("\t</body>");
                utf16Writer.WriteLine("</smil>");
            }
            finally
            {
                utf16Writer.Close();
            }
        }

        public static void MakeAsxFile(string myListType, string myOnePersonDir, string[] myFilePattern, string myMovieFolderName)
        {
            string AsxFilename = "";

            // ディレクトリオブジェクトを生成
            DirectoryInfo dirinfoTargetBase = new DirectoryInfo(myOnePersonDir);    // 一人分の基本フォルダ（movieフォルダの上位フォルダ）
            DirectoryInfo dirinfoMovie = new DirectoryInfo(myOnePersonDir + "\\" + myMovieFolderName);  // リスト作成対象の動画があるフォルダ

            // 基本フォルダが存在しない場合は処理はしない
            if (dirinfoTargetBase.Exists == false
                    || dirinfoMovie.Exists == false)
                return;

            // リストファイル名の生成（動画フォルダがMP4の場合はファイル名は「_MP4.asx」にする）
            if (myMovieFolderName.Equals("movie"))
                AsxFilename = dirinfoTargetBase.FullName + "\\_" + dirinfoTargetBase.Name + myListType;
            else
                AsxFilename = dirinfoTargetBase.FullName + "\\_" + myMovieFolderName + myListType;
            /// ※ ASXファイルは文字コードがUTF-16でないと日本語を認識してくれないのでUTF-16を使用
            StreamWriter utf16Writer = new StreamWriter(AsxFilename, false, Encoding.GetEncoding("UTF-16"));

            // 先頭部分を出力
            utf16Writer.WriteLine("<asx version = \"3.0\" >");

            try
            {
                for (int IndexArr = 0; IndexArr < myFilePattern.Length; IndexArr++)
                {
                    // リスト作成対象の動画ファイル情報を取得
                    string[] filesMovie = System.IO.Directory.GetFiles(dirinfoMovie.FullName, myFilePattern[IndexArr], System.IO.SearchOption.TopDirectoryOnly);

                    // リスト作成対象の動画ファイルが存在しない場合は処理しない
                    if (filesMovie.Length <= 0)
                        continue;

                    // 取得した動画ファイルの出力を行う
                    for (int IndexMovieArr = 0; IndexMovieArr < filesMovie.Length; IndexMovieArr++)
                    {
                        // ファイル名のみを取得
                        FileInfo file = new FileInfo(filesMovie[IndexMovieArr]);

                        // １動画分のリスト生成
                        utf16Writer.WriteLine("\t<entry>");
                        utf16Writer.WriteLine("\t\t<title>" + file.Name + "</title>");
                        utf16Writer.WriteLine("\t\t<ref href = \"" + filesMovie[IndexMovieArr] + "\" />");
                        utf16Writer.WriteLine("\t</entry>\r\n");
                    }
                }

                utf16Writer.WriteLine("</asx>");
            }
            finally
            {
                utf16Writer.Close();
            }
        }

    }
}
