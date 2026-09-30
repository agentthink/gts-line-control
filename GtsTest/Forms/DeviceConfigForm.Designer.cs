namespace GtsTest
{
    partial class DeviceConfigForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.GroupBox grpCommunication;
        private System.Windows.Forms.GroupBox grpModbus;
        private System.Windows.Forms.GroupBox grpMotion;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.TextBox txtIp;
        private System.Windows.Forms.NumericUpDown numPort;
        private System.Windows.Forms.NumericUpDown numStart;
        private System.Windows.Forms.NumericUpDown numCount;
        private System.Windows.Forms.NumericUpDown numAxis;
        private System.Windows.Forms.NumericUpDown numTarget;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.TableLayoutPanel mainLayout;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            lblTitle = new Label();
            mainLayout = new TableLayoutPanel();
            grpCommunication = new GroupBox();
            commLayout = new TableLayoutPanel();
            lblName = new Label();
            txtName = new TextBox();
            lblIp = new Label();
            txtIp = new TextBox();
            grpModbus = new GroupBox();
            modbusLayout = new TableLayoutPanel();
            lblPort = new Label();
            numPort = new NumericUpDown();
            lblStart = new Label();
            numStart = new NumericUpDown();
            lblCount = new Label();
            numCount = new NumericUpDown();
            grpMotion = new GroupBox();
            motionLayout = new TableLayoutPanel();
            lblAxis = new Label();
            numAxis = new NumericUpDown();
            lblTarget = new Label();
            numTarget = new NumericUpDown();
            buttonLayout = new TableLayoutPanel();
            flowPanel = new FlowLayoutPanel();
            btnCancel = new Button();
            btnOK = new Button();
            mainLayout.SuspendLayout();
            grpCommunication.SuspendLayout();
            commLayout.SuspendLayout();
            grpModbus.SuspendLayout();
            modbusLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numPort).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numStart).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numCount).BeginInit();
            grpMotion.SuspendLayout();
            motionLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numAxis).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numTarget).BeginInit();
            buttonLayout.SuspendLayout();
            flowPanel.SuspendLayout();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.Dock = DockStyle.Top;
            lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblTitle.ForeColor = Color.DarkSlateGray;
            lblTitle.Location = new Point(0, 0);
            lblTitle.Name = "lblTitle";
            lblTitle.Padding = new Padding(15, 10, 0, 0);
            lblTitle.Size = new Size(480, 45);
            lblTitle.TabIndex = 1;
            lblTitle.Text = "⚙️ 新增设备配置";
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // mainLayout
            // 
            mainLayout.ColumnCount = 1;
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            mainLayout.Controls.Add(grpCommunication, 0, 0);
            mainLayout.Controls.Add(grpModbus, 0, 1);
            mainLayout.Controls.Add(grpMotion, 0, 2);
            mainLayout.Controls.Add(buttonLayout, 0, 3);
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.Location = new Point(0, 45);
            mainLayout.Name = "mainLayout";
            mainLayout.Padding = new Padding(15, 25, 15, 20);
            mainLayout.RowCount = 4;
            mainLayout.RowStyles.Add(new RowStyle());
            mainLayout.RowStyles.Add(new RowStyle());
            mainLayout.RowStyles.Add(new RowStyle());
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            mainLayout.Size = new Size(480, 422);
            mainLayout.TabIndex = 0;
            // 
            // grpCommunication
            // 
            grpCommunication.Controls.Add(commLayout);
            grpCommunication.Dock = DockStyle.Fill;
            grpCommunication.Location = new Point(18, 28);
            grpCommunication.Name = "grpCommunication";
            grpCommunication.Padding = new Padding(10);
            grpCommunication.Size = new Size(444, 100);
            grpCommunication.TabIndex = 0;
            grpCommunication.TabStop = false;
            grpCommunication.Text = " 通讯配置 ";
            // 
            // commLayout
            // 
            commLayout.ColumnCount = 2;
            commLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            commLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            commLayout.Controls.Add(lblName, 0, 0);
            commLayout.Controls.Add(txtName, 1, 0);
            commLayout.Controls.Add(lblIp, 0, 1);
            commLayout.Controls.Add(txtIp, 1, 1);
            commLayout.Dock = DockStyle.Fill;
            commLayout.Location = new Point(10, 26);
            commLayout.Name = "commLayout";
            commLayout.Padding = new Padding(5);
            commLayout.RowCount = 2;
            commLayout.RowStyles.Add(new RowStyle());
            commLayout.RowStyles.Add(new RowStyle());
            commLayout.Size = new Size(424, 64);
            commLayout.TabIndex = 0;
            // 
            // lblName
            // 
            lblName.Location = new Point(8, 5);
            lblName.Name = "lblName";
            lblName.Size = new Size(74, 23);
            lblName.TabIndex = 0;
            lblName.Text = "设备名称：";
            lblName.TextAlign = ContentAlignment.MiddleRight;
            // 
            // txtName
            // 
            txtName.Dock = DockStyle.Fill;
            txtName.Location = new Point(88, 8);
            txtName.Name = "txtName";
            txtName.Size = new Size(328, 23);
            txtName.TabIndex = 1;
            // 
            // lblIp
            // 
            lblIp.Location = new Point(8, 34);
            lblIp.Name = "lblIp";
            lblIp.Size = new Size(74, 23);
            lblIp.TabIndex = 2;
            lblIp.Text = "IP 地址：";
            lblIp.TextAlign = ContentAlignment.MiddleRight;
            // 
            // txtIp
            // 
            txtIp.Dock = DockStyle.Fill;
            txtIp.Location = new Point(88, 37);
            txtIp.Name = "txtIp";
            txtIp.Size = new Size(328, 23);
            txtIp.TabIndex = 3;
            txtIp.Text = "192.168.1.10";
            // 
            // grpModbus
            // 
            grpModbus.Controls.Add(modbusLayout);
            grpModbus.Dock = DockStyle.Fill;
            grpModbus.Location = new Point(18, 134);
            grpModbus.Name = "grpModbus";
            grpModbus.Padding = new Padding(10);
            grpModbus.Size = new Size(444, 100);
            grpModbus.TabIndex = 1;
            grpModbus.TabStop = false;
            grpModbus.Text = " Modbus 参数 ";
            // 
            // modbusLayout
            // 
            modbusLayout.ColumnCount = 2;
            modbusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            modbusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            modbusLayout.Controls.Add(lblPort, 0, 0);
            modbusLayout.Controls.Add(numPort, 1, 0);
            modbusLayout.Controls.Add(lblStart, 0, 1);
            modbusLayout.Controls.Add(numStart, 1, 1);
            modbusLayout.Controls.Add(lblCount, 0, 2);
            modbusLayout.Controls.Add(numCount, 1, 2);
            modbusLayout.Dock = DockStyle.Fill;
            modbusLayout.Location = new Point(10, 26);
            modbusLayout.Name = "modbusLayout";
            modbusLayout.Padding = new Padding(5);
            modbusLayout.RowCount = 3;
            modbusLayout.RowStyles.Add(new RowStyle());
            modbusLayout.RowStyles.Add(new RowStyle());
            modbusLayout.RowStyles.Add(new RowStyle());
            modbusLayout.Size = new Size(424, 64);
            modbusLayout.TabIndex = 0;
            // 
            // lblPort
            // 
            lblPort.Location = new Point(8, 5);
            lblPort.Name = "lblPort";
            lblPort.Size = new Size(84, 23);
            lblPort.TabIndex = 0;
            lblPort.Text = "端口：";
            lblPort.TextAlign = ContentAlignment.MiddleRight;
            // 
            // numPort
            // 
            numPort.Dock = DockStyle.Fill;
            numPort.Location = new Point(98, 8);
            numPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            numPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numPort.Name = "numPort";
            numPort.Size = new Size(318, 23);
            numPort.TabIndex = 1;
            numPort.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // lblStart
            // 
            lblStart.Location = new Point(8, 34);
            lblStart.Name = "lblStart";
            lblStart.Size = new Size(84, 23);
            lblStart.TabIndex = 2;
            lblStart.Text = "起始地址：";
            lblStart.TextAlign = ContentAlignment.MiddleRight;
            // 
            // numStart
            // 
            numStart.Dock = DockStyle.Fill;
            numStart.Location = new Point(98, 37);
            numStart.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            numStart.Name = "numStart";
            numStart.Size = new Size(318, 23);
            numStart.TabIndex = 3;
            // 
            // lblCount
            // 
            lblCount.Location = new Point(8, 63);
            lblCount.Name = "lblCount";
            lblCount.Size = new Size(84, 23);
            lblCount.TabIndex = 4;
            lblCount.Text = "寄存器数量：";
            lblCount.TextAlign = ContentAlignment.MiddleRight;
            // 
            // numCount
            // 
            numCount.Dock = DockStyle.Fill;
            numCount.Location = new Point(98, 66);
            numCount.Maximum = new decimal(new int[] { 125, 0, 0, 0 });
            numCount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numCount.Name = "numCount";
            numCount.Size = new Size(318, 23);
            numCount.TabIndex = 5;
            numCount.Value = new decimal(new int[] { 10, 0, 0, 0 });
            // 
            // grpMotion
            // 
            grpMotion.Controls.Add(motionLayout);
            grpMotion.Dock = DockStyle.Fill;
            grpMotion.Location = new Point(18, 240);
            grpMotion.Name = "grpMotion";
            grpMotion.Padding = new Padding(10);
            grpMotion.Size = new Size(444, 100);
            grpMotion.TabIndex = 2;
            grpMotion.TabStop = false;
            grpMotion.Text = " 运动参数 ";
            // 
            // motionLayout
            // 
            motionLayout.ColumnCount = 2;
            motionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            motionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            motionLayout.Controls.Add(lblAxis, 0, 0);
            motionLayout.Controls.Add(numAxis, 1, 0);
            motionLayout.Controls.Add(lblTarget, 0, 1);
            motionLayout.Controls.Add(numTarget, 1, 1);
            motionLayout.Dock = DockStyle.Fill;
            motionLayout.Location = new Point(10, 26);
            motionLayout.Name = "motionLayout";
            motionLayout.Padding = new Padding(5);
            motionLayout.RowCount = 2;
            motionLayout.RowStyles.Add(new RowStyle());
            motionLayout.RowStyles.Add(new RowStyle());
            motionLayout.Size = new Size(424, 64);
            motionLayout.TabIndex = 0;
            // 
            // lblAxis
            // 
            lblAxis.Location = new Point(8, 5);
            lblAxis.Name = "lblAxis";
            lblAxis.Size = new Size(84, 23);
            lblAxis.TabIndex = 0;
            lblAxis.Text = "轴号：";
            lblAxis.TextAlign = ContentAlignment.MiddleRight;
            // 
            // numAxis
            // 
            numAxis.Dock = DockStyle.Fill;
            numAxis.Location = new Point(98, 8);
            numAxis.Maximum = new decimal(new int[] { 8, 0, 0, 0 });
            numAxis.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numAxis.Name = "numAxis";
            numAxis.Size = new Size(318, 23);
            numAxis.TabIndex = 1;
            numAxis.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // lblTarget
            // 
            lblTarget.Location = new Point(8, 34);
            lblTarget.Name = "lblTarget";
            lblTarget.Size = new Size(84, 23);
            lblTarget.TabIndex = 2;
            lblTarget.Text = "目标产量：";
            lblTarget.TextAlign = ContentAlignment.MiddleRight;
            // 
            // numTarget
            // 
            numTarget.Dock = DockStyle.Fill;
            numTarget.Location = new Point(98, 37);
            numTarget.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            numTarget.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numTarget.Name = "numTarget";
            numTarget.Size = new Size(318, 23);
            numTarget.TabIndex = 3;
            numTarget.Value = new decimal(new int[] { 100, 0, 0, 0 });
            // 
            // buttonLayout
            // 
            buttonLayout.ColumnCount = 2;
            buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            buttonLayout.Controls.Add(flowPanel, 1, 0);
            buttonLayout.Dock = DockStyle.Fill;
            buttonLayout.Location = new Point(18, 346);
            buttonLayout.Name = "buttonLayout";
            buttonLayout.RowCount = 1;
            buttonLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            buttonLayout.Size = new Size(444, 53);
            buttonLayout.TabIndex = 3;
            // 
            // flowPanel
            // 
            flowPanel.Controls.Add(btnCancel);
            flowPanel.Controls.Add(btnOK);
            flowPanel.Dock = DockStyle.Fill;
            flowPanel.FlowDirection = FlowDirection.RightToLeft;
            flowPanel.Location = new Point(247, 3);
            flowPanel.Name = "flowPanel";
            flowPanel.Padding = new Padding(0, 5, 0, 0);
            flowPanel.Size = new Size(194, 47);
            flowPanel.TabIndex = 0;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(101, 8);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(90, 32);
            btnCancel.TabIndex = 0;
            btnCancel.Text = "取消";
            // 
            // btnOK
            // 
            btnOK.BackColor = Color.DodgerBlue;
            btnOK.ForeColor = Color.White;
            btnOK.Location = new Point(5, 8);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(90, 32);
            btnOK.TabIndex = 1;
            btnOK.Text = "确定";
            btnOK.UseVisualStyleBackColor = false;
            // 
            // DeviceConfigForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.WhiteSmoke;
            ClientSize = new Size(480, 467);
            Controls.Add(mainLayout);
            Controls.Add(lblTitle);
            Font = new Font("Segoe UI", 9F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "DeviceConfigForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "添加设备 - 配置向导";
            mainLayout.ResumeLayout(false);
            grpCommunication.ResumeLayout(false);
            commLayout.ResumeLayout(false);
            commLayout.PerformLayout();
            grpModbus.ResumeLayout(false);
            modbusLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numPort).EndInit();
            ((System.ComponentModel.ISupportInitialize)numStart).EndInit();
            ((System.ComponentModel.ISupportInitialize)numCount).EndInit();
            grpMotion.ResumeLayout(false);
            motionLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numAxis).EndInit();
            ((System.ComponentModel.ISupportInitialize)numTarget).EndInit();
            buttonLayout.ResumeLayout(false);
            flowPanel.ResumeLayout(false);
            ResumeLayout(false);
        }
        private TableLayoutPanel commLayout;
        private Label lblName;
        private Label lblIp;
        private TableLayoutPanel modbusLayout;
        private Label lblPort;
        private Label lblStart;
        private Label lblCount;
        private TableLayoutPanel motionLayout;
        private Label lblAxis;
        private Label lblTarget;
        private TableLayoutPanel buttonLayout;
        private FlowLayoutPanel flowPanel;
    }
}