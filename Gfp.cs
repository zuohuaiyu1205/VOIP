using Montage.Utils;
using PacketDotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Montage
{
    public delegate void ProtocolAgent(IPv4Packet packet, Metadata.BaseMetadata metadata);
    /// <summary>
    /// GFP译码类
    /// </summary>
    public class Gfp
    {
        Stream gfpStream;         
        double frequency;
        DateTime signalTime;
        string taskName;        
        /// <summary>
        /// GFP构造函数
        /// </summary>
        /// <param name="Stream">译码流</param>
        /// <param name="frequency">信号频点</param>
        /// <param name="captureTime">信号捕获时间</param>
        /// <param name="taskId">任务ID</param>        
        public Gfp(Stream stream, double frequency, DateTime captureTime, string taskId = "")
        {            
            gfpStream = stream;
            gfpStream.Seek(0, SeekOrigin.Begin);
            this.frequency = frequency;
            this.taskName = taskId;
            this.signalTime = captureTime;
            this.Agent = this.AgentBase;
        }
        
        public void Process(int threadCount=1)
        {
            if (threadCount > 1)
            {
                int length = (int)(gfpStream.Length / threadCount);
                int position = 0;
                List<Task> list = new List<Task>();
                while (position < gfpStream.Length)
                {
                    int length2 = (int)(gfpStream.Length - position);
                    if (length2 < 2 * length)
                        length = length2;
                    byte[] buffer = new byte[length];
                    gfpStream.Read(buffer, 0, length);
                    MemoryStream stream = new MemoryStream();
                    stream.Write(buffer, 0, buffer.Length);
                    buffer = null;
                    stream.Seek(0, SeekOrigin.Begin);
                    position += length;
                    Task task = new Task(() =>
                    {
                        Processing(stream);
                    });
                    list.Add(task);
                }
                gfpStream.Close();
                gfpStream.Dispose();
                foreach (Task task in list)
                    task.Start();
                Task.WaitAll(list.ToArray());
            }
            else
                Processing(gfpStream);                     
        }
        private void Processing(Stream stream)
        {
            while (stream.CanRead && stream.Position < stream.Length)
            {
                GfpFrame frame = null;
                try
                {
                    frame = getNextFrame(stream);
                }
                catch (Exception ex)
                {
                    LogHelper.Error("读取下一帧出现错误", ex);
                    continue;
                }
                if (frame != null && frame.IpPackets != null)
                {
                    Metadata.BaseMetadata metadata = new Metadata.BaseMetadata(signalTime, frequency, frame);
                    IPv4Packet ipv4Packet = new IPv4Packet(new PacketDotNet.Utils.ByteArraySegment(frame.IpPackets));
                    if (ipv4Packet == null)
                        break;                    
                    if (ipv4Packet.Protocol == ProtocolType.Udp)
                    {
                        if (ipv4Packet.PayloadPacket == null)
                            continue;
                        UdpPacket udpPacket = ipv4Packet.PayloadPacket as UdpPacket;
                        if (udpPacket.Bytes[8] == 0x80)
                        {
                            Voip.Rtp.RtpPacket rtpPacket = new Voip.Rtp.RtpPacket(udpPacket);
                            string key = AsKey(ipv4Packet, udpPacket, rtpPacket);
                            switch (rtpPacket.PayloadType)
                            {
                                case 0:     //PCMU/G711                                   
                                case 4:     //G723                                    
                                case 8:     //PCMA                                   
                                case 9:     //G722                                    
                                case 18:    //G729
                                    if (Saver.Instance.Contains(key))
                                    {
                                        Saver.Instance.Add(key, rtpPacket);
                                        if (Saver.Instance.Count(key) == Config.Instance.IsolatedCount)
                                        {
                                            LogHelper.Info(string.Format("发现新用户数据，五元组为{0}", key));
                                        }
                                    }
                                    else
                                    {
                                        Saver.Instance.Create(key, rtpPacket, signalTime, frequency);                                        
                                    }
                                    break;
                                default:
                                    continue;
                            }
                        }
                        else
                        {
                            this.Agent?.Invoke(ipv4Packet, metadata);                            
                        }
                    }
                    else if (ipv4Packet.Protocol == ProtocolType.Tcp)
                    {
                        this.Agent?.Invoke(ipv4Packet, metadata);
                    }
                    else
                    {
                        this.Agent?.Invoke(ipv4Packet, metadata);
                    }
                }
            }
            stream.Close();
            stream.Dispose();
            GC.GetTotalMemory(true);
        }        

        /// <summary>
        /// 获取下一个GFP数据帧，不包括47字节的帧头
        /// </summary>
        /// <returns></returns>
        GfpFrame getNextFrame(Stream stream)
        {
            //*********************************************************************************
            //| 47个字节的帧头 = 0x99AB30B2A01707 + E0B6AB31(10个)
            //|长度|长度校验值|目的MAC|源MAC|业务类型|IP_DATA|校验值|
            //|  2 |    5     |   6   |  6  |    2   | 不定长|   4  |
            //|    业务类型：IP=0x0800/CDP=0x01E7/短帧=0x0027
            //*********************************************************************************
            byte buffer = getByte(stream, 0, false);               //先读取1个字节 
            while (stream.Position < stream.Length)
            {
                if (buffer == 0x45 && stream.Position > 21)     //疑是IP报头
                {
                    byte[] serviceType = getBytes(stream, -3, 2, true);       //业务类型
                    if (serviceType[0] == 0x08 && serviceType[1] == 0x00)      //IP业务
                    {
                        byte[] frameLengthBytes = getBytes(stream, -22, 2, true);                                         //读取gfp帧长，并计算
                        int tempLength = ((frameLengthBytes[0] << 8) + frameLengthBytes[1]) ^ 0xb6ab;             //和B6AB异或形成KKSS
                        int frameLength = (tempLength << 24 >> 24 << 8) + (tempLength >> 8);                     //帧长=SS*256+KK
                        byte[] IpLengthBytes = getBytes(stream, 1, 2, true);
                        int IpLength = (IpLengthBytes[0] << 8) + IpLengthBytes[1];
                        if (IpLength > 1500)                        //IP长度一般小于1500
                        {
                            buffer = getByte(stream, 0, false);
                            continue;
                        }                            
                        else if (IpLength != frameLength - 21 - 4)  //IP长度和GFP帧头长度不一致，忽略不处理
                        {
                            buffer = getByte(stream, 0, false);
                            continue;
                        }
                        byte[] frameBuffer = getBytes(stream, -22, IpLength + 21 + 4, false); //读取全部的gfp数据帧 
                        return new GfpFrame(frameBuffer);
                    }
                }
                buffer = getByte(stream, 0, false);                
            }
            return null;
        }
        /// <summary>
        /// 读取给定相对位置和大小的字节数组
        /// </summary>
        /// <param name="position">相对于当前位置的位置</param>
        /// <param name="count">读取字节的个数</param>
        /// <param name="backToOrigin">读取完后是否返回到原来位置，默认返回原来位置</param>
        /// <returns>字节数组</returns>
        byte[] getBytes(Stream stream, int position, int count,  bool backToOrigin=true)
        {
            byte[] buffer = new byte[count];
            long currentPosition = stream.Position;

            if (currentPosition + position + count <= stream.Length)
            {
                stream.Seek(position, SeekOrigin.Current);
                stream.Read(buffer, 0, count);
            }
            if (backToOrigin || position < 0)
                stream.Seek(currentPosition, SeekOrigin.Begin);
            return buffer;
        }
        /// <summary>
        /// 读取给定相对位置的字节
        /// </summary>
        /// <param name="position">相对于当前位置的位置</param>  
        /// <param name="backToOrigin">读取完后是否返回到原来位置，默认返回原来位置</param>
        /// <returns>字节</returns>
        byte getByte(Stream stream, int position = 0, bool backToOrigin = true)
        {
            byte[] buffer = getBytes(stream, position, 1, backToOrigin);
            return buffer[0];
        }
        byte getByte(Stream stream)
        {
            return getByte(stream, 0, false);
        }
        
        public ProtocolAgent Agent; 
        void AgentBase(IPv4Packet packet, Metadata.BaseMetadata metadata)
        {
            metadata.SourceAddress = packet.SourceAddress.ToString();
            metadata.TargetAddress = packet.DestinationAddress.ToString();            
            if (packet.Protocol == ProtocolType.Udp)
            {
                if (packet.PayloadPacket != null)
                {
                    UdpPacket udpPacket = packet.PayloadPacket as UdpPacket;
                    metadata.SourcePort = udpPacket.SourcePort;
                    metadata.TargetPort = udpPacket.DestinationPort;
                }
                metadata.ProtocolName = "UDP";
            }
            else if (packet.Protocol == ProtocolType.Tcp)
            {
                if (packet.PayloadPacket != null)
                {
                    TcpPacket tcpPacket = packet.PayloadPacket as TcpPacket;
                    metadata.SourcePort = tcpPacket.SourcePort;
                    metadata.TargetPort = tcpPacket.DestinationPort;
                }
                metadata.ProtocolName = "TCP";
            }
            else
            {
                metadata.ProtocolName = packet.Protocol.ToString();
            }
            Metadata.MetadataSaver.Instance.Add(metadata);
            //LogHelper.Debug(string.Format("发现{0}协议数据{1}-{2}", packet.Protocol.ToString(), packet.SourceAddress, packet.DestinationAddress));
        }  
         
        public string AsKey(IPv4Packet ipv4, UdpPacket udp, Voip.Rtp.RtpPacket rtp)
        {            
            return string.Format("{0}_{1}_{2}_{3}_{4}", ipv4.SourceAddress, ipv4.DestinationAddress, udp.SourcePort, udp.DestinationPort, rtp.SSRC);
        }    
    }
}
