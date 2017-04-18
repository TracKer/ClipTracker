using System;
using System.Data;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text;

namespace ClipTracker {

  struct DataStorageItem {
    public int Id;
    public string Type;
    public byte[] Data;
  }

  public delegate void GetCallback(int rowId, string type, byte[] data, object tag);

  class DataStorage: BasicStorage {
    private string _lastHash;

    public DataStorage(SQLiteConnection db, bool performInstall = false) : base(db, performInstall) { }

    protected override void Install() {
      var command = Db.CreateCommand();
      command.CommandText = "CREATE TABLE 'data' ('type' VARCHAR NOT NULL, 'data' BLOB DEFAULT NULL, 'hash' VARCHAR NOT NULL, 'date' DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP)";
      command.ExecuteNonQuery();
      command.Dispose();
    }

    protected override void Init() {
      // We have to initialize last hash, to avoid preforming useless operations.
      var command = Db.CreateCommand();
      command.CommandText = "SELECT hash FROM 'data' ORDER BY date DESC LIMIT 1";
      var hash = command.ExecuteScalar();
      if (hash != null) {
        _lastHash = (string) hash;
      }
      command.Dispose();
    }

    public void AddText(string text) {
      var hash = GetMd5FromString(text);
      // Since Clipboard is implemented in a very poor way in Windows, there can be situations while the same data may
      // come a couple of times. In that case we have to skip adding that data.
      if (hash == _lastHash) {
        return;
      }

      // Before adding new record we have to check if this data was already added, and if so, let's just update date.
      var command = Db.CreateCommand();
      command.CommandText = "SELECT rowid FROM 'data' WHERE hash = @Hash";
      command.Parameters.Add("@Hash", DbType.String).Value = hash;
      var rowId = command.ExecuteScalar();
      command.Dispose();
      if (rowId != null) {
        // It seems like record with this data was already added, so let's just update it's date.
        command = Db.CreateCommand();
        command.CommandText = "UPDATE 'data' SET date = datetime('now') WHERE rowid = @RowId";
        command.Parameters.Add("@RowId", DbType.UInt32).Value = rowId;
        command.ExecuteNonQuery();
        command.Dispose();

        _lastHash = hash;
        return;
      }

      // So data is new, let's add a new record then.
      command = Db.CreateCommand();
      command.CommandText = "INSERT INTO 'data' ('type', 'data', 'hash') VALUES (@Type, @Data, @Hash)";
      command.Parameters.Add("@Type", DbType.String).Value = "text/plain";
      command.Parameters.Add("@Data", DbType.Binary).Value = StringToBytes(text);
      command.Parameters.Add("@Hash", DbType.String).Value = hash;
      command.ExecuteNonQuery();
      command.Dispose();

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
      var command = Db.CreateCommand();
      command.CommandText = "SELECT rowid, type, data FROM 'data' ORDER BY date DESC LIMIT @Amount";
      command.Parameters.Add("@Amount", DbType.Int32).Value = amount;

      var reader = command.ExecuteReader();
      while (reader.Read()) {
        byte[] data = GetBytes(reader, 2);
        getCallback(reader.GetInt32(0), reader.GetString(1), data, tag);
      }
    }

    public DataStorageItem GetItem(int id) {
      var command = Db.CreateCommand();
      command.CommandText = "SELECT rowid, type, data FROM 'data' WHERE rowid = @Id";
      command.Parameters.Add("@Id", DbType.Int32).Value = id;

      DataStorageItem item;

      var reader = command.ExecuteReader();
      if (reader.HasRows) {
        reader.Read();

        item = new DataStorageItem {
          Id = reader.GetInt32(0),
          Type = reader.GetString(1),
          Data = GetBytes(reader, 2)
        };
      } else {
        item = new DataStorageItem {
          Id = 0
        };
      }

      return item;
    }
  }
}
