namespace Tizen.VisualStudio.OptionPages
{
    /// <summary>
    /// UI Genarated Class part of CertificateControl
    /// </summary>
    partial class CertificateControl
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CertificateControl));
            this.labelAuthorCert = new System.Windows.Forms.Label();
            this.textAuthorPath = new System.Windows.Forms.TextBox();
            this.textAuthorPass = new System.Windows.Forms.TextBox();
            this.labelAuthorPass = new System.Windows.Forms.Label();
            this.labelDistributorCert = new System.Windows.Forms.Label();
            this.textDistributorPath = new System.Windows.Forms.TextBox();
            this.labelDistributorPass = new System.Windows.Forms.Label();
            this.textDistributorPass = new System.Windows.Forms.TextBox();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.btnAuthorPath = new System.Windows.Forms.Button();
            this.btnDistributorPath = new System.Windows.Forms.Button();
            this.comboBoxCertType = new System.Windows.Forms.ComboBox();
            this.textProfilePath = new System.Windows.Forms.TextBox();
            this.btnProfilePath = new System.Windows.Forms.Button();
            this.comboBoxProfile = new System.Windows.Forms.ComboBox();
            this.labelProfile = new System.Windows.Forms.Label();
            this.labelProfileXml = new System.Windows.Forms.Label();
            this.labelCertificateCloseWarning = new System.Windows.Forms.Label();
            this.pbInvalidDistPath = new System.Windows.Forms.PictureBox();
            this.pbInvalidAuthorPath = new System.Windows.Forms.PictureBox();
            this.checkBoxShowDistpw = new System.Windows.Forms.CheckBox();
            this.checkBoxShowAuthorpw = new System.Windows.Forms.CheckBox();
            this.pbInvalidProfile = new System.Windows.Forms.PictureBox();
            this.pbInvalidProfilePath = new System.Windows.Forms.PictureBox();
            this.checkBoxUserCert = new System.Windows.Forms.CheckBox();
            this.pbInfoSign = new System.Windows.Forms.PictureBox();
            this.toolTipInfo = new System.Windows.Forms.ToolTip(this.components);
            this.toolTipError = new System.Windows.Forms.ToolTip(this.components);
            this.groupBoxMain = new System.Windows.Forms.GroupBox();
            this.groupBoxCertificate = new System.Windows.Forms.GroupBox();
            this.pbEmptyDistributorPass = new System.Windows.Forms.PictureBox();
            this.pbEmptyAuthorPass = new System.Windows.Forms.PictureBox();
            this.groupBoxProfile = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.pbInvalidDistPath)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbInvalidAuthorPath)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbInvalidProfile)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbInvalidProfilePath)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbInfoSign)).BeginInit();
            this.groupBoxMain.SuspendLayout();
            this.groupBoxCertificate.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbEmptyDistributorPass)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbEmptyAuthorPass)).BeginInit();
            this.groupBoxProfile.SuspendLayout();
            this.SuspendLayout();
            // 
            // labelAuthorCert
            // 
            this.labelAuthorCert.AutoSize = true;
            this.labelAuthorCert.Location = new System.Drawing.Point(29, 25);
            this.labelAuthorCert.Name = "labelAuthorCert";
            this.labelAuthorCert.Size = new System.Drawing.Size(88, 13);
            this.labelAuthorCert.TabIndex = 0;
            this.labelAuthorCert.Text = "Author Certificate";
            // 
            // textAuthorPath
            // 
            this.textAuthorPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textAuthorPath.Location = new System.Drawing.Point(138, 21);
            this.textAuthorPath.MaxLength = 512;
            this.textAuthorPath.Name = "textAuthorPath";
            this.textAuthorPath.Size = new System.Drawing.Size(427, 20);
            this.textAuthorPath.TabIndex = 1;
            this.textAuthorPath.TextChanged += new System.EventHandler(this.TextAuthorPath_TextChanged);
            // 
            // textAuthorPass
            // 
            this.textAuthorPass.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textAuthorPass.Location = new System.Drawing.Point(138, 64);
            this.textAuthorPass.MaxLength = 512;
            this.textAuthorPass.Name = "textAuthorPass";
            this.textAuthorPass.Size = new System.Drawing.Size(427, 20);
            this.textAuthorPass.TabIndex = 3;
            this.textAuthorPass.UseSystemPasswordChar = true;
            this.textAuthorPass.TextChanged += new System.EventHandler(this.TextAuthorPass_TextChanged);
            // 
            // labelAuthorPass
            // 
            this.labelAuthorPass.AutoSize = true;
            this.labelAuthorPass.Location = new System.Drawing.Point(28, 68);
            this.labelAuthorPass.Name = "labelAuthorPass";
            this.labelAuthorPass.Size = new System.Drawing.Size(87, 13);
            this.labelAuthorPass.TabIndex = 3;
            this.labelAuthorPass.Text = "Author Password";
            // 
            // labelDistributorCert
            // 
            this.labelDistributorCert.AutoSize = true;
            this.labelDistributorCert.Location = new System.Drawing.Point(12, 118);
            this.labelDistributorCert.Name = "labelDistributorCert";
            this.labelDistributorCert.Size = new System.Drawing.Size(104, 13);
            this.labelDistributorCert.TabIndex = 4;
            this.labelDistributorCert.Text = "Distributor Certificate";
            // 
            // textDistributorPath
            // 
            this.textDistributorPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textDistributorPath.Location = new System.Drawing.Point(138, 114);
            this.textDistributorPath.Name = "textDistributorPath";
            this.textDistributorPath.Size = new System.Drawing.Size(427, 20);
            this.textDistributorPath.TabIndex = 4;
            this.textDistributorPath.TextChanged += new System.EventHandler(this.TextDistributorPath_TextChanged);
            // 
            // labelDistributorPass
            // 
            this.labelDistributorPass.AutoSize = true;
            this.labelDistributorPass.Location = new System.Drawing.Point(11, 163);
            this.labelDistributorPass.Name = "labelDistributorPass";
            this.labelDistributorPass.Size = new System.Drawing.Size(103, 13);
            this.labelDistributorPass.TabIndex = 7;
            this.labelDistributorPass.Text = "Distributor Password";
            // 
            // textDistributorPass
            // 
            this.textDistributorPass.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textDistributorPass.Location = new System.Drawing.Point(138, 158);
            this.textDistributorPass.MaxLength = 512;
            this.textDistributorPass.Name = "textDistributorPass";
            this.textDistributorPass.Size = new System.Drawing.Size(427, 20);
            this.textDistributorPass.TabIndex = 7;
            this.textDistributorPass.UseSystemPasswordChar = true;
            this.textDistributorPass.TextChanged += new System.EventHandler(this.TextDistributorPass_TextChanged);
            // 
            // btnAuthorPath
            // 
            this.btnAuthorPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAuthorPath.Location = new System.Drawing.Point(572, 21);
            this.btnAuthorPath.MinimumSize = new System.Drawing.Size(30, 23);
            this.btnAuthorPath.Name = "btnAuthorPath";
            this.btnAuthorPath.Size = new System.Drawing.Size(30, 23);
            this.btnAuthorPath.TabIndex = 2;
            this.btnAuthorPath.Text = "...";
            this.btnAuthorPath.UseVisualStyleBackColor = true;
            this.btnAuthorPath.Click += new System.EventHandler(this.BtnAuthorPath_Click);
            // 
            // btnDistributorPath
            // 
            this.btnDistributorPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDistributorPath.Location = new System.Drawing.Point(572, 114);
            this.btnDistributorPath.MinimumSize = new System.Drawing.Size(30, 23);
            this.btnDistributorPath.Name = "btnDistributorPath";
            this.btnDistributorPath.Size = new System.Drawing.Size(30, 23);
            this.btnDistributorPath.TabIndex = 5;
            this.btnDistributorPath.Text = "...";
            this.btnDistributorPath.UseVisualStyleBackColor = true;
            this.btnDistributorPath.Click += new System.EventHandler(this.BtnDistributorPath_Click);
            // 
            // comboBoxCertType
            // 
            this.comboBoxCertType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxCertType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxCertType.Enabled = false;
            this.comboBoxCertType.FormattingEnabled = true;
            this.comboBoxCertType.Location = new System.Drawing.Point(16, 66);
            this.comboBoxCertType.MinimumSize = new System.Drawing.Size(86, 0);
            this.comboBoxCertType.Name = "comboBoxCertType";
            this.comboBoxCertType.Size = new System.Drawing.Size(548, 21);
            this.comboBoxCertType.TabIndex = 0;
            this.comboBoxCertType.SelectedIndexChanged += new System.EventHandler(this.ComboBoxCertType_SelectedIndexChanged);
            // 
            // textProfilePath
            // 
            this.textProfilePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textProfilePath.Location = new System.Drawing.Point(138, 30);
            this.textProfilePath.MaxLength = 512;
            this.textProfilePath.Name = "textProfilePath";
            this.textProfilePath.ReadOnly = true;
            this.textProfilePath.Size = new System.Drawing.Size(427, 20);
            this.textProfilePath.TabIndex = 11;
            // 
            // btnProfilePath
            // 
            this.btnProfilePath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnProfilePath.Location = new System.Drawing.Point(572, 30);
            this.btnProfilePath.MinimumSize = new System.Drawing.Size(30, 23);
            this.btnProfilePath.Name = "btnProfilePath";
            this.btnProfilePath.Size = new System.Drawing.Size(30, 23);
            this.btnProfilePath.TabIndex = 12;
            this.btnProfilePath.Text = "...";
            this.btnProfilePath.UseVisualStyleBackColor = true;
            this.btnProfilePath.Visible = false;
            this.btnProfilePath.Click += new System.EventHandler(this.BtnProfilePath_Click);
            // 
            // comboBoxProfile
            // 
            this.comboBoxProfile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxProfile.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxProfile.FormattingEnabled = true;
            this.comboBoxProfile.Location = new System.Drawing.Point(138, 69);
            this.comboBoxProfile.Name = "comboBoxProfile";
            this.comboBoxProfile.Size = new System.Drawing.Size(427, 21);
            this.comboBoxProfile.TabIndex = 13;
            this.comboBoxProfile.SelectedIndexChanged += new System.EventHandler(this.ComboBoxProfile_SelectedIndexChanged);
            // 
            // labelProfile
            // 
            this.labelProfile.AutoSize = true;
            this.labelProfile.Location = new System.Drawing.Point(81, 74);
            this.labelProfile.Name = "labelProfile";
            this.labelProfile.Size = new System.Drawing.Size(36, 13);
            this.labelProfile.TabIndex = 14;
            this.labelProfile.Text = "Profile";
            // 
            // labelProfileXml
            // 
            this.labelProfileXml.AutoSize = true;
            this.labelProfileXml.Location = new System.Drawing.Point(57, 35);
            this.labelProfileXml.Name = "labelProfileXml";
            this.labelProfileXml.Size = new System.Drawing.Size(61, 13);
            this.labelProfileXml.TabIndex = 15;
            this.labelProfileXml.Text = "Profile Path";
            // 
            // labelCertificateCloseWarning
            // 
            this.labelCertificateCloseWarning.AutoSize = true;
            this.labelCertificateCloseWarning.Location = new System.Drawing.Point(9, 98);
            this.labelCertificateCloseWarning.Name = "labelCertificateCloseWarning";
            this.labelCertificateCloseWarning.Size = new System.Drawing.Size(413, 13);
            this.labelCertificateCloseWarning.TabIndex = 18;
            this.labelCertificateCloseWarning.Text = "Please close Certificate Manager before changing the active profile, if already o" +
    "pened.";
            // 
            // pbInvalidDistPath
            // 
            this.pbInvalidDistPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pbInvalidDistPath.Image = ((System.Drawing.Image)(resources.GetObject("pbInvalidDistPath.Image")));
            this.pbInvalidDistPath.Location = new System.Drawing.Point(607, 119);
            this.pbInvalidDistPath.Name = "pbInvalidDistPath";
            this.pbInvalidDistPath.Size = new System.Drawing.Size(16, 16);
            this.pbInvalidDistPath.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pbInvalidDistPath.TabIndex = 19;
            this.pbInvalidDistPath.TabStop = false;
            this.pbInvalidDistPath.Visible = false;
            // 
            // pbInvalidAuthorPath
            // 
            this.pbInvalidAuthorPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pbInvalidAuthorPath.Image = ((System.Drawing.Image)(resources.GetObject("pbInvalidAuthorPath.Image")));
            this.pbInvalidAuthorPath.Location = new System.Drawing.Point(607, 26);
            this.pbInvalidAuthorPath.Name = "pbInvalidAuthorPath";
            this.pbInvalidAuthorPath.Size = new System.Drawing.Size(16, 16);
            this.pbInvalidAuthorPath.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pbInvalidAuthorPath.TabIndex = 18;
            this.pbInvalidAuthorPath.TabStop = false;
            this.pbInvalidAuthorPath.Visible = false;
            // 
            // checkBoxShowDistpw
            // 
            this.checkBoxShowDistpw.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxShowDistpw.AutoSize = true;
            this.checkBoxShowDistpw.Location = new System.Drawing.Point(589, 160);
            this.checkBoxShowDistpw.Name = "checkBoxShowDistpw";
            this.checkBoxShowDistpw.Size = new System.Drawing.Size(99, 17);
            this.checkBoxShowDistpw.TabIndex = 9;
            this.checkBoxShowDistpw.Text = "show password";
            this.checkBoxShowDistpw.UseVisualStyleBackColor = true;
            this.checkBoxShowDistpw.Visible = false;
            this.checkBoxShowDistpw.CheckedChanged += new System.EventHandler(this.CheckBoxShowDistpw_CheckedChanged);
            // 
            // checkBoxShowAuthorpw
            // 
            this.checkBoxShowAuthorpw.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxShowAuthorpw.AutoSize = true;
            this.checkBoxShowAuthorpw.Location = new System.Drawing.Point(589, 66);
            this.checkBoxShowAuthorpw.Name = "checkBoxShowAuthorpw";
            this.checkBoxShowAuthorpw.Size = new System.Drawing.Size(99, 17);
            this.checkBoxShowAuthorpw.TabIndex = 8;
            this.checkBoxShowAuthorpw.Text = "show password";
            this.checkBoxShowAuthorpw.UseVisualStyleBackColor = true;
            this.checkBoxShowAuthorpw.Visible = false;
            this.checkBoxShowAuthorpw.CheckedChanged += new System.EventHandler(this.CheckBoxShowAuthorpw_CheckedChanged);
            // 
            // pbInvalidProfile
            // 
            this.pbInvalidProfile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pbInvalidProfile.Image = ((System.Drawing.Image)(resources.GetObject("pbInvalidProfile.Image")));
            this.pbInvalidProfile.Location = new System.Drawing.Point(607, 74);
            this.pbInvalidProfile.MinimumSize = new System.Drawing.Size(16, 16);
            this.pbInvalidProfile.Name = "pbInvalidProfile";
            this.pbInvalidProfile.Size = new System.Drawing.Size(16, 16);
            this.pbInvalidProfile.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pbInvalidProfile.TabIndex = 17;
            this.pbInvalidProfile.TabStop = false;
            this.pbInvalidProfile.Visible = false;
            // 
            // pbInvalidProfilePath
            // 
            this.pbInvalidProfilePath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pbInvalidProfilePath.Image = ((System.Drawing.Image)(resources.GetObject("pbInvalidProfilePath.Image")));
            this.pbInvalidProfilePath.Location = new System.Drawing.Point(607, 35);
            this.pbInvalidProfilePath.MinimumSize = new System.Drawing.Size(16, 16);
            this.pbInvalidProfilePath.Name = "pbInvalidProfilePath";
            this.pbInvalidProfilePath.Size = new System.Drawing.Size(16, 16);
            this.pbInvalidProfilePath.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pbInvalidProfilePath.TabIndex = 16;
            this.pbInvalidProfilePath.TabStop = false;
            this.pbInvalidProfilePath.Visible = false;
            // 
            // checkBoxUserCert
            // 
            this.checkBoxUserCert.AutoSize = true;
            this.checkBoxUserCert.Location = new System.Drawing.Point(16, 31);
            this.checkBoxUserCert.MinimumSize = new System.Drawing.Size(235, 17);
            this.checkBoxUserCert.Name = "checkBoxUserCert";
            this.checkBoxUserCert.Size = new System.Drawing.Size(235, 17);
            this.checkBoxUserCert.TabIndex = 18;
            this.checkBoxUserCert.Text = "Sign the .TPK file using the following option.";
            this.checkBoxUserCert.UseVisualStyleBackColor = true;
            this.checkBoxUserCert.CheckedChanged += new System.EventHandler(this.CheckBoxUserCert_CheckedChanged);
            // 
            // pbInfoSign
            // 
            this.pbInfoSign.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pbInfoSign.Image = global::Tizen.VisualStudio.Resources.StatusInformation_16x;
            this.pbInfoSign.Location = new System.Drawing.Point(607, 35);
            this.pbInfoSign.Name = "pbInfoSign";
            this.pbInfoSign.Size = new System.Drawing.Size(16, 16);
            this.pbInfoSign.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pbInfoSign.TabIndex = 18;
            this.pbInfoSign.TabStop = false;
            // 
            // groupBoxMain
            // 
            this.groupBoxMain.Controls.Add(this.checkBoxUserCert);
            this.groupBoxMain.Controls.Add(this.comboBoxCertType);
            this.groupBoxMain.Controls.Add(this.pbInfoSign);
            this.groupBoxMain.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBoxMain.Location = new System.Drawing.Point(0, 0);
            this.groupBoxMain.MinimumSize = new System.Drawing.Size(343, 107);
            this.groupBoxMain.Name = "groupBoxMain";
            this.groupBoxMain.Size = new System.Drawing.Size(686, 109);
            this.groupBoxMain.TabIndex = 18;
            this.groupBoxMain.TabStop = false;
            // 
            // groupBoxCertificate
            // 
            this.groupBoxCertificate.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.groupBoxCertificate.Controls.Add(this.pbEmptyDistributorPass);
            this.groupBoxCertificate.Controls.Add(this.pbEmptyAuthorPass);
            this.groupBoxCertificate.Controls.Add(this.pbInvalidDistPath);
            this.groupBoxCertificate.Controls.Add(this.pbInvalidAuthorPath);
            this.groupBoxCertificate.Controls.Add(this.checkBoxShowDistpw);
            this.groupBoxCertificate.Controls.Add(this.checkBoxShowAuthorpw);
            this.groupBoxCertificate.Controls.Add(this.textDistributorPass);
            this.groupBoxCertificate.Controls.Add(this.btnDistributorPath);
            this.groupBoxCertificate.Controls.Add(this.textAuthorPass);
            this.groupBoxCertificate.Controls.Add(this.textAuthorPath);
            this.groupBoxCertificate.Controls.Add(this.labelAuthorPass);
            this.groupBoxCertificate.Controls.Add(this.labelAuthorCert);
            this.groupBoxCertificate.Controls.Add(this.labelDistributorCert);
            this.groupBoxCertificate.Controls.Add(this.btnAuthorPath);
            this.groupBoxCertificate.Controls.Add(this.textDistributorPath);
            this.groupBoxCertificate.Controls.Add(this.labelDistributorPass);
            this.groupBoxCertificate.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBoxCertificate.Enabled = false;
            this.groupBoxCertificate.Location = new System.Drawing.Point(0, 228);
            this.groupBoxCertificate.MinimumSize = new System.Drawing.Size(343, 203);
            this.groupBoxCertificate.Name = "groupBoxCertificate";
            this.groupBoxCertificate.Size = new System.Drawing.Size(686, 203);
            this.groupBoxCertificate.TabIndex = 16;
            this.groupBoxCertificate.TabStop = false;
            this.groupBoxCertificate.Text = "Certificates";
            // 
            // pbEmptyAuthorPass
            // 
            this.pbEmptyDistributorPass.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pbEmptyDistributorPass.Image = global::Tizen.VisualStudio.Resources.StatusInvalid_16x;
            this.pbEmptyDistributorPass.Location = new System.Drawing.Point(568, 159);
            this.pbEmptyDistributorPass.Name = "pbEmptyDistributorPass";
            this.pbEmptyDistributorPass.Size = new System.Drawing.Size(16, 16);
            this.pbEmptyDistributorPass.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pbEmptyDistributorPass.TabIndex = 21;
            this.pbEmptyDistributorPass.TabStop = false;
            // 
            // pbEmptyDistPass
            // 
            this.pbEmptyAuthorPass.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pbEmptyAuthorPass.Image = global::Tizen.VisualStudio.Resources.StatusInvalid_16x;
            this.pbEmptyAuthorPass.Location = new System.Drawing.Point(568, 66);
            this.pbEmptyAuthorPass.Name = "pbEmptyAuthorPass";
            this.pbEmptyAuthorPass.Size = new System.Drawing.Size(16, 16);
            this.pbEmptyAuthorPass.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pbEmptyAuthorPass.TabIndex = 20;
            this.pbEmptyAuthorPass.TabStop = false;
            // 
            // groupBoxProfile
            // 
            this.groupBoxProfile.Controls.Add(this.pbInvalidProfile);
            this.groupBoxProfile.Controls.Add(this.pbInvalidProfilePath);
            this.groupBoxProfile.Controls.Add(this.labelProfileXml);
            this.groupBoxProfile.Controls.Add(this.labelCertificateCloseWarning);
            this.groupBoxProfile.Controls.Add(this.comboBoxProfile);
            this.groupBoxProfile.Controls.Add(this.btnProfilePath);
            this.groupBoxProfile.Controls.Add(this.labelProfile);
            this.groupBoxProfile.Controls.Add(this.textProfilePath);
            this.groupBoxProfile.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBoxProfile.Enabled = false;
            this.groupBoxProfile.Location = new System.Drawing.Point(0, 109);
            this.groupBoxProfile.MinimumSize = new System.Drawing.Size(343, 119);
            this.groupBoxProfile.Name = "groupBoxProfile";
            this.groupBoxProfile.Size = new System.Drawing.Size(686, 119);
            this.groupBoxProfile.TabIndex = 17;
            this.groupBoxProfile.TabStop = false;
            this.groupBoxProfile.Text = "Profile";
            // 
            // CertificateControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.AutoSize = true;
            this.Controls.Add(this.groupBoxCertificate);
            this.Controls.Add(this.groupBoxProfile);
            this.Controls.Add(this.groupBoxMain);
            this.MinimumSize = new System.Drawing.Size(686, 650);
            this.Name = "CertificateControl";
            this.Size = new System.Drawing.Size(686, 650);
            ((System.ComponentModel.ISupportInitialize)(this.pbInvalidDistPath)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbInvalidAuthorPath)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbInvalidProfile)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbInvalidProfilePath)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbInfoSign)).EndInit();
            this.groupBoxMain.ResumeLayout(false);
            this.groupBoxMain.PerformLayout();
            this.groupBoxCertificate.ResumeLayout(false);
            this.groupBoxCertificate.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbEmptyDistributorPass)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbEmptyAuthorPass)).EndInit();
            this.groupBoxProfile.ResumeLayout(false);
            this.groupBoxProfile.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label labelAuthorCert;
        private System.Windows.Forms.TextBox textAuthorPath;
        private System.Windows.Forms.TextBox textAuthorPass;
        private System.Windows.Forms.Label labelAuthorPass;
        private System.Windows.Forms.Label labelDistributorCert;
        private System.Windows.Forms.TextBox textDistributorPath;
        private System.Windows.Forms.Label labelDistributorPass;
        private System.Windows.Forms.TextBox textDistributorPass;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.Button btnAuthorPath;
        private System.Windows.Forms.Button btnDistributorPath;
        private System.Windows.Forms.ComboBox comboBoxCertType;
        private System.Windows.Forms.TextBox textProfilePath;
        private System.Windows.Forms.Button btnProfilePath;
        private System.Windows.Forms.ComboBox comboBoxProfile;
        private System.Windows.Forms.Label labelProfile;
        private System.Windows.Forms.Label labelProfileXml;
        private System.Windows.Forms.Label labelCertificateCloseWarning;
        private System.Windows.Forms.GroupBox groupBoxCertificate;
        private System.Windows.Forms.GroupBox groupBoxProfile;
        private System.Windows.Forms.CheckBox checkBoxUserCert;
        private System.Windows.Forms.CheckBox checkBoxShowAuthorpw;
        private System.Windows.Forms.CheckBox checkBoxShowDistpw;
        private System.Windows.Forms.PictureBox pbInvalidProfilePath;
        private System.Windows.Forms.PictureBox pbInvalidDistPath;
        private System.Windows.Forms.PictureBox pbInvalidAuthorPath;
        private System.Windows.Forms.PictureBox pbInvalidProfile;
        private System.Windows.Forms.PictureBox pbInfoSign;
        private System.Windows.Forms.ToolTip toolTipInfo;
        private System.Windows.Forms.ToolTip toolTipError;
        private System.Windows.Forms.GroupBox groupBoxMain;
        private System.Windows.Forms.PictureBox pbEmptyDistributorPass;
        private System.Windows.Forms.PictureBox pbEmptyAuthorPass;
    }
}
