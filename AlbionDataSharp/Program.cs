﻿using Albion.Network;
using AlbionDataSharp.Handlers;
using PacketDotNet;
using SharpPcap;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;

namespace AlbionDataSharp
{
    public class Program
    {
        private static IPhotonReceiver receiver;

        private static void Main(string[] args)
        {
            ReceiverBuilder builder = ReceiverBuilder.Create();

            //ADD HANDLERS HERE
            builder.AddResponseHandler(new AuctionGetOffersResponseHandler());
            builder.AddResponseHandler(new AuctionGetRequestsResponseHandler());
            builder.AddResponseHandler(new JoinResponseHandler());

            receiver = builder.Build();

            Console.WriteLine("Start");

            CaptureDeviceList devices = CaptureDeviceList.New();

            foreach (var device in devices)
            {
                new Thread(() =>
                {
                    Console.WriteLine($"Open... {device.Description}");

                    device.OnPacketArrival += new PacketArrivalEventHandler(PacketHandler);
                    device.Open(new DeviceConfiguration 
                    {
                        Mode = DeviceModes.MaxResponsiveness,
                        ReadTimeout = 5000
                    });
                    device.Filter = "(host 5.45.187 or host 5.188.125) and udp port 5056";
                    device.StartCapture();
                })
                .Start();
            }

            Console.Read();
        }

        private static void PacketHandler(object sender, PacketCapture e)
        {
            try
            {
                UdpPacket packet = Packet.ParsePacket(e.GetPacket().LinkLayerType, e.GetPacket().Data).Extract<UdpPacket>();
                if (packet != null)
                {
                    //if (PlayerStatus.Server == Servers.unkown)
                    {
                        var srcIp = (packet.ParentPacket as IPv4Packet)?.SourceAddress?.ToString();
                        if (srcIp == null || string.IsNullOrEmpty(srcIp))
                        {
                            PlayerStatus.Server = Servers.Unknown;
                        }
                        else if (srcIp.Contains("5.188.125."))
                        {
                            PlayerStatus.Server = Servers.West;
                        }
                        else if (srcIp!.Contains("5.45.187."))
                        {
                            PlayerStatus.Server = Servers.East;
                        }
                    }
                    receiver.ReceivePacket(packet.PayloadData);
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);
            }

        }
    }
}

