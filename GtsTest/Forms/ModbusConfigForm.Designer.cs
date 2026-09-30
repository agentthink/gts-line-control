namespace GtsTest.Modbus
{
    partial class ModbusConfigForm
    {
        private System.ComponentModel.IContainer components = null;

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
            lblProtocol = new Label();
            cmbProtocol = new ComboBox();
            lblSlaveAddress = new Label();
            numSlaveAddress = new NumericUpDown();
            grpTcp = new GroupBox();
            tcpLayout = new TableLayoutPanel();
            lblIp = new Label();
            txtIp = new TextBox();
            lblPort = new Label();
            numPort = new NumericUpDown();
            grpRtu = new GroupBox();
            rtuLayout = new TableLayoutPanel();
            lblComPort = new Label();
            comLayout = new TableLayoutPanel();
            cmbComPort = new ComboBox();
            btnRefreshCom = new Button();
            lblBaudRate = new Label();
            cmbBaudRate = new ComboBox();
            lblDataBits = new Label();
            cmbDataBits = new ComboBox();
            lblStopBits = new Label();
            cmbStopBits = new ComboBox();
            lblParity = new Label();
            cmbParity = new ComboBox();
            lblDataType = new Label();
            cmbDataType = new ComboBox();
            lblAddressType = new Label();
            cmbAddressType = new ComboBox();
            lblDisplayFormat = new Label();
            cmbDisplayFormat = new ComboBox();
            lblByteOrder = new Label();
            cmbByteOrder = new ComboBox();
            lblStartAddress = new Label();
            numStartAddress = new NumericUpDown();
            lblRegisterCount = new Label();
            numRegisterCount = new NumericUpDown();
            btnOK = new Button();
            btnCancel = new Button();
            rootPanel = new Panel();
            rowButtons = new FlowLayoutPanel();
            rowAddress = new FlowLayoutPanel();
            rowByteOrder = new FlowLayoutPanel();
            rowDisplayFormat = new FlowLayoutPanel();
            rowDataType = new FlowLayoutPanel();
            rowAddressType = new FlowLayoutPanel();
            rowProtocol = new FlowLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)numSlaveAddress).BeginInit();
            grpTcp.SuspendLayout();
            tcpLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numPort).BeginInit();
            grpRtu.SuspendLayout();
            rtuLayout.SuspendLayout();
            comLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numStartAddress).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numRegisterCount).BeginInit();
            rootPanel.SuspendLayout();
            rowButtons.SuspendLayout();
            rowAddress.SuspendLayout();
            rowByteOrder.SuspendLayout();
            rowDisplayFormat.SuspendLayout();
            rowDataType.SuspendLayout();
            rowAddressType.SuspendLayout();
            rowProtocol.SuspendLayout();
            SuspendLayout();
            // 
            // lblProtocol
            // 
            lblProtocol.Location = new Point(3, 3);
            lblProtocol.Name = "lblProtocol";
            lblProtocol.Size = new Size(60, 28);
            lblProtocol.TabIndex = 0;
            lblProtocol.Text = "协议:";
            lblProtocol.TextAlign = ContentAlignment.MiddleRight;
            // 
            // cmbProtocol
            // 
            cmbProtocol.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbProtocol.Items.AddRange(new object[] { "TCP", "RTU" });
            cmbProtocol.Location = new Point(69, 6);
            cmbProtocol.Name = "cmbProtocol";
            cmbProtocol.Size = new Size(90, 25);
            cmbProtocol.TabIndex = 1;
            cmbProtocol.SelectedIndexChanged += CmbProtocol_SelectedIndexChanged;
            // 
            // lblSlaveAddress
            // 
            lblSlaveAddress.Location = new Point(165, 3);
            lblSlaveAddress.Name = "lblSlaveAddress";
            lblSlaveAddress.Size = new Size(80, 28);
            lblSlaveAddress.TabIndex = 2;
            lblSlaveAddress.Text = "从站地址:";
            lblSlaveAddress.TextAlign = ContentAlignment.MiddleRight;
            // 
            // numSlaveAddress
            // 
            numSlaveAddress.Location = new Point(251, 6);
            numSlaveAddress.Maximum = new decimal(new int[] { 247, 0, 0, 0 });
            numSlaveAddress.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numSlaveAddress.Name = "numSlaveAddress";
            numSlaveAddress.Size = new Size(86, 23);
            numSlaveAddress.TabIndex = 3;
            numSlaveAddress.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // grpTcp
            // 
            grpTcp.Controls.Add(tcpLayout);
            grpTcp.Dock = DockStyle.Top;
            grpTcp.Location = new Point(10, 45);
            grpTcp.Name = "grpTcp";
            grpTcp.Padding = new Padding(5, 6, 5, 6);
            grpTcp.Size = new Size(339, 94);
            grpTcp.TabIndex = 1;
            grpTcp.TabStop = false;
            grpTcp.Text = "TCP 参数";
            // 
            // tcpLayout
            // 
            tcpLayout.ColumnCount = 2;
            tcpLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
            tcpLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tcpLayout.Controls.Add(lblIp, 0, 0);
            tcpLayout.Controls.Add(txtIp, 1, 0);
            tcpLayout.Controls.Add(lblPort, 0, 1);
            tcpLayout.Controls.Add(numPort, 1, 1);
            tcpLayout.Dock = DockStyle.Fill;
            tcpLayout.Location = new Point(5, 22);
            tcpLayout.Name = "tcpLayout";
            tcpLayout.RowCount = 2;
            tcpLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            tcpLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            tcpLayout.Size = new Size(329, 66);
            tcpLayout.TabIndex = 0;
            // 
            // lblIp
            // 
            lblIp.Dock = DockStyle.Fill;
            lblIp.Location = new Point(3, 0);
            lblIp.Name = "lblIp";
            lblIp.Size = new Size(64, 34);
            lblIp.TabIndex = 0;
            lblIp.Text = "IP 地址:";
            lblIp.TextAlign = ContentAlignment.MiddleRight;
            // 
            // txtIp
            // 
            txtIp.Dock = DockStyle.Fill;
            txtIp.Location = new Point(73, 3);
            txtIp.Name = "txtIp";
            txtIp.Size = new Size(253, 23);
            txtIp.TabIndex = 1;
            txtIp.Text = "192.168.0.1";
            // 
            // lblPort
            // 
            lblPort.Dock = DockStyle.Fill;
            lblPort.Location = new Point(3, 34);
            lblPort.Name = "lblPort";
            lblPort.Size = new Size(64, 34);
            lblPort.TabIndex = 2;
            lblPort.Text = "端口:";
            lblPort.TextAlign = ContentAlignment.MiddleRight;
            // 
            // numPort
            // 
            numPort.Dock = DockStyle.Fill;
            numPort.Location = new Point(73, 37);
            numPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            numPort.Name = "numPort";
            numPort.Size = new Size(253, 23);
            numPort.TabIndex = 3;
            numPort.Value = new decimal(new int[] { 502, 0, 0, 0 });
            // 
            // grpRtu
            // 
            grpRtu.Controls.Add(rtuLayout);
            grpRtu.Dock = DockStyle.Top;
            grpRtu.Location = new Point(10, 139);
            grpRtu.Name = "grpRtu";
            grpRtu.Padding = new Padding(5, 6, 5, 6);
            grpRtu.Size = new Size(339, 190);
            grpRtu.TabIndex = 2;
            grpRtu.TabStop = false;
            grpRtu.Text = "RTU 参数";
            // 
            // rtuLayout
            // 
            rtuLayout.ColumnCount = 2;
            rtuLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
            rtuLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rtuLayout.Controls.Add(lblComPort, 0, 0);
            rtuLayout.Controls.Add(comLayout, 1, 0);
            rtuLayout.Controls.Add(lblBaudRate, 0, 1);
            rtuLayout.Controls.Add(cmbBaudRate, 1, 1);
            rtuLayout.Controls.Add(lblDataBits, 0, 2);
            rtuLayout.Controls.Add(cmbDataBits, 1, 2);
            rtuLayout.Controls.Add(lblStopBits, 0, 3);
            rtuLayout.Controls.Add(cmbStopBits, 1, 3);
            rtuLayout.Controls.Add(lblParity, 0, 4);
            rtuLayout.Controls.Add(cmbParity, 1, 4);
            rtuLayout.Dock = DockStyle.Fill;
            rtuLayout.Location = new Point(5, 22);
            rtuLayout.Name = "rtuLayout";
            rtuLayout.RowCount = 5;
            rtuLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            rtuLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 29F));
            rtuLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 31F));
            rtuLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            rtuLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 8F));
            rtuLayout.Size = new Size(329, 162);
            rtuLayout.TabIndex = 0;
            // 
            // lblComPort
            // 
            lblComPort.Dock = DockStyle.Fill;
            lblComPort.Location = new Point(3, 0);
            lblComPort.Name = "lblComPort";
            lblComPort.Size = new Size(64, 36);
            lblComPort.TabIndex = 0;
            lblComPort.Text = "串口号:";
            lblComPort.TextAlign = ContentAlignment.MiddleRight;
            // 
            // comLayout
            // 
            comLayout.ColumnCount = 2;
            comLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            comLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            comLayout.Controls.Add(cmbComPort, 0, 0);
            comLayout.Controls.Add(btnRefreshCom, 1, 0);
            comLayout.Dock = DockStyle.Fill;
            comLayout.Location = new Point(73, 3);
            comLayout.Name = "comLayout";
            comLayout.RowCount = 1;
            comLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            comLayout.Size = new Size(253, 30);
            comLayout.TabIndex = 1;
            // 
            // cmbComPort
            // 
            cmbComPort.Dock = DockStyle.Fill;
            cmbComPort.Location = new Point(3, 3);
            cmbComPort.Name = "cmbComPort";
            cmbComPort.Size = new Size(171, 25);
            cmbComPort.TabIndex = 0;
            // 
            // btnRefreshCom
            // 
            btnRefreshCom.Dock = DockStyle.Fill;
            btnRefreshCom.Location = new Point(180, 3);
            btnRefreshCom.Name = "btnRefreshCom";
            btnRefreshCom.Size = new Size(70, 24);
            btnRefreshCom.TabIndex = 1;
            btnRefreshCom.Text = "刷新";
            btnRefreshCom.Click += BtnRefreshCom_Click;
            // 
            // lblBaudRate
            // 
            lblBaudRate.Dock = DockStyle.Fill;
            lblBaudRate.Location = new Point(3, 36);
            lblBaudRate.Name = "lblBaudRate";
            lblBaudRate.Size = new Size(64, 29);
            lblBaudRate.TabIndex = 2;
            lblBaudRate.Text = "波特率:";
            lblBaudRate.TextAlign = ContentAlignment.MiddleRight;
            // 
            // cmbBaudRate
            // 
            cmbBaudRate.Dock = DockStyle.Fill;
            cmbBaudRate.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbBaudRate.Items.AddRange(new object[] { 9600, 19200, 38400, 115200 });
            cmbBaudRate.Location = new Point(73, 39);
            cmbBaudRate.Name = "cmbBaudRate";
            cmbBaudRate.Size = new Size(253, 25);
            cmbBaudRate.TabIndex = 3;
            // 
            // lblDataBits
            // 
            lblDataBits.Dock = DockStyle.Fill;
            lblDataBits.Location = new Point(3, 65);
            lblDataBits.Name = "lblDataBits";
            lblDataBits.Size = new Size(64, 31);
            lblDataBits.TabIndex = 4;
            lblDataBits.Text = "数据位:";
            lblDataBits.TextAlign = ContentAlignment.MiddleRight;
            // 
            // cmbDataBits
            // 
            cmbDataBits.Dock = DockStyle.Fill;
            cmbDataBits.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDataBits.Items.AddRange(new object[] { 5, 6, 7, 8 });
            cmbDataBits.Location = new Point(73, 68);
            cmbDataBits.Name = "cmbDataBits";
            cmbDataBits.Size = new Size(253, 25);
            cmbDataBits.TabIndex = 5;
            // 
            // lblStopBits
            // 
            lblStopBits.Dock = DockStyle.Fill;
            lblStopBits.Location = new Point(3, 96);
            lblStopBits.Name = "lblStopBits";
            lblStopBits.Size = new Size(64, 32);
            lblStopBits.TabIndex = 6;
            lblStopBits.Text = "停止位:";
            lblStopBits.TextAlign = ContentAlignment.MiddleRight;
            // 
            // cmbStopBits
            // 
            cmbStopBits.Dock = DockStyle.Fill;
            cmbStopBits.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbStopBits.Items.AddRange(new object[] { "1", "1.5", "2" });
            cmbStopBits.Location = new Point(73, 99);
            cmbStopBits.Name = "cmbStopBits";
            cmbStopBits.Size = new Size(253, 25);
            cmbStopBits.TabIndex = 7;
            // 
            // lblParity
            // 
            lblParity.Dock = DockStyle.Fill;
            lblParity.Location = new Point(3, 128);
            lblParity.Name = "lblParity";
            lblParity.Size = new Size(64, 34);
            lblParity.TabIndex = 8;
            lblParity.Text = "校验位:";
            lblParity.TextAlign = ContentAlignment.MiddleRight;
            // 
            // cmbParity
            // 
            cmbParity.Dock = DockStyle.Fill;
            cmbParity.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbParity.Items.AddRange(new object[] { "None", "Odd", "Even", "Mark", "Space" });
            cmbParity.Location = new Point(73, 131);
            cmbParity.Name = "cmbParity";
            cmbParity.Size = new Size(253, 25);
            cmbParity.TabIndex = 9;
            // 
            // lblDataType
            // 
            lblDataType.Location = new Point(3, 3);
            lblDataType.Name = "lblDataType";
            lblDataType.Size = new Size(70, 28);
            lblDataType.TabIndex = 0;
            lblDataType.Text = "数据类型:";
            lblDataType.TextAlign = ContentAlignment.MiddleRight;
            // 
            // cmbDataType
            // 
            cmbDataType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDataType.Items.AddRange(new object[] { "Int16", "UInt16", "Int32", "UInt32", "Float", "Double" });
            cmbDataType.Location = new Point(79, 6);
            cmbDataType.Name = "cmbDataType";
            cmbDataType.Size = new Size(120, 25);
            cmbDataType.TabIndex = 1;
            // 
            // lblAddressType
            // 
            lblAddressType.Location = new Point(3, 3);
            lblAddressType.Name = "lblAddressType";
            lblAddressType.Size = new Size(70, 28);
            lblAddressType.TabIndex = 0;
            lblAddressType.Text = "地址类型:";
            lblAddressType.TextAlign = ContentAlignment.MiddleRight;
            // 
            // cmbAddressType
            // 
            cmbAddressType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbAddressType.Items.AddRange(new object[] { "HoldingRegister", "Coil", "InputRegister", "DiscreteInput" });
            cmbAddressType.Location = new Point(79, 6);
            cmbAddressType.Name = "cmbAddressType";
            cmbAddressType.Size = new Size(120, 25);
            cmbAddressType.TabIndex = 1;
            // 
            // lblDisplayFormat
            // 
            lblDisplayFormat.Location = new Point(3, 3);
            lblDisplayFormat.Name = "lblDisplayFormat";
            lblDisplayFormat.Size = new Size(70, 28);
            lblDisplayFormat.TabIndex = 0;
            lblDisplayFormat.Text = "显示格式:";
            lblDisplayFormat.TextAlign = ContentAlignment.MiddleRight;
            // 
            // cmbDisplayFormat
            // 
            cmbDisplayFormat.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDisplayFormat.Items.AddRange(new object[] { "十进制", "十六进制(0x)", "十六进制", "八进制", "二进制", "ASCII" });
            cmbDisplayFormat.Location = new Point(79, 6);
            cmbDisplayFormat.Name = "cmbDisplayFormat";
            cmbDisplayFormat.Size = new Size(120, 25);
            cmbDisplayFormat.TabIndex = 1;
            // 
            // lblByteOrder
            // 
            lblByteOrder.Location = new Point(3, 3);
            lblByteOrder.Name = "lblByteOrder";
            lblByteOrder.Size = new Size(70, 28);
            lblByteOrder.TabIndex = 0;
            lblByteOrder.Text = "字节序:";
            lblByteOrder.TextAlign = ContentAlignment.MiddleRight;
            // 
            // cmbByteOrder
            // 
            cmbByteOrder.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbByteOrder.Items.AddRange(new object[] { "Big Endian (高字节在前)", "Little Endian (低字节在前)" });
            cmbByteOrder.Location = new Point(79, 6);
            cmbByteOrder.Name = "cmbByteOrder";
            cmbByteOrder.Size = new Size(180, 25);
            cmbByteOrder.TabIndex = 1;
            // 
            // lblStartAddress
            // 
            lblStartAddress.Location = new Point(3, 6);
            lblStartAddress.Name = "lblStartAddress";
            lblStartAddress.Size = new Size(70, 28);
            lblStartAddress.TabIndex = 0;
            lblStartAddress.Text = "起始地址:";
            lblStartAddress.TextAlign = ContentAlignment.MiddleRight;
            // 
            // numStartAddress
            // 
            numStartAddress.Location = new Point(79, 9);
            numStartAddress.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            numStartAddress.Name = "numStartAddress";
            numStartAddress.Size = new Size(80, 23);
            numStartAddress.TabIndex = 1;
            // 
            // lblRegisterCount
            // 
            lblRegisterCount.Location = new Point(165, 6);
            lblRegisterCount.Name = "lblRegisterCount";
            lblRegisterCount.Size = new Size(80, 28);
            lblRegisterCount.TabIndex = 2;
            lblRegisterCount.Text = "寄存器数量:";
            lblRegisterCount.TextAlign = ContentAlignment.MiddleRight;
            // 
            // numRegisterCount
            // 
            numRegisterCount.Location = new Point(251, 9);
            numRegisterCount.Maximum = new decimal(new int[] { 125, 0, 0, 0 });
            numRegisterCount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numRegisterCount.Name = "numRegisterCount";
            numRegisterCount.Size = new Size(80, 23);
            numRegisterCount.TabIndex = 3;
            numRegisterCount.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // btnOK
            // 
            btnOK.DialogResult = DialogResult.OK;
            btnOK.Location = new Point(180, 14);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(75, 34);
            btnOK.TabIndex = 1;
            btnOK.Text = "确定";
            btnOK.Click += btnOK_Click;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(261, 14);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 34);
            btnCancel.TabIndex = 0;
            btnCancel.Text = "取消";
            btnCancel.Click += btnCancel_Click;
            // 
            // rootPanel
            // 
            rootPanel.Controls.Add(rowButtons);
            rootPanel.Controls.Add(rowAddress);
            rootPanel.Controls.Add(rowByteOrder);
            rootPanel.Controls.Add(rowDisplayFormat);
            rootPanel.Controls.Add(rowDataType);
            rootPanel.Controls.Add(rowAddressType);
            rootPanel.Controls.Add(grpRtu);
            rootPanel.Controls.Add(grpTcp);
            rootPanel.Controls.Add(rowProtocol);
            rootPanel.Dock = DockStyle.Fill;
            rootPanel.Location = new Point(0, 0);
            rootPanel.Name = "rootPanel";
            rootPanel.Padding = new Padding(10, 11, 10, 11);
            rootPanel.Size = new Size(359, 578);
            rootPanel.TabIndex = 0;
            // 
            // rowButtons
            // 
            rowButtons.Controls.Add(btnCancel);
            rowButtons.Controls.Add(btnOK);
            rowButtons.Dock = DockStyle.Bottom;
            rowButtons.FlowDirection = FlowDirection.RightToLeft;
            rowButtons.Location = new Point(10, 510);
            rowButtons.Name = "rowButtons";
            rowButtons.Padding = new Padding(0, 11, 0, 11);
            rowButtons.Size = new Size(339, 57);
            rowButtons.TabIndex = 7;
            // 
            // rowAddress
            // 
            rowAddress.Controls.Add(lblStartAddress);
            rowAddress.Controls.Add(numStartAddress);
            rowAddress.Controls.Add(lblRegisterCount);
            rowAddress.Controls.Add(numRegisterCount);
            rowAddress.Dock = DockStyle.Top;
            rowAddress.Location = new Point(10, 472);
            rowAddress.Name = "rowAddress";
            rowAddress.Padding = new Padding(0, 6, 0, 0);
            rowAddress.Size = new Size(339, 40);
            rowAddress.TabIndex = 6;
            rowAddress.WrapContents = false;
            // 
            // rowByteOrder
            // 
            rowByteOrder.Controls.Add(lblByteOrder);
            rowByteOrder.Controls.Add(cmbByteOrder);
            rowByteOrder.Dock = DockStyle.Top;
            rowByteOrder.Location = new Point(10, 432);
            rowByteOrder.Name = "rowByteOrder";
            rowByteOrder.Padding = new Padding(0, 3, 0, 0);
            rowByteOrder.Size = new Size(339, 40);
            rowByteOrder.TabIndex = 5;
            rowByteOrder.WrapContents = false;
            // 
            // rowDisplayFormat
            // 
            rowDisplayFormat.Controls.Add(lblDisplayFormat);
            rowDisplayFormat.Controls.Add(cmbDisplayFormat);
            rowDisplayFormat.Dock = DockStyle.Top;
            rowDisplayFormat.Location = new Point(10, 398);
            rowDisplayFormat.Name = "rowDisplayFormat";
            rowDisplayFormat.Padding = new Padding(0, 3, 0, 0);
            rowDisplayFormat.Size = new Size(339, 34);
            rowDisplayFormat.TabIndex = 4;
            rowDisplayFormat.WrapContents = false;
            // 
            // rowDataType
            // 
            rowDataType.Controls.Add(lblDataType);
            rowDataType.Controls.Add(cmbDataType);
            rowDataType.Dock = DockStyle.Top;
            rowDataType.Location = new Point(10, 363);
            rowDataType.Name = "rowDataType";
            rowDataType.Padding = new Padding(0, 3, 0, 0);
            rowDataType.Size = new Size(339, 35);
            rowDataType.TabIndex = 3;
            rowDataType.WrapContents = false;
            // 
            // rowAddressType
            // 
            rowAddressType.Controls.Add(lblAddressType);
            rowAddressType.Controls.Add(cmbAddressType);
            rowAddressType.Dock = DockStyle.Top;
            rowAddressType.Location = new Point(10, 329);
            rowAddressType.Name = "rowAddressType";
            rowAddressType.Padding = new Padding(0, 3, 0, 0);
            rowAddressType.Size = new Size(339, 34);
            rowAddressType.TabIndex = 4;
            rowAddressType.WrapContents = false;
            // 
            // rowProtocol
            // 
            rowProtocol.Controls.Add(lblProtocol);
            rowProtocol.Controls.Add(cmbProtocol);
            rowProtocol.Controls.Add(lblSlaveAddress);
            rowProtocol.Controls.Add(numSlaveAddress);
            rowProtocol.Dock = DockStyle.Top;
            rowProtocol.Location = new Point(10, 11);
            rowProtocol.Name = "rowProtocol";
            rowProtocol.Padding = new Padding(0, 3, 0, 0);
            rowProtocol.Size = new Size(339, 34);
            rowProtocol.TabIndex = 0;
            rowProtocol.WrapContents = false;
            // 
            // ModbusConfigForm
            // 
            AcceptButton = btnOK;
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(359, 578);
            Controls.Add(rootPanel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ModbusConfigForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Modbus 配置";
            ((System.ComponentModel.ISupportInitialize)numSlaveAddress).EndInit();
            grpTcp.ResumeLayout(false);
            tcpLayout.ResumeLayout(false);
            tcpLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numPort).EndInit();
            grpRtu.ResumeLayout(false);
            rtuLayout.ResumeLayout(false);
            comLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numStartAddress).EndInit();
            ((System.ComponentModel.ISupportInitialize)numRegisterCount).EndInit();
            rootPanel.ResumeLayout(false);
            rowButtons.ResumeLayout(false);
            rowAddress.ResumeLayout(false);
            rowByteOrder.ResumeLayout(false);
            rowDisplayFormat.ResumeLayout(false);
            rowDataType.ResumeLayout(false);
            rowAddressType.ResumeLayout(false);
            rowProtocol.ResumeLayout(false);
            ResumeLayout(false);
        }

        // 控件字段声明
        private Label lblProtocol;
        private ComboBox cmbProtocol;
        private Label lblSlaveAddress;
        private NumericUpDown numSlaveAddress;
        private GroupBox grpTcp;
        private TableLayoutPanel tcpLayout;
        private Label lblIp;
        private TextBox txtIp;
        private Label lblPort;
        private NumericUpDown numPort;
        private GroupBox grpRtu;
        private TableLayoutPanel rtuLayout;
        private Label lblComPort;
        private TableLayoutPanel comLayout;
        private ComboBox cmbComPort;
        private Button btnRefreshCom;
        private Label lblBaudRate;
        private ComboBox cmbBaudRate;
        private Label lblDataBits;
        private ComboBox cmbDataBits;
        private Label lblStopBits;
        private ComboBox cmbStopBits;
        private Label lblParity;
        private ComboBox cmbParity;
        private Label lblDataType;
        private ComboBox cmbDataType;
        private Label lblAddressType;
        private ComboBox cmbAddressType;
        private Label lblDisplayFormat;
        private ComboBox cmbDisplayFormat;
        private Label lblByteOrder;
        private ComboBox cmbByteOrder;
        private Label lblStartAddress;
        private NumericUpDown numStartAddress;
        private Label lblRegisterCount;
        private NumericUpDown numRegisterCount;
        private Button btnOK;
        private Button btnCancel;
        private Panel rootPanel;
        private FlowLayoutPanel rowButtons;
        private FlowLayoutPanel rowAddress;
        private FlowLayoutPanel rowByteOrder;
        private FlowLayoutPanel rowDisplayFormat;
        private FlowLayoutPanel rowAddressType;  // 新增
        private FlowLayoutPanel rowDataType;
        private FlowLayoutPanel rowProtocol;
    }
}