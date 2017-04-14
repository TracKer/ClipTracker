using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing.Text;
using System.IO;
using System.Net.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace ClipTracker {

  struct DataStorageItem {
    public int id;
    public string type;
    public byte[] data;
  }

  public delegate void GetCallback(int rowId, string type, byte[] data, object tag);

  class DataStorage {
    private SQLiteConnection _db;
    private string _lastHash;

    public DataStorage(SQLiteConnection db, bool performInstall = false) {
      _db = db;
      var command = _db.CreateCommand();
      if (performInstall) {
        command.CommandText = "CREATE TABLE 'data' ('type' VARCHAR NOT NULL, 'data' BLOB DEFAULT NULL, 'hash' VARCHAR NOT NULL, 'date' DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP)";
        command.ExecuteNonQuery();
        command.Dispose();
      } else {
        // We have to initialize last hash, to avoid preforming useless operations.
        command = _db.CreateCommand();
        command.CommandText = "SELECT hash FROM 'data' ORDER BY date DESC LIMIT 1";
        var hash = command.ExecuteScalar();
        if (hash != null) {
          _lastHash = (string) hash;
        }
        command.Dispose();
      }
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

    public void AddText(string text) {
      var hash = GetMd5FromString(text);
      // Since Clipboard is implemented in a very poor way in Windows, there can be situations while the same data may
      // come a couple of times. In that case we have to skip adding that data.
      if (hash == _lastHash) {
        return;
      }

      // Before adding new record we have to check if this data was already added, and if so, let's just update date.
      var command = _db.CreateCommand();
      command.CommandText = "SELECT rowid FROM 'data' WHERE hash = @Hash";
      command.Parameters.Add("@Hash", DbType.String).Value = hash;
      var rowId = command.ExecuteScalar();
      if (rowId != null) {
        // It seems like record with this data was already added, so let's just update it's date.
        command = _db.CreateCommand();
        command.CommandText = "UPDATE 'data' SET date = datetime('now') WHERE rowid = @RowId";
        command.Parameters.Add("@RowId", DbType.UInt32).Value = rowId;
        command.ExecuteNonQuery();

        _lastHash = hash;
        return;
      }

      // So data is new, let's add a new record then.
      command = _db.CreateCommand();
      command.CommandText = "INSERT INTO 'data' ('type', 'data', 'hash') VALUES (@Type, @Data, @Hash)";
      command.Parameters.Add("@Type", DbType.String).Value = "text/plain";
      command.Parameters.Add("@Data", DbType.Binary).Value = StringToBytes(text);
      command.Parameters.Add("@Hash", DbType.String).Value = hash;
      command.ExecuteNonQuery();

      _lastHash = hash;
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
      SQLiteCommand Command = _db.CreateCommand();
      Command.CommandText = "SELECT rowid, type, data FROM 'data' ORDER BY date DESC LIMIT @Amount";
      Command.Parameters.Add("@Amount", DbType.Int32).Value = amount;

      SQLiteDataReader reader = Command.ExecuteReader();
      while (reader.Read()) {
        byte[] data = GetBytes(reader, 2);
        getCallback(reader.GetInt32(0), reader.GetString(1), data, tag);
      }
    }

    public DataStorageItem GetItem(int id) {
      SQLiteCommand Command = _db.CreateCommand();
      Command.CommandText = "SELECT rowid, type, data FROM 'data' WHERE rowid = @Id";
      Command.Parameters.Add("@Id", DbType.Int32).Value = id;

      var item = new DataStorageItem();
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
