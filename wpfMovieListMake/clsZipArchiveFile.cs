using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;

namespace wpfMovieListMake
{
    class ZipArchiveFile
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="myZipFile">解凍するZIP圧縮するファイル</param>
        /// <param name="myExtractPath">解凍するファイルの格納先</param>
        /// <param name="myTargetExt">解凍の対象とするファイルパターン</param>
        public static void ExtractPattern(string myZipFile, string myExtractPath, string[] myPattern)
        {
            // ZIPファイルが格納されているフォルダを取得
            DirectoryInfo ExtractDir;

            FileInfo ZipFile = new FileInfo(myZipFile);
            
            if (myExtractPath==null)
                ExtractDir = ZipFile.Directory;
            else
                ExtractDir = new DirectoryInfo(myExtractPath);


            // 解凍する時のストリーム
            FileStream fs = new System.IO.FileStream(ZipFile.FullName
                                                        , FileMode.Open
                                                        , FileAccess.Read
                                                        , FileShare.Read);
            ZipInputStream zis = new ZipInputStream(fs);

            // ZIP内の1ファイル分
            ZipEntry zipentry;

            string ExtractPathname = "";
            List<FileInfo> fileExtract = new List<FileInfo>();
            try
            {
                while ((zipentry = zis.GetNextEntry()) != null)
                {
                    // ディレクトリの場合は無視
                    if (zipentry.IsDirectory)
                        continue;

                    string ZipEntryFilename = zipentry.Name;
                    string ExtractFilename = "";

                    // パターン一致しない場合は無視
                    bool PatternConcordance = false;
                    for (int IndexArr = 0; IndexArr < myPattern.Length; IndexArr++)
                    {
                        Regex regex = new Regex(myPattern[IndexArr]);
                        Match match = regex.Match(ZipEntryFilename);

                        if (match.Success)
                        {
                            PatternConcordance = true;
                            break;
                        }
                    }

                    if (PatternConcordance == false)
                        continue;

                    // ZIP内でパス付きで解凍されている場合はパスを消してファイル名のみにする
                    ExtractFilename = Path.GetFileName(ZipEntryFilename);

                    // 展開先のパスファイル名
                    ExtractPathname = Path.Combine(ExtractDir.FullName, ExtractFilename);
                    fileExtract.Add(new FileInfo(ExtractPathname));

                    // ZIP内のファイルを指定フォルダに書き出す
                    int len;
                    byte[] buffer = new byte[2048];
                    FileStream writer = new System.IO.FileStream(ExtractPathname
                                                                    , FileMode.Create, FileAccess.Write, FileShare.Write);

                    while ((len = zis.Read(buffer, 0, buffer.Length)) > 0)
                        writer.Write(buffer, 0, len);

                    writer.Close();
                }
            }
            catch (SharpZipBaseException exp)
            {
                Debug.Write(exp);
                //閉じる
                zis.Close();
                fs.Close();

                // ファイルの解凍に失敗したファイルは削除する
                //  ※ 共有中のエラーで削除できないのでコメント化
                //foreach (FileInfo targetFile in fileExtract)
                //    File.Delete(targetFile.FullName);

                throw new SharpZipBaseException("ファイルの解凍に失敗しました [" + ExtractPathname + "]");
            }

            //閉じる
            zis.Close();
            fs.Close();

            // 情報を表示する
            //Console.WriteLine("ファイル名 : {0}", ze.Name);
            //Console.WriteLine("サイズ : {0} bytes", ze.Size);
            //Console.WriteLine("圧縮サイズ : {0} bytes",
            //    ze.CompressedSize);
            //Console.WriteLine("CRC : {0:X}", ze.Crc);
            //Console.WriteLine("日時 : {0}", ze.DateTime);
            //Console.WriteLine();

        }
    }
}
