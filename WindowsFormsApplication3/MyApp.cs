using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing;
using System.Configuration;
using System.Data;

namespace WindowsFormsApplication3
{
    //using System;
    //using System.Drawing;
    //using System.Windows.Forms;

        public class SysTrayApp : Form
        {

            // Delegate and imports from pinvoke.net:

            delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType,
                IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

            [DllImport("user32.dll")]
            static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr
               hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess,
               uint idThread, uint dwFlags);

            [DllImport("user32.dll")]
            static extern bool UnhookWinEvent(IntPtr hWinEventHook);

            // Constants from winuser.h
            const uint EVENT_SYSTEM_FOREGROUND = 3;
            const uint WINEVENT_OUTOFCONTEXT = 0;

            // Need to ensure delegate is not collected while we're using it,
            // storing it in a class field is simplest way to do this.
            static WinEventDelegate procDelegate = new WinEventDelegate(WinEventProc);

            [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
            public static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

            [DllImport("user32.dll", SetLastError = true)]
            static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

            // When you don't want the ProcessId, use this overload and pass IntPtr.Zero for the second parameter
            [DllImport("user32.dll")]
            static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

            [DllImport("user32.dll")]
            private static extern bool PostMessage(IntPtr hhwnd, uint msg, IntPtr wparam, IntPtr lparam);

            [DllImport("user32.dll")]
            private static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);

            private const uint WM_INPUTLANGCHANGEREQUEST = 0x0050;
            private const uint KLF_ACTIVATE = 1;
            //private const string en_US = "00000409";

            private static void ChangeLanguage(string code)
            {
                bool result = PostMessage(GetForegroundWindow(), WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, LoadKeyboardLayout(code, KLF_ACTIVATE));
                Console.WriteLine("result is " + result);
            }

            private static void ChangeLanguage(IntPtr code)
            {
                bool result = PostMessage(GetForegroundWindow(), WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, code);
                Console.WriteLine("result is " + result);
            }



            static void WinEventProc(IntPtr hWinEventHook, uint eventType,
                IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
            {
                //Console.WriteLine("Foreground changed to {0:x8}", hwnd.ToInt32());

                //IntPtr hwnd2 = GetForegroundWindow();
                //StringBuilder windowtitle = new StringBuilder(256);
                //if (GetWindowText(hwnd, windowtitle, windowtitle.Capacity) > 0)
                //    Console.WriteLine("window in focus is: " + windowtitle);
                uint pid = 1;
                GetWindowThreadProcessId(hwnd, out pid);
                Process p = Process.GetProcessById((int)pid);
                string appName = p.ProcessName;
                Console.WriteLine("appName: " + appName);

                foreach(SettingsProperty userProperty in Properties.Settings.Default.Properties) {
                    if (appName.ToLower().Contains((string) userProperty.Name))
                    {
                        InputLanguage language = GetInputLanguageByName((string) userProperty.DefaultValue);
                        if(language != null) {
                            ChangeLanguage(language.Handle);
                            Console.WriteLine("keyboard changed for foreground application "
                                + userProperty.Name + 
                                " to keyboard language: " + language.LayoutName);
                        }
                    }
                }
            }

            public static InputLanguage GetInputLanguageByName(string inputName)
            {
                foreach (InputLanguage lang in InputLanguage.InstalledInputLanguages)
                {
                    if (lang.Culture.EnglishName.ToLower().StartsWith(inputName))
                        return lang;
                }
                return null;
            }


            [STAThread]
            public static void Main()
            {
                

                // Listen for foreground changes across all processes/threads on current desktop...
                
                //MessageBox.Show("Tracking focus, close message box to exit.");

                // MessageBox provides the necessary mesage loop that SetWinEventHook requires.
                //MessageBox.Show("Tracking focus, close message box to exit.");

                IntPtr hhook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero,
                       procDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);

                Application.Run(new SysTrayApp());
               

                //UnhookWinEvent(hhook);


                
   
            }

            private NotifyIcon trayIcon;
            private ContextMenu trayMenu;

            public SysTrayApp()
            {
                // Create a simple tray menu with only one item.
                trayMenu = new ContextMenu();
                trayMenu.MenuItems.Add("Preferences", OnPreferences);
                trayMenu.MenuItems.Add("About", OnAbout);
                trayMenu.MenuItems.Add("Exit", OnExit);
                

                // Create a tray icon. In this example we use a
                // standard system icon for simplicity, but you
                // can of course use your own custom icon too.
                trayIcon = new NotifyIcon();
                trayIcon.Text = "Set a keyboard layout per application";
                trayIcon.Icon = new Icon(GetType(), "Oxygen-Icons.org-Oxygen-Apps-accessories-character-map.ico");

                // Add menu to tray icon and show it.
                trayIcon.ContextMenu = trayMenu;
                trayIcon.Visible = true;
            }

            protected override void OnLoad(EventArgs e)
            {
                Visible = false; // Hide form window.
                ShowInTaskbar = false; // Remove from taskbar.

                base.OnLoad(e);
            }

            private void OnPreferences(object sender, EventArgs e)
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("Application");
                dt.Columns.Add("Keyboard Language layout");
                foreach (SettingsProperty userProperty in Properties.Settings.Default.Properties)
                {
                    dt.Rows.Add(userProperty.Name, userProperty.DefaultValue);
                }

                Form form1 = new Form1(dt);
                form1.ShowDialog();
                if (form1.DialogResult == DialogResult.OK)
                {
                    // Display a message box indicating that the OK button was clicked.
                    //MessageBox.Show("The OK button on the form was clicked.");
                    // Optional: Call the Dispose method when you are finished with the dialog box.
                    Properties.Settings.Default.Properties.Clear();
                    foreach (DataRow row in dt.Rows)
                    {
                        string application = row[0].ToString();
                        string keyboardLayout = row[1].ToString();
                        //sp = Properties.Settings.Default.Properties[application];
                        //if(sp != null) {
                        //    Properties.Settings.Default.Properties[application].DefaultValue = keyboardLayout;
                        //}
                        //else
                        //{
                            SettingsProperty newProperty = new SettingsProperty(application);
                            newProperty.DefaultValue = keyboardLayout;
                            newProperty.IsReadOnly = false;
                            Properties.Settings.Default.Properties.Add(newProperty);

                        //}
                    }
                    Properties.Settings.Default.Save(); 
                }
                else
                {
                    // Display a message box indicating that the Cancel button was clicked.
                    MessageBox.Show("The Cancel button on the form was clicked.");
                    // Optional: Call the Dispose method when you are finished with the dialog box.
                    
                }
                form1.Dispose();
            }

            private void OnAbout(object sender, EventArgs e)
            {
                MessageBox.Show("Track the foreground application and change the keyboard layout according to an user preferences");
            }

            private void OnExit(object sender, EventArgs e)
            {
                Application.Exit();
            }

            protected override void Dispose(bool isDisposing)
            {
                if (isDisposing)
                {
                    // Release the icon resource.
                    trayIcon.Dispose();
                }

                base.Dispose(isDisposing);
            }

            


        }
  
}
