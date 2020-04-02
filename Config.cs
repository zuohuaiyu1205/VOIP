using Montage.Utils;
using System.IO;
using System.Xml.Serialization;

namespace Montage
{
    public class Config
    {
        static Config _instance;
        
        int bufferCount;
        int bufferSize;
        
        string waveFolder;
        
        //public string WorkFolder = @"C:\Users\weilaoshi\works\ebem-src\data\temp";
        //public string HistoryFolder = @"C:\Users\weilaoshi\works\ebem-src\data\temp\0000";
        //public string WaveFolder = @"C:\Users\weilaoshi\works\ebem-src\data\temp\0000";
        /// <summary>
        /// 小于此长度的IP报文将被忽略
        /// </summary>
        public int MinIpLength;
        /// <summary>
        /// 孤立点判断
        /// </summary>
        public int IsolatedCount;
        /// <summary>
        /// 孤立持续时间阈值
        /// </summary>
        public int IsolatedThreshold;
        /// <summary>
        /// 开启调试模式
        /// </summary>
        public bool IsDebug;
        /// <summary>
        /// 文件名正则表达式
        /// </summary>
        public string SignalFileRegex;
        
        string historyFolder;
        
        string workFolder;
        /// <summary>
        /// 工作目录
        /// </summary>
        public string WorkFolder
        {
            get
            {                
                return workFolder;
            }
            set { workFolder = value; }
        }
        /// <summary>
        /// 历史目录
        /// </summary>
        public string HistoryFolder
        {
            get
            {
                if (!Path.IsPathRooted(historyFolder))
                    historyFolder = Path.Combine(workFolder, historyFolder);
                return historyFolder;
            }
            set { historyFolder = value; }
        }
        /// <summary>
        /// 译码结果文件夹
        /// </summary>
        public string WaveFolder
        {
            get
            {
                if (!Path.IsPathRooted(waveFolder))
                    waveFolder = Path.Combine(workFolder, waveFolder);
                return waveFolder; 
            }
            set { waveFolder = value; }
        }
        static string filePath = "runtime.xml";
        Config()
        {            
            bufferCount = 4;
            bufferSize = 8;
            waveFolder = "Wave";
            MinIpLength = 60;
            IsolatedCount = 2;
            IsolatedThreshold = 102400;
            IsDebug = true;
            SignalFileRegex = @"\d{17}_\d+\.*\d*\.(bin)";
            historyFolder = "History";
            workFolder = @"C:\temp";            
        }
        public bool SaveToXml()
        {
            try
            {
                FileStream stream = new FileStream(filePath, FileMode.Create);
                XmlSerializer xmlserilize = new XmlSerializer(typeof(Config));
                xmlserilize.Serialize(stream, _instance);
                stream.Close();
                return true;
            }
            catch (System.Exception ex)
            {
                LogHelper.Error("保存系统运行时配置参数错误.", ex);
            }
            return false;
        }
        static Config LoadFromXml()
        {
            string fullPath = Path.Combine(System.Environment.CurrentDirectory, filePath);
            if (File.Exists(fullPath))
            {
                try
                {
                    FileStream stream = new FileStream(fullPath, FileMode.Open);
                    XmlSerializer xmlserilize = new XmlSerializer(typeof(Config));
                    Config config = xmlserilize.Deserialize(stream) as Config;
                    stream.Close();
                    return config;
                }
                catch (System.Exception ex)
                {
                    LogHelper.Error("读取系统运行时配置参数错误.", ex);
                }                
            }
            return null;       
        }
        /// <summary>
        /// 存储类的单件
        /// </summary>
        public static Config Instance
        {
            get
            {
                if (File.Exists(filePath) && _instance == null)
                    _instance = LoadFromXml();
                if (_instance == null)
                    _instance = new Config();
                return _instance;
            }
        }
        /// <summary>
        /// 缓存个数，至少为2
        /// </summary>
        public int BufferCount
        {
            get
            {
                return bufferCount;
            }

            set
            {
                bufferCount = value;
            }
        }
        /// <summary>
        /// 缓存大小，单位MB
        /// </summary>
        public int BufferSize
        {
            get
            {
                return bufferSize;
            }

            set
            {
                bufferSize = value;
            }
        }
    }
}
