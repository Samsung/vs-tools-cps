namespace Tizen.VisualStudio.OptionPages
{
    /// <summary>
    /// Class ToolsControl
    /// </summary>
    partial class ToolsControl
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnToolsFolderPath = new System.Windows.Forms.Button();
            this.textToolsFolderPath = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.label3 = new System.Windows.Forms.Label();
            this.textEmulatorManagerPath = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textCertificateManagerPath = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textDeviceManagerPath = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textSDBCommandPromptPath = new System.Windows.Forms.TextBox();
            this.pictureDeviceManagerPath = new System.Windows.Forms.PictureBox();
            this.pictureEmulatorManagerPath = new System.Windows.Forms.PictureBox();
            this.pictureCertificateManagerPath = new System.Windows.Forms.PictureBox();
            this.pictureSDBCommandPromptPath = new System.Windows.Forms.PictureBox();
            this.btn_reset = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureDeviceManagerPath)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureEmulatorManagerPath)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureCertificateManagerPath)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureSDBCommandPromptPath)).BeginInit();
            this.SuspendLayout();
            // 
            // btnToolsFolderPath
            // 
            this.btnToolsFolderPath.Location = new System.Drawing.Point(329, 18);
            this.btnToolsFolderPath.Name = "btnToolsFolderPath";
            this.btnToolsFolderPath.Size = new System.Drawing.Size(59, 21);
            this.btnToolsFolderPath.TabIndex = 5;
            this.btnToolsFolderPath.Text = "Browse";
            this.btnToolsFolderPath.UseVisualStyleBackColor = true;
            this.btnToolsFolderPath.Click += new System.EventHandler(this.BtnToolsFolderPath_Click);
            // 
            // textToolsFolderPath
            // 
            this.textToolsFolderPath.Location = new System.Drawing.Point(5, 19);
            this.textToolsFolderPath.MaxLength = 512;
            this.textToolsFolderPath.Name = "textToolsFolderPath";
            this.textToolsFolderPath.ReadOnly = true;
            this.textToolsFolderPath.Size = new System.Drawing.Size(318, 21);
            this.textToolsFolderPath.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(5, 4);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(151, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Tools Path (Tizen Studio)";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(5, 54);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(93, 13);
            this.label3.TabIndex = 9;
            this.label3.Text = "Emulator Manager";
            // 
            // textEmulatorManagerPath
            // 
            this.textEmulatorManagerPath.Location = new System.Drawing.Point(22, 70);
            this.textEmulatorManagerPath.MaxLength = 512;
            this.textEmulatorManagerPath.Name = "textEmulatorManagerPath";
            this.textEmulatorManagerPath.ReadOnly = true;
            this.textEmulatorManagerPath.Size = new System.Drawing.Size(398, 21);
            this.textEmulatorManagerPath.TabIndex = 10;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(5, 107);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(99, 13);
            this.label4.TabIndex = 11;
            this.label4.Text = "Certificate Manager";
            // 
            // textCertificateManagerPath
            // 
            this.textCertificateManagerPath.Location = new System.Drawing.Point(22, 123);
            this.textCertificateManagerPath.MaxLength = 512;
            this.textCertificateManagerPath.Name = "textCertificateManagerPath";
            this.textCertificateManagerPath.ReadOnly = true;
            this.textCertificateManagerPath.Size = new System.Drawing.Size(398, 21);
            this.textCertificateManagerPath.TabIndex = 12;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(5, 161);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(86, 13);
            this.label2.TabIndex = 13;
            this.label2.Text = "Device Manager";
            // 
            // textDeviceManagerPath
            // 
            this.textDeviceManagerPath.Location = new System.Drawing.Point(22, 177);
            this.textDeviceManagerPath.MaxLength = 512;
            this.textDeviceManagerPath.Name = "textDeviceManagerPath";
            this.textDeviceManagerPath.ReadOnly = true;
            this.textDeviceManagerPath.Size = new System.Drawing.Size(398, 21);
            this.textDeviceManagerPath.TabIndex = 14;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(5, 213);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(115, 13);
            this.label5.TabIndex = 15;
            this.label5.Text = "SDB Command Prompt";
            // 
            // textSDBCommandPromptPath
            // 
            this.textSDBCommandPromptPath.Location = new System.Drawing.Point(22, 229);
            this.textSDBCommandPromptPath.MaxLength = 512;
            this.textSDBCommandPromptPath.Name = "textSDBCommandPromptPath";
            this.textSDBCommandPromptPath.ReadOnly = true;
            this.textSDBCommandPromptPath.Size = new System.Drawing.Size(398, 21);
            this.textSDBCommandPromptPath.TabIndex = 16;
            // 
            // pictureDeviceManagerPath
            // 
            this.pictureDeviceManagerPath.Image = global::Tizen.VisualStudio.Resources.StatusOK_16x;
            this.pictureDeviceManagerPath.Location = new System.Drawing.Point(426, 180);
            this.pictureDeviceManagerPath.Name = "pictureDeviceManagerPath";
            this.pictureDeviceManagerPath.Size = new System.Drawing.Size(16, 16);
            this.pictureDeviceManagerPath.TabIndex = 17;
            this.pictureDeviceManagerPath.TabStop = false;
            // 
            // pictureEmulatorManagerPath
            // 
            this.pictureEmulatorManagerPath.Image = global::Tizen.VisualStudio.Resources.StatusOK_16x;
            this.pictureEmulatorManagerPath.Location = new System.Drawing.Point(426, 73);
            this.pictureEmulatorManagerPath.Name = "pictureEmulatorManagerPath";
            this.pictureEmulatorManagerPath.Size = new System.Drawing.Size(16, 16);
            this.pictureEmulatorManagerPath.TabIndex = 18;
            this.pictureEmulatorManagerPath.TabStop = false;
            // 
            // pictureCertificateManagerPath
            // 
            this.pictureCertificateManagerPath.Image = global::Tizen.VisualStudio.Resources.StatusOK_16x;
            this.pictureCertificateManagerPath.Location = new System.Drawing.Point(426, 126);
            this.pictureCertificateManagerPath.Name = "pictureCertificateManagerPath";
            this.pictureCertificateManagerPath.Size = new System.Drawing.Size(16, 16);
            this.pictureCertificateManagerPath.TabIndex = 19;
            this.pictureCertificateManagerPath.TabStop = false;
            // 
            // pictureSDBCommandPromptPath
            // 
            this.pictureSDBCommandPromptPath.Image = global::Tizen.VisualStudio.Resources.StatusOK_16x;
            this.pictureSDBCommandPromptPath.Location = new System.Drawing.Point(426, 232);
            this.pictureSDBCommandPromptPath.Name = "pictureSDBCommandPromptPath";
            this.pictureSDBCommandPromptPath.Size = new System.Drawing.Size(16, 16);
            this.pictureSDBCommandPromptPath.TabIndex = 20;
            this.pictureSDBCommandPromptPath.TabStop = false;
            // 
            // btn_reset
            // 
            this.btn_reset.Location = new System.Drawing.Point(394, 18);
            this.btn_reset.Name = "btn_reset";
            this.btn_reset.Size = new System.Drawing.Size(57, 21);
            this.btn_reset.TabIndex = 21;
            this.btn_reset.Text = "Reset";
            this.btn_reset.UseVisualStyleBackColor = true;
            this.btn_reset.Click += new System.EventHandler(this.Button_reset_Click);
            // 
            // ToolsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btn_reset);
            this.Controls.Add(this.pictureSDBCommandPromptPath);
            this.Controls.Add(this.pictureCertificateManagerPath);
            this.Controls.Add(this.pictureEmulatorManagerPath);
            this.Controls.Add(this.pictureDeviceManagerPath);
            this.Controls.Add(this.textSDBCommandPromptPath);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.textDeviceManagerPath);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textCertificateManagerPath);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.textEmulatorManagerPath);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.btnToolsFolderPath);
            this.Controls.Add(this.textToolsFolderPath);
            this.Controls.Add(this.label1);
            this.Name = "ToolsControl";
            this.Size = new System.Drawing.Size(496, 285);
            ((System.ComponentModel.ISupportInitialize)(this.pictureDeviceManagerPath)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureEmulatorManagerPath)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureCertificateManagerPath)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureSDBCommandPromptPath)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnToolsFolderPath;
        private System.Windows.Forms.TextBox textToolsFolderPath;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textEmulatorManagerPath;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textCertificateManagerPath;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textDeviceManagerPath;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textSDBCommandPromptPath;
        private System.Windows.Forms.PictureBox pictureDeviceManagerPath;
        private System.Windows.Forms.PictureBox pictureEmulatorManagerPath;
        private System.Windows.Forms.PictureBox pictureCertificateManagerPath;
        private System.Windows.Forms.PictureBox pictureSDBCommandPromptPath;
        private System.Windows.Forms.Button btn_reset;
    }
}
