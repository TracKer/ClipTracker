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

  public delegate void GetCallback(int rowId, string type, byte[] data, object tag);

  class Storage {

    public SQLiteConnection db;
    private string locationPath;
    private string filePath;
    private string lastHash;

    public Storage() {
      var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
      this.locationPath = Path.Combine(appDataPath, "TRCK2K");
      this.locationPath = Path.Combine(locationPath, "ClipTracker");
      this.filePath = Path.Combine(this.locationPath, "storage.sqlite");

      if (!File.Exists(this.filePath)) {
        // Looks like storage is not exist yet, let's perform installation then and create a new storage.
        PerformInstallation();
      }
      db = new SQLiteConnection("Data Source=" + filePath + "; Version=3;");
      db.Open();

      // We have to initialize last hash, to avoid preforming useless operations.
      var command = db.CreateCommand();
      command.CommandText = "SELECT hash FROM 'data' ORDER BY date DESC LIMIT 1";
      var hash = command.ExecuteScalar();
      if (hash != null) {
        lastHash = (string) hash;
      }
    }

    private void PerformInstallation() {
      // Perform installation.
      Directory.CreateDirectory(locationPath);
      SQLiteConnection.CreateFile(filePath);
      db = new SQLiteConnection("Data Source=" + filePath + "; Version=3;");
      db.Open();

      var command = db.CreateCommand();
      command.CommandText = "CREATE TABLE 'data' ('type' VARCHAR NOT NULL, 'data' BLOB DEFAULT NULL, 'hash' VARCHAR NOT NULL, 'date' DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP)";
      command.ExecuteNonQuery();
      command.Dispose();

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

    public void AddText(string text) {
      var hash = GetMd5FromString(text);
      // Since Clipboard is implemented in a very poor way in Windows, there can be situations while the same data may
      // come a couple of times. In that case we have to skip adding that data.
      if (hash == lastHash) {
        return;
      }

      // Before adding new record we have to check if this data was already added, and if so, let's just update date.
      var command = db.CreateCommand();
      command.CommandText = "SELECT rowid FROM 'data' WHERE hash = @Hash";
      command.Parameters.Add("@Hash", DbType.String).Value = hash;
      var rowId = command.ExecuteScalar();
      if (rowId != null) {
        // It seems like record with this data was already added, so let's just update it's date.
        command = db.CreateCommand();
        command.CommandText = "UPDATE 'data' SET date = datetime('now') WHERE rowid = @RowId";
        command.Parameters.Add("@RowId", DbType.UInt32).Value = rowId;
        command.ExecuteNonQuery();

        lastHash = hash;
        return;
      }

      // So data is new, let's add a new record then.
      command = db.CreateCommand();
      command.CommandText = "INSERT INTO 'data' ('type', 'data', 'hash') VALUES (@Type, @Data, @Hash)";
      command.Parameters.Add("@Type", DbType.String).Value = "text/plain";
      command.Parameters.Add("@Data", DbType.Binary).Value = StringToBytes(text);
      command.Parameters.Add("@Hash", DbType.String).Value = hash;
      command.ExecuteNonQuery();

      lastHash = hash;
    }

    private static string GetMd5FromString(string text) {
      // byte array representation of that string
      var encodedText = new UTF8Encoding().GetBytes(text);

      // need MD5 to calculate the hash
      var hash = ((HashAlgorithm) CryptoConfig.CreateFromName("MD5")).ComputeHash(encodedText);

      // string representation (similar to UNIX format)
      return BitConverter.ToString(hash)
        // without dashes
        .Replace("-", string.Empty)
        // make lowercase
        .ToLower();
    }

    public void GetAmount(int amount, GetCallback getCallback, object tag) {
      SQLiteCommand Command = db.CreateCommand();
      Command.CommandText = "SELECT rowid, type, data FROM 'data' ORDER BY date DESC LIMIT @Amount";
      Command.Parameters.Add("@Amount", DbType.Int32).Value = amount;

      SQLiteDataReader reader = Command.ExecuteReader();
      while (reader.Read()) {
        byte[] data = GetBytes(reader, 2);
        getCallback(reader.GetInt32(0), reader.GetString(1), data, tag);
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
