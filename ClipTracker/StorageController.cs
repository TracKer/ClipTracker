using System;
using System.Data.SQLite;
using System.IO;

namespace ClipTracker {
  class StorageController: IDisposable {
    private readonly SQLiteConnection _db;
    public readonly DataStorage DataStorage;

    public StorageController() {
      var filePath = GetFilePath();
      var installNeeded = false;

      if (!File.Exists(filePath)) {
        // Looks like storage is not exist yet, let's perform installation then and create a new storage.
        PerformInstallation();
        installNeeded = true;
      }

      // Init database connection.
      _db = new SQLiteConnection("Data Source=" + filePath + "; Version=3;");
      _db.Open();

      DataStorage = new DataStorage(_db, installNeeded);

    }

    private static void PerformInstallation() {
      // Perform installation.
      Directory.CreateDirectory(GetLocationPath());
      SQLiteConnection.CreateFile(GetFilePath());
    }

    private static string GetLocationPath() {
      var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
      var locationPath = Path.Combine(appDataPath, "TRCK2K");
      locationPath = Path.Combine(locationPath, "ClipTracker");
      return locationPath;
    }

    private static string GetFilePath() {
      return Path.Combine(GetLocationPath(), "storage.sqlite");
    }

    public void Dispose() {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing) {
      _db.Close();
      _db.Dispose();
    }
  }
}
