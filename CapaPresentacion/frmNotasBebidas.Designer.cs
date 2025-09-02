namespace CapaPresentacion
{
    partial class frmNotasBebidas
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
            this.pnlNotBebidas = new System.Windows.Forms.Panel();
            this.pnlCabecera = new System.Windows.Forms.Panel();
            this.btnCerrar = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.pnlCentralBebidas = new System.Windows.Forms.Panel();
            this.btnSLechug = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.button5 = new System.Windows.Forms.Button();
            this.button6 = new System.Windows.Forms.Button();
            this.button7 = new System.Windows.Forms.Button();
            this.button8 = new System.Windows.Forms.Button();
            this.button9 = new System.Windows.Forms.Button();
            this.button10 = new System.Windows.Forms.Button();
            this.pnlVisualizador = new System.Windows.Forms.Panel();
            this.txtProdSelect = new System.Windows.Forms.TextBox();
            this.txtNotasBeb = new System.Windows.Forms.TextBox();
            this.btnContinuar = new System.Windows.Forms.Button();
            this.pnlNotBebidas.SuspendLayout();
            this.pnlCabecera.SuspendLayout();
            this.pnlCentralBebidas.SuspendLayout();
            this.pnlVisualizador.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlNotBebidas
            // 
            this.pnlNotBebidas.Controls.Add(this.pnlCentralBebidas);
            this.pnlNotBebidas.Controls.Add(this.pnlCabecera);
            this.pnlNotBebidas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlNotBebidas.Location = new System.Drawing.Point(0, 0);
            this.pnlNotBebidas.Name = "pnlNotBebidas";
            this.pnlNotBebidas.Size = new System.Drawing.Size(1249, 598);
            this.pnlNotBebidas.TabIndex = 0;
            // 
            // pnlCabecera
            // 
            this.pnlCabecera.Controls.Add(this.btnCerrar);
            this.pnlCabecera.Controls.Add(this.label1);
            this.pnlCabecera.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlCabecera.Location = new System.Drawing.Point(0, 0);
            this.pnlCabecera.Name = "pnlCabecera";
            this.pnlCabecera.Size = new System.Drawing.Size(1249, 59);
            this.pnlCabecera.TabIndex = 0;
            // 
            // btnCerrar
            // 
            this.btnCerrar.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnCerrar.Location = new System.Drawing.Point(0, 0);
            this.btnCerrar.Name = "btnCerrar";
            this.btnCerrar.Size = new System.Drawing.Size(86, 59);
            this.btnCerrar.TabIndex = 3;
            this.btnCerrar.Text = "X";
            this.btnCerrar.UseVisualStyleBackColor = true;
            this.btnCerrar.Click += new System.EventHandler(this.btnCerrar_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Verdana", 26.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(401, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(364, 42);
            this.label1.TabIndex = 2;
            this.label1.Text = "Opciones Bebidas";
            // 
            // pnlCentralBebidas
            // 
            this.pnlCentralBebidas.AutoScroll = true;
            this.pnlCentralBebidas.Controls.Add(this.btnContinuar);
            this.pnlCentralBebidas.Controls.Add(this.pnlVisualizador);
            this.pnlCentralBebidas.Controls.Add(this.button10);
            this.pnlCentralBebidas.Controls.Add(this.button9);
            this.pnlCentralBebidas.Controls.Add(this.button8);
            this.pnlCentralBebidas.Controls.Add(this.button7);
            this.pnlCentralBebidas.Controls.Add(this.button6);
            this.pnlCentralBebidas.Controls.Add(this.button5);
            this.pnlCentralBebidas.Controls.Add(this.button4);
            this.pnlCentralBebidas.Controls.Add(this.button3);
            this.pnlCentralBebidas.Controls.Add(this.button2);
            this.pnlCentralBebidas.Controls.Add(this.button1);
            this.pnlCentralBebidas.Controls.Add(this.btnSLechug);
            this.pnlCentralBebidas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlCentralBebidas.Location = new System.Drawing.Point(0, 59);
            this.pnlCentralBebidas.Name = "pnlCentralBebidas";
            this.pnlCentralBebidas.Size = new System.Drawing.Size(1249, 539);
            this.pnlCentralBebidas.TabIndex = 1;
            // 
            // btnSLechug
            // 
            this.btnSLechug.FlatAppearance.BorderSize = 0;
            this.btnSLechug.Font = new System.Drawing.Font("Verdana", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSLechug.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnSLechug.Location = new System.Drawing.Point(12, 6);
            this.btnSLechug.Name = "btnSLechug";
            this.btnSLechug.Size = new System.Drawing.Size(171, 162);
            this.btnSLechug.TabIndex = 3;
            this.btnSLechug.Text = "Helada";
            this.btnSLechug.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.FlatAppearance.BorderSize = 0;
            this.button1.Font = new System.Drawing.Font("Verdana", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.ForeColor = System.Drawing.SystemColors.ControlText;
            this.button1.Location = new System.Drawing.Point(189, 6);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(171, 162);
            this.button1.TabIndex = 4;
            this.button1.Text = "Sin Helar";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            this.button2.FlatAppearance.BorderSize = 0;
            this.button2.Font = new System.Drawing.Font("Verdana", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button2.ForeColor = System.Drawing.SystemColors.ControlText;
            this.button2.Location = new System.Drawing.Point(366, 6);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(171, 162);
            this.button2.TabIndex = 5;
            this.button2.Text = "Tibio";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // button3
            // 
            this.button3.FlatAppearance.BorderSize = 0;
            this.button3.Font = new System.Drawing.Font("Verdana", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button3.ForeColor = System.Drawing.SystemColors.ControlText;
            this.button3.Location = new System.Drawing.Point(543, 6);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(171, 162);
            this.button3.TabIndex = 6;
            this.button3.Text = "Con Azucar";
            this.button3.UseVisualStyleBackColor = true;
            // 
            // button4
            // 
            this.button4.FlatAppearance.BorderSize = 0;
            this.button4.Font = new System.Drawing.Font("Verdana", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button4.ForeColor = System.Drawing.SystemColors.ControlText;
            this.button4.Location = new System.Drawing.Point(720, 6);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(171, 162);
            this.button4.TabIndex = 7;
            this.button4.Text = "Sin Azucar";
            this.button4.UseVisualStyleBackColor = true;
            // 
            // button5
            // 
            this.button5.FlatAppearance.BorderSize = 0;
            this.button5.Font = new System.Drawing.Font("Verdana", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button5.ForeColor = System.Drawing.SystemColors.ControlText;
            this.button5.Location = new System.Drawing.Point(12, 342);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(171, 162);
            this.button5.TabIndex = 8;
            this.button5.Text = "Grande";
            this.button5.UseVisualStyleBackColor = true;
            // 
            // button6
            // 
            this.button6.FlatAppearance.BorderSize = 0;
            this.button6.Font = new System.Drawing.Font("Verdana", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button6.ForeColor = System.Drawing.SystemColors.ControlText;
            this.button6.Location = new System.Drawing.Point(720, 174);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(171, 162);
            this.button6.TabIndex = 9;
            this.button6.Text = "Con Edulcorante";
            this.button6.UseVisualStyleBackColor = true;
            // 
            // button7
            // 
            this.button7.FlatAppearance.BorderSize = 0;
            this.button7.Font = new System.Drawing.Font("Verdana", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button7.ForeColor = System.Drawing.SystemColors.ControlText;
            this.button7.Location = new System.Drawing.Point(12, 174);
            this.button7.Name = "button7";
            this.button7.Size = new System.Drawing.Size(171, 162);
            this.button7.TabIndex = 10;
            this.button7.Text = "Sin Edulcorante";
            this.button7.UseVisualStyleBackColor = true;
            // 
            // button8
            // 
            this.button8.FlatAppearance.BorderSize = 0;
            this.button8.Font = new System.Drawing.Font("Verdana", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button8.ForeColor = System.Drawing.SystemColors.ControlText;
            this.button8.Location = new System.Drawing.Point(189, 174);
            this.button8.Name = "button8";
            this.button8.Size = new System.Drawing.Size(171, 162);
            this.button8.TabIndex = 11;
            this.button8.Text = "Para Llevar";
            this.button8.UseVisualStyleBackColor = true;
            // 
            // button9
            // 
            this.button9.FlatAppearance.BorderSize = 0;
            this.button9.Font = new System.Drawing.Font("Verdana", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button9.ForeColor = System.Drawing.SystemColors.ControlText;
            this.button9.Location = new System.Drawing.Point(366, 174);
            this.button9.Name = "button9";
            this.button9.Size = new System.Drawing.Size(171, 162);
            this.button9.TabIndex = 12;
            this.button9.Text = "Sin Lactossa";
            this.button9.UseVisualStyleBackColor = true;
            // 
            // button10
            // 
            this.button10.FlatAppearance.BorderSize = 0;
            this.button10.Font = new System.Drawing.Font("Verdana", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button10.ForeColor = System.Drawing.SystemColors.ControlText;
            this.button10.Location = new System.Drawing.Point(543, 174);
            this.button10.Name = "button10";
            this.button10.Size = new System.Drawing.Size(171, 162);
            this.button10.TabIndex = 13;
            this.button10.Text = "Esencia A Parte";
            this.button10.UseVisualStyleBackColor = true;
            // 
            // pnlVisualizador
            // 
            this.pnlVisualizador.Controls.Add(this.txtNotasBeb);
            this.pnlVisualizador.Controls.Add(this.txtProdSelect);
            this.pnlVisualizador.Location = new System.Drawing.Point(897, 6);
            this.pnlVisualizador.Name = "pnlVisualizador";
            this.pnlVisualizador.Size = new System.Drawing.Size(340, 484);
            this.pnlVisualizador.TabIndex = 14;
            // 
            // txtProdSelect
            // 
            this.txtProdSelect.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtProdSelect.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtProdSelect.Location = new System.Drawing.Point(0, 0);
            this.txtProdSelect.Name = "txtProdSelect";
            this.txtProdSelect.Size = new System.Drawing.Size(340, 22);
            this.txtProdSelect.TabIndex = 0;
            // 
            // txtNotasBeb
            // 
            this.txtNotasBeb.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtNotasBeb.Location = new System.Drawing.Point(3, 28);
            this.txtNotasBeb.Multiline = true;
            this.txtNotasBeb.Name = "txtNotasBeb";
            this.txtNotasBeb.Size = new System.Drawing.Size(334, 453);
            this.txtNotasBeb.TabIndex = 1;
            // 
            // btnContinuar
            // 
            this.btnContinuar.Location = new System.Drawing.Point(997, 493);
            this.btnContinuar.Name = "btnContinuar";
            this.btnContinuar.Size = new System.Drawing.Size(162, 43);
            this.btnContinuar.TabIndex = 15;
            this.btnContinuar.Text = "Continuar";
            this.btnContinuar.UseVisualStyleBackColor = true;
            this.btnContinuar.Click += new System.EventHandler(this.btnContinuar_Click);
            // 
            // frmNotasBebidas
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlDark;
            this.ClientSize = new System.Drawing.Size(1249, 598);
            this.Controls.Add(this.pnlNotBebidas);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "frmNotasBebidas";
            this.Text = "frmNotasBebidas";
            this.Load += new System.EventHandler(this.frmNotasBebidas_Load);
            this.pnlNotBebidas.ResumeLayout(false);
            this.pnlCabecera.ResumeLayout(false);
            this.pnlCabecera.PerformLayout();
            this.pnlCentralBebidas.ResumeLayout(false);
            this.pnlVisualizador.ResumeLayout(false);
            this.pnlVisualizador.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlNotBebidas;
        private System.Windows.Forms.Panel pnlCabecera;
        private System.Windows.Forms.Button btnCerrar;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel pnlCentralBebidas;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button btnSLechug;
        private System.Windows.Forms.Button button10;
        private System.Windows.Forms.Button button9;
        private System.Windows.Forms.Button button8;
        private System.Windows.Forms.Button button7;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.Panel pnlVisualizador;
        private System.Windows.Forms.TextBox txtProdSelect;
        private System.Windows.Forms.TextBox txtNotasBeb;
        private System.Windows.Forms.Button btnContinuar;
    }
}