using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ClipTracker {
  public partial class FormSettings : Form {
    public FormSettings() {
      InitializeComponent();
    }

    public static bool CanWriteKey(string key) {
      try {
        RegistryPermission r = new RegistryPermission(RegistryPermissionAccess.Write, key);
        r.Demand();
        return true;
      }
      catch (Exception exception) {
        return false;
      }
    }

    public static bool CanReadKey(string key) {
      try {
        RegistryPermission r = new RegistryPermission(RegistryPermissionAccess.Read, key);
        r.Demand();
        return true;
      }
      catch (Exception exception) {
        return false;
      }
    }

    private void btnClose_Click(object sender, EventArgs e) {
      Close();
    }

    private void FormSettings_Shown(object sender, EventArgs e) {
      if (CanReadKey("ClipTracker")) {
        try {
          RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);
          if (rk == null) {
            throw new Exception("Failed accessing registry.");
          }
          var value = rk.GetValue("ClipTracker", "").ToString();
          if (value != Application.ExecutablePath) {
            throw new Exception("Wrong path in registry");
          }
          cbLoadOnStartup.Checked = true;
        }
        catch (Exception exception) {
          cbLoadOnStartup.Checked = false;
        }

        cbLoadOnStartup.Enabled = CanWriteKey("ClipTracker");
      } else {
        cbLoadOnStartup.Checked = false;
        cbLoadOnStartup.Enabled = false;
      }
    }

    private void cbLoadOnStartup_CheckedChanged(object sender, EventArgs e) {
      if (CanWriteKey("ClipTracker")) {
        try {
          RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
          if (rk == null) {
            throw new Exception("Failed accessing registry.");
          }
          if (cbLoadOnStartup.Checked) {
            rk.SetValue("ClipTracker", Application.ExecutablePath);
          } else {
            rk.DeleteValue("ClipTracker", false);
          }
        }
        catch (Exception exception) { }
      }
    }
  }
}
