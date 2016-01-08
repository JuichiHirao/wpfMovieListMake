using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Data;
using System.Data.SqlClient;

namespace wpfMovieListMake
{
    class MovieSiteContentsParent
    {
        public static List<MovieSiteContents> GetDbContents(string mySiteName)
        {
            DbConnection dbcon = new DbConnection();

            List<MovieSiteContents> listMContents = new List<MovieSiteContents>();

            string queryString = "SELECT ID, NAME,MOVIE_NEWDATE,RATING,SITE_NAME,LABEL "
                + ", COMMENT, REMARK, PARENT_PATH, MOVIE_COUNT, PHOTO_COUNT, EXTENSION, TAG "
                + "FROM MOVIE_SITECONTENTS "
                + "WHERE SITE_NAME = @pSiteName "
                + "ORDER BY MOVIE_NEWDATE DESC ";
            //"SELECT ID, NAME, SIZE, FILE_DATE, LABEL, SELL_DATE, PRODUCT_NUMBER, FILE_COUNT, EXTENSION, RATING, COMMENT FROM MOVIE_FILES WHERE LABEL = @pLabel ORDER BY FILE_DATE DESC ";

            dbcon.openConnection();

            SqlCommand command = new SqlCommand(queryString, dbcon.getSqlConnection());

            SqlParameter param = new SqlParameter("@pSiteName", SqlDbType.VarChar);
            param.Value = mySiteName;
            command.Parameters.Add(param);

            SqlDataReader reader = command.ExecuteReader();

            do
            {
                while (reader.Read())
                {
                    MovieSiteContents data = new MovieSiteContents();

                    data.Id = DbExportCommon.GetDbInt(reader, 0);
                    data.Name = DbExportCommon.GetDbString(reader, 1);
                    data.MovieNewDate = DbExportCommon.GetDbDateTime(reader, 2);
                    data.Rating = DbExportCommon.GetDbInt(reader, 3);
                    data.SiteName = DbExportCommon.GetDbString(reader, 4);
                    data.Label = DbExportCommon.GetDbString(reader, 5);
                    data.Comment = DbExportCommon.GetDbString(reader, 6);
                    data.Remark = DbExportCommon.GetDbString(reader, 7);
                    data.ParentPath = DbExportCommon.GetDbString(reader, 8);
                    data.MovieCount = DbExportCommon.GetDbString(reader, 9);
                    data.PhotoCount = DbExportCommon.GetDbString(reader, 10);
                    data.Extension = DbExportCommon.GetDbString(reader, 11);
                    //data.Tag = DbExportCommon.GetDbString(reader, 12);
                    //data.ChildTableName = MovieContents.TABLE_KIND_MOVIE_SITECONTENTS;

                    listMContents.Add(data);
                }
            } while (reader.NextResult());

            reader.Close();

            dbcon.closeConnection();

            return listMContents;
        }
    }
}
