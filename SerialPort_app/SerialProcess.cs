using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SerialPort_app
{
    class SerialProcess
    {

        Thread receiveThread;

        System.IO.Ports.SerialPort serialPortx1;

        public Queue<byte> myQ = new Queue<byte>();


        public SerialProcess(System.IO.Ports.SerialPort xPort)
        {
            serialPortx1 = xPort;

            receiveThread = new Thread(RecvThread);
            receiveThread.Start();
        }

        public void RecvThread()
        {
            byte rxByte = 47;

            while (true)
            {
                if (serialPortx1.IsOpen)
                {
                    if (serialPortx1.BytesToRead > 0)
                    {
                        while (serialPortx1.BytesToRead > 0)
                        {
                            rxByte = (byte)serialPortx1.ReadByte();
                            myQ.Enqueue(rxByte);
                        }
                    }
                }

                Thread.Sleep(10);
            }
                
        }

        public int bufferBytesToRead
        {
            get
            {
                return myQ.Count;
            }

        }

        public void stopSerialProcess()
        {
            receiveThread.Abort();
        }

        public bool IsOpen
        {
            get
            {
                return serialPortx1.IsOpen; 
            }            
        }
        
        public void Write(byte[] buffer, int offset, int count)
        {
            serialPortx1.Write(buffer, offset, count);
        }
    }
}
