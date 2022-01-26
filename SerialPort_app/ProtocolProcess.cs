using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SerialPort_app
{
    class ProtocolProcess
    {
        Thread receivePacketProcess;
        SerialProcess serialRawPck;
        //CRC_16 CRC16_Calculator = new CRC_16();

        //public bool dataRecvFlag = false;
        
        //byte[] RecvBuffer;

        public byte[] TransmitBuffer;

        public string TransmitBufferInHexString = "";
//        public string ReceiveBufferInHexString = "";

        

        public struct recvBufferField
        {
            public bool recvFlag;
            public bool crcFlag;
            public bool chsumFlag;
            public string pckInHexString;

            public byte ADR1;
            public byte ADR2;
            public byte Cmd;
            public ushort Len;
            public byte[] Data;

            public byte CRC1;
            public byte CRC2;

            public byte chsum;
            //public recvBufferField()
            //{
            //    recvFlag = false;
            //}
        }

        public recvBufferField RecvDataField; //= new recvBufferField();           //çoklu tanımlama yap
          
        public ProtocolProcess(SerialProcess x)
        {
            serialRawPck = x;// new SerialProcess(x);

            receivePacketProcess = new Thread(RecvPckThread);
            receivePacketProcess.Start();

            //RecvDataField = new recvBufferField();

            RecvDataField.recvFlag = false;
        }
        


        //Header	ADR1	ADR2	Command	   Len	 CRC1	 CRC2	End Of Pck
        //FD                                                            FE

        //Header0    Header1     Len0  Len1     data8*len     XorChsum     EndOfPck
        //  55         AA         x      x         x              x           FE               
        public void RecvPckThread()
        {
           ushort dataL;

            while (true)
            {
                if (serialRawPck.IsOpen)
                {
                    while (serialRawPck.bufferBytesToRead > 0)
                    {
                        if ((serialRawPck.myQ.Peek() == 0x55) && (serialRawPck.myQ.ElementAt(1) == 0xAA))
                        {
                            if (serialRawPck.bufferBytesToRead > 5)
                            {
                                dataL = (ushort)(serialRawPck.myQ.ElementAt(2) | ((ushort)serialRawPck.myQ.ElementAt(3) << 8));

                                if (serialRawPck.bufferBytesToRead > (5 + dataL))
                                {
                                    if (serialRawPck.myQ.ElementAt(5 + dataL) == 0xFE)
                                    {
                                        serialRawPck.myQ.Dequeue();     //header0
                                        serialRawPck.myQ.Dequeue();     //header1    
                                        serialRawPck.myQ.Dequeue();     //Len0
                                        serialRawPck.myQ.Dequeue();     //Len11  

                                        //RecvDataField.ADR1 = serialRawPck.myQ.Dequeue();
                                        //RecvDataField.ADR2 = serialRawPck.myQ.Dequeue();
                                        //RecvDataField.Cmd = serialRawPck.myQ.Dequeue();
                                        RecvDataField.Len = dataL;

                                        RecvDataField.Data = new byte[dataL];

                                        for (int i = 0; i < RecvDataField.Len; i++)
                                            RecvDataField.Data[i] = serialRawPck.myQ.Dequeue();

                                        //RecvDataField.CRC1 = serialRawPck.myQ.Dequeue();
                                        //RecvDataField.CRC2 = serialRawPck.myQ.Dequeue();

                                        RecvDataField.chsum = serialRawPck.myQ.Dequeue();

                                        RecvDataField.recvFlag = true;

                                        if (Chsum_Calculator(RecvDataField.Len, RecvDataField.Data) == RecvDataField.chsum)
                                            RecvDataField.chsumFlag = true;
                                        else
                                            RecvDataField.chsumFlag = false;

                                        //RecvDataField.crcFlag = CRC16_Calculator.ComputeAndCompare_CRC16(RecvDataField.ADR1, RecvDataField.ADR2, RecvDataField.Cmd, RecvDataField.Len, RecvDataField.Data, RecvDataField.CRC1, RecvDataField.CRC2);

                                        writeRecvPckInString();

                                        break;
                                    }
                                    else
                                        serialRawPck.myQ.Dequeue();
                                }
                                else
                                    break;
                            }
                            else
                                break;
                        }
                        else
                            serialRawPck.myQ.Dequeue();
                    }
                }

                Thread.Sleep(100);
            }
        }

        public void fillTransmitBuffer(byte devID, byte subID, byte cmd, byte[] dataBuffer, byte length)
        {
            //Header	ADR1	ADR2	Command	   Len	 CRC1	 CRC2	End Of Pck
            //FD                                                            FE
            
            TransmitBuffer = new byte[length + 8];
            UInt16 crcResult;

            TransmitBuffer[0] = 0xFD;
            TransmitBuffer[1] = devID;
            TransmitBuffer[2] = subID;
            TransmitBuffer[3] = cmd;
            TransmitBuffer[4] = length;

            for (int i = 0; i < length; i++)
                TransmitBuffer[5 + i] = dataBuffer[i];

            crcResult = CRC16_Calculator.Compute_CRC16(TransmitBuffer, 1, (byte)(length + 4));

            TransmitBuffer[length + 5] = (byte)(crcResult & 0x00FF);
            TransmitBuffer[length + 6] = (byte)((crcResult >> 8) & 0x00FF);
            TransmitBuffer[length + 7] = 0xFE;

            if (serialRawPck.IsOpen)
                serialRawPck.Write(TransmitBuffer, 0, TransmitBuffer.Length);


            //paketi sadece string formda gostermek icin
            TransmitBufferInHexString = "Tx: ";
            for (int i = 0; i < TransmitBuffer.Length; i++)
                TransmitBufferInHexString += string.Format("{0:x2}", TransmitBuffer[i]).ToUpper() + " ";

            TransmitBufferInHexString += " - " + System.DateTime.Now.ToLongTimeString() + Environment.NewLine;

        }

        public void stopPackageProcess()
        {
            receivePacketProcess.Abort();
        }

        private void writeRecvPckInString()
        {
            //paketi sadece string formda gostermek icin

            RecvDataField.pckInHexString = "Rx: FD " + string.Format("{0:x2} ", RecvDataField.ADR1).ToUpper() + string.Format("{0:x2} ", RecvDataField.ADR2).ToUpper() +
                                                      string.Format("{0:x2} ", RecvDataField.Cmd).ToUpper()  + string.Format("{0:x2} ", RecvDataField.Len).ToUpper();

            for (int i = 0; i < RecvDataField.Data.Length; i++)
                RecvDataField.pckInHexString += string.Format("{0:x2} ", RecvDataField.Data[i]).ToUpper();

            RecvDataField.pckInHexString += string.Format("{0:x2} ", RecvDataField.CRC1).ToUpper() + string.Format("{0:x2} ", RecvDataField.CRC2).ToUpper() +  "FE" + " - " + System.DateTime.Now.ToLongTimeString();

            if (RecvDataField.crcFlag == false)
                RecvDataField.pckInHexString += " CRC ERR";

            RecvDataField.pckInHexString += Environment.NewLine;
        }

        private byte Chsum_Calculator(ushort len, byte[] data)
        {
            byte chsum_calc = (byte)((byte)(len >> 8)  ^ (byte)(len & 0x00FF));

            for (int i = 0; i < len; i++)
                chsum_calc ^= data[i];

            return chsum_calc;
        }

    }
}
