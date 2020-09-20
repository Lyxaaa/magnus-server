using MySql.Data.MySqlClient;
using System;


//source of code Ocph23 et al Answer to "How to connect to MySQL Database?" https://stackoverflow.com/questions/21618015/how-to-connect-to-mysql-database
//retrived 24/08/2020
namespace DatabaseConnection {
    public class DBConnection {
        private DBConnection() {
        }

        private string databaseName = string.Empty;
        public string DatabaseName {
            get { return databaseName; }
            set { databaseName = value; }
        }

        public string Password { get; set; }
        private MySqlConnection connection = null;
        public MySqlConnection Connection {
            get { return connection; }
        }

        private static DBConnection _instance = null;
        public static DBConnection Instance() {
            if (_instance == null)
                _instance = new DBConnection();
            return _instance;
        }

        public bool IsConnect() {
            try {

                if (Connection == null) {
                    if (String.IsNullOrEmpty(databaseName))
                        return false;
                    string connstring = string.Format("Server=localhost; database={0}; UID='root'; password=''", databaseName);
                    connection = new MySqlConnection(connstring);
                    connection?.Open();
                }

                return connection != null;
            } catch (Exception e){
                return false;
            }
        }

        public void Close() {
            connection.Close();
        }
    }
}
