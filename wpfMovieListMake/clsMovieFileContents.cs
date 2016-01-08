using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.ComponentModel;
using System.IO;

namespace wpfMovieListMake
{
    class MovieFileContents : MovieContents
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

        public void Parse()
        {
            string WorkStr = Regex.Replace(Name.Substring(1), ".* \\[", "");

            Regex regex = new Regex("\\[");

            if (regex.Match(Name.Substring(1)).Success)
            {
                string WorkStr2 = Regex.Replace(WorkStr, "\\].*", "");
                WorkStr = Regex.Replace(WorkStr2, " [0-9]*.*", "");
                ProductNumber = WorkStr.ToUpper();

                string DateStr = Regex.Replace(WorkStr2, ".* ", "");

                if (DateStr.Length != 8)
                    return;

                string format = "yyyyMMdd"; // "yyyyMMddHHmmss";
                try
                {
                    SellDate = DateTime.ParseExact(DateStr, format, null);
                }
                catch (Exception)
                {
                    return;
                }
            }
        }

        private bool _IsExistsThumbnail;

        public bool IsExistsThumbnail
        {
            get
            {
                return _IsExistsThumbnail;
            }
            set
            {
                _IsExistsThumbnail = value;
                ImageUri = GetImageUri(_IsExistsThumbnail);
            }
        }

        private string _ImageUri;

        public string ImageUri
        {
            get
            {
                return _ImageUri;
            }
            set
            {
                _ImageUri = value;
            }
        }

        private string GetImageUri(bool myExistsThumbnail)
        {
            string WorkImageUri = "";

            DirectoryInfo dirinfo = new DirectoryInfo(Environment.CurrentDirectory);

            if (IsExistsThumbnail)        // サムネイル画像あり
                WorkImageUri = System.IO.Path.Combine(dirinfo.FullName, "32.png");
            else
                WorkImageUri = System.IO.Path.Combine(dirinfo.FullName, "00.png");

            return WorkImageUri;
        }

        public void DbExport(DbConnection myDbCon)
        {
            string sqlCommand = "INSERT INTO MOVIE_FILES (NAME, SIZE, FILE_DATE, LABEL, SELL_DATE, PRODUCT_NUMBER, FILE_COUNT, EXTENSION) VALUES( @pName, @pSize, @pFileDate, @pLabel, @pSellDate, @pProductNumber, @pFileCount, @pExtension )";
            SqlCommand command = new SqlCommand();

            command = new SqlCommand(sqlCommand, myDbCon.getSqlConnection());

            SqlParameter[] sqlparams = new SqlParameter[8];
            // Create and append the parameters for the Update command.
            sqlparams[0] = new SqlParameter("@pName", SqlDbType.VarChar);
            sqlparams[0].Value = Name;

            sqlparams[1] = new SqlParameter("@pSize", SqlDbType.Decimal);
            sqlparams[1].Value = Size;

            sqlparams[2] = new SqlParameter("@pFileDate", SqlDbType.DateTime);
            sqlparams[2].Value = FileDate;

            sqlparams[3] = new SqlParameter("@pLabel", SqlDbType.VarChar);
            sqlparams[3].Value = Label;

            sqlparams[4] = new SqlParameter("@pSellDate", SqlDbType.DateTime);
            if (SellDate.Year >= 2000)
                sqlparams[4].Value = SellDate;
            else
                sqlparams[4].Value = Convert.DBNull;

            sqlparams[5] = new SqlParameter("@pProductNumber", SqlDbType.VarChar);
            sqlparams[5].Value = ProductNumber;

            sqlparams[6] = new SqlParameter("@pFileCount", SqlDbType.Int);
            sqlparams[6].Value = FileCount;

            sqlparams[7] = new SqlParameter("@pExtension", SqlDbType.VarChar);
            sqlparams[7].Value = Extension;

            myDbCon.SetParameter(sqlparams);
            myDbCon.execSqlCommand(sqlCommand);

            return;
        }
        public void DbDelete(DbConnection myDbCon)
        {
            string sqlCommand = "DELETE FROM MOVIE_FILES WHERE ID = @pId";
            SqlCommand command = new SqlCommand();

            command = new SqlCommand(sqlCommand, myDbCon.getSqlConnection());

            SqlParameter[] sqlparams = new SqlParameter[1];
            // Create and append the parameters for the Update command.
            sqlparams[0] = new SqlParameter("@pId", SqlDbType.VarChar);
            sqlparams[0].Value = Id;

            myDbCon.SetParameter(sqlparams);
            myDbCon.execSqlCommand(sqlCommand);

            return;
        }
    }
}
