using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace PipeServer2016
{
    class DataItem
    {
        public int datalength;
        public byte[] byteAllData; //所有数据，算一个完整的数据。
        public byte[] byteDeviceID; //这个是设备的ID的byte数组
        public string deviceMac;
        public int currentsendbulk; //
        public int totalsendbulk; //
        public bool isSendDataToServer; //是否上传AD采集的数据到服务器，默认是false
        public bool isADSwitchOn; //是否AD采集，默认是false
        public bool isSetADTimeOn; //是否AD采集，默认是false
        public bool isReadADTimeOn; //是否AD采集，默认是false

        public int readytosethour; // 准备设定的采样时间的小时
        public int readytosetminute; //准备设定的采样时间的分钟

        public byte[] byteCurrentPackageData; //当今接收包的数据。
        public int currentReceivePackageLength; //接收包的长度，可能不是2002，可能分包了

        public Socket socket;   //Socket of the client

    }
}
