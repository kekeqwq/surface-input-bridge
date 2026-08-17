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


        // button union
        [FieldOffset(4)]
        public uint ulButtons;


        [FieldOffset(4)]
        public ushort usButtonFlags;


        [FieldOffset(6)]
        public ushort usButtonData;


        [FieldOffset(8)]
        public uint ulRawButtons;


        [FieldOffset(12)]
        public int lLastX;


        [FieldOffset(16)]
        public int lLastY;


        [FieldOffset(20)]
        public uint ulExtraInformation;
    }



    const ushort RI_MOUSE_LEFT_BUTTON_DOWN  = 0x0001;
    const ushort RI_MOUSE_LEFT_BUTTON_UP    = 0x0002;

    const ushort RI_MOUSE_RIGHT_BUTTON_DOWN = 0x0004;
    const ushort RI_MOUSE_RIGHT_BUTTON_UP   = 0x0008;

    const ushort RI_MOUSE_WHEEL             = 0x0400;



    class HiddenWindow : NativeWindow
    {
        public HiddenWindow()
        {
            CreateHandle(
                new CreateParams()
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
                Parse(
                    m.LParam
                );
            }


            base.WndProc(
                ref m
            );
        }
    }



    public static void Start()
    {
        var window = new HiddenWindow();


        bool ok =
            RegisterRawInputDevices(
                [
                    new RAWINPUTDEVICE
                    {
                        usUsagePage = 0x01,
                        usUsage = 0x02,

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
    }




    static void Parse(
        IntPtr lParam
    )
    {
        uint size = 0;


        GetRawInputData(
            lParam,
            RID_INPUT,
            IntPtr.Zero,
            ref size,
            (uint)
            Marshal.SizeOf<RAWINPUTHEADER>()
        );


        IntPtr buffer =
            Marshal.AllocHGlobal(
                (int)size
            );


        try
        {
            GetRawInputData(
                lParam,
                RID_INPUT,
                buffer,
                ref size,
                (uint)
                Marshal.SizeOf<RAWINPUTHEADER>()
            );


            RAWINPUTHEADER header =
                Marshal.PtrToStructure<RAWINPUTHEADER>(
                    buffer
                );


            if(header.dwType != RIM_TYPEMOUSE)
                return;



            RAWMOUSE mouse =
                Marshal.PtrToStructure<RAWMOUSE>(
                    IntPtr.Add(
                        buffer,
                        Marshal.SizeOf<RAWINPUTHEADER>()
                    )
                );



            // move

            if(
                mouse.lLastX != 0 ||
                mouse.lLastY != 0
            )
            {
                Console.WriteLine(
                    $"MOVE {mouse.lLastX},{mouse.lLastY}"
                );


                OnMove?.Invoke(
                    mouse.lLastX,
                    mouse.lLastY
                );
            }



            // left button

            if(
                (mouse.usButtonFlags &
                RI_MOUSE_LEFT_BUTTON_DOWN) != 0
            )
            {
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
                OnButton?.Invoke(
                    1,
                    0
                );
            }



            // right button

            if(
                (mouse.usButtonFlags &
                RI_MOUSE_RIGHT_BUTTON_DOWN) != 0
            )
            {
                Console.WriteLine(
                    "RIGHT DOWN"
                );


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
                Console.WriteLine(
                    "RIGHT UP"
                );


                OnButton?.Invoke(
                    2,
                    0
                );
            }



            // wheel

            if(
                (mouse.usButtonFlags &
                RI_MOUSE_WHEEL) != 0
            )
            {
                short value =
                    (short)
                    mouse.usButtonData;


                int delta =
                    value / 120;


                Console.WriteLine(
                    $"WHEEL {delta}"
                );


                OnWheel?.Invoke(
                    delta
                );
            }

        }
        finally
        {
            Marshal.FreeHGlobal(
                buffer
            );
        }
    }





    [DllImport("user32.dll")]
    static extern bool RegisterRawInputDevices(
        RAWINPUTDEVICE[] devices,
        uint count,
        uint size
    );



    [DllImport("user32.dll")]
    static extern uint GetRawInputData(
        IntPtr hRawInput,
        uint command,
        IntPtr data,
        ref uint size,
        uint headerSize
    );
}