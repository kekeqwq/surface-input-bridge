using System;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;


class Program
{

    static readonly UdpClient udp = new();


    static readonly IPEndPoint linux =
        new(
            IPAddress.Parse("192.168.2.130"),
            5000
        );


    static bool BridgeEnabled = false;



    static void Main()
    {
        Console.WriteLine(
            "Mouse Bridge"
        );



        MouseHook.Start();



        FocusWatcher.OnChanged =
            obs =>
            {
                BridgeEnabled = obs;


                MouseHook.Block = obs;


                Console.WriteLine(
                    obs
                    ? "REMOTE ACTIVE"
                    : "LOCAL ACTIVE"
                );
            };



        FocusWatcher.Start();



        RawMouse.OnMove =
            (dx,dy)=>
            {
                Console.WriteLine(
                    $"MOVE {dx},{dy}"
                );


                if(BridgeEnabled)
                    SendMove(dx,dy);
            };



        RawMouse.OnButton =
            (button,state)=>
            {
                Console.WriteLine(
                    $"BUTTON {button} {state}"
                );


                if(BridgeEnabled)
                    SendButton(
                        button,
                        state
                    );
            };



        RawMouse.OnWheel =
            delta =>
            {
                Console.WriteLine(
                    $"WHEEL {delta}"
                );


                if(BridgeEnabled)
                    SendWheel(delta);
            };



        RawMouse.Start();



        Application.Run();
    }





    static void SendMove(
        int dx,
        int dy
    )
    {
        byte[] packet =
            new byte[9];


        packet[0]=3;


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




    static void SendWheel(
        int delta
    )
    {
        byte[] packet =
            new byte[5];


        packet[0]=5;


        Array.Copy(
            BitConverter.GetBytes(delta),
            0,
            packet,
            1,
            4
        );


        udp.Send(
            packet,
            packet.Length,
            linux
        );
    }
}