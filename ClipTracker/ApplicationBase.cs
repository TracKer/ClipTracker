using System;
using System.Linq;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace ClipTracker {

  class ApplicationBase : IDisposable {
    private static ApplicationBase _instance;

    /// <summary>
    /// Singleton pattern.
    /// </summary>
    public static ApplicationBase GetInstance() {
      // For excluding ability of creation of two objects in multithread application.
      if (_instance == null) {
        lock (typeof(ApplicationBase)) {
          if (_instance == null) {
            _instance = new ApplicationBase();
          }
        }
      }

      return _instance;
    }

    public Container Components;
    public NotifyIcon TrayIcon;
    public ContextMenu TrayMenu;
    public readonly StorageController StorageController;
    private ClipboardWatcher ClipboardWatcher;
    private GetCallback StorageGetCallback;

    private ApplicationBase() {
      // Initialize instances.
      StorageController = new StorageController();
      StorageGetCallback = AddTrayMenuItemFromDb;
      ClipboardWatcher = new ClipboardWatcher();

      // Create a simple tray menu with only one item.
      TrayMenu = new ContextMenu();
      TrayMenu.MenuItems.Add("Settings", OnMenuSettingsClick);
      TrayMenu.MenuItems.Add("-");
      TrayMenu.MenuItems.Add("Exit", OnMenuExitClick);

      Components = new Container();
      TrayIcon = new NotifyIcon(Components) {
        ContextMenu = TrayMenu,
        Icon = new Icon(SystemIcons.Application, 40, 40),
        Text = "ClipTracker",
        Visible = true,
      };
      TrayIcon.MouseClick += OnTrayIconClick;

      Application.ApplicationExit += OnApplicationExit;
      ClipboardWatcher.ClipboardUpdate += OnClipboardUpdate;
    }

    private void OnClipboardCopyClick(object sender, EventArgs e) {
      var menuItem = (MenuItem) sender;
      var id = (int) menuItem.Tag;
      var storageItem = StorageController.DataStorage.GetItem(id);
      if (storageItem.Id == id) {
        var data = DataStorage.BytesToString(storageItem.Data);
        Clipboard.SetText(data);
      }
    }

    private void OnTrayIconClick(object sender, MouseEventArgs e) {
      if (e.Button == MouseButtons.Left) {
        // Hack from here:
        // http://stackoverflow.com/a/3782665/563049
        var popupMenu = new ContextMenu();
        StorageController.DataStorage.GetAmount(30, StorageGetCallback, popupMenu);
        var tmpMenu = TrayIcon.ContextMenu;
        TrayIcon.ContextMenu = popupMenu;
        var mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
        mi.Invoke(TrayIcon, null);
        TrayIcon.ContextMenu = tmpMenu;
      }
    }

    public void AddTrayMenuItemFromDb(int rowId, string type, byte[] data, object tag) {
      if (type == "text/plain") {
        var text = DataStorage.BytesToString(data);
        text = text.Trim();
        if (text.Length > 43) {
          var left = text.Substring(0, 20);
          var right = text.Substring(text.Length - 20);
          text = left + "..." + right;
        }
        var item = new MenuItem(text) {Tag = rowId};
        item.Click += OnClipboardCopyClick;
        ((ContextMenu) tag).MenuItems.Add(item);
      }
    }

    private void OnClipboardUpdate(object sender, EventArgs e) {
      if (Clipboard.ContainsText()) {
        StorageController.DataStorage.AddText(Clipboard.GetText());
      }
    }

    private void OnMenuSettingsClick(object sender, EventArgs e) {
      new FormSettings().ShowDialog();
    }

    private void OnMenuExitClick(object sender, EventArgs e) {
      // Perform exit from application.
      Application.Exit();
    }

    private void OnApplicationExit(object sender, EventArgs e) {
      // Call dispose method for cleanup.
      Dispose();
    }

    public void Dispose() {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing) {
      ClipboardWatcher.Dispose();
      StorageController.Dispose();

      // If called not from destuctor.
      if (disposing) {
        // Release the icon resource.
        Components.Dispose();
      }
    }
  }
}
