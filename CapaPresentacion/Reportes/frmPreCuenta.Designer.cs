namespace CapaPresentacion.Reportes
{
    partial class frmPreCuenta
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
            this.components = new System.ComponentModel.Container();
            Microsoft.Reporting.WinForms.ReportDataSource reportDataSource1 = new Microsoft.Reporting.WinForms.ReportDataSource();
            Microsoft.Reporting.WinForms.ReportDataSource reportDataSource2 = new Microsoft.Reporting.WinForms.ReportDataSource();
            this.rvPrecuenta = new Microsoft.Reporting.WinForms.ReportViewer();
            this.dsPrecuenta = new CapaPresentacion.dsPrecuenta();
            this.dsPrecuentaBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.dtDetalleBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.dtCabeceraBindingSource = new System.Windows.Forms.BindingSource(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.dsPrecuenta)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dsPrecuentaBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtDetalleBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtCabeceraBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // rvPrecuenta
            // 
            this.rvPrecuenta.Dock = System.Windows.Forms.DockStyle.Fill;
            reportDataSource1.Name = "dsCabecera";
            reportDataSource1.Value = this.dtCabeceraBindingSource;
            reportDataSource2.Name = "dsDetalle";
            reportDataSource2.Value = this.dtDetalleBindingSource;
            this.rvPrecuenta.LocalReport.DataSources.Add(reportDataSource1);
            this.rvPrecuenta.LocalReport.DataSources.Add(reportDataSource2);
            this.rvPrecuenta.LocalReport.ReportEmbeddedResource = "CapaPresentacion.Reportes.rvPrecuenta.rdlc";
            this.rvPrecuenta.Location = new System.Drawing.Point(0, 0);
            this.rvPrecuenta.Name = "rvPrecuenta";
            this.rvPrecuenta.ServerReport.BearerToken = null;
            this.rvPrecuenta.Size = new System.Drawing.Size(639, 450);
            this.rvPrecuenta.TabIndex = 0;
            // 
            // dsPrecuenta
            // 
            this.dsPrecuenta.DataSetName = "dsPrecuenta";
            this.dsPrecuenta.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema;
            // 
            // dsPrecuentaBindingSource
            // 
            this.dsPrecuentaBindingSource.DataSource = this.dsPrecuenta;
            this.dsPrecuentaBindingSource.Position = 0;
            // 
            // dtDetalleBindingSource
            // 
            this.dtDetalleBindingSource.DataMember = "dtDetalle";
            this.dtDetalleBindingSource.DataSource = this.dsPrecuentaBindingSource;
            // 
            // dtCabeceraBindingSource
            // 
            this.dtCabeceraBindingSource.DataMember = "dtCabecera";
            this.dtCabeceraBindingSource.DataSource = this.dsPrecuentaBindingSource;
            // 
            // frmPreCuenta
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(639, 450);
            this.Controls.Add(this.rvPrecuenta);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "frmPreCuenta";
            this.Text = "frmPreCuenta";
            this.Load += new System.EventHandler(this.frmPreCuenta_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dsPrecuenta)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dsPrecuentaBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtDetalleBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dtCabeceraBindingSource)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Microsoft.Reporting.WinForms.ReportViewer rvPrecuenta;
        private System.Windows.Forms.BindingSource dtCabeceraBindingSource;
        private System.Windows.Forms.BindingSource dsPrecuentaBindingSource;
        private dsPrecuenta dsPrecuenta;
        private System.Windows.Forms.BindingSource dtDetalleBindingSource;
    }
}