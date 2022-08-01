/*
 * Copyright 2021(c) Samsung Electronics Co., Ltd  All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * 	http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace Tizen.VisualStudio.OptionPages
{
    partial class TidlControl
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
            this.proxyCheck = new System.Windows.Forms.CheckBox();
            this.stubCheck = new System.Windows.Forms.CheckBox();
            this.rpcCheck = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.cppRadio = new System.Windows.Forms.RadioButton();
            this.label5 = new System.Windows.Forms.Label();
            this.csharpRadio = new System.Windows.Forms.RadioButton();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.cRadio = new System.Windows.Forms.RadioButton();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // proxyCheck
            // 
            this.proxyCheck.AutoSize = true;
            this.proxyCheck.Location = new System.Drawing.Point(13, 41);
            this.proxyCheck.Margin = new System.Windows.Forms.Padding(4);
            this.proxyCheck.Name = "proxyCheck";
            this.proxyCheck.Size = new System.Drawing.Size(65, 21);
            this.proxyCheck.TabIndex = 0;
            this.proxyCheck.Text = "Proxy";
            this.proxyCheck.UseVisualStyleBackColor = true;
            this.proxyCheck.CheckedChanged += new System.EventHandler(this.CheckChanged);
            // 
            // stubCheck
            // 
            this.stubCheck.AutoSize = true;
            this.stubCheck.Location = new System.Drawing.Point(211, 41);
            this.stubCheck.Margin = new System.Windows.Forms.Padding(4);
            this.stubCheck.Name = "stubCheck";
            this.stubCheck.Size = new System.Drawing.Size(59, 21);
            this.stubCheck.TabIndex = 1;
            this.stubCheck.Text = "Stub";
            this.stubCheck.UseVisualStyleBackColor = true;
            this.stubCheck.CheckedChanged += new System.EventHandler(this.CheckChanged);
            // 
            // rpcCheck
            // 
            this.rpcCheck.AutoSize = true;
            this.rpcCheck.Location = new System.Drawing.Point(13, 69);
            this.rpcCheck.Margin = new System.Windows.Forms.Padding(4);
            this.rpcCheck.Name = "rpcCheck";
            this.rpcCheck.Size = new System.Drawing.Size(69, 21);
            this.rpcCheck.TabIndex = 2;
            this.rpcCheck.Text = "Rpclib";
            this.rpcCheck.UseVisualStyleBackColor = true;
            this.rpcCheck.UseWaitCursor = true;
            this.rpcCheck.CheckedChanged += new System.EventHandler(this.CheckChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label1.Location = new System.Drawing.Point(35, 16);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(69, 29);
            this.label1.TabIndex = 3;
            this.label1.Text = "TIDL";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(35, 57);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(274, 17);
            this.label2.TabIndex = 4;
            this.label2.Text = "General settings for TIDL code generation";
            // 
            // panel1
            // 
            this.panel1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.panel1.Controls.Add(this.cRadio);
            this.panel1.Controls.Add(this.cppRadio);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Controls.Add(this.csharpRadio);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.proxyCheck);
            this.panel1.Controls.Add(this.rpcCheck);
            this.panel1.Controls.Add(this.stubCheck);
            this.panel1.Cursor = System.Windows.Forms.Cursors.Default;
            this.panel1.Location = new System.Drawing.Point(33, 94);
            this.panel1.Margin = new System.Windows.Forms.Padding(4);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(416, 186);
            this.panel1.TabIndex = 5;
            // 
            // cppRadio
            // 
            this.cppRadio.AutoSize = true;
            this.cppRadio.Location = new System.Drawing.Point(273, 149);
            this.cppRadio.Name = "cppRadio";
            this.cppRadio.Size = new System.Drawing.Size(54, 21);
            this.cppRadio.TabIndex = 7;
            this.cppRadio.TabStop = true;
            this.cppRadio.Text = "C++";
            this.cppRadio.UseVisualStyleBackColor = true;
            this.cppRadio.CheckedChanged += new System.EventHandler(this.RadioCheckedChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(4, 126);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(90, 20);
            this.label5.TabIndex = 6;
            this.label5.Text = "Language";
            // 
            // csharpRadio
            // 
            this.csharpRadio.AutoSize = true;
            this.csharpRadio.Location = new System.Drawing.Point(13, 149);
            this.csharpRadio.Margin = new System.Windows.Forms.Padding(4);
            this.csharpRadio.Name = "csharpRadio";
            this.csharpRadio.Size = new System.Drawing.Size(46, 21);
            this.csharpRadio.TabIndex = 5;
            this.csharpRadio.TabStop = true;
            this.csharpRadio.Text = "C#";
            this.csharpRadio.UseVisualStyleBackColor = true;
            this.csharpRadio.CheckedChanged += new System.EventHandler(this.RadioCheckedChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(4, 5);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(125, 20);
            this.label3.TabIndex = 0;
            this.label3.Text = "TIDL Settings";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(30, 322);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(340, 17);
            this.label4.TabIndex = 6;
            this.label4.Text = "*Tidl Output files will be generated in the same folder";
            // 
            // cRadio
            // 
            this.cRadio.AutoSize = true;
            this.cRadio.Location = new System.Drawing.Point(145, 149);
            this.cRadio.Margin = new System.Windows.Forms.Padding(4);
            this.cRadio.Name = "cRadio";
            this.cRadio.Size = new System.Drawing.Size(38, 21);
            this.cRadio.TabIndex = 8;
            this.cRadio.TabStop = true;
            this.cRadio.Text = "C";
            this.cRadio.UseVisualStyleBackColor = true;
            this.cRadio.CheckedChanged += new System.EventHandler(this.RadioCheckedChanged);
            // 
            // TidlControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label4);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "TidlControl";
            this.Size = new System.Drawing.Size(505, 359);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox proxyCheck;
        private System.Windows.Forms.CheckBox stubCheck;
        private System.Windows.Forms.CheckBox rpcCheck;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.RadioButton csharpRadio;
        private System.Windows.Forms.RadioButton cppRadio;
        private System.Windows.Forms.RadioButton cRadio;
    }
}
