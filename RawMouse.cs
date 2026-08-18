using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;


class RawMouse
{
    const int WM_INPUT = 0x00FF;

    const uint RID_INPUT = 0x10000003;

    const uint RIDEV_INPUTSINK = 0x00000100;

    const uint RIM_TYPEMOUSE = 0;



    public static Action<int, int>? OnMove;

    public static Action<byte, byte>? OnButton;

    public static Action<int>? OnWheel;



    // ============================================================
    // Keep the hidden window alive for the entire lifetime
    // ============================================================

    static HiddenWindow? window;

    static bool started;



    // ============================================================
    // Mouse state
    // ============================================================

    static bool leftDown;

    static bool rightDown;



    // ============================================================
    // RAWINPUT structures
    // ============================================================

    [StructLayout(LayoutKind.Sequential)]
    struct RAWINPUTDEVICE
    {
        public ushort usUsagePage;

        public ushort usUsage;

        public uint dwFlags;

        public IntPtr hwndTarget;
    }



    [StructLayout(LayoutKind.Sequential)]
    struct RAWINPUTHEADER
    {
        public uint dwType;

        public uint dwSize;

        public IntPtr hDevice;

        public IntPtr wParam;
    }



    [StructLayout(LayoutKind.Explicit)]
    struct RAWMOUSE
    {
        [FieldOffset(0)]
        public ushort usFlags;


        // ========================================================
        // Button union
        // ========================================================

        [FieldOffset(4)]
        public uint ulButtons;


        [FieldOffset(4)]
        public ushort usButtonFlags;


        [FieldOffset(6)]
        public ushort usButtonData;


        [FieldOffset(8)]
        public uint ulRawButtons;


        // ========================================================
        // Relative movement
        // ========================================================

        [FieldOffset(12)]
        public int lLastX;


        [FieldOffset(16)]
        public int lLastY;


        [FieldOffset(20)]
        public uint ulExtraInformation;
    }



    // ============================================================
    // Raw mouse button flags
    // ============================================================

    const ushort RI_MOUSE_LEFT_BUTTON_DOWN  = 0x0001;

    const ushort RI_MOUSE_LEFT_BUTTON_UP    = 0x0002;

    const ushort RI_MOUSE_RIGHT_BUTTON_DOWN = 0x0004;

    const ushort RI_MOUSE_RIGHT_BUTTON_UP   = 0x0008;

    const ushort RI_MOUSE_WHEEL             = 0x0400;



    // ============================================================
    // Hidden window
    // ============================================================

    class HiddenWindow : NativeWindow
    {
        public HiddenWindow()
        {
            CreateHandle(
                new CreateParams
                {
                    Caption = "RawMouseBridge"
                }
            );
        }



        protected override void WndProc(
            ref Message m
        )
        {
            if(m.Msg == WM_INPUT)
            {
                try
                {
                    Parse(
                        m.LParam
                    );
                }
                catch(Exception ex)
                {
                    Console.WriteLine(
                        $"[RawMouse ERROR] {ex}"
                    );
                }
            }


            base.WndProc(
                ref m
            );
        }
    }



    // ============================================================
    // Start
    // ============================================================

    public static void Start()
    {
        if(started)
        {
            Console.WriteLine(
                "RawMouse already started"
            );

            return;
        }



        // Keep this object alive.
        window =
            new HiddenWindow();



        bool ok =
            RegisterRawInputDevices(
                [
                    new RAWINPUTDEVICE
                    {
                        // Generic Desktop
                        usUsagePage = 0x01,

                        // Mouse
                        usUsage = 0x02,

                        // Receive even when this window
                        // does not have focus.
                        dwFlags =
                            RIDEV_INPUTSINK,

                        hwndTarget =
                            window.Handle
                    }
                ],
                1,
                (uint)
                Marshal.SizeOf<RAWINPUTDEVICE>()
            );



        Console.WriteLine(
            $"RawMouse={ok}"
        );


        Console.WriteLine(
            $"RawMouse HWND={window.Handle}"
        );



        if(!ok)
        {
            int error =
                Marshal.GetLastWin32Error();


            Console.WriteLine(
                $"[RawMouse ERROR] RegisterRawInputDevices failed: {error}"
            );


            window.DestroyHandle();

            window = null;

            return;
        }



        started = true;
    }



    // ============================================================
    // Parse raw input
    // ============================================================

