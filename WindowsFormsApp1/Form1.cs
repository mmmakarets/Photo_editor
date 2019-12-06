using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        Image<Bgr, byte> imgColoredInput;
        Image<Gray, byte> imgGrayInput;

        Image<Bgr, byte> imgColoredOutput;
        Image<Gray, byte> imgGrayOutput;

        List<string> tasks = new List<string>()
        {
            "Рівномірне квантування(моно)",
            "Квантування кольорів по популярності(кольор.)",
            "Квантування медіанним перетином(кольор.)",
            "Бінаризація (моно)",
            "Білий шум (кольор.)",
            "Фільтр Гауса (кольор.)",
            "Фільтр Лапласа (моно)"
        };

        string formatToSave = ".png";
        //string formatToSave = ".jpg";

        // OTHER 
        private readonly int N1 = 256;
        private readonly int N2 = 64;
        private static readonly int N = 16;
        public enum Chanel
        {
            B = 0,
            G,
            R
        }

        public Form1()
        {
            InitializeComponent();
            label1.Text = "";
            label2.Text = "";
            label3.Text = "";
            label4.Text = "";
            comboBox1.Items.AddRange(tasks.ToArray());
        }

        //OPEN
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                histogramBox1.ClearHistogram();
                histogramBox2.ClearHistogram();
                histogramBox3.ClearHistogram();
                histogramBox4.ClearHistogram();
                imageBox3.Image = null;
                imageBox4.Image = null;

                OpenFileDialog ofd = new OpenFileDialog();
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    imgColoredInput = new Image<Bgr, byte>(ofd.FileName);
                    imgGrayInput = imgColoredInput.Clone().Convert<Gray, byte>();

                    Bitmap myBitmap = new Bitmap(ofd.FileName);

                    var originalWidth = imgColoredInput.Width;
                    var originalHeight = imgGrayInput.Height;
                    var originalDepth = Image.GetPixelFormatSize(myBitmap.PixelFormat);
                    var hashSet = new HashSet<int>();

                    for (int i = 0; i < originalHeight; i++)
                    {
                        for (int j = 0; j < originalWidth; j++)
                        {
                            hashSet.Add(imgColoredInput.Data[i, j, 0] * 1000000 + imgColoredInput.Data[i, j, 1] * 1000 + imgColoredInput.Data[i, j, 2]);
                        }
                    }

                    var colorsCount = hashSet.Count();

                    label1.Text = "Width : " + originalWidth;
                    label2.Text = "Height : " + originalHeight;
                    label3.Text = "Depth : " + originalDepth;
                    label4.Text = "Colors : " + colorsCount;

                    Image<Bgr, byte> resizedColoredImage = imgColoredInput.Clone().Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.Inter.Linear);
                    imageBox1.Image = resizedColoredImage;

                    Image<Gray, byte> resizedGrayImage = imgGrayInput.Clone().Resize(imageBox2.Width, imageBox2.Height, Emgu.CV.CvEnum.Inter.Linear);
                    imageBox2.Image = resizedGrayImage;


                    histogramBox1.GenerateHistograms(imgColoredInput, 256);
                    histogramBox1.Refresh();
                    histogramBox2.GenerateHistograms(imgGrayInput, 256);
                    histogramBox2.Refresh();
                }
            }
            catch (Exception ex)
            {

            }
        }

        // Save colored
        private void button2_Click(object sender, EventArgs e)
        {
            if (imgColoredOutput != null)
            {
                imgColoredOutput.Save("coloredResult" + Guid.NewGuid().ToString() + formatToSave);
            }
        }

        // Save gray
        private void button3_Click(object sender, EventArgs e)
        {
            if (imgGrayOutput != null)
            {
                imgGrayOutput.Save("grayResult" + Guid.NewGuid().ToString() + formatToSave);
            }
        }

        // Calc
        private void button4_Click(object sender, EventArgs e)
        {
            if (imgColoredInput != null && comboBox1.SelectedItem != null)
            {
                histogramBox3.ClearHistogram();
                histogramBox3.Refresh();
                histogramBox4.ClearHistogram();
                histogramBox4.Refresh();
                imageBox3.Image = null;
                imageBox4.Image = null;
                imgColoredOutput = null;
                imgGrayOutput = null;
                switch (comboBox1.SelectedItem)
                {
                    case "Рівномірне квантування(моно)":
                        {
                            imgGrayOutput = imgGrayInput.Clone().Convert<Gray, byte>();

                            for (int i = 0; i < imgGrayInput.Height; i++)
                            {
                                for (int j = 0; j < imgGrayInput.Width; j++)
                                {
                                    imgGrayOutput.Data[i, j, 0] = (byte)(imgGrayOutput.Data[i, j, 0] / (float)(N1 / N2));
                                }
                            }

                            Image<Gray, byte> resizedQuantGrayImage = imgGrayOutput.Clone().Resize(imageBox4.Width, imageBox4.Height, Emgu.CV.CvEnum.Inter.Linear);
                            imageBox4.Image = resizedQuantGrayImage;
                            break;
                        }
                    case "Квантування кольорів по популярності(кольор.)":
                        {
                            var originalWidth = imgColoredInput.Width;
                            var originalHeight = imgColoredInput.Height;
                            var dictionary = new Dictionary<int, int>();
                            for (int i = 0; i < originalHeight; i++)
                            {
                                for (int j = 0; j < originalWidth; j++)
                                {
                                    var key = (int)(imgColoredInput.Data[i, j, 0] * 1000000 + imgColoredInput.Data[i, j, 1] * 1000 + imgColoredInput.Data[i, j, 2]);

                                    if (!dictionary.ContainsKey(key))
                                    {
                                        dictionary.Add(key, 0);
                                    }
                                    dictionary[key]++;
                                }
                            }


                            var metaList = dictionary.OrderByDescending(x => x.Value).Take(N2).OrderBy(x => x.Key).ToList();

                            imgColoredOutput = imgColoredInput.Clone();
                            for (int i = 0; i < originalHeight; i++)
                            {
                                for (int j = 0; j < originalWidth; j++)
                                {
                                    var pix = GetClosedPixel(new int[] { imgColoredOutput.Data[i, j, 0], imgColoredOutput.Data[i, j, 1], imgColoredOutput.Data[i, j, 2] }, metaList);

                                    imgColoredOutput.Data[i, j, 0] = (byte)pix[0];
                                    imgColoredOutput.Data[i, j, 1] = (byte)pix[1];
                                    imgColoredOutput.Data[i, j, 2] = (byte)pix[2];
                                }
                            }

                            Image<Bgr, byte> quantResizedImage = imgColoredOutput.Clone().Resize(imageBox3.Width, imageBox3.Height, Emgu.CV.CvEnum.Inter.Linear);
                            imageBox3.Image = quantResizedImage;
                            break;
                        }
                    case "Квантування медіанним перетином(кольор.)":
                        {
                            imgColoredOutput = imgColoredInput.Clone();
                            var originalWidth = imgColoredInput.Width;
                            var originalHeight = imgColoredInput.Height;

                            var palette = new List<byte[]>();
                            for (int i = 0; i < imgColoredInput.Data.GetLength(0); i++)
                            {
                                for (int j = 0; j < imgColoredInput.Data.GetLength(1); j++)
                                {
                                    palette.Add(new byte[] { imgColoredInput.Data[i, j, (int)Chanel.B], imgColoredInput.Data[i, j, (int)Chanel.G], imgColoredInput.Data[i, j, (int)Chanel.R] });
                                }
                            }

                            var palettes = SuperFunction(palette, 0);
                            var newColors = SuperFunction2(palettes);


                            for (int i = 0; i < originalHeight; i++)
                            {
                                for (int j = 0; j < originalWidth; j++)
                                {
                                    var coloredPixel = GetClosedPixel(new byte[] { imgColoredInput.Data[i, j, (int)Chanel.B], imgColoredInput.Data[i, j, (int)Chanel.G], imgColoredInput.Data[i, j, (int)Chanel.R] }, newColors);
                                    imgColoredOutput.Data[i, j, (int)Chanel.B] = coloredPixel[(int)Chanel.B];
                                    imgColoredOutput.Data[i, j, (int)Chanel.G] = coloredPixel[(int)Chanel.G];
                                    imgColoredOutput.Data[i, j, (int)Chanel.R] = coloredPixel[(int)Chanel.R];
                                }
                            }

                            Image<Bgr, byte> quantResizedImage = imgColoredOutput.Clone().Resize(imageBox3.Width, imageBox3.Height, Emgu.CV.CvEnum.Inter.Linear);
                            imageBox3.Image = quantResizedImage;
                            break;
                        }
                    case "Бінаризація (моно)":
                        {
                            imgGrayOutput = imgGrayInput.Clone().Convert<Gray, byte>();
                            for (int i = 0; i < imgColoredInput.Height; i++)
                            {
                                for (int j = 0; j < imgColoredInput.Width; j++)
                                {
                                    var tmp = 0.299 * imgColoredInput.Data[i, j, 2] + 0.587 * imgColoredInput.Data[i, j, 1] + 0.114 * imgColoredInput.Data[i, j, 0];
                                    imgGrayOutput.Data[i, j, 0] = (byte)(tmp > 128 ? 255 : 0);
                                }
                            }
                            Image<Gray, byte> resizedBinImage = imgGrayOutput.Clone().Resize(imageBox4.Width, imageBox4.Height, Emgu.CV.CvEnum.Inter.Linear);
                            imageBox4.Image = resizedBinImage;
                            break;
                        }
                    case "Білий шум (кольор.)":
                        {
                            var rand = new Random();
                            var whitePalette = new Matrix<float>(imgColoredInput.Height, imgColoredInput.Width);
                            for (int i = 0; i < whitePalette.Height; i++)
                            {
                                for (int j = 0; j < whitePalette.Width; j++)
                                {
                                    whitePalette[i, j] = rand.Next(0, 255);
                                }
                            }

                            imgColoredOutput = new Image<Bgr, byte>(imgColoredInput.Width, imgColoredInput.Height);
                            for (int i = 0; i < imgColoredInput.Height; i++)
                            {
                                for (int j = 0; j < imgColoredInput.Width; j++)
                                {
                                    imgColoredOutput.Data[i, j, 0] = (byte)(0.5 * (imgColoredInput.Data[i, j, 0] + whitePalette[i, j]));
                                    imgColoredOutput.Data[i, j, 1] = (byte)(0.5 * (imgColoredInput.Data[i, j, 1] + whitePalette[i, j]));
                                    imgColoredOutput.Data[i, j, 2] = (byte)(0.5 * (imgColoredInput.Data[i, j, 2] + whitePalette[i, j]));
                                }
                            }
                            Image<Bgr, byte> resizedImage = imgColoredOutput.Clone().Resize(imageBox3.Width, imageBox3.Height, Emgu.CV.CvEnum.Inter.Linear);
                            imageBox3.Image = resizedImage;
                            break;
                        }
                    case "Фільтр Гауса (кольор.)":
                        {
                            imgColoredOutput = imgColoredInput.Clone();
                            var size = 5;
                            var kernel = gaussian_kernel(size);

                            for (int i = 0; i < imgColoredInput.Height; i++)
                            {
                                for (int j = 0; j < imgColoredInput.Width; j++)
                                {
                                    var new_red = 0.0;
                                    var new_green = 0.0;
                                    var new_blue = 0.0;

                                    for (int k = 0; k < size; k++)
                                    {
                                        for (int l = 0; l < size; l++)
                                        {
                                            if (i + k < imgColoredInput.Height && j + l < imgColoredInput.Width)
                                            {
                                                new_red += imgColoredInput.Data[i + k, j + l, 2] * kernel[k, l];
                                                new_green += imgColoredInput.Data[i + k, j + l, 1] * kernel[k, l];
                                                new_blue += imgColoredInput.Data[i + k, j + l, 0] * kernel[k, l];
                                            }
                                        }
                                    }

                                    imgColoredOutput.Data[i, j, 0] = (byte)new_blue;
                                    imgColoredOutput.Data[i, j, 1] = (byte)new_green;
                                    imgColoredOutput.Data[i, j, 2] = (byte)new_red;
                                }
                            }
                            Image<Bgr, byte> resizedImage = imgColoredOutput.Clone().Resize(imageBox3.Width, imageBox3.Height, Emgu.CV.CvEnum.Inter.Linear);
                            imageBox3.Image = resizedImage;
                            break;
                        }
                    case "Фільтр Лапласа (моно)":
                        {
                            imgGrayOutput = imgGrayInput.Clone().Convert<Gray, byte>();

                            var kernel = new float[,]
                            {
                                { 1, 1, 1 },
                                { 1, -8, 1 },
                                { 1, 1, 1 }
                            };

                            var kernelSize = 3;

                            for (int image_h = 0; image_h < imgGrayInput.Height; image_h++)
                            {
                                for (int image_w = 0; image_w < imgGrayInput.Width; image_w++)
                                {
                                    double gx_derivative = 0.0;

                                    for (int kern_h = 0; kern_h < kernelSize; kern_h++)
                                    {
                                        for (int kern_w = 0; kern_w < kernelSize; kern_w++)
                                        {
                                            if (image_h - kern_h + 1 >= 0 && image_h - kern_h + 1 < imgGrayInput.Height &&
                                                image_w - kern_w + 1 >= 0 && image_w - kern_w + 1 < imgGrayInput.Width)
                                            {
                                                gx_derivative += kernel[kern_h, kern_w] * imgGrayInput.Data[image_h - kern_h + 1, image_w - kern_w + 1, 0];
                                            }
                                        }
                                    }

                                    double new_val = Math.Abs(gx_derivative);
                                    if (new_val > 255)
                                    {
                                        imgGrayOutput.Data[image_h, image_w, 0] = 255;
                                    }
                                    else
                                    {
                                        imgGrayOutput.Data[image_h, image_w, 0] = (byte)new_val;
                                    }
                                }
                            }
                            Image<Gray, byte> resizedBinImage = imgGrayOutput.Clone().Resize(imageBox4.Width, imageBox4.Height, Emgu.CV.CvEnum.Inter.Linear);
                            imageBox4.Image = resizedBinImage;
                            break;
                        }
                    default:
                        break;
                }
                if (imgColoredOutput != null)
                {
                    histogramBox3.GenerateHistograms(imgColoredOutput, 256);
                    histogramBox3.Refresh();
                }
                if (imgGrayOutput != null)
                {
                    histogramBox4.GenerateHistograms(imgGrayOutput, 256);
                    histogramBox4.Refresh();
                }
            }
        }

        public static int[] GetClosedPixel(int[] pixel, List<KeyValuePair<int, int>> list)
        {
            double dist = int.MaxValue;
            var b = 0;
            var g = 0;
            var r = 0;

            for (int i = 0; i < list.Count; i++)
            {
                var tmp = list[i].Key;

                var newR = tmp % 1000;
                tmp /= 1000;
                var newG = tmp % 1000;
                tmp /= 1000;
                var newB = tmp;

                var newDist = Math.Pow(pixel[0] - newB, 2) + Math.Pow(pixel[1] - newG, 2) + Math.Pow(pixel[2] - newR, 2);
                if (newDist < dist)
                {
                    dist = newDist;
                    b = newB;
                    g = newG;
                    r = newR;
                }
            }

            return new int[] { b, g, r };
        }
        public static byte[] GetClosedPixel(byte[] pixels, List<byte[]> list)
        {
            double dist = int.MaxValue;
            byte[] newColor = new byte[3];

            for (int i = 0; i < list.Count; i++)
            {
                var newDist = Math.Pow(pixels[(int)Chanel.B] - list[i][(int)Chanel.B], 2) +
                    Math.Pow(pixels[(int)Chanel.G] - list[i][(int)Chanel.G], 2) +
                    Math.Pow(pixels[(int)Chanel.R] - list[i][(int)Chanel.R], 2);

                if (newDist < dist)
                {
                    dist = newDist;
                    newColor = list[i];
                }
            }

            return newColor;
        }

        public static List<List<byte[]>> SuperFunction(List<byte[]> palette, int step)
        {
            var newPalettes = new List<List<byte[]>>();

            if (step == Math.Log(N, 2))
            {
                newPalettes.Add(palette);
                return newPalettes;
            }

            var blueRange = palette.Select(x => x[(int)Chanel.B]).Max() - palette.Select(x => x[(int)Chanel.B]).Min();
            var greenRange = palette.Select(x => x[(int)Chanel.G]).Max() - palette.Select(x => x[(int)Chanel.G]).Min();
            var redRange = palette.Select(x => x[(int)Chanel.R]).Max() - palette.Select(x => x[(int)Chanel.R]).Min();

            var ranges = new List<int> { blueRange, greenRange, redRange };
            var chanelWithMaxRange = ranges.FindIndex(x => x == ranges.Max());

            palette = palette.OrderBy(x => x[chanelWithMaxRange]).ToList();

            var palette1 = palette.Take(palette.Count / 2).ToList();
            var palette2 = palette.Skip(palette.Count / 2).ToList();

            newPalettes.AddRange(SuperFunction(palette1, step + 1));
            newPalettes.AddRange(SuperFunction(palette2, step + 1));

            return newPalettes;
        }

        public static List<byte[]> SuperFunction2(List<List<byte[]>> palettes)
        {
            var newColors = new List<byte[]>();
            for (int i = 0; i < palettes.Count; i++)
            {
                newColors.Add(new byte[] {
                    (byte)(palettes[i].Select(x => (int)x[(int)Chanel.B]).ToArray().Sum()/palettes[i].Count),
                    (byte)(palettes[i].Select(x => (int)x[(int)Chanel.G]).ToArray().Sum()/palettes[i].Count),
                    (byte)(palettes[i].Select(x => (int)x[(int)Chanel.R]).ToArray().Sum()/palettes[i].Count)
                });
            }
            return newColors;
        }
        double[,] gaussian_kernel(int size, int sigma = 3)
        {
            List<double> ax = new List<double>();
            var topAx = (size - 1) / 2.0;
            var botAx = -(size - 1) / 2.0;
            for (double i = botAx; i <= topAx; i++)
            {
                ax.Add(i);
            }

            var xx = new double[size, size];
            var yy = new double[size, size];

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    xx[i, j] = Math.Pow(ax[j], 2);
                    yy[i, j] = Math.Pow(ax[i], 2);
                }
            }

            var sum = new double[size, size];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    sum[i, j] = Math.Exp(-0.5 * (xx[i, j] + yy[i, j]) / Math.Pow(sigma, 2));
                }
            }

            var kernSum = 0.0;
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    kernSum += sum[i, j];
                }
            }

            var result = new double[size, size];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    result[i, j] = sum[i, j] / kernSum;
                }
            }

            return result;
        }
    }
}
