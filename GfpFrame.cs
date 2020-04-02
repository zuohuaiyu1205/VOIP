using Montage.Utils;
using System.Linq;

namespace Montage
{
    /// <summary>
    /// Gfp数据帧类
    /// </summary>
    public class GfpFrame
    {
        byte[] buffer;
        public GfpFrame(byte[] gfpFrameBuffer)
        {
            buffer = gfpFrameBuffer;
        }
        /// <summary>
        /// GFP的源MAC地址
        /// </summary>
        public byte[] SourceMac
        {
            get
            {
                return buffer.Skip(7).Take(6).ToArray();
            }
        }
        /// <summary>
        /// GFP目的MAC地址
        /// </summary>
        public byte[] TargetMac
        {
            get
            {
                return buffer.Skip(13).Take(6).ToArray();
            }
        }
        /// <summary>
        /// 业务代码
        /// </summary>
        public string BusinessCode
        {
            get
            {
                return Helper.ByteToHexStr(buffer.Skip(19).Take(2).ToArray());
            }
        }
        /// <summary>
        /// 是否IP业务类型
        /// </summary>
        public bool IsIpService
        {
            get
            {
                if (BusinessCode == "0800")
                    return true;
                return false;
            }
        }
        /// <summary>
        /// GFP的CRC校验码
        /// </summary>
        public string CrcCode
        {
            get
            {
                return Helper.ByteToHexStr(buffer.Skip(buffer.Length - 4).Take(4).ToArray());
            }
        }
        /// <summary>
        /// 获取IP数据
        /// </summary>
        public byte[] IpPackets
        {
            get
            {
                if (IsIpService)
                    return buffer.Skip(21).Take(buffer.Length - 21 - 4).ToArray();      //跳过起始21个字节的帧头和结尾4个字节的循环校验代码
                return null;
            }
        }
        /// <summary>
        /// 字节集
        /// </summary>
        public byte[] Bytes 
        {
            get { return buffer; }
        }
        /// <summary>
        /// 转化为十六进制字符串
        /// </summary>
        /// <returns></returns>
        public string ToHex()
        {
            return Helper.ByteToHexStr(this.buffer);
        }
    }
}
