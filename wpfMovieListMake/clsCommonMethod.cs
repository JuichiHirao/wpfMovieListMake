using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;

namespace wpfMovieListMake
{
    class CommonMethod
    {
        public static List<SiteInfo> GetComboBoxSiteInfo(DbConnection myDbCon)
        {
            List<SiteInfo> listSiteInfo = null;

            try
            {
                //string queryString = "SELECT NAME FROM MOVIE_CONTENTS GROUP BY NAME";
                string queryString = "SELECT NAME FROM MOVIE_SITESTORE GROUP BY NAME";
                SqlDataAdapter adapter = new SqlDataAdapter(queryString, myDbCon.getSqlConnection());

                DataSet dtsetGroupName = new DataSet();
                adapter.Fill(dtsetGroupName, "SiteInfo");

                // SiteInfoクラスを使用しないとAnonymousType0の型が戻りになる
                // その場合にSelectedItemで取得した時に各プロパティの値がとれない
                // ※ 複数行で取得した場合はforeach内でプロパティ名で値が取得出来る
                var rows = dtsetGroupName.Tables["SiteInfo"].AsEnumerable();
                var query = rows.Select(el => new SiteInfo
                {
                    name = el["NAME"].ToString()
                });

                // 以下のようにqueryを直接ItemsSourceに設定すると次のようなエラーになる
                //   'EditItem' is not allowed for this view（'EditItem' は、このビューに対して許可されていません）
                //   ※ linq to queryからの取得元がGROUP BYで更新不可な構成のためで、List型へ移してから設定する
                // dgridGroupItem.ItemsSource = query;

                SiteInfo allsite = new SiteInfo();
                allsite.name = "全て";
                listSiteInfo = query.ToList<SiteInfo>();
                listSiteInfo.Insert(0, allsite);

                return listSiteInfo;
            }
            catch (Exception exp)
            {
                Debug.Write(exp);
            }

            return listSiteInfo;
        }
        /// <summary>
        /// 存在するコンテンツ内のパス名をサイト内のパス群から取得する
        /// </summary>
        /// <param name="myDbCon"></param>
        /// <returns></returns>
        public static string GetExistContentsPath(string mySiteName, string myContentsName, DbConnection myDbCon)
        {
            string queryString = "SELECT PATH FROM MOVIE_SITESTORE WHERE NAME = '" + mySiteName + "'";
            SqlCommand command = new SqlCommand(queryString, myDbCon.getSqlConnection());

            myDbCon.openConnection();

            SqlDataReader reader = command.ExecuteReader();

            string FilePathname = "";
            do
            {
                while (reader.Read())
                {
                    string ContentsPath = DbExportCommon.GetDbString(reader, 0);

                    string LinkPath = System.IO.Path.Combine(ContentsPath, myContentsName);

                    if (Directory.Exists(LinkPath))
                    {
                        FilePathname = LinkPath;
                        break;
                    }
                    else if (System.IO.File.Exists(LinkPath))
                    {
                        FilePathname = LinkPath;
                        break;
                    }
                }
            } while (reader.NextResult());
            reader.Close();

            myDbCon.closeConnection();

            return FilePathname;
        }

        /// <summary>
        /// 存在するコンテンツ内のパス名をサイト内のパス群から取得する
        /// </summary>
        /// <param name="myDbCon"></param>
        /// <returns></returns>
        public static List<SiteInfo> GetSiteStoreInfo(DbConnection myDbCon)
        {
            List<SiteInfo> listSiteInfo = new List<SiteInfo>();

            string queryString = "SELECT NAME, PATH FROM MOVIE_CONTENTS ";
            SqlCommand command = new SqlCommand(queryString, myDbCon.getSqlConnection());

            myDbCon.openConnection();

            SqlDataReader reader = command.ExecuteReader();

            do
            {
                while (reader.Read())
                {
                    SiteInfo siteinfo = new SiteInfo();

                    siteinfo.name = DbExportCommon.GetDbString(reader, 0);
                    siteinfo.path = DbExportCommon.GetDbString(reader, 1);

                    listSiteInfo.Add(siteinfo);
                }
            } while (reader.NextResult());
            reader.Close();

            myDbCon.closeConnection();

            return listSiteInfo;
        }

