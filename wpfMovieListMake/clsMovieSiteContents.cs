using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;

namespace wpfMovieListMake
{
    class MovieSiteContents : MovieContents, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        protected bool _IsSelected;
        public bool IsSelected
        {
            get
            {
                return _IsSelected;
            }
            set
            {
                _IsSelected = value;
                NotifyPropertyChanged("IsSelected");
            }
        }

        public string SiteName { get; set; }
        public DateTime MovieNewDate { get; set; }
        public string ParentPath { get; set; }
        public string MovieCount { get; set; }
        public string PhotoCount { get; set; }
        private TargetFile targetfile { get; set; }

        public MovieSiteContents()
        {
            SiteName = "";
            ParentPath = "";
            MovieCount = "";
            PhotoCount = "";
            Extension = "";
        }

        public MovieSiteContents(string mySiteName, TargetFile myFile)
        {
            SiteName = mySiteName;
            Name = myFile.Name;
            string strDir = Regex.Match(myFile.DirInfo.Parent.FullName, ".*" + mySiteName + "\\\\(?<abc>.*)").Groups["abc"].Value;
            ParentPath = strDir;
            //ParentPath = myFile.DirInfo.Parent.Name.Replace(mySiteName, "");

            MovieCount = myFile.strMovieCount;
            PhotoCount = myFile.PhotoCount.ToString();
            Extension = "";

            targetfile = myFile;
        }

        public void SetContentsInfo()
        {
            MovieCount = GetMovieCount(targetfile);

            string[] files = Directory.GetFiles(targetfile.DirInfo.FullName, "*jpg", SearchOption.AllDirectories);
            PhotoCount = files.Count().ToString();

        }

        /// <summary>
        /// 動画の件数をカウントする、通常はxx(zz)件だが舞ワイフはxx(yy)/xx(yy)の形式でリターン（xxはwmvファイル、yyはzip形式）zzはmovieフォルダ以外の動画ファイル
        /// </summary>
        /// <param name="myTargetFile"></param>
        /// <returns></returns>
        private string GetMovieCount(TargetFile myTargetFile)
        {
            int NormalCount = 0, BlueCount = 0;
            string DisplayCount = "";

            string[] files = Directory.GetFiles(myTargetFile.DirInfo.FullName, "*", SearchOption.AllDirectories);

            Regex regex = new Regex(MovieFileContents.REGEX_MOVIE_EXTENTION);
            List<string> listFiles = new List<string>();
            List<string> listExt = new List<string>();

            string WorkExtension = "";
            foreach (var file in files)
            {
                FileInfo fileinfo = new FileInfo(file.ToString());
                if (regex.IsMatch(fileinfo.Name.ToLower()))
                {
                    if (WorkExtension.IndexOf(fileinfo.Extension.Replace(".", "")) < 0)
                    {
                        if (WorkExtension.Length > 0)
                            WorkExtension += ",";
                        WorkExtension += fileinfo.Extension.Replace(".", "");
                    }

                    if (SiteName.Equals("舞ワイフ"))
                    {
                        if (Regex.Match(fileinfo.Name, "^w.*").Success)
                            NormalCount++;

                        if (Regex.Match(fileinfo.Name, "^Bw.*").Success)
                            BlueCount++;
                    }
                    else
                        NormalCount++;

                    if (MovieNewDate < fileinfo.LastWriteTime)
                        MovieNewDate = fileinfo.LastWriteTime;
                }
            }
            Extension = WorkExtension.ToUpper();

            if (BlueCount > 0)
                DisplayCount = NormalCount + "/" + BlueCount;
            else
                DisplayCount = NormalCount.ToString();

            //if (!Directory.Exists(MovieSearchPath))
            //    return "";

            return DisplayCount;
        }

