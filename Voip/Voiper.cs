using g729ASM;
using Montage.Utils;
using Montage.Voip.Rtp;
using PacketDotNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Montage.Voip
{
    public class Voiper
    {
        public Metadata.RtpMetadata Metadata;
        ArrayList packetsList;
        public Voiper()
        {
            packetsList = new ArrayList();
        }
        public Voiper(Metadata.RtpMetadata metadata)
        {
            Metadata = metadata;
            packetsList = new ArrayList();
        }
        public Voiper(DateTime signalCreateTime, double frequency, UdpPacket udpPacket)
        {
            Metadata = new Metadata.RtpMetadata(signalCreateTime, frequency, (IPv4Packet)udpPacket.ParentPacket);
            packetsList = new ArrayList();
            packetsList.Add(new RtpPacket(udpPacket));
        }
        public void Add(Rtp.RtpPacket packet)
        {
            lock (packetsList)
            {
                if (!Contains(packet))
                {
                    packetsList.Add(packet);
                }                
            }    
            //if(packet.Marker)
            //{
            //    int i = 0;
            //    ToPcm();
            //}
        }
        public bool Contains(Rtp.RtpPacket rtpPacket)     //相同序列号的消重
        {
            foreach (Rtp.RtpPacket rtp in packetsList)
            {
                if (rtp.SequenceNumber == rtpPacket.SequenceNumber)
                    return true;
            }
            return false;
        }
        
        public int Count
        {
            get { return packetsList.Count; }
        }
        MemoryStream GetDoubleChannelStream()
        {
            try
            {
                MemoryStream pcmStream = this.ToPcm();
                MemoryStream doubleChannelStream = this.DoubleChannelStream(pcmStream);
                return doubleChannelStream;
            }
            catch (Exception ex)
            {
                LogHelper.Error("译码过程出错.", ex);
                return null;
            }
        }
        MemoryStream ToPcm()
        {
            MemoryStream stream = new MemoryStream();
            foreach (Rtp.RtpPacket rtp in packetsList)
            {
                byte[] buffer = rtp.DataPointer;
                stream.Write(buffer, 0, buffer.Length);
            }
            stream.Seek(0, SeekOrigin.Begin);
            MemoryStream pcmStream = null;
            if (Metadata.PayloadType == 18) pcmStream = G729ToPcm(stream);
            if (Metadata.PayloadType == 0) pcmStream = G711ToPcm(stream);
            stream.Close();
            stream.Dispose();
            pcmStream.Seek(0, SeekOrigin.Begin);
            return pcmStream;
        }
        MemoryStream DoubleChannelStream(MemoryStream pcmStream)
        {
            MemoryStream doubleChannelStream = new MemoryStream();
            while (pcmStream.Position < pcmStream.Length)
            {
                byte[] buffer = new byte[] { 0x00, 0x00 };
                pcmStream.Read(buffer, 0, 2);
                doubleChannelStream.Write(buffer, 0, 2);    //左声道
                doubleChannelStream.Write(buffer, 0, 2);    //右声道                
            }
            pcmStream.Close();
            doubleChannelStream.Seek(0, SeekOrigin.Begin);
            return doubleChannelStream;
        }
        MemoryStream G729ToPcm(MemoryStream ipv4DataStream)
        {
            //int ret = G729.GT_G729_Init(2, ""); //create two channels(channel 0 and 1)
            //try
            //{                
            //    G729.GT_G729_ResetChan(0);          //always reset before using a channel for a call/voice stream
            //}
            //catch (Exception ex)
            //{
            //    LogHelper.Error(string.Format("G729模块初始化时错误."), ex);
            //} 
            MemoryStream pcmStream = new MemoryStream();
            byte[] pG729 = new byte[10];
            int ret = 0;
            while (true)
            {
                byte[] pLinear = new byte[160]; //set your data then
                int readLen = ipv4DataStream.Read(pG729, 0, 10);
                if (readLen < 10)
                {
                    break;
                }
                ret = Coding.G7292.Decode(0, pG729, pLinear); //pass 10 bytes 729 data in, it will output 160 linear(PCM) data
                if (ret <= 0)
                {
                    return null;
                }
                pcmStream.Write(pLinear, 0, 160);
            }
            //G729.GT_G729_Free();
            pcmStream.Seek(0, SeekOrigin.Begin);
            return pcmStream;
        }
        MemoryStream G711ToPcm(MemoryStream ipv4DataStream)
        {
            byte[] pLinear = new byte[40]; //set your data then
            byte[] pG711 = new byte[20];
            MemoryStream pcmStream = new MemoryStream();
            while (true)
            {
                int readLen = ipv4DataStream.Read(pG711, 0, 20);
                if (readLen < 20)
                {
                    break;
                }
                pLinear = Voip.Coding.G7112.ULawDecode(pG711, 0, 20);
                pcmStream.Write(pLinear, 0, 40);
            }
            pcmStream.Seek(0, SeekOrigin.Begin);
            return pcmStream;
        }

        public void Save(string dirName, string fileName)
        {
            string path = Path.Combine(dirName, fileName);
            packetsList.Sort(new RtpComparer());
            if (CopyMemoryToFile(GetDoubleChannelStream(), path + ".wav"))
            {
                LogHelper.Info(string.Format("译码结果已经保存为{0}.wav", fileName));                
            }
        }
        public void SaveMetadata(string dirName, string fileName)
        {
            string path = Path.Combine(dirName, fileName);
            FileStream stream = new FileStream(path + ".xml", FileMode.Create);
            XmlSerializer xmlserilize = new XmlSerializer(typeof(Metadata.RtpMetadata));
            xmlserilize.Serialize(stream, Metadata);
            stream.Close();
            LogHelper.Info(string.Format("元数据已经保存为{0}.xml", fileName));
        }
        bool CopyMemoryToFile(MemoryStream ms, string fileName)
        {
            if (File.Exists(fileName))
            {
                FileStream fs = null;
                try
                {
                    fs = new FileStream(fileName, FileMode.Open);
                }
                catch (Exception ex)
                {
                    LogHelper.Error(string.Format("打开文件{0}时错误.", fileName), ex);
                    return false;
                }
                int fsLength = (int)fs.Length;
                int msLength = (int)ms.Length;
                byte[] buffer = new byte[fsLength];
                fs.Seek(40, SeekOrigin.Begin);
                fs.Read(buffer, 0, buffer.Length);
                fs.Close();


                byte[] header = createWavHeader(fsLength + msLength);
                fs = new FileStream(fileName, FileMode.Create);
                fs.Write(header, 0, header.Length);
                fs.Write(buffer, 0, buffer.Length);

                buffer = new byte[ms.Length];
                ms.Read(buffer, 0, buffer.Length);
                fs.Write(buffer, 0, buffer.Length);

                buffer = null;
                header = null;
                ms.Close();
                fs.Close();
                ms.Dispose();
                fs.Dispose();

            }
            else
            {
                FileStream fs = new FileStream(fileName, FileMode.Create);

                byte[] header = createWavHeader((int)ms.Length);
                fs.Write(header, 0, header.Length);
                byte[] buffer = new byte[ms.Length];
                ms.Read(buffer, 0, buffer.Length);
                fs.Write(buffer, 0, buffer.Length);

                buffer = null;
                header = null;
                ms.Close();
                fs.Close();
                ms.Dispose();
                fs.Dispose();
            }
            return true;
        }
        byte[] createWavHeader(int length)
        {
            byte[] wavHeader ={	                                
                                // RIFF WAVE Chunk
                                0x52, 0x49, 0x46, 0x46,		// "RIFF"
                                0x00, 0x00, 0x00, 0x00,		// 总长度 整个wav文件大小减去ID和Size所占用的字节数（话音长加40）
                                0x57, 0x41, 0x56, 0x45,		// "WAVE"
	
                                // Format Chunk
                                0x66, 0x6D, 0x74, 0x20,		// "fmt "
                                0x10, 0x00, 0x00, 0x00,		// 过渡字节不定
                                0x01, 0x00,			        // 编码方式
                                0x02, 0x00,			        // 声道数目
                                0x40, 0x1F, 0x00, 0x00,		// 采样频率   8000
                                0x00, 0x7D, 0x00, 0x00,		// 每秒所需字节数=采样频率8000*2字节（16bit采样）*2（双声道）=0x7D00
                                0x04, 0x00,			        // ？？？
                                0x10, 0x00,			        // ？？？
	
                                // Data Chunk
                                0x64, 0x61, 0x74, 0x61,		// "data"
                                0x00, 0x00, 0x00, 0x00		//话音长
                             };
            BitConverter.GetBytes(length).CopyTo(wavHeader, 4);
            BitConverter.GetBytes(length + 40).CopyTo(wavHeader, 40);

            return wavHeader;
        }
        
    }
    class RtpComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            short a = (x as RtpPacket).SequenceNumber;
            short b = (y as RtpPacket).SequenceNumber;
            return (int)((ushort)a - (ushort)b);
        }

    }
}
