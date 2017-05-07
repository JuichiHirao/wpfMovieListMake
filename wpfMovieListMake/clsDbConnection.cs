using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;

namespace wpfMovieListMake
{
    public class DbConnection
    {
        string settings;
        private SqlConnection dbcon = null;
        private SqlTransaction dbtrans = null;

        private SqlParameter[] parameters;

        public DbConnection()
        {
            settings = "Data Source=tcp:WinServer2016DB;Initial Catalog=jhContents;Persist Security Info=True;User ID=sa;Password=11Jhirao";
//            settings = "Data Source=tcp:192.168.11.199;Initial Catalog=jhContents;Persist Security Info=True;User ID=sa;Password=11Jhirao";

            dbcon = new SqlConnection(settings);
        }
        ~DbConnection()
        {
            try
            {
                dbcon.Close();
            }
            catch (InvalidOperationException)
            {
                // 何もしない 2005/11/28の対応を参照
            }
        }
        public void openConnection()
        {
            if (dbcon.State != ConnectionState.Open)
                dbcon.Open();
        }
        public void closeConnection()
        {
            if (dbcon.State != ConnectionState.Closed)
                dbcon.Close();
        }
        public SqlConnection getSqlConnection()
        {
            return dbcon;
        }
        /// <summary>
        /// 指定されたＳＱＬ文を実行する
        /// </summary>
        public void execSqlCommand(string mySqlCommand)
        {
            SqlCommand dbcmd = dbcon.CreateCommand();

            dbcmd.CommandText = mySqlCommand;

            // トランザクションが開始済の場合
            if (dbtrans == null)
                this.openConnection();
            else
            {
                this.openConnection();
                dbcmd.Connection = this.getSqlConnection();
                dbcmd.Transaction = this.dbtrans;
            }

            if (parameters != null)
            {
                for (int IndexParam = 0; IndexParam < parameters.Length; IndexParam++)
                {
                    dbcmd.Parameters.Add(parameters[IndexParam]);
                }
            }

            dbcmd.ExecuteNonQuery();

            if (dbtrans == null)
                dbcon.Close();
        }
        public void SetParameter(SqlParameter[] myParams)
        {
            parameters = myParams;
        }
        public int getCountSql(string mySqlCommand)
        {
            SqlCommand myCommand;
            SqlDataReader myReader;

            int Count = 0;

            //dbcon.Open();

            // トランザクションが開始済の場合
            if (dbtrans == null)
            {
                this.openConnection();
                myCommand = new SqlCommand(mySqlCommand, this.getSqlConnection());
            }
            else
            {
                myCommand = new SqlCommand(mySqlCommand, this.getSqlConnection());
                myCommand.Connection = this.getSqlConnection();
                myCommand.Transaction = this.dbtrans;
            }
            //myCommand = new SqlCommand( mySqlCommand, dbcon );
            if (parameters != null)
            {
                for (int IndexParam = 0; IndexParam < parameters.Length; IndexParam++)
                {
                    myCommand.Parameters.Add(parameters[IndexParam]);
                }
            }

            myReader = myCommand.ExecuteReader();

            if (myReader.Read())
            {
                if (myReader.IsDBNull(0))
                {
                    parameters = null;
                    myReader.Close();
                    throw new NullReferenceException("SQL ERROR");
                }

                Count = myReader.GetInt32(0);
            }
            else
            {
                parameters = null;
                myReader.Close();
                return -1;
            }

            myReader.Close();
            parameters = null;

            return Count;
        }
        public int getIntSql(string mySqlCommand)
        {
            SqlCommand myCommand;
            SqlDataReader myReader;

            int myInteger = 0;

            //dbcon.Open();

            // トランザクションが開始済の場合
            if (dbtrans == null)
            {
                this.openConnection();
                myCommand = new SqlCommand(mySqlCommand, this.getSqlConnection());
            }
            else
            {
                myCommand = new SqlCommand(mySqlCommand, this.getSqlConnection());
                myCommand.Connection = this.getSqlConnection();
                myCommand.Transaction = this.dbtrans;
            }

            if (parameters != null)
            {
                for (int IndexParam = 0; IndexParam < parameters.Length; IndexParam++)
                {
                    myCommand.Parameters.Add(parameters[IndexParam]);
                }
            }
            //myCommand = new SqlCommand( mySqlCommand, dbcon );

            myReader = myCommand.ExecuteReader();

            if (myReader.Read())
                myInteger = myReader.GetInt32(0);
            else
                myInteger = 0;

            myReader.Close();

            return myInteger;
        }
    }
}
