namespace SASEGMTest
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.buttonUnInit = new System.Windows.Forms.Button();
            this.numericUpDownCommPort = new System.Windows.Forms.NumericUpDown();
            this.buttonInit = new System.Windows.Forms.Button();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.buttonRomSignature = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.buttonSASVersionAndSerial = new System.Windows.Forms.Button();
            this.buttonLegacyBonus = new System.Windows.Forms.Button();
            this.numericUpDownPollAddress = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownCommPort)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPollAddress)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonUnInit
            // 
            this.buttonUnInit.Location = new System.Drawing.Point(12, 539);
            this.buttonUnInit.Name = "buttonUnInit";
            this.buttonUnInit.Size = new System.Drawing.Size(75, 23);
            this.buttonUnInit.TabIndex = 0;
            this.buttonUnInit.Text = "Unitialise";
            this.buttonUnInit.UseVisualStyleBackColor = true;
            this.buttonUnInit.Click += new System.EventHandler(this.buttonUnInit_Click);
            // 
            // numericUpDownCommPort
            // 
            this.numericUpDownCommPort.Location = new System.Drawing.Point(11, 30);
            this.numericUpDownCommPort.Name = "numericUpDownCommPort";
            this.numericUpDownCommPort.Size = new System.Drawing.Size(120, 20);
            this.numericUpDownCommPort.TabIndex = 1;
            this.numericUpDownCommPort.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // buttonInit
            // 
            this.buttonInit.Location = new System.Drawing.Point(12, 510);
            this.buttonInit.Name = "buttonInit";
            this.buttonInit.Size = new System.Drawing.Size(75, 23);
            this.buttonInit.TabIndex = 2;
            this.buttonInit.Text = "Initialise";
            this.buttonInit.UseVisualStyleBackColor = true;
            this.buttonInit.Click += new System.EventHandler(this.buttonInit_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.label2);
            this.splitContainer1.Panel1.Controls.Add(this.label1);
            this.splitContainer1.Panel1.Controls.Add(this.numericUpDownPollAddress);
            this.splitContainer1.Panel1.Controls.Add(this.buttonLegacyBonus);
            this.splitContainer1.Panel1.Controls.Add(this.buttonSASVersionAndSerial);
            this.splitContainer1.Panel1.Controls.Add(this.buttonRomSignature);
            this.splitContainer1.Panel1.Controls.Add(this.numericUpDownCommPort);
            this.splitContainer1.Panel1.Controls.Add(this.buttonUnInit);
            this.splitContainer1.Panel1.Controls.Add(this.buttonInit);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.panel1);
            this.splitContainer1.Size = new System.Drawing.Size(525, 574);
            this.splitContainer1.SplitterDistance = 145;
            this.splitContainer1.TabIndex = 3;
            // 
            // buttonRomSignature
            // 
            this.buttonRomSignature.Location = new System.Drawing.Point(13, 100);
            this.buttonRomSignature.Name = "buttonRomSignature";
            this.buttonRomSignature.Size = new System.Drawing.Size(119, 23);
            this.buttonRomSignature.TabIndex = 3;
            this.buttonRomSignature.Text = "Rom Signature";
            this.buttonRomSignature.UseVisualStyleBackColor = true;
            this.buttonRomSignature.Click += new System.EventHandler(this.buttonRomSignature_Click);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.listBox1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(376, 574);
            this.panel1.TabIndex = 0;
            // 
            // listBox1
            // 
            this.listBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(0, 0);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(376, 574);
            this.listBox1.TabIndex = 0;
            // 
            // buttonSASVersionAndSerial
            // 
            this.buttonSASVersionAndSerial.Location = new System.Drawing.Point(13, 129);
            this.buttonSASVersionAndSerial.Name = "buttonSASVersionAndSerial";
            this.buttonSASVersionAndSerial.Size = new System.Drawing.Size(119, 23);
            this.buttonSASVersionAndSerial.TabIndex = 4;
            this.buttonSASVersionAndSerial.Text = "SAS Ver / EGM Serial";
            this.buttonSASVersionAndSerial.UseVisualStyleBackColor = true;
            this.buttonSASVersionAndSerial.Click += new System.EventHandler(this.buttonSASVersionAndSerial_Click);
            // 
            // buttonLegacyBonus
            // 
            this.buttonLegacyBonus.Location = new System.Drawing.Point(12, 158);
            this.buttonLegacyBonus.Name = "buttonLegacyBonus";
            this.buttonLegacyBonus.Size = new System.Drawing.Size(119, 23);
            this.buttonLegacyBonus.TabIndex = 5;
            this.buttonLegacyBonus.Text = "Legacy Bonus";
            this.buttonLegacyBonus.UseVisualStyleBackColor = true;
            this.buttonLegacyBonus.Click += new System.EventHandler(this.buttonLegacyBonus_Click);
            // 
            // numericUpDownPollAddress
            // 
            this.numericUpDownPollAddress.Location = new System.Drawing.Point(12, 74);
            this.numericUpDownPollAddress.Name = "numericUpDownPollAddress";
            this.numericUpDownPollAddress.Size = new System.Drawing.Size(120, 20);
            this.numericUpDownPollAddress.TabIndex = 6;
            this.numericUpDownPollAddress.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(11, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(58, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "Comm Port";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 58);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(65, 13);
            this.label2.TabIndex = 8;
            this.label2.Text = "Poll Address";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(525, 574);
            this.Controls.Add(this.splitContainer1);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownCommPort)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPollAddress)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button buttonUnInit;
        private System.Windows.Forms.NumericUpDown numericUpDownCommPort;
        private System.Windows.Forms.Button buttonInit;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Button buttonRomSignature;
        private System.Windows.Forms.Button buttonSASVersionAndSerial;
        private System.Windows.Forms.Button buttonLegacyBonus;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numericUpDownPollAddress;
    }
}

