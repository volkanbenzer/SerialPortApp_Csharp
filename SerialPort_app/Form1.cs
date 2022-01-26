using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace SerialPort_app
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        SerialProcess serialProcess1;
        ProtocolProcess protocolProcess1;

        private void Form1_Load(object sender, EventArgs e)
        {
            serialProcess1 = new SerialProcess(serialPort1);
            protocolProcess1 = new ProtocolProcess(serialProcess1);

            ReceiveProcessor = new Thread(receiveProcessor);
            ReceiveProcessor.Start();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen == true)
            {
                button3.Text = "Open";
                serialPort1.Close();

                //serialProcess1.stopSerialProcess();
                //protocolProcess1.stopPackageProcess();
            }
            else
            {
                serialPort1.PortName = "COM" + numericUpDown1.Value.ToString();
                button3.Text = "Close";

                serialPort1.BaudRate = Convert.ToInt32(textBox5.Text);

                serialPort1.Open();

                serialProcess1.stopSerialProcess();
                protocolProcess1.stopPackageProcess();

                serialProcess1 = new SerialProcess(serialPort1);
                protocolProcess1 = new ProtocolProcess(serialProcess1);

            }
        }


    }
}
