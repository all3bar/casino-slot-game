using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SASEGMTest
{
    public partial class Form1 : Form
    {

        SASEGMWrapper sasWrapper = SASEGMWrapper.Instance;

        byte pollAddress = 1;
        
        public Form1()
        {
            InitializeComponent();

            sasWrapper.ExceptionCode += new SASEGMWrapper.ExceptionCodeEventHandler(sasWrapper_ExceptionCode);
            sasWrapper.ROMSignature += new SASEGMWrapper.ROMSignatureEventHandler(sasWrapper_ROMSignature);
            sasWrapper.SASVersionAndMachineSerialNumber += new SASEGMWrapper.SASVersionAndMachineSerialNumberEventHandler(sasWrapper_SASVersionAndMachineSerialNumber);
        }

        void sasWrapper_SASVersionAndMachineSerialNumber(object sender, SASVersionAndMachineSerialEventArgs e)
        {
            AddListBoxItem(String.Format("Gaming Machine Serial Number: {0} SAS Version: {1}", e.SerialNumber, e.SASVersion));
        }

        void sasWrapper_ROMSignature(object sender, ROMSignatureEventArgs e)
        {
            AddListBoxItem(String.Format("Rom Signature Response: 0x{0:X4}", e.ROMSignature));
        }

        void sasWrapper_ExceptionCode(object sender, ExceptionCodeEventArgs e)
        {
            AddListBoxItem(String.Format("{0:X2} : {1}",e.ExceptionCode, e.ExceptionCodeMessage));
        }

        private delegate void AddListBoxItemDelegate(object item);

        private void AddListBoxItem(object item)
        {
            if (this.listBox1.InvokeRequired)
            {
                this.listBox1.Invoke(new AddListBoxItemDelegate(this.AddListBoxItem), item);
            }
            else
            {
                this.listBox1.Items.Add(item);
                this.listBox1.SelectedIndex = listBox1.Items.Count - 1;
                this.listBox1.SelectedIndex = -1;

                if (listBox1.Items.Count > 100)
                {
                    listBox1.Items.RemoveAt(1);
                }

            }
        }

 
        private void buttonInit_Click(object sender, EventArgs e)
        {

            pollAddress = Convert.ToByte(numericUpDownPollAddress.Value);
            numericUpDownPollAddress.Enabled = false;
            
            SASEGMWrapper.SAS_Initialise(String.Format("COM{0}",numericUpDownCommPort.Value.ToString()));

            SASEGMWrapper.SAS_RegisterExceptionCallback(SASEGMWrapper.exceptionCodeCallBackDelegate);
            SASEGMWrapper.SAS_RegisterROMSignatureCallback(SASEGMWrapper.romSignatureCallbackDelgate);
            SASEGMWrapper.SAS_RegisterSASVersionAndSerialCallback(SASEGMWrapper.sasVersionAndSerialCallbackDelegate);

            SASEGMWrapper.SAS_AddEGM(pollAddress);

        }

        private void buttonUnInit_Click(object sender, EventArgs e)
        {
            SASEGMWrapper.SAS_UnInitialise();
            numericUpDownPollAddress.Enabled = true;
        }

        private void buttonRomSignature_Click(object sender, EventArgs e)
        {
            SASEGMWrapper.SAS_RequestROMSignature(1, 0x0000);
        }

        private void buttonSASVersionAndSerial_Click(object sender, EventArgs e)
        {
            SASEGMWrapper.SAS_RequestSASVersionAndEGMSerial(1);
        }

        private void buttonLegacyBonus_Click(object sender, EventArgs e)
        {
            dialogNumericInput dni = new dialogNumericInput();

            dni.Text = "Enter Credits";
            dni.SetInstructionText("Enter credits to transfer");
            if (dni.ShowDialog() == DialogResult.OK)
            {
                SASEGMWrapper.SAS_InitiateLegacyBonus(1, Convert.ToUInt32(dni.Input) /*credits*/, 0);
            }
           
        }

        
        //
        // Exception Code Callback 
        //

        /*public static void ExceptionCodeCallback(byte exceptionCode)
        {
            StringBuilder exception = new StringBuilder(256);

            try
            {
                SASEGMWrapper.SAS_GetSASExceptionCodeText(exceptionCode, exception.Capacity, exception);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


            //AddListBoxItem(exceptionCode.ToString() + " " + exception.ToString());
           
        }*/








    }
}
