using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;


public static class FocusWatcher
{
    public static Action<bool>? OnChanged;


    static Thread? thread;


    static bool lastState = false;


    const string TargetProcess = "obs64";



    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();



    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(
        IntPtr hWnd,
        out uint processId
    );




    public static void Start()
    {
        thread = new Thread(
            Loop
        );

        thread.IsBackground = true;

        thread.Start();
    }





    static void Loop()
    {
        while(true)
        {
            bool active =
                IsObsFocused();



            if(active != lastState)
            {
                lastState = active;


                Console.WriteLine(
                    active
                    ? "OBS FOCUS"
                    : "OBS LOST FOCUS"
                );


                OnChanged?.Invoke(
                    active
                );
            }


            Thread.Sleep(200);
        }
    }





    static bool IsObsFocused()
    {
        IntPtr hwnd =
            GetForegroundWindow();


        if(hwnd == IntPtr.Zero)
            return false;



        GetWindowThreadProcessId(
            hwnd,
            out uint pid
        );


        try
        {
            var p =
                Process.GetProcessById(
                    (int)pid
                );


            return
                p.ProcessName
                .Equals(
                    TargetProcess,
                    StringComparison.OrdinalIgnoreCase
                );
        }
        catch
        {
            return false;
        }
    }
}