using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace ClipTracker {
    class Storage {
        private SQLiteConnection db;
        private String locationPath;
        private String filePath;

        public Storage() {
            String appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            this.locationPath = Path.Combine(appDataPath, "TRCK2K");
            this.filePath = Path.Combine(this.locationPath, "storage.sqlite");

            if (!File.Exists(this.filePath)) {
                this.PerformInstallation();
            }
            this.OpenConnection(this.filePath);
        }

        private void OpenConnection(String filePath) {
            this.db = new SQLiteConnection("Data Source=" + filePath + "; Version=3;");
            this.db.Open();
        }

        private void PerformInstallation() {
            // Perform installation.
            Directory.CreateDirectory(locationPath);
            SQLiteConnection.CreateFile(filePath);
            OpenConnection(filePath);
            ExecuteSimpleQuery(
                "CREATE TABLE 'data' ('type' VARCHAR NOT NULL , 'external_data_id' INTEGER DEFAULT NULL, 'data' TEXT DEFAULT NULL, 'date' DATETIME NOT NULL  DEFAULT CURRENT_TIMESTAMP)"
            );
            ExecuteSimpleQuery(
                "CREATE TABLE 'external_data' ('file_name' VARCHAR NOT NULL  UNIQUE , 'md5' VARCHAR NOT NULL , 'size' INTEGER NOT NULL , 'date' DATETIME NOT NULL  DEFAULT CURRENT_TIMESTAMP)"
            );
            db.Close();
            db.Dispose();
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing) {
            db.Close();
            db.Dispose();
        }

        public int ExecuteSimpleQuery(String query) {
            SQLiteCommand Command = db.CreateCommand();
            Command.CommandText = query;
            return Command.ExecuteNonQuery();
        }

        public void AddText(String text) {
            SQLiteCommand Command = db.CreateCommand();
            Command.CommandText = "INSERT INTO 'data' ('type', 'data') VALUES ('text', @Data)";
            Command.Parameters.Add("@Data", DbType.String).Value = text;
            Command.ExecuteNonQuery();
        }
    }
}
