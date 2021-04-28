namespace Gy001Tools
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.btGen = new System.Windows.Forms.Button();
            this.tbCount = new System.Windows.Forms.TextBox();
            this.tbGuts = new System.Windows.Forms.TextBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.tbClientGuts = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btGen
            // 
            this.btGen.Location = new System.Drawing.Point(349, 40);
            this.btGen.Name = "btGen";
            this.btGen.Size = new System.Drawing.Size(75, 23);
            this.btGen.TabIndex = 2;
            this.btGen.Text = "生成Id(&G)";
            this.btGen.UseVisualStyleBackColor = true;
            this.btGen.Click += new System.EventHandler(this.btGen_Click);
            // 
            // tbCount
            // 
            this.tbCount.Location = new System.Drawing.Point(243, 40);
            this.tbCount.Name = "tbCount";
            this.tbCount.Size = new System.Drawing.Size(100, 21);
            this.tbCount.TabIndex = 1;
            this.tbCount.Text = "20";
            // 
            // tbGuts
            // 
            this.tbGuts.Location = new System.Drawing.Point(12, 147);
            this.tbGuts.Multiline = true;
            this.tbGuts.Name = "tbGuts";
            this.tbGuts.Size = new System.Drawing.Size(447, 332);
            this.tbGuts.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(166, 43);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(71, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "生成数量(&N)";
            // 
            // tbClientGuts
            // 
            this.tbClientGuts.Location = new System.Drawing.Point(465, 147);
            this.tbClientGuts.Multiline = true;
            this.tbClientGuts.Name = "tbClientGuts";
            this.tbClientGuts.Size = new System.Drawing.Size(364, 332);
            this.tbClientGuts.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 132);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 12);
            this.label2.TabIndex = 0;
            this.label2.Text = "服务器Id";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(465, 132);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(53, 12);
            this.label3.TabIndex = 0;
            this.label3.Text = "客户端Id";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(841, 491);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tbClientGuts);
            this.Controls.Add(this.tbGuts);
            this.Controls.Add(this.tbCount);
            this.Controls.Add(this.btGen);
            this.Name = "Form1";
            this.Text = "制作工具";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btGen;
        private System.Windows.Forms.TextBox tbCount;
        private System.Windows.Forms.TextBox tbGuts;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbClientGuts;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
    }
}

