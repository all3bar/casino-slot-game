using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SASEGMTest
{
    public partial class dialogNumericInput : Form
    {
        public dialogNumericInput()
        {
            InitializeComponent();
        }

        public void SetInstructionText(string text)
        {
            this.labelInstruction.Text = text;
        }

        public void SetFormName(string text)
        {
            this.Text = text;
        }

        public string Input
        {
            get { return this.maskedTextBoxInput.Text; }
        }
    }
}
