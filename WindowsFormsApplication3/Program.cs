using System;
using System.Windows;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;

class ForegroundTracker
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

    public static void Main2()
    {
        // Listen for foreground changes across all processes/threads on current desktop...
        IntPtr hhook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero,
                procDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);

        // MessageBox provides the necessary mesage loop that SetWinEventHook requires.
        MessageBox.Show("Tracking focus, close message box to exit.");

        UnhookWinEvent(hhook);
    }

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
    private const string en_US = "00000409";

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
        
        
        string languageStr = "en";
        if (appName.ToLower().Contains("vlc"))
        {
            languageStr ="serb";
           
        }
        
        InputLanguage language = GetInputLanguageByName(languageStr);
        ChangeLanguage(language.Handle);
        Console.WriteLine("keyboard changed to: " + language.LayoutName);
        
        
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
    //public static void SetKeyboardLayout(InputLanguage layout)
    //{
     //   InputLanguage.CurrentInputLanguage = layout;
    //} 

}