        /// <summary>
        /// 存在するコンテンツ内のパス名をサイト内のパス群から取得する
        /// </summary>
        /// <param name="myDbCon"></param>
        /// <returns></returns>
        public static List<SiteStore> GetSiteStore(DbConnection myDbCon)
        {
            List<SiteStore> listSiteStore = new List<SiteStore>();

            //string queryString = "SELECT ID, NAME, PATH, KIND FROM MOVIE_SITESTORE ORDER BY NAME, PATH";
            string queryString = "SELECT ID, NAME, EXPLANATION, LABEL, KIND FROM MOVIE_GROUP WHERE KIND = 3 ORDER BY NAME, EXPLANATION";

            SqlCommand command = new SqlCommand(queryString, myDbCon.getSqlConnection());

            myDbCon.openConnection();

            SqlDataReader reader = command.ExecuteReader();

            do
            {
                while (reader.Read())
                {
                    SiteStore site = new SiteStore();

                    site.Id = DbExportCommon.GetDbInt(reader, 0);
                    site.Name = DbExportCommon.GetDbString(reader, 1);
                    site.Explanation = DbExportCommon.GetDbString(reader, 2);
                    site.Label = DbExportCommon.GetDbString(reader, 3);
                    site.Kind = DbExportCommon.GetDbInt(reader, 4);

                    listSiteStore.Add(site);
                }
            } while (reader.NextResult());
            reader.Close();

            myDbCon.closeConnection();

            return listSiteStore;
        }

        /// <summary>
        /// MOVIE_SITESTOREに行を作成する
        /// </summary>
        /// <param name="myDbCon"></param>
        /// <returns></returns>
        public static void InsertSiteStore(SiteStore myTargetData, DbConnection myDbCon)
        {
            List<SiteInfo> listSiteInfo = new List<SiteInfo>();

            string cmdUpdate = "INSERT INTO MOVIE_SITESTORE(NAME, LABEL, EXPLANATION, KIND) VALUES (@pName, @pLabel, @pExplanation, @pKind)";

            SqlParameter[] sqlparams = new SqlParameter[4];

            sqlparams[0] = new SqlParameter("@pName", SqlDbType.VarChar);
            sqlparams[0].Value = myTargetData.Name;

            sqlparams[1] = new SqlParameter("@pLabel", SqlDbType.VarChar);
            sqlparams[1].Value = myTargetData.Label;

            sqlparams[2] = new SqlParameter("@pExplanation", SqlDbType.VarChar);
            sqlparams[2].Value = myTargetData.Explanation;

            sqlparams[3] = new SqlParameter("@pKind", SqlDbType.Int);
            sqlparams[3].Value = myTargetData.Kind;

            myDbCon.openConnection();

            myDbCon.SetParameter(sqlparams);
            myDbCon.execSqlCommand(cmdUpdate);

            myDbCon.closeConnection();

            return;
        }

        /// <summary>
        /// MOVIE_SITESTOREに行の更新をする
        /// </summary>
        /// <param name="myDbCon"></param>
        /// <returns></returns>
        public static void UpdateSiteStore(SiteStore myTargetData, DbConnection myDbCon)
        {
            List<SiteInfo> listSiteInfo = new List<SiteInfo>();

            string cmdUpdate = "UPDATE MOVIE_SITESTORE SET NAME = @pName, LABEL = @pLabel, EXPLANATION = @pExplanation, KIND = @pKind WHERE ID = @pId";

            SqlParameter[] sqlparams = new SqlParameter[5];

            sqlparams[0] = new SqlParameter("@pName", SqlDbType.VarChar);
            sqlparams[0].Value = myTargetData.Name;

            sqlparams[1] = new SqlParameter("@pLabel", SqlDbType.VarChar);
            sqlparams[1].Value = myTargetData.Label;

            sqlparams[2] = new SqlParameter("@pExplanation", SqlDbType.VarChar);
            sqlparams[2].Value = myTargetData.Explanation;

            sqlparams[3] = new SqlParameter("@pKind", SqlDbType.Int);
            sqlparams[3].Value = myTargetData.Kind;

            sqlparams[4] = new SqlParameter("@pId", SqlDbType.Int);
            sqlparams[5].Value = myTargetData.Id;

            myDbCon.openConnection();

            myDbCon.SetParameter(sqlparams);
            myDbCon.execSqlCommand(cmdUpdate);

            myDbCon.closeConnection();

            return;
        }

        /// <summary>
        /// MOVIE_SITESTOREのID一致ROWを削除する
        /// </summary>
        /// <param name="myDbCon"></param>
        /// <returns></returns>
        public static void DeleteSiteStore(SiteStore myTargetData, DbConnection myDbCon)
        {
            List<SiteInfo> listSiteInfo = new List<SiteInfo>();

            string cmdUpdate = "DELETE FROM MOVIE_SITESTORE WHERE ID = @pId";

            SqlParameter[] sqlparams = new SqlParameter[1];

            sqlparams[0] = new SqlParameter("@pId", SqlDbType.Int);
            sqlparams[0].Value = myTargetData.Id;

            myDbCon.openConnection();

            myDbCon.SetParameter(sqlparams);
            myDbCon.execSqlCommand(cmdUpdate);

            myDbCon.closeConnection();

            return;
        }

