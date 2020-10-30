using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using ZXing;
using Emgu.CV;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.Flann;
using Emgu.CV.Cuda;
using System.Runtime.InteropServices;
using Emgu.CV.CvEnum;
using Emgu.CV.OCR;
using Emgu.CV.UI;

namespace QRSode
{
    public partial class Form1 : Form
    {
        private Image<Hls, byte> template;
        public Form1()
        {
            InitializeComponent();
        }
        FilterInfoCollection filterInfoCollection;
        VideoCaptureDevice videoCaptureDevice;

        private void Form1_Load(object sender, EventArgs e)
        {
            filterInfoCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo Device in filterInfoCollection)
                cboCamera.Items.Add(Device.Name);
            cboCamera.SelectedIndex = 0;
            videoCaptureDevice = new VideoCaptureDevice();


        }

        private void FinalFrame_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            pictureBox1.Image = (Bitmap)eventArgs.Frame.Clone();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (videoCaptureDevice.IsRunning == true)
                videoCaptureDevice.Stop();
        }

        private void btnStart_Click_1(object sender, EventArgs e)
        {
            videoCaptureDevice = new VideoCaptureDevice(filterInfoCollection[cboCamera.SelectedIndex].MonikerString);
            videoCaptureDevice.NewFrame += FinalFrame_NewFrame;
            videoCaptureDevice.Start();
            //timer was used for QR code reading
            //timer1.Start();
        }

        private void timer1_Tick_1(object sender, EventArgs e)
        {
            /* old code for barcode reading
            BarcodeReader Reader = new BarcodeReader();
            if (((Bitmap)pictureBox1.Image) != null)
            {
                Result result = Reader.Decode((Bitmap)pictureBox1.Image);
                if (result != null)
                    txtQrCode.Text = result.ToString();
            }
            */
        }

        private void ApplyMultiObjectDetectionTM(float threshold = 6000000f)
        {
            try
            {
                //var imgScene = imgList["Input"].Clone();
                var imgScene = new Bitmap(pictureBox1.Image).ToImage<Hls, byte>();
                //var template = new Bitmap(pictureBox1.Image).ToImage<Bgr, byte>();
                //var template = new Bitmap(pictureBox2.Image).ToImage<Bgr, byte>();
                String timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                String path = "C:\\images\\board\\imgScene_" + timeStamp + ".png";
                imgScene.Save(path);
                
                if (imgScene != null)
                {
                    //use a copy as not to adjust the original image
                    using (Image<Hls, Byte> Temp = imgScene.Copy())
                    {
                        //Temp[0] += 100;
                        //Temp[1] += 50;
                        Temp[2] += 100;
                        Temp.Save("C:\\images\\board\\imgScene_sat" + timeStamp + ".png");
                        imgScene = Temp.Convert<Hls, byte>();
                    }
                }
 
                

                Mat imgOut = new Mat();
                CvInvoke.MatchTemplate(imgScene, template, imgOut, Emgu.CV.CvEnum.TemplateMatchingType.Sqdiff);
                

                Mat imgOutNorm = new Mat();

                //CvInvoke.Normalize(imgOut, imgOutNorm, 0, 1, Emgu.CV.CvEnum.NormType.MinMax, Emgu.CV.CvEnum.DepthType.Cv64F);

                Matrix<float> matches = new Matrix<float>(imgOut.Size);
                //imgOutNorm.CopyTo(matches);
                //imgOut.CopyTo(matches);
                /*
                for (int i =0; i < matches.Cols;i++) {
                    for (int j = 0; j < matches.Rows;j++) {
                        System.Console.WriteLine(matches[j,i]);
                    }
                }
                */
                double minValue = 0, maxVal = 0;
                Point minLoc = new Point();
                Point maxLoc = new Point();

                CvInvoke.MinMaxLoc(matches, ref minValue, ref maxVal, ref minLoc, ref maxLoc);
                imgOut.CopyTo(matches);
                //imgOutNorm.CopyTo(matches);
                int count = 0;
                CvInvoke.MinMaxLoc(matches, ref minValue, ref maxVal, ref minLoc, ref maxLoc);
                while (minValue <= threshold && count < 16)
                {
                    CvInvoke.MinMaxLoc(matches, ref minValue, ref maxVal, ref minLoc, ref maxLoc);
                    Rectangle r = new Rectangle(minLoc, template.Size);
                    CvInvoke.Rectangle(imgScene, r, new MCvScalar(255, 0, 0), 1);
                    matches[minLoc.Y, minLoc.X] = threshold+1;
                    matches[maxLoc.Y, maxLoc.X] = threshold+1;
                    count++;

                }
                pictureBox3.Image = imgScene.AsBitmap();
                timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                String path2 = "C:\\images\\board\\match_" + timeStamp + ".png";
                imgScene.Save(path2);

                


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void tempbtn_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    template = new Image<Hls, byte>(dialog.FileName);
                    pictureBox2.Image = template.AsBitmap();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void detectbtn_Click(object sender, EventArgs e)
        {
            ApplyMultiObjectDetectionTM();
        }
    }
}
