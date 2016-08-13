using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        private ApplicationBase() {
            // Create a simple tray menu with only one item.
            TrayMenu = new ContextMenu();
            TrayMenu.MenuItems.Add("Exit", OnExit);

            Components = new Container();
            TrayIcon = new NotifyIcon(Components) {
                ContextMenu = TrayMenu,
                Icon = new Icon(SystemIcons.Application, 40, 40),
                Text = "ClipTracker",
                Visible = true
            };
//            notifyIcon.ContextMenuStrip.Opening += ContextMenuStrip_Opening;
//            notifyIcon.DoubleClick += notifyIcon_DoubleClick;

            Application.ApplicationExit += new EventHandler(OnApplicationExit);
        }

        void OnApplicationExit(object sender, EventArgs e) {
            // Call dispose method for cleanup.
            Dispose();
        }

        private void OnExit(object sender, EventArgs e) {
            // Perform exit from application.
            Application.Exit();
        }

        public void Dispose() {
            // Release the icon resource.
            Components.Dispose();
//            trayIcon.Dispose();
        }
    }
}
