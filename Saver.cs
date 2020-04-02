using Montage.Utils;
using System;
using System.Collections;
using System.IO;
using Montage.Voip.Rtp;
using System.Xml.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Montage
{
    public class Saver
    {
        static Saver _instance;
        Hashtable historyHashtable;
        Saver()
        {
            historyHashtable = new Hashtable();            
        }
        /// <summary>
        /// 存储类的单件
        /// </summary>
        public static Saver Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Saver();
                return _instance;
            }
        }
        public bool Contains(string key)     //相同序列号的消重
        {
            return historyHashtable.ContainsKey(key);
        }
        public int Count(string key)
        {
            Voip.Voiper voiper = historyHashtable[key] as Voip.Voiper;
            return voiper.Count;
        }
        public void Add(string key, RtpPacket rtpPacket)
        {
            lock (historyHashtable)
            {
                if (historyHashtable.Contains(key))       //历史中有处理过
                {
                    Voip.Voiper voiper = historyHashtable[key] as Voip.Voiper;
                    voiper.Add(rtpPacket);
                }                                     
            }
        }
        public void Create(string key, RtpPacket rtpPackert, DateTime signalCreateTime, double frequency)
        {            
            lock (historyHashtable)
            {
                Voip.Voiper voiper = new Voip.Voiper(signalCreateTime, frequency, rtpPackert.ParentPacket);
                historyHashtable[key] = voiper;                                
            }
        }
        //bool CopyMemoryToFile(MemoryStream ms, string fileName)
        //{
        //    if (File.Exists(fileName))
        //    {
        //        FileStream fs = null;
        //        try
        //        {
        //            fs = new FileStream(fileName, FileMode.Open);
        //        }
        //        catch (Exception ex)
        //        {
        //            LogHelper.Error(string.Format("打开文件{0}时错误.", fileName), ex);
        //            return false;
        //        }
        //        int fsLength = (int)fs.Length;
        //        int msLength = (int)ms.Length;
        //        byte[] buffer = new byte[fsLength];
        //        fs.Seek(40, SeekOrigin.Begin);
        //        fs.Read(buffer, 0, buffer.Length);
        //        fs.Close();


        //        byte[] header = createWavHeader(fsLength + msLength);
        //        fs = new FileStream(fileName, FileMode.Create);
        //        fs.Write(header, 0, header.Length);
        //        fs.Write(buffer, 0, buffer.Length);

        //        buffer = new byte[ms.Length];
        //        ms.Read(buffer, 0, buffer.Length);
        //        fs.Write(buffer, 0, buffer.Length);

        //        buffer = null;
        //        header = null;
        //        ms.Close();
        //        fs.Close();
        //        ms.Dispose();
        //        fs.Dispose();

        //    }
        //    else
        //    {
        //        FileStream fs = new FileStream(fileName, FileMode.Create);

        //        byte[] header = createWavHeader((int)ms.Length);
        //        fs.Write(header, 0, header.Length);
        //        byte[] buffer = new byte[ms.Length];
        //        ms.Read(buffer, 0, buffer.Length);
        //        fs.Write(buffer, 0, buffer.Length);

        //        buffer = null;
        //        header = null;
        //        ms.Close();
        //        fs.Close();
        //        ms.Dispose();
        //        fs.Dispose();
        //    }
        //    return true;
        //}
        /// <summary>
        /// 获取双声道
        /// </summary>
        /// <returns></returns>
        //public MemoryStream GetDoubleChannelStream(IList list)
        //{
        //    try
        //    {
        //        MemoryStream pcmStream = this.ToPcm(list);
        //        MemoryStream doubleChannelStream = this.DoubleChannelStream(pcmStream);
        //        return doubleChannelStream;
        //    }
        //    catch (Exception ex)
        //    {
        //        LogHelper.Error("译码过程出错.", ex);
        //        return null;
        //    }
        //}

        //MemoryStream ToPcm(IList list)
        //{
        //    MemoryStream stream = new MemoryStream();
        //    Voip.Rtp.RtpPacket rtp2 = (Voip.Rtp.RtpPacket)list[0];
        //    foreach (Voip.Rtp.RtpPacket rtp in list)
        //    {
        //        byte[] buffer = rtp.DataPointer;
        //        stream.Write(buffer, 0, buffer.Length);
        //    }
        //    stream.Seek(0, SeekOrigin.Begin);
        //    MemoryStream pcmStream = null;
        //    if (rtp2.PayloadType == 18) pcmStream = G729ToPcm(stream);
        //    if (rtp2.PayloadType == 0) pcmStream = G711ToPcm(stream);
        //    stream.Close();
        //    stream.Dispose();
        //    pcmStream.Seek(0, SeekOrigin.Begin);
        //    return pcmStream;
        //}

        //MemoryStream DoubleChannelStream(MemoryStream pcmStream)
        //{
        //    MemoryStream doubleChannelStream = new MemoryStream();
        //    while (pcmStream.Position < pcmStream.Length)
        //    {
        //        byte[] buffer = new byte[] { 0x00, 0x00 };
        //        pcmStream.Read(buffer, 0, 2);
        //        doubleChannelStream.Write(buffer, 0, 2);    //左声道
        //        doubleChannelStream.Write(buffer, 0, 2);    //右声道                
        //    }
        //    pcmStream.Close();
        //    doubleChannelStream.Seek(0, SeekOrigin.Begin);
        //    return doubleChannelStream;
        //}

        //MemoryStream G729ToPcm(MemoryStream ipv4DataStream)
        //{
        //    int ret = Voip.Coding.G729.Init(2, ""); //create two channels(channel 0 and 1)
        //    Voip.Coding.G729.ResetChannel(0);          //always reset before using a channel for a call/voice stream

        //    MemoryStream pcmStream = new MemoryStream();
        //    byte[] pG729 = new byte[10];
        //    while (true)
        //    {
        //        byte[] pLinear = new byte[160]; //set your data then
        //        int readLen = ipv4DataStream.Read(pG729, 0, 10);
        //        if (readLen < 10)
        //        {
        //            break;
        //        } 
        //        ret = Voip.Coding.G729.Decode(0, pG729, pLinear); //pass 10 bytes 729 data in, it will output 160 linear(PCM) data
        //        if (ret <= 0)
        //        {
        //            return null;
        //        }
        //        pcmStream.Write(pLinear, 0, 160);
        //    }
        //    Voip.Coding.G729.Free();
        //    pcmStream.Seek(0, SeekOrigin.Begin);
        //    return pcmStream;
        //}

        //MemoryStream G711ToPcm(MemoryStream ipv4DataStream)
        //{
        //    byte[] pLinear = new byte[40]; //set your data then
        //    byte[] pG711 = new byte[20];
        //    MemoryStream pcmStream = new MemoryStream();
        //    while (true)
        //    {
        //        int readLen = ipv4DataStream.Read(pG711, 0, 20);
        //        if (readLen < 20)
        //        {
        //            break;
        //        }

        //        pLinear = Voip.Coding.G711.ULawDecode(pG711, 0, 20);

        //        pcmStream.Write(pLinear, 0, 40);
        //    }
        //    pcmStream.Seek(0, SeekOrigin.Begin);
        //    return pcmStream;
        //}
        /// <summary>
        /// 生成WAV文件头
        /// </summary>
        /// <param name="length">文件长度</param>
        /// <returns>文件头</returns>
        //byte[] createWavHeader(int length)
        //{
        //    byte[] wavHeader ={	                                
        //                        // RIFF WAVE Chunk
        //                        0x52, 0x49, 0x46, 0x46,		// "RIFF"
        //                        0x00, 0x00, 0x00, 0x00,		// 总长度 整个wav文件大小减去ID和Size所占用的字节数（话音长加40）
        //                        0x57, 0x41, 0x56, 0x45,		// "WAVE"
	
        //                        // Format Chunk
        //                        0x66, 0x6D, 0x74, 0x20,		// "fmt "
        //                        0x10, 0x00, 0x00, 0x00,		// 过渡字节不定
        //                        0x01, 0x00,			        // 编码方式
        //                        0x02, 0x00,			        // 声道数目
        //                        0x40, 0x1F, 0x00, 0x00,		// 采样频率   8000
        //                        0x00, 0x7D, 0x00, 0x00,		// 每秒所需字节数=采样频率8000*2字节（16bit采样）*2（双声道）=0x7D00
        //                        0x04, 0x00,			        // ？？？
        //                        0x10, 0x00,			        // ？？？
	
        //                        // Data Chunk
        //                        0x64, 0x61, 0x74, 0x61,		// "data"
        //                        0x00, 0x00, 0x00, 0x00		//话音长
        //                     };
        //    BitConverter.GetBytes(length).CopyTo(wavHeader, 4);
        //    BitConverter.GetBytes(length + 40).CopyTo(wavHeader, 40);

        //    return wavHeader;
        //}
        public void Save(string folder, int minLength)
        {
            //List<Task> taskList = new List<Task>();
            foreach (DictionaryEntry entry in historyHashtable)
            {
                string fileName = entry.Key.ToString();
                Voip.Voiper voiper = entry.Value as Voip.Voiper;
                if (voiper.Count > minLength)
                {
                    //Task task = new Task(() => {
                    try
                    {
                        voiper.Save(folder, fileName);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error(string.Format("输出语音文件{0}.wav错误.", fileName), ex);
                    }
                    try
                    {
                        voiper.SaveMetadata(folder, fileName);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error(string.Format("输出元数据文件{0}.xml错误.", fileName), ex);
                    }
                    //});
                    //task.Start();
                    //taskList.Add(task);

                }                      
            }
            //Task.WaitAll(taskList.ToArray());
            //taskList.Clear();                 
            historyHashtable.Clear();            
        }
    }
    
}
