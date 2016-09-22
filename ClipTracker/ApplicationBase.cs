using System;
using System.Linq;
using System.ComponentModel;
using System.Drawing;
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
    private Storage Storage;
    private ClipboardWatcher ClipboardWatcher;
    private GetCallback StorageGetCallback;

    private ApplicationBase() {
      // Initialize instances.
      Storage = new Storage();
      StorageGetCallback = AddTrayMenuItemFromDb;
      ClipboardWatcher = new ClipboardWatcher();

      // Create a simple tray menu with only one item.
      TrayMenu = new ContextMenu();
      TrayMenu.MenuItems.Add("Exit", OnExit);
      TrayMenu.MenuItems.Add("-");
      TrayMenu.Popup += OnTrayMenuPopup;

      Components = new Container();
      TrayIcon = new NotifyIcon(Components) {
        ContextMenu = TrayMenu,
        Icon = new Icon(SystemIcons.Application, 40, 40),
        Text = "ClipTracker",
        Visible = true,
      };
//            notifyIcon.ContextMenuStrip.Opening += ContextMenuStrip_Opening;
//            notifyIcon.DoubleClick += notifyIcon_DoubleClick;

      Application.ApplicationExit += OnApplicationExit;
      ClipboardWatcher.ClipboardUpdate += OnClipboardUpdate;
    }

    private void OnClipboardCopyClick(object sender, EventArgs e) {
      var menuItem = (MenuItem) sender;
      var id = (int) menuItem.Tag;
      var storageItem = Storage.GetItem(id);
      if (storageItem.id == id) {
        var data = Storage.BytesToString(storageItem.data);
        Clipboard.SetText(data);
      }
    }

    public void AddTrayMenuItemFromDb(int rowId, string type, byte[] data) {
      if (type == "text/plain") {
        var text = Storage.BytesToString(data);
        if (text.Length > 43) {
          var left = text.Substring(0, 20);
          var right = text.Substring(text.Length - 20);
          text = left + "..." + right;
        }
        var item = new MenuItem(text) {Tag = rowId};
        item.Click += OnClipboardCopyClick;
        TrayMenu.MenuItems.Add(item);
      }
    }

    private void OnTrayMenuPopup(object sender, EventArgs e) {
      // Cleanup menu.
      foreach (var menuItem in TrayMenu.MenuItems.OfType<MenuItem>().Reverse()) {
        var isClipboard = (menuItem.Tag != null);
        if (isClipboard) {
          menuItem.Dispose();
        }
      }

      Storage.GetAmount(10, StorageGetCallback);
    }

    private void OnClipboardUpdate(object sender, EventArgs e) {
      if (Clipboard.ContainsText()) {
        Storage.AddText(Clipboard.GetText());
      }
    }

    private void OnExit(object sender, EventArgs e) {
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
      Storage.Dispose();

      // If called not from destuctor.
      if (disposing) {
        // Release the icon resource.
        Components.Dispose();
      }
    }
  }
}
