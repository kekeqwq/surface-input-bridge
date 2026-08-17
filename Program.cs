using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Windows.Forms;


class Program
{
    const int WM_INPUT = 0x00FF;

    const uint RID_INPUT = 0x10000003;

    const uint RIDEV_INPUTSINK = 0x00000100;



    static readonly UdpClient udp = new();


    static readonly IPEndPoint linux =
        new(
            IPAddress.Parse("192.168.2.130"),
            5000
        );



    static bool BridgeEnabled = false;



    static int lastX = -1;
    static int lastY = -1;


    static bool lastLeft = false;




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



    [StructLayout(LayoutKind.Sequential)]
    struct RAWHID
    {
        public uint dwSizeHid;
        public uint dwCount;
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





    class HiddenWindow : NativeWindow
    {
        public HiddenWindow()
        {
            CreateHandle(
                new CreateParams()
                {
                    Caption = "TouchpadBridge"
                }
            );
        }



        protected override void WndProc(
            ref Message m
        )
        {
            if(m.Msg == WM_INPUT)
            {
                ParseRaw(
                    m.LParam
                );
            }


            base.WndProc(
                ref m
            );
        }
    }





    static void ParseRaw(
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



            if(header.dwType != 2)
                return;



            int offset =
                Marshal.SizeOf<RAWINPUTHEADER>();


            RAWHID hid =
                Marshal.PtrToStructure<RAWHID>(
                    IntPtr.Add(
                        buffer,
                        offset
                    )
                );



            byte[] data =
                new byte[hid.dwSizeHid];



            Marshal.Copy(
                IntPtr.Add(
                    buffer,
                    offset +
                    Marshal.SizeOf<RAWHID>()
                ),
                data,
                0,
                data.Length
            );



            if(data.Length < 6)
                return;




            bool left =
                data[^1] == 1;



            if(left != lastLeft)
            {
                lastLeft = left;


                Console.WriteLine(
                    left
                    ? "LEFT DOWN"
                    : "LEFT UP"
                );


                if(BridgeEnabled)
                {
                    SendButton(
                        1,
                        left
                        ? (byte)1
                        : (byte)0
                    );
                }
            }




            if(data[1] != 3)
                return;



            int x =
                data[2] |
                (data[3] << 8);


            int y =
                data[4] |
                (data[5] << 8);



            if(lastX < 0)
            {
                lastX = x;
                lastY = y;
                return;
            }



            int dx =
                x - lastX;


            int dy =
                y - lastY;



            lastX = x;
            lastY = y;



            if(dx == 0 && dy == 0)
                return;



            Console.WriteLine(
                $"MOVE {dx},{dy}"
            );



            if(BridgeEnabled)
            {
                SendMove(
                    dx,
                    dy
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





    static void SendMove(
        int dx,
        int dy
    )
    {
        byte[] packet =
            new byte[9];


        packet[0] = 3;


        Array.Copy(
            BitConverter.GetBytes(dx),
            0,
            packet,
            1,
            4
        );


        Array.Copy(
            BitConverter.GetBytes(dy),
            0,
            packet,
            5,
            4
        );



        udp.Send(
            packet,
            packet.Length,
            linux
        );
    }





    static void SendButton(
        byte button,
        byte state
    )
    {
        byte[] packet =
        [
            4,
            button,
            state
        ];



        udp.Send(
            packet,
            packet.Length,
            linux
        );
    }





    static void Main()
    {
        Console.WriteLine(
            "Surface Touchpad Bridge"
        );



        MouseHook.Start();



        FocusWatcher.OnChanged =
            obs =>
            {
                BridgeEnabled = obs;


                MouseHook.Block = obs;


                Console.WriteLine(
                    obs
                    ? "OBS INPUT ACTIVE"
                    : "OBS INPUT INACTIVE"
                );
            };



        FocusWatcher.Start();




        HiddenWindow window =
            new();



        bool ok =
            RegisterRawInputDevices(
                new[]
                {
                    new RAWINPUTDEVICE
                    {
                        usUsagePage = 0x0D,
                        usUsage = 0x05,
                        dwFlags = RIDEV_INPUTSINK,
                        hwndTarget = window.Handle
                    }
                },
                1,
                (uint)
                Marshal.SizeOf<RAWINPUTDEVICE>()
            );



        Console.WriteLine(
            $"RawInput={ok}"
        );



        Application.Run();
    }
}