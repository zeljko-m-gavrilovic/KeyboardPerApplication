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
using System.Collections;

namespace BigNumbers.letters
{
   

        public class SystemTrayIcon : Form
        {

            /*
             * Delegate and imports from pinvoke.net:
            */
            delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType,
                IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

            /* Need to ensure delegate is not collected while we're using it,
             * storing it in a class field is simplest way to do this.
             */
            static WinEventDelegate procDelegate = new WinEventDelegate(WinEventProc);

            [DllImport("user32.dll")]
            static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr
               hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess,
               uint idThread, uint dwFlags);

            [DllImport("user32.dll")]
            static extern bool UnhookWinEvent(IntPtr hWinEventHook);

            /*
             * Constants from winuser.h
             */
            const uint EVENT_SYSTEM_FOREGROUND = 3;
            const uint WINEVENT_OUTOFCONTEXT = 0;

            private const uint WM_INPUTLANGCHANGEREQUEST = 0x0050;
            private const uint KLF_ACTIVATE = 1;

            
            [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
            public static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

            [DllImport("user32.dll", SetLastError = true)]
            static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

            /*
             * When you don't want the ProcessId, use this overload and pass IntPtr.Zero for the second parameter
             */
            [DllImport("user32.dll")]
            static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

            [DllImport("user32.dll")]
            private static extern bool PostMessage(IntPtr hhwnd, uint msg, IntPtr wparam, IntPtr lparam);

            [DllImport("user32.dll")]
            private static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);

            private static InputLanguage GetInputLanguageByName(string inputName)
            {
                foreach (InputLanguage lang in InputLanguage.InstalledInputLanguages)
                {
                    if (lang.Culture.EnglishName.ToLower().StartsWith(inputName))
                        return lang;
                }
                return null;
            }

            static void WinEventProc(IntPtr hWinEventHook, uint eventType,
                IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
            {
                /*
                 * Take a foreground application
                 */
                uint pid = 1;
                GetWindowThreadProcessId(hwnd, out pid);
                Process p = Process.GetProcessById((int)pid);
                string appName = p.ProcessName;
                //Console.WriteLine("appName: " + appName);

                /*
                 * Change the keyboard language for a foreground application if user has preferences 
                 */
                List<string[]> preferences = getPreferences();
                for(int i = 0; i < preferences.Count; i++) {
                    string application = preferences[i][0];
                    if(application != null) {    
                        if (appName.ToLower().Contains(application.ToLower()))
                        {
                            string keyboard = preferences[i][1];
                            InputLanguage language = GetInputLanguageByName(keyboard);
                            if(language != null) {
                                bool result = PostMessage(GetForegroundWindow(), WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, language.Handle);
                                return;
                                //Console.WriteLine("keyboard changed for foreground application "
                                    //+ userProperty.Name + 
                                //    + " to keyboard language: " + language.LayoutName);
                            }
                        }
                    }
                }
                InputLanguage defaultLanguage = InputLanguage.DefaultInputLanguage;
                PostMessage(GetForegroundWindow(), WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, defaultLanguage.Handle);
                    //Console.WriteLine("keyboard changed for foreground application "
                    //                //+ userProperty.Name +
                    //                + " to default keyboard language: " + defaultLanguage.LayoutName);
            }

           
            private static IntPtr hhook;
            NotifyIcon trayIcon;
            
           

            [STAThread]
            public static void Main()
            {
                hhook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero,
                       procDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);
                Application.Run(new SystemTrayIcon());
            }
            
            public SystemTrayIcon()
            {
                /*
                 * Create a simple tray context menu with items
                 */
                ContextMenuStrip trayMenu = new ContextMenuStrip();
                ToolStripMenuItem preferencesMenuItem = new ToolStripMenuItem ("Preferences");
                preferencesMenuItem.Click += new EventHandler(OnPreferences);
                trayMenu.Items.Add(preferencesMenuItem);

                ToolStripMenuItem aboutMenuItem = new ToolStripMenuItem("About");
                aboutMenuItem.Click += new EventHandler(OnAbout);
                trayMenu.Items.Add(aboutMenuItem);

                ToolStripMenuItem exitMenuItem = new ToolStripMenuItem("Exit");
                exitMenuItem.Click += new EventHandler(OnExit);
                trayMenu.Items.Add(exitMenuItem);
                

                /*
                 * Create a tray icon
                */
                trayIcon = new NotifyIcon();
                string trayMessage = "Use right mouse click to open the context menu";
                trayIcon.Text = trayMessage;
                trayIcon.Icon = new Icon(GetType(), "Oxygen-Icons.org-Oxygen-Apps-accessories-character-map.ico");
                trayIcon.ContextMenuStrip = trayMenu;
                trayIcon.Visible = true;
                trayIcon.BalloonTipTitle = "letters";
                trayIcon.BalloonTipText = trayMessage;
                trayIcon.ShowBalloonTip(100);
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
               
                /*
                 * parse the stored properties
                 */
                List<string[]> result = getPreferences();

                for (int i = 0; i < result.Count; i++)
                {
                    dt.Rows.Add(result[i][0], result[i][1]);
                }

                Form preferencesForm = new PreferencesForm(dt);
                preferencesForm.ShowDialog();
                if (preferencesForm.DialogResult == DialogResult.OK)
                {
                    string propertiesToBeStored = "";
                    foreach (DataRow row in dt.Rows)
                    {
                        string application = row[0].ToString();
                        string keyboardLayout = row[1].ToString();
                        bool validData = (application != null) && (application.Length > 0) &&
                            (keyboardLayout != null) && (keyboardLayout.Length > 0);
                        if (validData)
                        {
                            string pair = application + '=' +  keyboardLayout + ";";
                            propertiesToBeStored = propertiesToBeStored + pair;
                        }
                    }
                    Properties.Settings.Default.preferences = propertiesToBeStored;
                    Properties.Settings.Default.Save();
                }
            
                preferencesForm.Dispose();
            }

            private static List<string[]> getPreferences()
            {
                string properties = Properties.Settings.Default.preferences;
                List<string[]> result = new List<string[]>();
                if(properties != null && properties.Length > 0) {
                    string[] pairs = properties.Split(';');
                    for (int i = 0; i < pairs.Length; i++)
                    {
                        if(pairs[i].Length > 0 && pairs[i].Contains('=')) {
                            string[] values = pairs[i].Split('=');
                            string application = values[0];
                            string keyboard = values[1];
                            result.Add(new string[2] {application, keyboard});
                        }
                    }
                }
                return result;
            }



            private void OnAbout(object sender, EventArgs e)
            {
                new AboutBox().ShowDialog();
            }

            private void OnExit(object sender, EventArgs e)
            {
                UnhookWinEvent(hhook);
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