using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.ComponentModel;

namespace ClipTracker {

  sealed class ClipboardWatcher : NativeWindow, IDisposable {
    static EventHandlerList eventHandlers = new EventHandlerList();

    private static readonly object EVENT_CLIPBOARDUPDATE = new object();

    public static event EventHandler ClipboardUpdate {
      add { eventHandlers.AddHandler(EVENT_CLIPBOARDUPDATE, value); }
      remove { eventHandlers.RemoveHandler(EVENT_CLIPBOARDUPDATE, value); }
    }

    private static void RaiseClipboardUpdate() {
      Delegate update = eventHandlers[EVENT_CLIPBOARDUPDATE];
      if (update != null) {
        ((EventHandler) update)(null, EventArgs.Empty);
      }
    }

    const int WM_CLIPBOARDUPDATE = 0x031D;

    protected override void WndProc(ref Message m) {
      if (m.Msg == WM_CLIPBOARDUPDATE) {
        RaiseClipboardUpdate();
//                IDataObject iData = Clipboard.GetDataObject();
//
//                if (iData.GetDataPresent(DataFormats.Text)) {
////                    String List = String.Join(Environment.NewLine, iData.GetFormats());
////                    MessageBox.Show(List, "Copy");
////                    MessageBox.Show((string) iData.GetData(DataFormats.Text), "");
////                    MessageBox.Show(Clipboard.GetText(), "");
//                }
      }

      base.WndProc(ref m);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    public ClipboardWatcher() {
      CreateParams cp = new CreateParams();
      cp.Parent = (IntPtr) (-3); // HWND_MESSAGE
      CreateHandle(cp);

      AddClipboardFormatListener(Handle);
    }

    public void Dispose() {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing) {
      RemoveClipboardFormatListener(Handle);
      DestroyHandle();
    }
  }
}
