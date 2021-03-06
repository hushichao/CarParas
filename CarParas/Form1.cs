﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CarParasBll;

namespace CarParas
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private BackgroundWorker worker = new BackgroundWorker(); //正式做事情的地方


        private async void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
           var oprea1 = new CarParasOperation();
           var task1 = new Task(async () =>
           {
               await oprea1.ImportBasePartas();
           });
           task1.Start();

           var task2 = new Task(async () =>
           {
               await oprea1.ImportOptionPartas();
           });
           task2.Start();

           var task3 = new Task(async () =>
           {
               await oprea1.ImportColorPartas();
           });
           task3.Start();

      
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            var oprea = new CarParasOperation();
            await oprea.GetAutoHomeCarSeriesData();
            button2.Enabled = true;
            MessageBox.Show("成功");

            //var del = new OpDelate( oprea.Test);
            //IAsyncResult ar = del.BeginInvoke( new AsyncCallback(CallbackMethod), del);

        } 


        public delegate void OpDelate();

        void CallbackMethod(IAsyncResult ar)
        {
            OpDelate del = (OpDelate)ar.AsyncState;
       
        }

        private void button3_Click(object sender, EventArgs e)
        {
            int count;
            int.TryParse(textBox1.Text, out count);
            if (count <= 0)
            {
                MessageBox.Show("图片数量不对");
                return;
            }
            var oprea1 = new CarParasOperation();
            var task1 = new Task(async () =>
            {
                await oprea1.ImportAutoImgs(count);
            });
            task1.Start();
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            var oprea1 = new CarParasOperation();
          await  oprea1.ImportCarBrands();
        }

        private void button5_Click(object sender, EventArgs e)
        {

        }


    }
}
