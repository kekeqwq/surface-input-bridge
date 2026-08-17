using System;
using System.Runtime.InteropServices;


public static class MouseHook
{
    public static bool Block = false;


    private static IntPtr hookId = IntPtr.Zero;


    private static LowLevelMouseProc proc =
        HookCallback;



    const int WH_MOUSE_LL = 14;


    const int WM_MOUSEMOVE = 0x0200;
    const int WM_LBUTTONDOWN = 0x0201;
    const int WM_LBUTTONUP = 0x0202;
    const int WM_RBUTTONDOWN = 0x0204;
    const int WM_RBUTTONUP = 0x0205;
    const int WM_MBUTTONDOWN = 0x0207;
    const int WM_MBUTTONUP = 0x0208;



    delegate IntPtr LowLevelMouseProc(
        int nCode,
        IntPtr wParam,
        IntPtr lParam
    );



    [StructLayout(LayoutKind.Sequential)]
    struct MSLLHOOKSTRUCT
    {
        public int x;
        public int y;

        public uint mouseData;
        public uint flags;
        public uint time;

        public IntPtr dwExtraInfo;
    }





    [DllImport("user32.dll")]
    static extern IntPtr SetWindowsHookEx(
        int idHook,
        LowLevelMouseProc lpfn,
        IntPtr hMod,
        uint dwThreadId
    );



    [DllImport("user32.dll")]
    static extern bool UnhookWindowsHookEx(
        IntPtr hhk
    );



    [DllImport("user32.dll")]
    static extern IntPtr CallNextHookEx(
        IntPtr hhk,
        int nCode,
        IntPtr wParam,
        IntPtr lParam
    );



    [DllImport("kernel32.dll")]
    static extern IntPtr GetModuleHandle(
        string lpModuleName
    );



    [DllImport("user32.dll")]
    static extern int ShowCursor(
        bool bShow
    );



    static bool cursorHidden = false;



    public static void SetCursorHidden(
        bool hidden
    )
    {
        if(hidden == cursorHidden)
            return;


        cursorHidden = hidden;


        if(hidden)
        {
            while(
                ShowCursor(false) >= 0
            )
            {
            }
        }
        else
        {
            while(
                ShowCursor(true) < 0
            )
            {
            }
        }
    }





    public static void Start()
    {
        hookId =
            SetHook(proc);


        Console.WriteLine(
            $"MouseHook={hookId}"
        );
    }





    public static void Stop()
    {
        if(hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(
                hookId
            );

            hookId = IntPtr.Zero;
        }


        SetCursorHidden(false);
    }





    static IntPtr SetHook(
        LowLevelMouseProc proc
    )
    {
        using var cur =
            System.Diagnostics.Process
            .GetCurrentProcess();


        using var mod =
            cur.MainModule;


        return SetWindowsHookEx(
            WH_MOUSE_LL,
            proc,
            GetModuleHandle(
                mod.ModuleName
            ),
            0
        );
    }





    static IntPtr HookCallback(
        int nCode,
        IntPtr wParam,
        IntPtr lParam
    )
    {

        if(nCode >= 0)
        {
            int msg =
                wParam.ToInt32();


            if(Block)
            {
                switch(msg)
                {
                    case WM_MOUSEMOVE:
                    case WM_LBUTTONDOWN:
                    case WM_LBUTTONUP:
                    case WM_RBUTTONDOWN:
                    case WM_RBUTTONUP:
                    case WM_MBUTTONDOWN:
                    case WM_MBUTTONUP:

                        return (IntPtr)1;
                }
            }
        }



        return CallNextHookEx(
            hookId,
            nCode,
            wParam,
            lParam
        );
    }
}