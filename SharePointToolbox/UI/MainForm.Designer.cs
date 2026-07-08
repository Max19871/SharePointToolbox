namespace SharePointToolbox.UI
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnLogin = new Button();
            lblUserTitle = new Label();
            lblUser = new Label();
            lblSiteTitle = new Label();
            lblSite = new Label();
            grpPractice = new GroupBox();
            btnCreateFolder = new Button();
            txtPracticeName = new TextBox();
            grpLog = new GroupBox();
            txtLog = new RichTextBox();
            grpPractice.SuspendLayout();
            grpLog.SuspendLayout();
            SuspendLayout();
            // 
            // btnLogin
            // 
            btnLogin.Location = new Point(552, 120);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(75, 23);
            btnLogin.TabIndex = 0;
            btnLogin.Text = "Login";
            btnLogin.UseVisualStyleBackColor = true;
            btnLogin.Click += btnLogin_Click;
            // 
            // lblUserTitle
            // 
            lblUserTitle.AutoSize = true;
            lblUserTitle.Location = new Point(8, 8);
            lblUserTitle.Name = "lblUserTitle";
            lblUserTitle.Size = new Size(42, 15);
            lblUserTitle.TabIndex = 1;
            lblUserTitle.Text = "Utente";
            // 
            // lblUser
            // 
            lblUser.AutoSize = true;
            lblUser.Location = new Point(8, 40);
            lblUser.Name = "lblUser";
            lblUser.Size = new Size(93, 15);
            lblUser.TabIndex = 2;
            lblUser.Text = "Non autenticato";
            // 
            // lblSiteTitle
            // 
            lblSiteTitle.AutoSize = true;
            lblSiteTitle.Location = new Point(8, 72);
            lblSiteTitle.Name = "lblSiteTitle";
            lblSiteTitle.Size = new Size(27, 15);
            lblSiteTitle.TabIndex = 3;
            lblSiteTitle.Text = "Sito";
            // 
            // lblSite
            // 
            lblSite.AutoSize = true;
            lblSite.Location = new Point(8, 104);
            lblSite.Name = "lblSite";
            lblSite.Size = new Size(12, 15);
            lblSite.TabIndex = 4;
            lblSite.Text = "-";
            // 
            // grpPractice
            // 
            grpPractice.Controls.Add(btnCreateFolder);
            grpPractice.Controls.Add(txtPracticeName);
            grpPractice.Location = new Point(8, 144);
            grpPractice.Name = "grpPractice";
            grpPractice.Size = new Size(312, 80);
            grpPractice.TabIndex = 5;
            grpPractice.TabStop = false;
            grpPractice.Text = "Pratica";
            // 
            // btnCreateFolder
            // 
            btnCreateFolder.Enabled = false;
            btnCreateFolder.Location = new Point(216, 32);
            btnCreateFolder.Name = "btnCreateFolder";
            btnCreateFolder.Size = new Size(88, 23);
            btnCreateFolder.TabIndex = 1;
            btnCreateFolder.Text = "Crea cartella";
            btnCreateFolder.UseVisualStyleBackColor = true;
            btnCreateFolder.Click += btnCreateFolder_Click;
            // 
            // txtPracticeName
            // 
            txtPracticeName.Location = new Point(8, 32);
            txtPracticeName.Name = "txtPracticeName";
            txtPracticeName.Size = new Size(200, 23);
            txtPracticeName.TabIndex = 0;
            // 
            // grpLog
            // 
            grpLog.Controls.Add(txtLog);
            grpLog.Location = new Point(8, 248);
            grpLog.Name = "grpLog";
            grpLog.Size = new Size(312, 144);
            grpLog.TabIndex = 6;
            grpLog.TabStop = false;
            grpLog.Text = "Log";
            // 
            // txtLog
            // 
            txtLog.Dock = DockStyle.Fill;
            txtLog.Location = new Point(3, 19);
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.Size = new Size(306, 122);
            txtLog.TabIndex = 0;
            txtLog.Text = "";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(984, 661);
            Controls.Add(grpLog);
            Controls.Add(grpPractice);
            Controls.Add(lblSite);
            Controls.Add(lblSiteTitle);
            Controls.Add(lblUser);
            Controls.Add(lblUserTitle);
            Controls.Add(btnLogin);
            MinimumSize = new Size(900, 650);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SharePoint Toolbox";
            grpPractice.ResumeLayout(false);
            grpPractice.PerformLayout();
            grpLog.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnLogin;
        private Label lblUserTitle;
        private Label lblUser;
        private Label lblSiteTitle;
        private Label lblSite;
        private GroupBox grpPractice;
        private Button btnCreateFolder;
        private TextBox txtPracticeName;
        private GroupBox grpLog;
        private RichTextBox txtLog;
    }
}
