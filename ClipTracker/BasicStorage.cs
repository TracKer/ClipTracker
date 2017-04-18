using System.Data.SQLite;
using System.IO;

namespace ClipTracker {
  public abstract class BasicStorage {
    protected SQLiteConnection Db;

    public BasicStorage(SQLiteConnection db, bool performInstall = false) {
      Db = db;
      if (performInstall) {
        Install();
      } else {
        Init();
      }
    }

    protected abstract void Install();
    protected abstract void Init();

    public static byte[] StringToBytes(string str) {
      byte[] bytes = new byte[str.Length * sizeof(char)];
      System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
      return bytes;
    }

    public static string BytesToString(byte[] bytes) {
      char[] chars = new char[bytes.Length / sizeof(char)];
      System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
      return new string(chars);
    }

    protected static byte[] GetBytes(SQLiteDataReader reader, int i) {
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