    static void Parse(
        IntPtr lParam
    )
    {
        uint size = 0;



        // --------------------------------------------------------
        // Query required buffer size
        // --------------------------------------------------------

        uint result =
            GetRawInputData(
                lParam,
                RID_INPUT,
                IntPtr.Zero,
                ref size,
                (uint)
                Marshal.SizeOf<RAWINPUTHEADER>()
            );



        if(size == 0)
        {
            Console.WriteLine(
                "[RawMouse] GetRawInputData returned zero size"
            );

            return;
        }



        IntPtr buffer =
            Marshal.AllocHGlobal(
                checked((int)size)
            );



        try
        {
            // ----------------------------------------------------
            // Read raw input
            // ----------------------------------------------------

            uint read =
                GetRawInputData(
                    lParam,
                    RID_INPUT,
                    buffer,
                    ref size,
                    (uint)
                    Marshal.SizeOf<RAWINPUTHEADER>()
                );



            if(read == 0 ||
               read == uint.MaxValue)
            {
                Console.WriteLine(
                    "[RawMouse] GetRawInputData failed"
                );

                return;
            }



            // ----------------------------------------------------
            // Header
            // ----------------------------------------------------

            RAWINPUTHEADER header =
                Marshal.PtrToStructure<RAWINPUTHEADER>(
                    buffer
                );



            if(header.dwType != RIM_TYPEMOUSE)
                return;



            // ----------------------------------------------------
            // Mouse data
            // ----------------------------------------------------

            int headerSize =
                Marshal.SizeOf<RAWINPUTHEADER>();



            RAWMOUSE mouse =
                Marshal.PtrToStructure<RAWMOUSE>(
                    IntPtr.Add(
                        buffer,
                        headerSize
                    )
                );



            // ====================================================
            // Movement
            // ====================================================

            if(
                mouse.lLastX != 0 ||
                mouse.lLastY != 0
            )
            {
                OnMove?.Invoke(
                    mouse.lLastX,
                    mouse.lLastY
                );
            }



            // ====================================================
            // Left button
            // ====================================================

            if(
                (mouse.usButtonFlags &
                RI_MOUSE_LEFT_BUTTON_DOWN) != 0
            )
            {
                leftDown = true;


                OnButton?.Invoke(
                    1,
                    1
                );
            }



            if(
                (mouse.usButtonFlags &
                RI_MOUSE_LEFT_BUTTON_UP) != 0
            )
            {
                leftDown = false;


                OnButton?.Invoke(
                    1,
                    0
                );
            }



            // ====================================================
            // Right button
            // ====================================================

            if(
                (mouse.usButtonFlags &
                RI_MOUSE_RIGHT_BUTTON_DOWN) != 0
            )
            {
                rightDown = true;


                OnButton?.Invoke(
                    2,
                    1
                );
            }



            if(
                (mouse.usButtonFlags &
                RI_MOUSE_RIGHT_BUTTON_UP) != 0
            )
            {
                rightDown = false;


                OnButton?.Invoke(
                    2,
                    0
                );
            }



            // ====================================================
            // Wheel
            // ====================================================

            if(
                (mouse.usButtonFlags &
                RI_MOUSE_WHEEL) != 0
            )
            {
                short wheelValue =
                    unchecked(
                        (short)
                        mouse.usButtonData
                    );


                int delta =
                    wheelValue / 120;



                if(delta != 0)
                {
                    OnWheel?.Invoke(
                        delta
                    );
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(
                buffer
            );
        }
    }



    // ============================================================
    // Reset mouse state
    //
    // We will use this later when fixing FocusWatcher.
    // ============================================================

    public static void ResetButtons()
    {
        if(leftDown)
        {
            leftDown = false;

            OnButton?.Invoke(
                1,
                0
            );
        }



        if(rightDown)
        {
            rightDown = false;

            OnButton?.Invoke(
                2,
                0
            );
        }
    }



    // ============================================================
    // Win32
    // ============================================================

    [DllImport(
        "user32.dll",
        SetLastError = true
    )]
    static extern bool RegisterRawInputDevices(
        RAWINPUTDEVICE[] devices,
        uint count,
        uint size
    );



    [DllImport(
        "user32.dll",
        SetLastError = true
    )]
    static extern uint GetRawInputData(
        IntPtr hRawInput,
        uint command,
        IntPtr data,
        ref uint size,
        uint headerSize
    );
}
