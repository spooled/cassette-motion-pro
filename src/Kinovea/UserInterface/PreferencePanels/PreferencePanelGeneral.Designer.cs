#region License
/*
Copyright © Joan Charmant 2011.
jcharmant@gmail.com

This file is part of Kinovea.

Kinovea is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License version 2
as published by the Free Software Foundation.

Kinovea is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Kinovea. If not, see http://www.gnu.org/licenses/.
*/
#endregion
namespace Kinovea.Root
{
	partial class PreferencePanelGeneral
	{
		/// <summary>
		/// Designer variable used to keep track of non-visual components.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Disposes resources used by the control.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				if (components != null) {
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// This method is required for Windows Forms designer support.
		/// Do not change the method contents inside the source code editor. The Forms designer might
		/// not be able to load this method if it was changed manually.
		/// </summary>
		private void InitializeComponent()
		{
      this.cmbHistoryCount = new System.Windows.Forms.ComboBox();
      this.lblLanguage = new System.Windows.Forms.Label();
      this.lblHistoryCount = new System.Windows.Forms.Label();
      this.cmbLanguage = new System.Windows.Forms.ComboBox();
      this.cbEnableDebugLogs = new System.Windows.Forms.CheckBox();
      this.cbEnableAllLanguages = new System.Windows.Forms.CheckBox();
                        this.lblFitterName = new System.Windows.Forms.Label();
            this.lblStudioPhone = new System.Windows.Forms.Label();
            this.lblStudioEmail = new System.Windows.Forms.Label();
            this.lblStudioWebsite = new System.Windows.Forms.Label();
            this.tbFitterName = new System.Windows.Forms.TextBox();
            this.tbStudioPhone = new System.Windows.Forms.TextBox();
            this.tbStudioEmail = new System.Windows.Forms.TextBox();
            this.tbStudioWebsite = new System.Windows.Forms.TextBox();
      this.SuspendLayout();
      //
      // cmbHistoryCount
      //
      this.cmbHistoryCount.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.cmbHistoryCount.FormattingEnabled = true;
      this.cmbHistoryCount.Items.AddRange(new object[] {
            "0",
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10"});
      this.cmbHistoryCount.Location = new System.Drawing.Point(302, 114);
      this.cmbHistoryCount.Name = "cmbHistoryCount";
      this.cmbHistoryCount.Size = new System.Drawing.Size(36, 21);
      this.cmbHistoryCount.TabIndex = 13;
      this.cmbHistoryCount.SelectedIndexChanged += new System.EventHandler(this.cmbHistoryCount_SelectedIndexChanged);
      //
      // lblLanguage
      //
      this.lblLanguage.AutoSize = true;
      this.lblLanguage.Location = new System.Drawing.Point(29, 47);
      this.lblLanguage.Name = "lblLanguage";
      this.lblLanguage.Size = new System.Drawing.Size(61, 13);
      this.lblLanguage.TabIndex = 12;
      this.lblLanguage.Text = "Language :";
      //
      // lblHistoryCount
      //
      this.lblHistoryCount.AutoSize = true;
      this.lblHistoryCount.Location = new System.Drawing.Point(29, 117);
      this.lblHistoryCount.Name = "lblHistoryCount";
      this.lblHistoryCount.Size = new System.Drawing.Size(160, 13);
      this.lblHistoryCount.TabIndex = 14;
      this.lblHistoryCount.Text = "Number of files in recent history :";
      //
      // cmbLanguage
      //
      this.cmbLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.cmbLanguage.FormattingEnabled = true;
      this.cmbLanguage.Items.AddRange(new object[] {
            "English",
            "Français"});
      this.cmbLanguage.Location = new System.Drawing.Point(302, 47);
      this.cmbLanguage.Name = "cmbLanguage";
      this.cmbLanguage.Size = new System.Drawing.Size(104, 21);
      this.cmbLanguage.TabIndex = 11;
      this.cmbLanguage.SelectedIndexChanged += new System.EventHandler(this.cmbLanguage_SelectedIndexChanged);
      //
      // cbEnableDebugLogs
      //
      this.cbEnableDebugLogs.AutoSize = true;
      this.cbEnableDebugLogs.Location = new System.Drawing.Point(32, 150);
      this.cbEnableDebugLogs.Name = "cbEnableDebugLogs";
      this.cbEnableDebugLogs.Size = new System.Drawing.Size(114, 17);
      this.cbEnableDebugLogs.TabIndex = 55;
      this.cbEnableDebugLogs.Text = "Enable debug logs";
      this.cbEnableDebugLogs.UseVisualStyleBackColor = true;
      this.cbEnableDebugLogs.CheckedChanged += new System.EventHandler(this.ChkEnableDebugLog_CheckedChanged);
      //
      // cbEnableAllLanguages
      //
      this.cbEnableAllLanguages.AutoSize = true;
      this.cbEnableAllLanguages.Location = new System.Drawing.Point(32, 81);
      this.cbEnableAllLanguages.Name = "cbEnableAllLanguages";
      this.cbEnableAllLanguages.Size = new System.Drawing.Size(124, 17);
      this.cbEnableAllLanguages.TabIndex = 56;
      this.cbEnableAllLanguages.Text = "Enable all languages";
      this.cbEnableAllLanguages.UseVisualStyleBackColor = true;
      this.cbEnableAllLanguages.CheckedChanged += new System.EventHandler(this.cbEnableAllLanguages_CheckedChanged);

            // lblFitterName
            this.lblFitterName.AutoSize = true;
            this.lblFitterName.Location = new System.Drawing.Point(30, 184);
            this.lblFitterName.Name = "lblFitterName";
            this.lblFitterName.Size = new System.Drawing.Size(63, 13);
            this.lblFitterName.TabIndex = 10;
            this.lblFitterName.Text = "Fitter Name:";

            // lblStudioPhone
            this.lblStudioPhone.AutoSize = true;
            this.lblStudioPhone.Location = new System.Drawing.Point(30, 214);
            this.lblStudioPhone.Name = "lblStudioPhone";
            this.lblStudioPhone.Size = new System.Drawing.Size(73, 13);
            this.lblStudioPhone.TabIndex = 11;
            this.lblStudioPhone.Text = "Studio Phone:";

            // lblStudioEmail
            this.lblStudioEmail.AutoSize = true;
            this.lblStudioEmail.Location = new System.Drawing.Point(30, 244);
            this.lblStudioEmail.Name = "lblStudioEmail";
            this.lblStudioEmail.Size = new System.Drawing.Size(65, 13);
            this.lblStudioEmail.TabIndex = 12;
            this.lblStudioEmail.Text = "Studio Email:";

            // lblStudioWebsite
            this.lblStudioWebsite.AutoSize = true;
            this.lblStudioWebsite.Location = new System.Drawing.Point(30, 274);
            this.lblStudioWebsite.Name = "lblStudioWebsite";
            this.lblStudioWebsite.Size = new System.Drawing.Size(79, 13);
            this.lblStudioWebsite.TabIndex = 13;
            this.lblStudioWebsite.Text = "Studio Website:";

            // tbFitterName
            this.tbFitterName.Location = new System.Drawing.Point(30, 200);
            this.tbFitterName.Name = "tbFitterName";
            this.tbFitterName.Size = new System.Drawing.Size(200, 20);
            this.tbFitterName.TabIndex = 6;
            this.tbFitterName.TextChanged += new System.EventHandler(this.tbFitterName_TextChanged);

            // tbStudioPhone
            this.tbStudioPhone.Location = new System.Drawing.Point(30, 230);
            this.tbStudioPhone.Name = "tbStudioPhone";
            this.tbStudioPhone.Size = new System.Drawing.Size(200, 20);
            this.tbStudioPhone.TabIndex = 7;
            this.tbStudioPhone.TextChanged += new System.EventHandler(this.tbStudioPhone_TextChanged);

            // tbStudioEmail
            this.tbStudioEmail.Location = new System.Drawing.Point(30, 260);
            this.tbStudioEmail.Name = "tbStudioEmail";
            this.tbStudioEmail.Size = new System.Drawing.Size(200, 20);
            this.tbStudioEmail.TabIndex = 8;
            this.tbStudioEmail.TextChanged += new System.EventHandler(this.tbStudioEmail_TextChanged);

            // tbStudioWebsite
            this.tbStudioWebsite.Location = new System.Drawing.Point(30, 290);
            this.tbStudioWebsite.Name = "tbStudioWebsite";
            this.tbStudioWebsite.Size = new System.Drawing.Size(200, 20);
            this.tbStudioWebsite.TabIndex = 9;
            this.tbStudioWebsite.TextChanged += new System.EventHandler(this.tbStudioWebsite_TextChanged);

            //
            // PreferencePanelGeneral
            //
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.BackColor = System.Drawing.Color.Gainsboro;
      this.Controls.Add(this.cbEnableAllLanguages);
            this.Controls.Add(this.lblFitterName);
      this.Controls.Add(this.lblStudioPhone);
      this.Controls.Add(this.lblStudioEmail);
      this.Controls.Add(this.lblStudioWebsite);
      this.Controls.Add(this.tbFitterName);
      this.Controls.Add(this.tbStudioPhone);
      this.Controls.Add(this.tbStudioEmail);
      this.Controls.Add(this.tbStudioWebsite);
      this.Controls.Add(this.cbEnableDebugLogs);
      this.Controls.Add(this.cmbHistoryCount);
      this.Controls.Add(this.lblLanguage);
      this.Controls.Add(this.lblHistoryCount);
      this.Controls.Add(this.cmbLanguage);
      this.Name = "PreferencePanelGeneral";
      this.Size = new System.Drawing.Size(490, 322);
      this.ResumeLayout(false);
      this.PerformLayout();

		}
		private System.Windows.Forms.ComboBox cmbLanguage;
		private System.Windows.Forms.Label lblHistoryCount;
		private System.Windows.Forms.Label lblLanguage;
		private System.Windows.Forms.ComboBox cmbHistoryCount;
        private System.Windows.Forms.CheckBox cbEnableDebugLogs;
        private System.Windows.Forms.CheckBox cbEnableAllLanguages;
                private System.Windows.Forms.Label lblFitterName;
        private System.Windows.Forms.Label lblStudioPhone;
        private System.Windows.Forms.Label lblStudioEmail;
        private System.Windows.Forms.Label lblStudioWebsite;
        private System.Windows.Forms.TextBox tbFitterName;
        private System.Windows.Forms.TextBox tbStudioPhone;
        private System.Windows.Forms.TextBox tbStudioEmail;
        private System.Windows.Forms.TextBox tbStudioWebsite;
    }
}
