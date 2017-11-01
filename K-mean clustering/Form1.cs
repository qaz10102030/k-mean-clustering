using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using Accord.Math;
using Accord.Statistics;

namespace K_mean_clustering
{
    public struct Cluster
    {
        public Color pixel { set; get; }
        public int cluster { set; get; }
    }
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Form.CheckForIllegalCrossThreadCalls = false;
        }
        OpenFileDialog file = new OpenFileDialog();
        Random rnd = new Random(Guid.NewGuid().GetHashCode());
        Bitmap myPic;
        List<Cluster> originColor = new List<Cluster>();
        List<int> originR = new List<int>();
        List<int> originG = new List<int>();
        List<int> originB = new List<int>();
        Stopwatch sw = new Stopwatch();
        double[,] S;

        private void Work(List<Cluster> originColor, List<Color> originCluster, int K)
        {
            List<Cluster> resultBitmap = new List<Cluster>();
            List<Color> resultCluster = new List<Color>();
            int stage = 1;
            bool isMeanChange = true;
            while (isMeanChange)
            {
                    listBox1.Items.Add("");
                    listBox1.Items.Add("----------第" + stage +"階段開始----------");
                    listBox1.Items.Add("分類階段");
                    listBox1.TopIndex = listBox1.Items.Count - 1;
                    sw.Reset();
                    sw.Start();
                var tupleObj = clustering(originColor, originCluster, K);
                originColor = tupleObj.Item1;
                resultBitmap = tupleObj.Item2;
                int bitmapChange = tupleObj.Item3;
                    sw.Stop();
                    listBox1.Items.Add("分類耗時：" + sw.Elapsed.TotalSeconds.ToString() + "s");
                if(bitmapChange > 0)
                    listBox1.Items.Add("圖片有改變，共改變" + bitmapChange + "個像素點");
                else
                    listBox1.Items.Add("圖片無改變");
                    listBox1.TopIndex = listBox1.Items.Count - 1;
                    listBox1.Items.Add("畫圖階段");
                    listBox1.TopIndex = listBox1.Items.Count - 1;
                    sw.Reset();
                    sw.Start();
                drawBitmap(resultBitmap);
                    sw.Stop();
                    listBox1.Items.Add("畫圖耗時：" + sw.Elapsed.TotalSeconds.ToString() + "s");
                    listBox1.Items.Add("尋找新的平均值");
                    listBox1.TopIndex = listBox1.Items.Count - 1;
                resultCluster = findMean(originColor, originCluster, K);
                    listBox1.Items.Add("新的平均值");
                    listBox1.TopIndex = listBox1.Items.Count - 1;
                for (int i = 0; i < K; i++)
                {
                    Color temp = resultCluster[i];
                        listBox1.Items.Add("K[" + i + "]\tR = " + temp.R + "\tG = " + temp.G + "\tB = " + temp.B);
                        listBox1.TopIndex = listBox1.Items.Count - 1;
                }
                    listBox1.Items.Add("判斷平均值是否有改變...");
                    listBox1.TopIndex = listBox1.Items.Count - 1;
                int meanChange = 0;
                for (int i = 0; i < originCluster.Count(); i++)
                {
                    Color origin = originCluster[i];
                    Color result = resultCluster[i];
                    if (origin.Equals(result))
                        meanChange++;
                }
                if (meanChange == K)
                {
                        listBox1.Items.Add("無改變，分類完成!!!!");
                        listBox1.TopIndex = listBox1.Items.Count - 1;
                    isMeanChange = false;
                    button2.Enabled = true;
                }
                else {
                        listBox1.Items.Add("有" + (K - meanChange) + "項改變，重新分類...");
                        listBox1.TopIndex = listBox1.Items.Count - 1;
                    originCluster = resultCluster;
                }
                listBox1.Items.Add("----------第" + stage + "階段結束----------");
                listBox1.TopIndex = listBox1.Items.Count - 1;
                stage++;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            file.Filter = "圖片檔|*.jpg;*.png";
            file.ShowDialog();
            if (file.FileName != "")
            {
                originColor.Clear();
                myPic = new Bitmap(file.FileName.ToString());
                pictureBox1.BackgroundImageLayout = ImageLayout.Stretch;
                pictureBox1.BackgroundImage = myPic;

                //取圖片像素
                int width = myPic.Width;
                int height = myPic.Height;
                for (int i = 1; i < width; i++)
                {
                    for (int j = 1; j < height; j++)
                    {
                        Color temp = myPic.GetPixel(i, j);
                        originColor.Add(new Cluster {pixel = temp,cluster = -1});
                    }
                }
                button2.Enabled = true;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            pictureBox2.BackgroundImage = null;
            if (textBox1.Text != "" && IsNumeric(textBox1.Text))
            {
                List<Color> originCluster = new List<Color>();
                listBox1.Items.Clear();
                button2.Enabled = false;
                int K = int.Parse(textBox1.Text);
                listBox1.Items.Add("初始化階段隨機找樣本內的顏色當平均值");
                //隨機從bitmap裡找K個顏色當中心
                for (int i = 0; i < K; i++)
                {
                    Color temp = originColor[rnd.Next(0, originColor.Count())].pixel;
                    originCluster.Add(temp);
                    listBox1.Items.Add("K[" + i + "]\tR = " + temp.R + "\tG = " + temp.G + "\tB = " + temp.B);
                    listBox1.TopIndex = listBox1.Items.Count - 1;
                }
                if (radioButton1.Checked)
                    listBox1.Items.Add("使用<歐式距離>計算距離");
                else if (radioButton2.Checked)
                {
                    listBox1.Items.Add("使用<馬式距離>計算距離");
                    listBox1.TopIndex = listBox1.Items.Count - 1;
                    originR.Clear();
                    originG.Clear();
                    originB.Clear();
                    double varR, varG, varB, covRG, covRB, covGB;
                    for (int i = 0; i < originColor.Count; i++)
                    {
                        originR.Add(originColor[i].pixel.R);
                        originG.Add(originColor[i].pixel.G);
                        originB.Add(originColor[i].pixel.B);
                    }
                    int[] R = new int[256];
                    int[] G = new int[256];
                    int[] B = new int[256];
                    for (int i = 0; i < originR.Count; i++)
                    {
                        R[originR[i]]++;
                    }
                    for (int i = 0; i < originG.Count; i++)
                    {
                        G[originG[i]]++;
                    }
                    for (int i = 0; i < originB.Count; i++)
                    {
                        B[originB[i]]++;
                    }

                    varR = calcVariance(R, originColor.Count);
                    varG = calcVariance(G, originColor.Count);
                    varB = calcVariance(B, originColor.Count);
                    covRG = calcCovariance(R, G, originColor.Count);
                    covRB = calcCovariance(R, B, originColor.Count);
                    covGB = calcCovariance(G, B, originColor.Count);
                    S = new double[,]
                    {
                        { varR  , covRG , covRB },
                        { covRG , varG  , covGB },
                        { covRB , covGB , varB  }
                    };
                }
                ThreadStart starter = () => Work(originColor, originCluster, K);
                Thread doWork = new Thread(starter);
                doWork.Start();
            }
        }

        private static bool IsNumeric(string TextBoxValue)
        {
            try
            {
                int i = Convert.ToInt32(TextBoxValue);
                return true;
            }
            catch
            {
                try
                {
                    double i = Convert.ToDouble(TextBoxValue);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        private double calcVariance(int[] array,int count)
        {
            double calcMean = 0.0;
            double squareMean = 0.0;
            for (int i = 0; i <= 255; i++)
            {
                calcMean += i * (array[i] / (double)count);
            }
            for (int i = 0; i <= 255; i++)
            {
                squareMean += Math.Pow(i, 2) * (array[i] / (double)count);
            }
            return squareMean - Math.Pow(calcMean, 2);
        }

        private double calcCovariance(int[] array1, int[] array2, int count)
        {
            double calcMean1 = 0.0, calcMean2 = 0.0;
            for (int i = 0; i <= 255; i++)
            {
                calcMean1 += i * (array1[i] / (double)count);
                calcMean2 += i * (array2[i] / (double)count);
            }
            double cov = 0.0;
            for (int i = 0; i <= 255; i++)
            {
                cov += ((array1[i] - calcMean1) * (array2[i] - calcMean2)) / count;
            }

            return cov;
        }

        private double calcEuclideanDistance(Color c1, Color c2)
        {
            return Math.Sqrt(Math.Pow((c1.R - c2.R), 2) + Math.Pow((c1.G - c2.G), 2) + Math.Pow((c1.B - c2.B), 2));
        }

        private double calcMahalanobisDistance(Color c1, Color c2)
        {
            double res = 0.0;
            var matrix = new double[,] {
                { c1.R - c2.R},
                { c1.G - c2.G},
                { c1.B - c2.B}
            };
            var transMatrix = matrix.Transpose();
            var inversS = S.Inverse();
            var one = Matrix.Multiply(transMatrix, inversS);
            var two = Matrix.Multiply(one, matrix);
            return two[0,0];
        }

        private Tuple<List<Cluster> , List<Cluster>, int> clustering(List<Cluster> orginBitmap, List<Color> originCluster, int K)
        {
            //算圖中每個點跟樣本點的距離，並找最短
            List<Cluster> resultBitmap = new List<Cluster>();
            object test = new object();
            int bitmapChange = 0;
            double[] dis = new double[K];            
            
            for (int i = 0; i < orginBitmap.Count(); i++)
            {
                Cluster temp = orginBitmap[i];
                Color bitmap = temp.pixel;
                for (int j = 0; j < K; j++)
                {
                    Color cluster = originCluster[j];
                    if (radioButton1.Checked)
                        dis[j] = calcEuclideanDistance(bitmap, cluster);
                    else if (radioButton2.Checked)
                        dis[j] = calcMahalanobisDistance(bitmap, cluster);
                }
                int index = Array.IndexOf(dis, dis.Min());
                temp.cluster = index;
                if (index != orginBitmap[i].cluster)
                    bitmapChange++;
                orginBitmap[i] = temp;
                resultBitmap.Add(new Cluster { pixel = originCluster[index], cluster = index });
            }
            
            return new Tuple<List<Cluster>, List<Cluster>, int>(orginBitmap, resultBitmap,bitmapChange);
        }

        private void drawBitmap(List<Cluster> resultBitmap)
        {
            //畫回去
            int width = myPic.Width;
            int height = myPic.Height;
            Bitmap tempBitmap = new Bitmap(width, height);
            int count = 0;
            for (int i = 1; i < width; i++)
            {
                for (int j = 1; j < height; j++)
                {
                    tempBitmap.SetPixel(i, j, resultBitmap[count].pixel);
                    count++;
                }
            }
            pictureBox2.BackgroundImageLayout = ImageLayout.Stretch;
            pictureBox2.BackgroundImage = tempBitmap;
        }

        private List<Color> findMean(List<Cluster> originColor, List<Color> originCluster, int K)
        {
            List<List<Color>> temp = new List<List<Color>>();
            for (int i = 0; i < K; i++)
            {
                List<Color> temp2 = new List<Color>();
                for (int j = 0; j < originColor.Count(); j++)
                {
                    if(originColor[j].cluster == i)
                    {
                        temp2.Add(originColor[j].pixel);
                    }
                }
                temp.Add(temp2);
            }

            List<Color> resultCluster = new List<Color>();
            for (int i = 0; i < temp.Count(); i++)
            {
                List<Color> temp2 = temp[i];
                int R = 0, G = 0, B = 0,count = temp2.Count();
                for (int j = 0; j < count; j++)
                {
                    R += temp2[j].R;
                    G += temp2[j].G;
                    B += temp2[j].B;
                }
                if (count != 0)
                {
                    int resR = R / count;
                    int resG = G / count;
                    int resB = B / count;
                    Color newMean = Color.FromArgb(resR, resG, resB);
                    resultCluster.Add(newMean);
                }
                else
                {
                    resultCluster.Add(originCluster[i]);
                }
            }
            return resultCluster;
        }
    }
}
