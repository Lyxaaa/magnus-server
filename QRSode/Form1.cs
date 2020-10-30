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
        private int picindex = 0;
        Bitmap[] tiles;
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

        private void ApplyMultiObjectDetectionTM(float threshold = 5000000f)
        {
            try
            {
                var imgScene = new Bitmap(pictureBox1.Image).ToImage<Hls, byte>();
                String timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                String path = "C:\\images\\board\\imgScene_" + timeStamp + ".png";
                imgScene.Save(path);
                
                if (imgScene != null)
                {
                    //use a copy as not to adjust the original image
                    //this adjusts the saturation
                    using (Image<Hls, Byte> Temp = imgScene.Copy())
                    {
                        //Temp[0] += 0;
                        //Temp[1] += 0;
                        Temp[2] += 100;
                        Temp.Save("C:\\images\\board\\imgScene_sat" + timeStamp + ".png");
                        imgScene = Temp.Convert<Hls, byte>();
                    }
                }
                //seperate the image into tiles
                Bitmap imgbitmap = imgScene.AsBitmap();
                System.Drawing.Imaging.PixelFormat format = imgbitmap.PixelFormat;
                int twidth = imgbitmap.Width/8;
                int theight = imgbitmap.Height/8;
                tiles = new Bitmap[64];
                for (int i = 0; i < 8; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        int index = i + (8 * j);
                        RectangleF tileRect = new RectangleF(i * twidth, j * theight, twidth, theight);
                        tiles[index] = imgbitmap.Clone(tileRect, format);
                        //tiles[index].Save("C:\\images\\board\\SatTile_"+ index + "_" + timeStamp + ".png");
                        tiles[index].Save("C:\\images\\board\\SatTile_" + index + "_" + ".png");
                    }
                }

                for (int i = 0; i < tiles.Length; i++) {
                    var tile = tiles[i].ToImage<Bgr, byte>();

                    Mat imgOut = new Mat();
                    
                    //this does the matching useing square difference between the template and the image
                    CvInvoke.MatchTemplate(tile, template, imgOut, Emgu.CV.CvEnum.TemplateMatchingType.Sqdiff);


                    Mat imgOutNorm = new Mat();

                    //we dont want to normalize as that would cause their to always be a "perfect" match
                    //CvInvoke.Normalize(imgOut, imgOutNorm, 0, 1, Emgu.CV.CvEnum.NormType.MinMax, Emgu.CV.CvEnum.DepthType.Cv64F);

                    //creates the matrix that will contain the difference values
                    Matrix<float> matches = new Matrix<float>(imgOut.Size);

                    double minValue = 0, maxVal = 0;
                    Point minLoc = new Point();
                    Point maxLoc = new Point();

                    //fill the match matrix
                    imgOut.CopyTo(matches);
                    //find the min max and associated locations
                    CvInvoke.MinMaxLoc(matches, ref minValue, ref maxVal, ref minLoc, ref maxLoc);
                    //will need to adjust the threshhold value to get good matches
                    if (minValue <= threshold)
                    {
                        Rectangle r = new Rectangle(minLoc, template.Size);
                        CvInvoke.Rectangle(tile, r, new MCvScalar(255, 0, 0), 1);
                        matches[minLoc.Y, minLoc.X] = threshold + 1;
                        matches[maxLoc.Y, maxLoc.X] = threshold + 1;
                        txtQrCode.Text += "Yes tile: " + i + " Sqdiff:" + minValue + System.Environment.NewLine;
                    }
                    else {
                        txtQrCode.Text += "No tile: " + i + " Sqdiff:" + minValue + System.Environment.NewLine;
                    }
                    //tile.Save("C:\\images\\board\\SatTile_matched_" + i + "_" + timeStamp + ".png");
                    tiles[i] = tile.ToBitmap();
                    tile.Save("C:\\images\\board\\SatTile_matched_" + i + ".png");

                }




                //pictureBox3.Image = imgScene.AsBitmap();
                picindex = 0;
                pictureBox3.Image = tiles[picindex];
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

        private void txtQrCode_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void nextbtn_Click(object sender, EventArgs e)
        {
            picindex = (picindex + 1) % 64;
            pictureBox3.Image = tiles[picindex];
        }
    }
}