        public void DbExport(DbConnection myDbCon)
        {
            string sqlCommand = "INSERT MOVIE_SITECONTENTS ";
            sqlCommand += "( SITE_NAME, NAME, PARENT_PATH, MOVIE_NEWDATE, MOVIE_COUNT, PHOTO_COUNT, EXTENSION ) ";
            sqlCommand += "VALUES( @pSiteName, @pName, @pParentPath, @pMovieNewDate, @pMovieCount, @pPhotoCount, @pExtension )";

            SqlCommand command = new SqlCommand();

            command = new SqlCommand(sqlCommand, myDbCon.getSqlConnection());

            SqlParameter[] sqlparams = new SqlParameter[7];
            // Create and append the parameters for the Update command.
            sqlparams[0] = new SqlParameter("@pSiteName", SqlDbType.VarChar);
            sqlparams[0].Value = SiteName;

            sqlparams[1] = new SqlParameter("@pName", SqlDbType.VarChar);
            sqlparams[1].Value = Name;

            sqlparams[2] = new SqlParameter("@pParentPath", SqlDbType.VarChar);
            sqlparams[2].Value = ParentPath;

            sqlparams[3] = new SqlParameter("@pMovieNewDate", SqlDbType.DateTime);
            if (MovieNewDate.Year >= 2000)
                sqlparams[3].Value = MovieNewDate;
            else
                sqlparams[3].Value = Convert.DBNull;

            sqlparams[4] = new SqlParameter("@pMovieCount", SqlDbType.VarChar);
            sqlparams[4].Value = MovieCount;

            sqlparams[5] = new SqlParameter("@pPhotoCount", SqlDbType.VarChar);
            sqlparams[5].Value = PhotoCount;

            sqlparams[6] = new SqlParameter("@pExtension", SqlDbType.VarChar);
            sqlparams[6].Value = Extension;

            myDbCon.SetParameter(sqlparams);
            myDbCon.execSqlCommand(sqlCommand);
        }

        public void DbUpdate(DbConnection myDbCon)
        {
            string sqlCommand = "UPDATE MOVIE_SITECONTENTS ";
            sqlCommand += "SET SITE_NAME = @pSiteName, NAME = @pName, PARENT_PATH = @pParentPath, MOVIE_NEWDATE = @pMovieNewDate, MOVIE_COUNT = @pMovieCount, PHOTO_COUNT = @pPhotoCount, EXTENSION = @pExtension ";
            sqlCommand += "WHERE ID = @pId ";

            MovieFileContentsParent parent = new MovieFileContentsParent();

            SqlCommand command = new SqlCommand();

            command = new SqlCommand(sqlCommand, myDbCon.getSqlConnection());

            SqlParameter[] sqlparams = new SqlParameter[8];
            // Create and append the parameters for the Update command.
            sqlparams[0] = new SqlParameter("@pSiteName", SqlDbType.VarChar);
            sqlparams[0].Value = SiteName;

            sqlparams[1] = new SqlParameter("@pName", SqlDbType.VarChar);
            sqlparams[1].Value = Name;

            sqlparams[2] = new SqlParameter("@pParentPath", SqlDbType.VarChar);
            sqlparams[2].Value = ParentPath;

            sqlparams[3] = new SqlParameter("@pMovieNewDate", SqlDbType.DateTime);
            if (MovieNewDate.Year >= 2000)
                sqlparams[3].Value = MovieNewDate;
            else
                sqlparams[3].Value = Convert.DBNull;

            sqlparams[4] = new SqlParameter("@pMovieCount", SqlDbType.VarChar);
            sqlparams[4].Value = MovieCount;

//            sqlparams[4] = new SqlParameter("@pSellDate", SqlDbType.DateTime);
//            if (data.SellDate.Year >= 2000)
//                sqlparams[4].Value = data.SellDate;
//            else
//                sqlparams[4].Value = Convert.DBNull;

            sqlparams[5] = new SqlParameter("@pPhotoCount", SqlDbType.VarChar);
            sqlparams[5].Value = PhotoCount;

            sqlparams[6] = new SqlParameter("@pExtension", SqlDbType.VarChar);
            sqlparams[6].Value = Extension;

            sqlparams[7] = new SqlParameter("@pId", SqlDbType.Int);
            sqlparams[7].Value = Id;

            myDbCon.SetParameter(sqlparams);
            myDbCon.execSqlCommand(sqlCommand);
        }

