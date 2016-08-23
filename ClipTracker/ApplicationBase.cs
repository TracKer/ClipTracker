using System;
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
                    if (_instance == null)
                        _instance = new ApplicationBase();
                }
            }

            return _instance;
        }

        public Container Components;
        public NotifyIcon TrayIcon;
        public ContextMenu TrayMenu;
        private Storage Storage;
        private ClipboardWatcher ClipboardWatcher;

        private ApplicationBase() {
            // Initialize instances.
            Storage = new Storage();
            ClipboardWatcher = new ClipboardWatcher();

            // Create a simple tray menu with only one item.
            TrayMenu = new ContextMenu();
            TrayMenu.MenuItems.Add("Exit", OnExit);

            Components = new Container();
            TrayIcon = new NotifyIcon(Components) {
                ContextMenu = TrayMenu,
                Icon = new Icon(SystemIcons.Application, 40, 40),
                Text = "ClipTracker",
                Visible = true,
            };
//            notifyIcon.ContextMenuStrip.Opening += ContextMenuStrip_Opening;
//            notifyIcon.DoubleClick += notifyIcon_DoubleClick;

            Application.ApplicationExit += new EventHandler(OnApplicationExit);
            ClipboardWatcher.ClipboardUpdate += new EventHandler(OnClipboardUpdate);
        }

        private void OnClipboardUpdate(object sender, EventArgs e) {
            MessageBox.Show("Clipboard updated", "Info");
            if (Clipboard.ContainsText()) {
                Storage.AddText(Clipboard.GetText());
            }
        }

        private void OnExit(object sender, EventArgs e) {
            // Perform exit from application.
            Application.Exit();
        }

        void OnApplicationExit(object sender, EventArgs e) {
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