        /// <summary>
        /// 存在するコンテンツ内のパス名をサイト内のパス群から取得する
        /// </summary>
        /// <param name="myDbCon"></param>
        /// <returns></returns>
        public static DateTime GetContentsDateOld(GroupInfo myTargetInfo, DbConnection myDbCon)
        {
            List<SiteInfo> listSiteInfo = new List<SiteInfo>();

            string queryString = "SELECT CONTENTS_DATE FROM MOVIE_CONTENTS WHERE ACTRESS_NAME = @pName AND CONTENTS_DATE IS NOT NULL ORDER BY CONTENTS_DATE ";
            SqlCommand command = new SqlCommand(queryString, myDbCon.getSqlConnection());

            myDbCon.openConnection();

            SqlParameter sqlparam = new SqlParameter();

            sqlparam = new SqlParameter("@pName", SqlDbType.VarChar);
            sqlparam.Value = myTargetInfo.name;

            command.Parameters.Add(sqlparam);

            SqlDataReader reader = command.ExecuteReader();

            DateTime dt = new DateTime();
            if (reader.Read())
            {
                dt = DbExportCommon.GetDbDateTime(reader, 0);
            }

            reader.Close();

            myDbCon.closeConnection();

            return dt;
        }

        /// <summary>
        /// MOVIE_ACTRESSのACTIVITY_DATEを更新する
        /// </summary>
        /// <param name="myDbCon"></param>
        /// <returns></returns>
        public static List<SiteInfo> UpdateGroupInfoActivityDate(DateTime myUpdateDate, GroupInfo myTargetInfo, DbConnection myDbCon)
        {
            List<SiteInfo> listSiteInfo = new List<SiteInfo>();

            string cmdUpdate = "update MOVIE_ACTRESS set ACTIVITY_DATE = @pActivityDate where ID = @pId";

            SqlParameter[] sqlparams = new SqlParameter[2];

            sqlparams[0] = new SqlParameter("@pActivityDate", SqlDbType.DateTime);
            sqlparams[0].Value = myUpdateDate;

            sqlparams[1] = new SqlParameter("@pId", SqlDbType.Int);
            sqlparams[1].Value = myTargetInfo.id;

            myDbCon.openConnection();

            myDbCon.SetParameter(sqlparams);
            myDbCon.execSqlCommand(cmdUpdate);

            myDbCon.closeConnection();

            return listSiteInfo;
        }

        public static void MakeWplFile(string myListType, string myMakeTargetDir, string myMovieFilename)
        {
            string WplFilename = "";

            // ディレクトリオブジェクトを生成
            DirectoryInfo dirinfoMakeTarget = new DirectoryInfo(myMakeTargetDir);    // 基本フォルダ

            FileInfo fileinfoMovie = new FileInfo(myMovieFilename);

            // 基本フォルダが存在しない場合は処理はしない
            if (dirinfoMakeTarget.Exists == false
                    || fileinfoMovie.Exists == false)
                return;

            string MovieName = fileinfoMovie.Name.Replace(fileinfoMovie.Extension, "");

            // リストファイル名の生成（動画フォルダがMP4の場合はファイル名は「_MP4.asx」にする）
            WplFilename = System.IO.Path.Combine(dirinfoMakeTarget.FullName, "_" + MovieName + "." + myListType);

            /// ※ ASXファイルは文字コードがUTF-16でないと日本語を認識してくれないのでUTF-16を使用
            StreamWriter utf16Writer = new StreamWriter(WplFilename, false, Encoding.GetEncoding("UTF-16"));

            // 先頭部分を出力
            utf16Writer.WriteLine("<?wpl version=\"1.0\"?>");
            utf16Writer.WriteLine("<smil>");
            utf16Writer.WriteLine("\t<body>");
            utf16Writer.WriteLine("\t\t<seq>");

            try
            {
                // １動画分のリスト生成
                utf16Writer.WriteLine("\t\t\t<media src=\"" + myMovieFilename + "\" />");
                utf16Writer.WriteLine("\t\t</seq>");
                utf16Writer.WriteLine("\t</body>");
                utf16Writer.WriteLine("</smil>");
            }
            finally
            {
                utf16Writer.Close();
            }
        }

    }

    public class GroupInfo : INotifyPropertyChanged
    {
        public GroupInfo()
        {
            id = -1;
            name = "";
            count = "";
            remark = "";
            date = new DateTime();
            checkresult = "";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public int id { get; set; }
        public string name { get; set; }
        public string count { get; set; }
        public string remark { get; set; }
        public DateTime date { get; set; }
        public string searchflag { get; set; }
        private string _checkresult;
        public string checkresult
        {
            get
            {
                return _checkresult;
            }
            set
            {
                _checkresult = value;

                NotifyPropertyChanged("checkresult");
            }
        }
    }
    public class SiteInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public string name { get; set; }
        private string _checkresult;
        public string checkresult
        {
            get
            {
                return _checkresult;
            }
            set
            {
                _checkresult = value;

                NotifyPropertyChanged("checkresult");
            }
        }
        public string path { get; set; }
        public string kind { get; set; }
    }
}
