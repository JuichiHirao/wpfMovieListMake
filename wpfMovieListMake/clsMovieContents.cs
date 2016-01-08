using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace wpfMovieListMake
{
    class MovieContents
    {
        public static string REGEX_MOVIE_EXTENTION = @".*\.avi$|.*\.wmv$|.*\.mpg$|.*ts$|.*divx$|.*mp4$|.*asf$|.*mkv$|.*rm$|.*3gp$|.*flv$";
        //  @".*\.avi$|.*\.wmv$|.*\.mpg$|.*ts$|.*divx$|.*mp4$|.*asf$|.*jpg$|.*jpeg$|.*iso$|.*mkv$";

        public int Id { get; set; }
        public string Name { get; set; }
        public string Label { get; set; }
        public long Size { get; set; }
        public string DispFileDate { get; set; }
        public DateTime FileDate { get; set; }
        public string DispSellDate { get; set; }
        public string ProductNumber { get; set; }
        public DateTime SellDate { get; set; }
        public string Extension { get; set; }
        public int FileCount { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public string Remark { get; set; }

        public MovieContents()
        {
            Name = "";
            Label = "";
            DispFileDate = "";
            DispSellDate = "";
            ProductNumber = "";
            Comment = "";
            Remark = "";
        }
    }
}
