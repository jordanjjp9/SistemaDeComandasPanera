namespace CapaPresentacion
{
    partial class frmListaProductos
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
            this.pnlListPrd = new System.Windows.Forms.Panel();
            this.pnlCabecera = new System.Windows.Forms.Panel();
            this.btnClose = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.dgvListPrd = new System.Windows.Forms.DataGridView();
            this.txtBusqProd = new System.Windows.Forms.TextBox();
            this.pnlListPrd.SuspendLayout();
            this.pnlCabecera.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvListPrd)).BeginInit();
            this.SuspendLayout();
            // 
            // pnlListPrd
            // 
            this.pnlListPrd.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.pnlListPrd.Controls.Add(this.pnlCabecera);
            this.pnlListPrd.Controls.Add(this.dgvListPrd);
            this.pnlListPrd.Controls.Add(this.txtBusqProd);
            this.pnlListPrd.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlListPrd.Location = new System.Drawing.Point(0, 0);
            this.pnlListPrd.Name = "pnlListPrd";
            this.pnlListPrd.Size = new System.Drawing.Size(900, 500);
            this.pnlListPrd.TabIndex = 0;
            // 
            // pnlCabecera
            // 
            this.pnlCabecera.Controls.Add(this.btnClose);
            this.pnlCabecera.Controls.Add(this.label1);
            this.pnlCabecera.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlCabecera.Location = new System.Drawing.Point(0, 0);
            this.pnlCabecera.Name = "pnlCabecera";
            this.pnlCabecera.Size = new System.Drawing.Size(900, 42);
            this.pnlCabecera.TabIndex = 2;
            this.pnlCabecera.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pnlCabecera_MouseDown);
            // 
            // btnClose
            // 
            this.btnClose.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnClose.Location = new System.Drawing.Point(842, 0);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(58, 42);
            this.btnClose.TabIndex = 1;
            this.btnClose.Text = "X";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(335, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(257, 31);
            this.label1.TabIndex = 0;
            this.label1.Text = "Lista de Productos";
            // 
            // dgvListPrd
            // 
            this.dgvListPrd.BackgroundColor = System.Drawing.SystemColors.Control;
            this.dgvListPrd.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvListPrd.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvListPrd.Location = new System.Drawing.Point(58, 117);
            this.dgvListPrd.Name = "dgvListPrd";
            this.dgvListPrd.Size = new System.Drawing.Size(781, 344);
            this.dgvListPrd.TabIndex = 1;
            this.dgvListPrd.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvListPrd_CellDoubleClick);
            this.dgvListPrd.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvListPrd_CellEndEdit);
            this.dgvListPrd.KeyDown += new System.Windows.Forms.KeyEventHandler(this.dgvListPrd_KeyDown);
            // 
            // txtBusqProd
            // 
            this.txtBusqProd.BackColor = System.Drawing.SystemColors.InactiveCaption;
            this.txtBusqProd.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtBusqProd.Location = new System.Drawing.Point(188, 69);
            this.txtBusqProd.Name = "txtBusqProd";
            this.txtBusqProd.Size = new System.Drawing.Size(532, 26);
            this.txtBusqProd.TabIndex = 0;
            this.txtBusqProd.TextChanged += new System.EventHandler(this.txtBusqProd_TextChanged);
            // 
            // frmListaProductos
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.ClientSize = new System.Drawing.Size(900, 500);
            this.Controls.Add(this.pnlListPrd);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "frmListaProductos";
            this.Text = "frmListaProductos";
            this.Load += new System.EventHandler(this.frmListaProductos_Load);
            this.pnlListPrd.ResumeLayout(false);
            this.pnlListPrd.PerformLayout();
            this.pnlCabecera.ResumeLayout(false);
            this.pnlCabecera.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvListPrd)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlListPrd;
        private System.Windows.Forms.DataGridView dgvListPrd;
        private System.Windows.Forms.TextBox txtBusqProd;
        private System.Windows.Forms.Panel pnlCabecera;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Label label1;
    }
}