namespace hlsmx
{
    partial class ServerEditForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) { components.Dispose(); }
            base.Dispose(disposing);
        }

private void InitializeComponent()
        {
            this.labelName = new System.Windows.Forms.Label();
            this.textName = new System.Windows.Forms.TextBox();
            this.labelPriority = new System.Windows.Forms.Label();
            this.comboPriority = new System.Windows.Forms.ComboBox();
            this.checkHidden = new System.Windows.Forms.CheckBox();
            this.labelExe = new System.Windows.Forms.Label();
            this.textExe = new System.Windows.Forms.TextBox();
            this.buttonBrowse = new System.Windows.Forms.Button();
            this.labelParams = new System.Windows.Forms.Label();
            this.textParams = new System.Windows.Forms.TextBox();
            this.buttonOk = new System.Windows.Forms.Button();
            this.buttonAffinity = new System.Windows.Forms.Button();
            this.groupAffinity = new System.Windows.Forms.GroupBox();
            this.listCores = new System.Windows.Forms.CheckedListBox();
            this.labelRcon = new System.Windows.Forms.Label();
            this.textRcon = new System.Windows.Forms.TextBox();
            this.labelRconHint = new System.Windows.Forms.Label();
            this.labelTemplate = new System.Windows.Forms.Label();
            this.comboTemplate = new System.Windows.Forms.ComboBox();
            this.groupAffinity.SuspendLayout();
            this.SuspendLayout();
            this.labelName.AutoSize = true;
            this.labelName.Location = new System.Drawing.Point(6, 13);
            this.labelName.Name = "labelName";
            this.labelName.Size = new System.Drawing.Size(59, 12);
            this.labelName.TabIndex = 0;
            this.labelName.Text = "Name:";
            this.textName.Location = new System.Drawing.Point(83, 8);
            this.textName.Name = "textName";
            this.textName.Size = new System.Drawing.Size(100, 21);
            this.textName.TabIndex = 1;
            this.labelPriority.AutoSize = true;
            this.labelPriority.Location = new System.Drawing.Point(192, 13);
            this.labelPriority.Name = "labelPriority";
            this.labelPriority.Size = new System.Drawing.Size(71, 12);
            this.labelPriority.TabIndex = 2;
            this.labelPriority.Text = "Priority:";
            this.comboPriority.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboPriority.FormattingEnabled = true;
            this.comboPriority.Items.AddRange(new object[] {
            "Low",
            "Below Normal",
            "Normal",
            "Above Normal",
            "High",
            "Realtime"});
            this.comboPriority.Location = new System.Drawing.Point(262, 9);
            this.comboPriority.Name = "comboPriority";
            this.comboPriority.Size = new System.Drawing.Size(74, 20);
            this.comboPriority.TabIndex = 3;
            this.checkHidden.AutoSize = true;
            this.checkHidden.Location = new System.Drawing.Point(346, 12);
            this.checkHidden.Name = "checkHidden";
            this.checkHidden.Size = new System.Drawing.Size(84, 16);
            this.checkHidden.TabIndex = 4;
            this.checkHidden.Text = "Hide console";
            this.checkHidden.UseVisualStyleBackColor = true;
            this.labelExe.AutoSize = true;
            this.labelExe.Location = new System.Drawing.Point(6, 44);
            this.labelExe.Name = "labelExe";
            this.labelExe.Size = new System.Drawing.Size(71, 12);
            this.labelExe.TabIndex = 5;
            this.labelExe.Text = "Executable:";
            this.textExe.Location = new System.Drawing.Point(83, 41);
            this.textExe.Name = "textExe";
            this.textExe.Size = new System.Drawing.Size(275, 21);
            this.textExe.TabIndex = 6;
            this.buttonBrowse.Location = new System.Drawing.Point(364, 40);
            this.buttonBrowse.Name = "buttonBrowse";
            this.buttonBrowse.Size = new System.Drawing.Size(75, 23);
            this.buttonBrowse.TabIndex = 7;
            this.buttonBrowse.Text = "Browse";
            this.buttonBrowse.UseVisualStyleBackColor = true;
            this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
            this.labelParams.AutoSize = true;
            this.labelParams.Location = new System.Drawing.Point(6, 104);
            this.labelParams.Name = "labelParams";
            this.labelParams.Size = new System.Drawing.Size(239, 12);
            this.labelParams.TabIndex = 8;
            this.labelParams.Text = "Launch parameters (console mode \"-console\" required):";
            this.textParams.Location = new System.Drawing.Point(7, 120);
            this.textParams.Multiline = true;
            this.textParams.Name = "textParams";
            this.textParams.Size = new System.Drawing.Size(432, 84);
            this.textParams.TabIndex = 9;
            this.buttonOk.Location = new System.Drawing.Point(247, 209);
            this.buttonOk.Name = "buttonOk";
            this.buttonOk.Size = new System.Drawing.Size(75, 23);
            this.buttonOk.TabIndex = 10;
            this.buttonOk.Text = "OK";
            this.buttonOk.UseVisualStyleBackColor = true;
            this.buttonOk.Click += new System.EventHandler(this.buttonOk_Click);
            this.buttonAffinity.Location = new System.Drawing.Point(328, 209);
            this.buttonAffinity.Name = "buttonAffinity";
            this.buttonAffinity.Size = new System.Drawing.Size(111, 23);
            this.buttonAffinity.TabIndex = 11;
            this.buttonAffinity.Text = "CPU affinity >>";
            this.buttonAffinity.UseVisualStyleBackColor = true;
            this.buttonAffinity.Click += new System.EventHandler(this.buttonAffinity_Click);
            this.groupAffinity.Controls.Add(this.listCores);
            this.groupAffinity.Location = new System.Drawing.Point(8, 231);
            this.groupAffinity.Name = "groupAffinity";
            this.groupAffinity.Size = new System.Drawing.Size(431, 170);
            this.groupAffinity.TabIndex = 12;
            this.groupAffinity.TabStop = false;
            this.groupAffinity.Text = "CPU affinity";
            this.listCores.CheckOnClick = true;
            this.listCores.FormattingEnabled = true;
            this.listCores.Location = new System.Drawing.Point(3, 17);
            this.listCores.MultiColumn = true;
            this.listCores.Name = "listCores";
            this.listCores.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.listCores.Size = new System.Drawing.Size(425, 148);
            this.listCores.TabIndex = 2;
            this.labelRcon.AutoSize = true;
            this.labelRcon.Location = new System.Drawing.Point(6, 75);
            this.labelRcon.Name = "labelRcon";
            this.labelRcon.Size = new System.Drawing.Size(35, 12);
            this.labelRcon.TabIndex = 13;
            this.labelRcon.Text = "RCON:";
            this.textRcon.Location = new System.Drawing.Point(83, 71);
            this.textRcon.Name = "textRcon";
            this.textRcon.Size = new System.Drawing.Size(150, 21);
            this.textRcon.TabIndex = 8;
            this.textRcon.UseSystemPasswordChar = true;
            this.labelRconHint.AutoSize = true;
            this.labelRconHint.Location = new System.Drawing.Point(239, 75);
            this.labelRconHint.Name = "labelRconHint";
            this.labelRconHint.Size = new System.Drawing.Size(180, 12);
            this.labelRconHint.TabIndex = 14;
            this.labelRconHint.Text = "(blank = +rcon_password from params)";
            this.labelTemplate.AutoSize = true;
            this.labelTemplate.Location = new System.Drawing.Point(250, 104);
            this.labelTemplate.Name = "labelTemplate";
            this.labelTemplate.Size = new System.Drawing.Size(59, 12);
            this.labelTemplate.TabIndex = 15;
            this.labelTemplate.Text = "Template:";
            this.comboTemplate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboTemplate.FormattingEnabled = true;
            this.comboTemplate.Location = new System.Drawing.Point(315, 97);
            this.comboTemplate.Name = "comboTemplate";
            this.comboTemplate.Size = new System.Drawing.Size(124, 20);
            this.comboTemplate.TabIndex = 16;
            this.comboTemplate.SelectionChangeCommitted += new System.EventHandler(this.comboTemplate_SelectionChangeCommitted);
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(444, 407);
            this.Controls.Add(this.comboTemplate);
            this.Controls.Add(this.labelTemplate);
            this.Controls.Add(this.labelRconHint);
            this.Controls.Add(this.textRcon);
            this.Controls.Add(this.labelRcon);
            this.Controls.Add(this.groupAffinity);
            this.Controls.Add(this.buttonAffinity);
            this.Controls.Add(this.buttonOk);
            this.Controls.Add(this.textParams);
            this.Controls.Add(this.labelParams);
            this.Controls.Add(this.buttonBrowse);
            this.Controls.Add(this.textExe);
            this.Controls.Add(this.labelExe);
            this.Controls.Add(this.checkHidden);
            this.Controls.Add(this.comboPriority);
            this.Controls.Add(this.labelPriority);
            this.Controls.Add(this.textName);
            this.Controls.Add(this.labelName);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ServerEditForm";
            this.Text = "ServerEditForm";
            this.Shown += new System.EventHandler(this.ServerEditForm_Shown);
            this.groupAffinity.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

private System.Windows.Forms.Label labelName;
        private System.Windows.Forms.TextBox textName;
        private System.Windows.Forms.Label labelPriority;
        private System.Windows.Forms.ComboBox comboPriority;
        private System.Windows.Forms.CheckBox checkHidden;
        private System.Windows.Forms.Label labelExe;
        private System.Windows.Forms.TextBox textExe;
        private System.Windows.Forms.Button buttonBrowse;
        private System.Windows.Forms.Label labelParams;
        private System.Windows.Forms.TextBox textParams;
        private System.Windows.Forms.Button buttonOk;
        private System.Windows.Forms.Button buttonAffinity;
        private System.Windows.Forms.GroupBox groupAffinity;
        private System.Windows.Forms.CheckedListBox listCores;
        private System.Windows.Forms.Label labelRcon;
        private System.Windows.Forms.TextBox textRcon;
        private System.Windows.Forms.Label labelRconHint;
        private System.Windows.Forms.Label labelTemplate;
        private System.Windows.Forms.ComboBox comboTemplate;
    }
}