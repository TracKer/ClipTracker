using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Net.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace ClipTracker {

    struct StorageItem {
        public int id;
        public string type;
        public byte[] data;
    }

    public delegate void GetCallback(int rowId, string type, byte[] data);

    class Storage {
        public SQLiteConnection db;
        private string locationPath;
        private string filePath;

        public Storage() {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            this.locationPath = Path.Combine(appDataPath, "TRCK2K");
            this.locationPath = Path.Combine(locationPath, "ClipTracker");
            this.filePath = Path.Combine(this.locationPath, "storage.sqlite");

            if (!File.Exists(this.filePath)) {
                this.PerformInstallation();
            }
            db = new SQLiteConnection("Data Source=" + filePath + "; Version=3;");
            db.Open();
        }

        private void PerformInstallation() {
            // Perform installation.
            Directory.CreateDirectory(locationPath);
            SQLiteConnection.CreateFile(filePath);
            db = new SQLiteConnection("Data Source=" + filePath + "; Version=3;");
            db.Open();

            SQLiteCommand Command;
            Command = db.CreateCommand();
            Command.CommandText = "CREATE TABLE 'data' ('type' VARCHAR NOT NULL , 'data' BLOB DEFAULT NULL, 'date' DATETIME NOT NULL  DEFAULT CURRENT_TIMESTAMP)";
            Command.ExecuteNonQuery();
            Command.Dispose();

            db.Close();
            db.Dispose();
        }

        static byte[] StringToBytes(string str) {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static string BytesToString(byte[] bytes) {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing) {
            db.Close();
            db.Dispose();
        }

        public void AddText(String text) {
            SQLiteCommand Command = db.CreateCommand();
            Command.CommandText = "INSERT INTO 'data' ('type', 'data') VALUES (@Type, @Data)";
            Command.Parameters.Add("@Type", DbType.String).Value = "text/plain";
            Command.Parameters.Add("@Data", DbType.Binary).Value = StringToBytes(text);
            Command.ExecuteNonQuery();
        }

        public void GetAmount(int amount, GetCallback getCallback) {
            SQLiteCommand Command = db.CreateCommand();
            Command.CommandText = "SELECT rowid, type, data FROM 'data' ORDER BY date DESC LIMIT @Amount";
            Command.Parameters.Add("@Amount", DbType.Int32).Value = amount;

            SQLiteDataReader reader = Command.ExecuteReader();
            while (reader.Read()) {
                byte[] data = GetBytes(reader, 2);
                getCallback(reader.GetInt32(0), reader.GetString(1), data);
            }
        }

        public StorageItem GetItem(int id) {
            SQLiteCommand Command = db.CreateCommand();
            Command.CommandText = "SELECT rowid, type, data FROM 'data' WHERE rowid = @Id";
            Command.Parameters.Add("@Id", DbType.Int32).Value = id;

            var item = new StorageItem();
            item.id = 0;

            SQLiteDataReader reader = Command.ExecuteReader();
            if (reader.HasRows) {
                reader.Read();

                item.id = reader.GetInt32(0);
                item.type = reader.GetString(1);
                item.data = GetBytes(reader, 2);
            }

            return item;
        }

        static byte[] GetBytes(SQLiteDataReader reader, int i) {
            const int CHUNK_SIZE = 2 * 1024;
            byte[] buffer = new byte[CHUNK_SIZE];
            long bytesRead;
            long fieldOffset = 0;
            using (MemoryStream stream = new MemoryStream()) {
                while ((bytesRead = reader.GetBytes(i, fieldOffset, buffer, 0, buffer.Length)) > 0) {
                    stream.Write(buffer, 0, (int) bytesRead);
                    fieldOffset += bytesRead;
                }
                return stream.ToArray();
            }
        }
    }
}