        public void DbDelete(DbConnection myDbCon)
        {
            string sqlCommand = "DELETE FROM MOVIE_SITECONTENTS ";
            sqlCommand += "WHERE ID = @pId ";

            MovieFileContentsParent parent = new MovieFileContentsParent();

            SqlCommand command = new SqlCommand();

            command = new SqlCommand(sqlCommand, myDbCon.getSqlConnection());

            SqlParameter[] sqlparams = new SqlParameter[1];

            sqlparams[0] = new SqlParameter("@pId", SqlDbType.Int);
            sqlparams[0].Value = Id;

            myDbCon.SetParameter(sqlparams);
            myDbCon.execSqlCommand(sqlCommand);
        }

        public static MovieSiteContents GetDbDataByName(string mySiteName, string myParentPath, string myName, DbConnection myDbCon)
        {
            SqlDataReader reader = null;

            MovieSiteContents sitedata = null;

            try
            {
                string sqlCommand = "SELECT ID, NAME, SITE_NAME, PARENT_PATH, MOVIE_COUNT, PHOTO_COUNT, MOVIE_NEWDATE, EXTENSION FROM MOVIE_SITECONTENTS WHERE SITE_NAME = @pSiteName AND NAME = @pName AND PARENT_PATH = @pParentPath";

                SqlCommand command = new SqlCommand(sqlCommand, myDbCon.getSqlConnection());

                myDbCon.openConnection();

                SqlParameter param = new SqlParameter();

                param = new SqlParameter("@pSiteName", SqlDbType.VarChar);
                param.Value = mySiteName;
                command.Parameters.Add(param);

                param = new SqlParameter("@pName", SqlDbType.VarChar);
                param.Value = myName;
                command.Parameters.Add(param);

                param = new SqlParameter("@pParentPath", SqlDbType.VarChar);
                param.Value = myParentPath;
                command.Parameters.Add(param);

                reader = command.ExecuteReader();

                if (reader.Read())
                {
                    sitedata = new MovieSiteContents();

                    sitedata.Id = DbExportCommon.GetDbInt(reader, 0);
                    sitedata.Name = DbExportCommon.GetDbString(reader, 1);
                    sitedata.SiteName = DbExportCommon.GetDbString(reader, 2);
                    sitedata.ParentPath = DbExportCommon.GetDbString(reader, 3);
                    sitedata.MovieCount = DbExportCommon.GetDbString(reader, 4);
                    sitedata.PhotoCount = DbExportCommon.GetDbString(reader, 5);
                    sitedata.MovieNewDate = DbExportCommon.GetDbDateTime(reader, 6);
                    sitedata.Extension = DbExportCommon.GetDbString(reader, 7);
                }

                reader.Close();
            }
            catch (Exception exp)
            {
                Debug.Write(exp);

                throw new Exception(exp.Message);
            }
            finally
            {
                reader.Close();
            }

            return sitedata;
        }

        /// <summary>
        /// データベースの更新が必要かのチェックを行う
        /// </summary>
        /// <param name="myData"></param>
        /// <returns>string 更新対象の列名</returns>
        public string IsDbUpdate(MovieSiteContents myData)
        {
            string updatecolname = "";
            string srcDate = MovieNewDate.ToString("yyyy/MM/dd HH:mm:SS");
            string DestDate = MovieNewDate.ToString("yyyy/MM/dd HH:mm:SS");
            if (!srcDate.Equals(DestDate))
                updatecolname += "MovieNewDate,";

            if (MovieCount != myData.MovieCount)
                updatecolname += "MovieCount,";

            if (PhotoCount != myData.PhotoCount)
                updatecolname += "PhotoCount,";

            if (ParentPath != myData.ParentPath)
                updatecolname += "ParentPath,";

            if (Extension != myData.Extension)
                updatecolname += "Extension,";

            return updatecolname;
        }

    }
}
