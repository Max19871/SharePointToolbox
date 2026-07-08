namespace SharePointToolbox.UI
{
    partial class MainMaterialForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel pnlSidebar;
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Panel pnlContent;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            pnlSidebar = new Panel();
            pnlHeader = new Panel();
            pnlContent = new Panel();
            statusStrip = new StatusStrip();
            lblStatus = new ToolStripStatusLabel();

            SuspendLayout();

            // Sidebar
            pnlSidebar.Dock = DockStyle.Left;
            pnlSidebar.Width = 220;
            pnlSidebar.BackColor = Color.FromArgb(45, 45, 48);

            // Header
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 70;
            pnlHeader.BackColor = Color.White;

            // Content
            pnlContent.Dock = DockStyle.Fill;
            pnlContent.BackColor = Color.FromArgb(245, 245, 245);

            // Status
            statusStrip.Items.Add(lblStatus);
            statusStrip.Dock = DockStyle.Bottom;
            lblStatus.Text = "Pronto";

            Controls.Add(pnlContent);
            Controls.Add(pnlHeader);
            Controls.Add(pnlSidebar);
            Controls.Add(statusStrip);

            ClientSize = new Size(1400, 900);
            MinimumSize = new Size(1200, 800);
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SharePoint Toolbox";

            ResumeLayout(false);
            PerformLayout();
        }
    }
}
