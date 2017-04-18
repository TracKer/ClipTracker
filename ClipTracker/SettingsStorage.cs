using System;
using System.Data;
using System.Data.SQLite;

namespace ClipTracker {

  class SettingsStorage: BasicStorage {
    public SettingsStorage(SQLiteConnection db, bool performInstall = false) : base(db, performInstall) { }

    protected override void Install() {
      var command = Db.CreateCommand();
      command.CommandText = "CREATE TABLE 'settings' ('name' VARCHAR NOT NULL ,'value' VARCHAR DEFAULT (null))";
      command.ExecuteNonQuery();
      command.Dispose();
    }

    protected override void Init() {
      // Nothing here.
    }

    public decimal GetExpirationPeriod() {
      var command = Db.CreateCommand();
      command.CommandText = "SELECT value FROM 'settings' WHERE name = 'expiration_period' LIMIT 1";
      var value = command.ExecuteScalar();
      if (value != null) {
        return decimal.Parse((string) value);
      }
      return 0;
    }

    public void SetExpirationPeriod(decimal value) {
      try {
        var command = Db.CreateCommand();
        command.CommandText = "UPDATE 'settings' SET value = @Value WHERE name = @Name";
        command.Parameters.Add("@Name", DbType.String).Value = "expiration_period";
        command.Parameters.Add("@Value", DbType.String).Value = value.ToString();
        var updated = command.ExecuteNonQuery();
        command.Dispose();
        if (updated == 0) {
          throw new Exception("Update failed.");
        }
      }
      catch (Exception e) {
        var command = Db.CreateCommand();
        command.CommandText = "INSERT INTO 'settings' ('name', 'value') VALUES (@Name, @Value)";
        command.Parameters.Add("@Name", DbType.String).Value = "expiration_period";
        command.Parameters.Add("@Value", DbType.String).Value = value.ToString();
        command.ExecuteNonQuery();
        command.Dispose();
      }
    }
  }
}
