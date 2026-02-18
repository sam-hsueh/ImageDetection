using ImageFeatureDetection.Domain;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using Utilities;
using static TorchSharp.torch.distributions.constraints;
using Color = System.Drawing.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using ColorW = System.Windows.Media.Color;
using MessageBox = System.Windows.Forms.MessageBox;
using Pen = System.Drawing.Pen;
using Point = System.Drawing.Point;
using Window = System.Windows.Window;
namespace ImageFeatureDetection
{
    /// <summary>
    /// Interaction logic for MainWindoRWidth.xaml
    /// </summary>
    public partial class Home : System.Windows.Controls.UserControl
    {
        MainWindowViewModel? mwv;
        string curDir = "";
        Bitmap sourceBitmap, OrignalBitmap;
        private string modelPath;
        bool MousePress = false;
        Pen gPen = new Pen(Color.Maroon, 1);
        Pen rPen = new Pen(Color.Red, 2);
        public Home()
        {
            InitializeComponent();
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 50;
            timer.Tick += new System.EventHandler(timer_Tick);
            Loaded += Home_Loaded;
            curDir = Properties.Settings.Default.InitDir;
            MaxF = Properties.Settings.Default.MaxF;
        }
      
        Color GetAlphaColor(Color color,int alpha)
        {
            return Color.FromArgb(alpha, color);
        }
        public void Home_Loaded(object sender, RoutedEventArgs e)
        {
            Window? window = Window.GetWindow(this);
            mwv = this.DataContext as MainWindowViewModel;
            mwv!.mainW = this;
            curDir = Properties.Settings.Default.InitDir;
            DataGrid_SelectionChanged(null, null);
            timer.Start();
        }
        System.Windows.Forms.Timer timer;
        public int ctime = 5;
        private void timer_Tick(object? sender, EventArgs e)
        {
            if (ctime > 0)
                DrawF();
            ctime--;
            if (ctime < 0)
            {
                timer.Stop();
                ctime = 5;
            }
        }

        private void Home_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
            GC.Collect();
            System.Windows.Application.Current.Shutdown();
            Environment.Exit(0);
        }


        public static ColorW[]? colors { get; set; } = new ColorW[] { (ColorW)ColorConverter.ConvertFromString("#FF3F51B5"), (ColorW)ColorConverter.ConvertFromString("#FF3A7E00"), (ColorW)ColorConverter.ConvertFromString("#FFB00020") };

       
        public static ObservableCollection<SelectableFiles> FileList
        {
            set;
            get;
        } = new ObservableCollection<SelectableFiles>();
        ObservableCollection<string> CatList
        {
            set;
            get;
        } = new ObservableCollection<string>();


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog openolderDialog = new FolderBrowserDialog();
            if (curDir == "")
                curDir = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            openolderDialog.InitialDirectory = curDir;
            var result = openolderDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                sourceBitmap = null;
                curDir = openolderDialog.SelectedPath;
                OpenFolder();
            }
            else
                return;
        }

        void OpenFolder()
        {
            string path = curDir;
            try
            {
                FileList.Clear();
                foreach (string Path in Directory.GetFiles(path))
                {
                    var PExt = new FileInfo(Path).Extension.ToLower();
                    if (PExt == ".jpg" || PExt == ".bmp" || PExt == ".png") //筛选图片格式
                    {
                        var fi = new FileInfo(Path);
                        FileList.Add(
                            new SelectableFiles() { FileName = fi.Name, Directory = fi.Directory.FullName }
                            );
                    }
                }
                mwv!.FileList = FileList;
                if (FileList.Count > 0)
                {
                    FileListGrid.SelectedIndex = 0;
                    //FileName = (string)FileListGrid.Items[FileListGrid.SelectedIndex];
                    for (int i = 0; i < FileList.Count; i++)
                    {
                        var imgFile = curDir + @"\Project\DataSets\Images\Train\"+ FileList[i].FileName;
                        var labelFile = curDir + @"\Project\DataSets\Labels\Train\" + FileList[i].FileName.Replace(new FileInfo(FileList[i].FileName).Extension.ToLower(), ".txt");
                        if (File.Exists(imgFile) && File.Exists(labelFile))
                        {
                            mwv!.FileList[i].IsSelected = true;
                        }
                    }
                }
                mwv!.OriginalImageDir = curDir;
                mwv!.ProjectDir = curDir + @"\Project";
            }
            catch(Exception ex) { throw ex; };
        }
        string FileName = "";
        bool hovarSP = false;
        List<Point> curPS;
        static object _object = new object();
        float labelw = 22f;
        StringFormat sf = new StringFormat();
        float fontsize = 12f;
        float fontsize2 = 22f;
        int margin = 40;
        float rate = 15f;
        float wratio = 1.0f, hratio = 1.0f;
        float rw = 8;//样本输出宽度
        int dw = 20;//显示宽度
        public void DrawF(int x = 0, int y = 0)
        {
            if (sourceBitmap == null)
                return;
            lock (_object)
            {
                Bitmap bm = new Bitmap(displayBitmap);
                using (Graphics g = Graphics.FromImage(bm))
                {
                    // Graphics g = bufferedGraphics.Graphics;
                    //Graphics g = Graphics.FromHwnd(GWpf.Handle);
                    //Bitmap bmp = new Bitmap(GWpf.Image, GWpf.Width, GWpf.Height);
                    //Graphics g = Graphics.FromImage(bmp);
                   
                    //var s = mwv!.FeatureList[i].FPoints;
                    //int cat = 0;
                    Color c = PrimaryColor[0];
                    Color b = PrimaryColor[12];
                    for (int i = 0; i < mwv!.FeatureList.Count; i++)
                    {
                        if (mwv!.FeatureList[i].FPoints.Count == 1)
                            if (SelectedFP == i)
                            {
                                g.FillRectangle(new SolidBrush(GetAlphaColor(c, 100)), new Rectangle(mwv!.FeatureList[i].FPoints[0].X - dw, mwv!.FeatureList[i].FPoints[0].Y - dw, 2 * dw, 2 * dw));
                                g.FillEllipse(new SolidBrush(Color.Yellow), new Rectangle(mwv!.FeatureList[i].FPoints[0].X - 3, mwv!.FeatureList[i].FPoints[0].Y - 3, 6, 6));
                            }
                            else if (HoverFP == i)
                            {
                                g.FillRectangle(new SolidBrush(GetAlphaColor(c, 100)), new Rectangle(mwv!.FeatureList[i].FPoints[0].X - dw, mwv!.FeatureList[i].FPoints[0].Y - dw, 2 * dw, 2 * dw));
                                g.FillEllipse(new SolidBrush(Color.Yellow), new Rectangle(mwv!.FeatureList[i].FPoints[0].X - 3, mwv!.FeatureList[i].FPoints[0].Y - 3, 6, 6));
                            }
                            else
                            {
                                g.FillRectangle(new SolidBrush(GetAlphaColor(b, 150)), new Rectangle(mwv!.FeatureList[i].FPoints[0].X - dw, mwv!.FeatureList[i].FPoints[0].Y - dw, 2 * dw, 2 * dw));
                                g.FillEllipse(new SolidBrush(Color.YellowGreen), new Rectangle(mwv!.FeatureList[i].FPoints[0].X - 3, mwv!.FeatureList[i].FPoints[0].Y - 3, 6, 6));
                            }
                    }
                }
                DrawToGraphics(bm);
            }
            GC.Collect();
        }
        private unsafe void DrawToGraphics(Bitmap bm)
        {
            lock (GWpf.Lock)
            {
                GWpf.GFX.SmoothingMode = SmoothingMode.AntiAlias;
                GWpf.GFX.SmoothingMode = SmoothingMode.HighQuality;
                GWpf.GFX.CompositingQuality = CompositingQuality.HighQuality;
                GWpf.GFX.PixelOffsetMode = PixelOffsetMode.HighQuality;
                Graphics g = GWpf.GFX;
                if (bm == null)
                    return;
                g.Clear(Color.White);
                g.DrawImage(bm, 0, 0);
                GWpf.Paint();
            }
        }

        
        StringBuilder sb = new StringBuilder();
      

        private void DataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (FileListGrid.SelectedIndex > -1)
            {
                try
                {
                    int k = FileListGrid.SelectedIndex;
                    FileName = mwv!.FileList[k].FileName;
                    string FilePath = curDir + @"\" + FileName;
                    StreamReader streamReader = new StreamReader(FilePath);
                    OrignalBitmap = (Bitmap)Bitmap.FromStream(streamReader.BaseStream);
                    streamReader.Close();

                    var image = Cv2.ImRead(FilePath);

                    int width = (int)GWpf.Width;
                    int height = (int)GWpf.Height;
                    if (width < 0)
                        return;
                    wratio = 1.0f;
                    hratio = 1.0f;
                    //if (SketchImg.IsChecked == true)
                    {
                        wratio = (float)((float)OrignalBitmap.Width / (float)width);
                        hratio = (float)((float)OrignalBitmap.Height / (float)height);
                        sourceBitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                        sourceBitmap.SetResolution(OrignalBitmap.HorizontalResolution, OrignalBitmap.VerticalResolution);
                        Graphics graphic = Graphics.FromImage(sourceBitmap);
                        graphic.SmoothingMode = SmoothingMode.HighQuality;
                        graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphic.DrawImage(OrignalBitmap, new Rectangle(0, 0, width, height));
                        graphic.Dispose();
                    }
                    displayBitmap = BitmapAdjust(sourceBitmap, (float)Brightness.Value / 100f, (float)(Contrast.Value) / 100f);
                    richtextBox1.Document.Blocks.Clear();
                    richtextBox1.AppendText("原图：" + OrignalBitmap.Width + " X " + OrignalBitmap.Height + " X " + System.Drawing.Image.GetPixelFormatSize(OrignalBitmap.PixelFormat) / 8);
                    richtextBox1.AppendText("\r\n现图：" + sourceBitmap.Width + " X " + sourceBitmap.Height + " X " + System.Drawing.Image.GetPixelFormatSize(sourceBitmap.PixelFormat) / 8);
                    if (mwv!.FeatureList.Count > 0)
                    {
                        bool isOut = true;
                        for (int i = 0; i < mwv!.FeatureList.Count; i++)
                        {
                            if (mwv!.FeatureList[i].FPoints == null || mwv!.FeatureList[i].FPoints.Count == 0)
                            {
                                isOut = true;
                                break;
                            }
                            var p1x = (mwv!.FeatureList[i].FPoints[0].X);
                            var p1y = (mwv!.FeatureList[i].FPoints[0].Y);
                            var p2x = (mwv!.FeatureList[i].FPoints[0].X);
                            var p2y = (mwv!.FeatureList[i].FPoints[0].Y);
                            var cx = (p1x + p2x) / 2.0 * OrignalBitmap.Width / wratio;
                            var cy = (p1y + p2y) / 2.0 * OrignalBitmap.Height / hratio;
                            var ps = new Point[] { new Point((int)(cx), (int)(cy)) };

                            if (cx < dw || cx > sourceBitmap.Width - dw || cy < dw || cy > sourceBitmap.Height - dw)
                            {
                                isOut = false;
                            }
                        }
                        if (isOut)
                        {
                            mwv!.FeatureList.Clear();
                        }
                    }

                    var labelFile = curDir + @"\Project\DataSets\Labels\Train\" + FileList[k].FileName.Replace(new FileInfo(FileList[k].FileName).Extension.ToLower(), ".txt");                    
                    if (File.Exists(labelFile))
                    {
                        mwv!.FeatureList.Clear();
                        using (StreamReader sr = File.OpenText(labelFile))
                        {
                            string s = "";
                            while ((s = sr.ReadLine()) != null)
                            {
                                string[] arr = s.Split(' ');
                                if (arr.Length == 5)
                                {
                                    int cat = int.Parse(arr[0]);
                                    var p1x = (double.Parse(arr[1]));
                                    var p1y = (double.Parse(arr[2]));
                                    var p2x = (double.Parse(arr[3]));
                                    var p2y = (double.Parse(arr[4]));
                                    var cx = (p1x + p2x) / 2.0 * OrignalBitmap.Width / wratio;
                                    var cy = (p1y + p2y) / 2.0 * OrignalBitmap.Height / hratio;
                                    var ps = new Point[] { new Point((int)(cx), (int)(cy)) };
                                    mwv!.FeatureList.Add(new SelectableFeature() { FPoints = ps.ToList(), Shape = 0, Cat = cat, Description = "Rectangle" });
                                }
                            }
                            //       FeaturesList.SelectedIndex = 0;
                        }
                    }
                    else
                    {
                        mwv!.FeatureList.Clear();
                    }
                    if (mwv!.FeatureList != null && mwv!.FeatureList.Count > Properties.Settings.Default.FeatureCount)
                        for (int i = mwv!.FeatureList.Count - 1; i >= Properties.Settings.Default.FeatureCount; i--)
                        {
                            mwv!.FeatureList.RemoveAt(i);
                        }
                    DrawF();
                }
                catch(Exception ex )
                {
                    //if (FileListGrid.SelectedIndex < FileListGrid.Items.Count - 1)
                    //    FileListGrid.SelectedIndex++;
                }
            }
        }
        void SaveFeatureFile()
        {
            if (sourceBitmap == null || mwv!.FeatureList.Count == 0 || FileName == "")
                return;
            string ProjectDataPath = (curDir + @"\Project\DataSets");
            string imgPath = ProjectDataPath + @"\Images\Train\";
            string labelPath = ProjectDataPath + @"\Labels\Train\";
            if (!Directory.Exists(imgPath))
                Directory.CreateDirectory(imgPath);
            if (!Directory.Exists(labelPath))
                Directory.CreateDirectory(labelPath);
            int d = FileName.LastIndexOf('.');
            var Name = FileName.Substring(0, d);
            var Ext = FileName.Substring(d + 1);
            var oimgPath = curDir + @"\" + FileName;
            var nimgPath = imgPath + @"\" + FileName;

            int W = OrignalBitmap.Width;
            int H = OrignalBitmap.Height;
            File.Copy(oimgPath, nimgPath, true);
            var lFile = labelPath + @"\" + Name + ".txt";
            var rateX = wratio / W;
            var rateY = hratio / H;
            List<Point2f> points = new System.Collections.Generic.List<Point2f>();
            sb = new StringBuilder();
            {
                for (int i = 0; i < Math.Min(mwv!.FeatureList.Count, Properties.Settings.Default.FeatureCount); i++)
                {
                    sb.Append(i + " ");
                    var cx = mwv!.FeatureList[i].FPoints[0].X * rateX;
                    var cy = mwv!.FeatureList[i].FPoints[0].Y * rateY;
                    var x1 = cx - rw / W;
                    var x2 = cx + rw / W;
                    var nx = (x1 + x2) / 2.0 / rateX;
                    var y1 = cy - rw / H;
                    var y2 = cy + rw / H;
                    var ny = (y1 + y2) / 2.0 / rateY;
                    points.Add(new Point2f(cx, cy));
                    sb.Append(x1.ToString("F9") + " " + y1.ToString("F9") + " " + x2.ToString("F9") + " " + y2.ToString("F9") + "\n");
                }
                using (TextWriter textWriter = new StreamWriter(new FileStream(lFile, FileMode.Create, FileAccess.Write, FileShare.Write, 4096, true), System.Text.Encoding.Default))
                {
                    textWriter.Write(sb.ToString());
                }
                mwv!.FileList[FileListGrid.SelectedIndex].IsSelected = true;
            }
            if (isRot.IsChecked == true)
            {
                Mat rm = Cv2.ImRead(oimgPath);
                int angs = Convert.ToInt32(angles.Text);
                for (int j = -angs; j <= angs; j++)
                {
                    if (j == 0)
                        continue;
                    Mat M = Cv2.GetRotationMatrix2D(new Point2f(W / 2f, H / 2f), j, 1.0);
                    Mat nimg = new Mat();
                    Cv2.WarpAffine(rm, nimg, M, new OpenCvSharp.Size(W, H));
                    nimgPath = imgPath + @"\" + Name + "_" + j + "." + Ext;
                    lFile = labelPath + @"\" + Name + "_" + j + "." + "txt";
                    Cv2.ImWrite(nimgPath, nimg);
                    var st = Math.Sin(j / 57.29578);
                    var ct = Math.Cos(j / 57.29578);
                    sb = new StringBuilder();
                    {
                        for (int i = 0; i < Math.Min(mwv!.FeatureList.Count, Properties.Settings.Default.FeatureCount); i++)
                      // for(int i = 0; i < points.Count; i++) 
                        {
                            sb.Append(i + " ");
                            var cx = mwv!.FeatureList[i].FPoints[0].X * rateX;
                            var cy = mwv!.FeatureList[i].FPoints[0].Y * rateY;
                            var ncx = ct * (cx - 0.5) + st * (cy - 0.5) + 0.5;
                            var ncy = -st * (cx - 0.5) + ct * (cy - 0.5) + 0.5;

                            var x1 = ncx - rw / W;
                            var x2 = ncx + rw / W;
                            var y1 = ncy - rw / H;
                            var y2 = ncy + rw / H;
                            //var x1 = points[i].Item1.X ;
                            //var x2 = points[i].Item2.X;
                            //var y1 = points[i].Item1.Y;
                            //var y2 = points[i].Item2.Y;
                            //var nx1 = ct * (x1 - 0.5) + st * (y1 - 0.5) + 0.5;
                            //var ny1 = st * (x1 - 0.5) - ct * (y1 - 0.5) + 0.5;
                            //var nx2 = ct * (x2 - 0.5) + st * (y2 - 0.5) + 0.5;
                            //var ny2 = st * (x2 - 0.5) - ct * (y2 - 0.5) + 0.5;
                            sb.Append(x1.ToString("F9") + " " + y1.ToString("F9") + " " + x2.ToString("F9") + " " + y2.ToString("F9") + "\n");
                        }
                        using (TextWriter textWriter = new StreamWriter(new FileStream(lFile, FileMode.Create, FileAccess.Write, FileShare.Write, 4096, true), System.Text.Encoding.Default))
                        {
                            textWriter.Write(sb.ToString());
                        }
                    }
                }
            }
            mwv!.FileList[FileListGrid.SelectedIndex].IsSelected = true;
        }
        public string ImageToBase64(string imgpath)
        {
            using (System.Drawing.Image image = System.Drawing.Image.FromFile(imgpath))
            {
                using (MemoryStream m = new MemoryStream())
                {
                    image.Save(m, image.RawFormat);
                    byte[] imageBytes = m.ToArray();
                    var base64String = Convert.ToBase64String(imageBytes);
                    return base64String;
                }
            }
        }
        static string ConvertImageToBase64(string imagePath)
        {
            byte[] imageBytes = File.ReadAllBytes(imagePath);
            return Convert.ToBase64String(imageBytes);
        }
        public static string ToPixelBuffer2(string FilePath)
        {
            Bitmap bmp = (Bitmap)Bitmap.FromFile(FilePath);
            BitmapData sourceData =
                       bmp.LockBits(new Rectangle(0, 0,
                       bmp.Width, bmp.Height),
                       ImageLockMode.ReadOnly,
                       System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                       //bmp.PixelFormat);
            byte[] pixelBuffer = new byte[sourceData.Stride *
                                          sourceData.Height];

            Marshal.Copy(sourceData.Scan0, pixelBuffer, 0,
                                       pixelBuffer.Length);

            bmp.UnlockBits(sourceData);
            return Convert.ToBase64String(pixelBuffer);
        }

        public static byte[] ToPixelBuffer(string FilePath)
        {
            Bitmap bmp = (Bitmap)Bitmap.FromFile(FilePath);
            BitmapData sourceData =
                       bmp.LockBits(new Rectangle(0, 0,
                       bmp.Width, bmp.Height),
                       ImageLockMode.ReadOnly,
                       bmp.PixelFormat);
            int Channels = System.Drawing.Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
            byte[] pixelBuffer = new byte[sourceData.Width * Channels * sourceData.Height];
            unsafe
            {
                // base pointers
                byte* src = (byte*)sourceData.Scan0.ToPointer();
                fixed (byte* dst = pixelBuffer)
                {
                    int sourceStride = sourceData.Stride;
                    for (int y = 0; y < sourceData.Height; y++)
                    {
                        byte* s = (byte*)(src + y * sourceStride);
                        byte* d = (byte*)(dst + y * sourceData.Width * Channels);

                        for (int x = 0; x < sourceData.Width; x++, s += Channels, d += Channels)
                        {
                            for (int i = 0; i < Channels; i++)
                            {
                                d[i] = s[i];
                            }
                        }
                    }
                }
            }
            bmp.UnlockBits(sourceData);
            return pixelBuffer;
            //return Convert.ToBase64String(pixelBuffer);
        }

        private void pictureBox1_SizeChanged(object sender, EventArgs e)
        {
            DataGrid_SelectionChanged(null, null);
        }

        private void FeaturesList_SelectedIndexChanged(object sender, EventArgs e)
        {
            //if (mwv!.FeatureList.Count > 0 && FeaturesList.SelectedItems.Count > 0)
            //{
            //    SelectedF = FeaturesList.SelectedIndices[0];
            //    if (mwv!.FeatureList[SelectedF].Shape == 0)
            //        BRect.IsChecked = true;
            //    else
            //        BPolygon.IsChecked = true;
            //}
            //else
            //{
            //    SelectedF = -1;
            //}
            //DrawF();
        }

        private void Form_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            //if (e.Key == Key.Escape)
            //{
            //    if (curPS != null && curPS.Count > 0)
            //    { curPS.Clear(); DrawF(); }
            //    else if (SelectedF >= 0 || HoverFP >= 0)
            //    {
            //        SelectedF = -1;
            //        HoverFP = -1;
            //        DrawF();
            //    }
            //}
            //else if (e.Key == Key.Delete)
            //{
            //    if (mwv!.FeatureList != null && mwv!.FeatureList.Count > 0)
            //    {
            //        mwv!.FeatureList.RemoveAt(SelectedF);
            //        DrawF();
            //    }
            //}
        }
        int MaxF = Properties.Settings.Default.MaxF;
        private void GWpf_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (sourceBitmap == null || e.Button == MouseButtons.Right)
                return;

            if (e.X >= dw && e.X < sourceBitmap.Width - dw && e.Y >= dw && e.Y < sourceBitmap.Height - dw)
            {
                if (HoverFP >= 0)
                {
                    SelectedFP = HoverFP;
                    mousePx = (int)e.X;
                    mousePy = (int)e.Y;
                    MousePress = true;
                    //FeaturesList[0] .SelectedIndex = SelectedF;
                }
                else
                {
                    var p = new Point((int)e.X, (int)e.Y);
                    var Points = new List<Point>()
                    { p};
                    //if (mwv?.FeatureList[i].FPoints.Count < 3)
                    mwv!.FeatureList.Add(new SelectableFeature() { FPoints= Points, Shape = 0, Cat = 0, Description = "Rectangle" });
                    DrawF();
                }
            }
        }
        private void GWpf_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (sourceBitmap == null || /*BRect.IsChecked == true && (e.X < 5 + RWidth.Value / 2 && e.X >= sourceBitmap.Width - 5 - RWidth.Value / 2 && e.Y < 5 + RHeight.Value / 2 && e.Y >= sourceBitmap.Height - 5 - RHeight.Value / 2) ||BRect.IsChecked == false &&*/ (e.X < 5 && e.X >= sourceBitmap.Width - 5 && e.Y < 5 && e.Y >= sourceBitmap.Height - 5))
                return;

            if (MousePress && (e.Button == MouseButtons.Left/*||e.Button==MouseButtons.Right*/))
            {
                if (SelectedFP >= 0 && HoverFP >= 0)
                {
                    GWpf.Cursor = System.Windows.Input.Cursors.Hand;

                    var curF = mwv!.FeatureList[HoverFP];
                    var dx = e.X - mousePx;
                    var dy = e.Y - mousePy;
                    {
                        Point s = curF.FPoints[0];
                        s.X += (int)dx;
                        s.Y += (int)dy;
                        if (s.X < 5)
                            s.X = 5;
                        if (s.Y < 5)
                            s.Y = 5;
                        if (s.X >= sourceBitmap.Width - 5)
                            s.X = sourceBitmap.Width - 6;
                        if (s.Y >= sourceBitmap.Height - 5)
                            s.Y = sourceBitmap.Height - 6;
                        curF.FPoints[0] = s;
                    }
                }
                mousePx = (int)e.X;
                mousePy = (int)e.Y;
                DrawF();
            }
            else
            {
                GWpf.Cursor = System.Windows.Input.Cursors.None;

                //if (mwv!.FeatureList == null|| mwv!.FeatureList.Count == 0)
                //{
                //    mwv!.FeatureList!.Add(new SelectableFeature());
                //     return;
                //}
                if (mwv!.FeatureList.Count > 0)
                {
                    for (int i = 0; i < mwv!.FeatureList.Count; i++)
                    {
                        var s = mwv!.FeatureList[i];
                        if (s.FPoints != null && s.FPoints.Count > 0)
                        {
                            if (Math.Abs(s.FPoints[0].X - e.X) < dw && Math.Abs(s.FPoints[0].Y - e.Y) < dw)
                            {
                                HoverFP = i;
                                GWpf.Cursor = System.Windows.Input.Cursors.Hand;
                                DrawF();
                                return;
                            }
                        }
                    }
                    SelectedFP = -1;
                    HoverFP = -1;
                    DrawF();
                }
            }
        }

        private void GWpf_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            //HoverFP = -1;
            //SelectedFP = -1;

            //            MousePress = false;
            //          DrawF();
        }
        public static Bitmap BitmapAdjust(Bitmap bmp, float brightness, float contrast/*, float gamma*/)
        {
            Bitmap dst = new Bitmap(bmp.Width, bmp.Height, bmp.PixelFormat);

            // Create the ImageAttributes object and apply the ColorMatrix
            ImageAttributes attributes = new ImageAttributes();
            ColorMatrix matrix = new ColorMatrix(new float[][]{
                  new float[] {contrast, 0, 0, 0, 0}, // scale red
                  new float[] {0, contrast, 0, 0, 0}, // scale green
                  new float[] {0, 0, contrast, 0, 0}, // scale blue
                  new float[] {0, 0, 0, 1.0f, 0}, // don't scale alpha
                  new float[] {brightness, brightness, brightness, 0, 1}
            });

            using (Graphics g = Graphics.FromImage(dst))
            {
                attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                //                attributes.SetGamma(gamma);
                g.DrawImage(bmp,
                            new Rectangle(0, 0, dst.Width, dst.Height),
                            0, 0, dst.Width, dst.Height,
                            GraphicsUnit.Pixel,
                            attributes);
            }
            return dst;
        }

        private void GWpf_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DataGrid_SelectionChanged(null, null);
        }

        private void Contrast_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (displayBitmap == null)
                return;
            displayBitmap = BitmapAdjust(sourceBitmap, (float)Brightness.Value / 100f, (float)(Contrast.Value) / 100f);
            //DrawToGraphics(displayBitmap);
            DrawF();
        }

        private void SketchImg_Checked(object sender, RoutedEventArgs e)
        {
            DataGrid_SelectionChanged(null, null);
        }

        private void MNext_Click(object sender, RoutedEventArgs e)
        {
            SaveFeatureFile();
            if (FileListGrid.SelectedIndex < FileListGrid.Items.Count - 1)
                FileListGrid.SelectedIndex++;
        }


        private void FeaturesList_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mwv!.FeatureList.Count > 0 && FeaturesList.SelectedItems.Count > 0)
            {
              //  SelectedF = FeaturesList.SelectedIndex;
                //if (mwv!.FeatureList[SelectedF].Shape == 0)
                //    BRect.IsChecked = true;
                //else
//                    BPolygon.IsChecked = true;
            }
            else
            {
  //              SelectedF = -1;
            }
  //          DrawF();
        }

        Bitmap displayBitmap;

        bool closed = false;
      //  int HoverF = -1, SelectedF = -1;
        int HoverFP = -1,SelectedFP=-1/*, SelectedFP = -1*/;

        private void GWpf_GdiContextDraw(int e)
        {
            if (e == 0)
            {
                if (curPS != null && curPS.Count > 0)
                { curPS.Clear(); DrawF(); }
            }
            
            else if (e == 3)
            {
                if (HoverFP>=0)
                {
                    if(mwv!.FeatureList != null && mwv!.FeatureList[HoverFP] !=null)
                    {
                        mwv!.FeatureList.RemoveAt(HoverFP);
                        HoverFP = -1;
                        DrawF();
                    }
                }
            }
        }

        int mousePx = -1, mousePy = -1;

        List<int> CheckMaskCrossAndMerge(ref byte[] Mask)
        {
            List<int> ms = new List<int>();
            byte[] mask = (byte[])Mask.Clone();
            for (int i = 0; i < mwv!.FeatureList.Count; i++)
            {
                if (mwv!.FeatureList[i].Mask!=null)
                {
                    for (int j = 0; j < mask.Length; j++) 
                    {
                        if (mask[j]==1&& mwv!.FeatureList[i].Mask[j]==1)
                        {
                            ms.Add(i); break;
                        }
                    }
                }
            }
            if (ms.Count == 0)
                return null;
            for (int i = 0; i < mask.Length; i++)
            {
                byte b = mask[i];
                if (b == 1)
                    continue;
                for (int j = 0; j < ms.Count; j++)
                {
                    if (mwv!.FeatureList[ms[j]].Mask[i] > 0)
                    {
                        b = 1;
                        break;
                    }
                }
                mask[i] = b;
            }
            Mask = mask;
            return ms;
        }

        private void isRot_Checked(object sender, RoutedEventArgs e)
        {
            if(isRot.IsChecked==true)
                angles.Visibility = Visibility.Visible;
            else
            {
                angles.Visibility = Visibility.Collapsed;
            }
        }

        private void ContextMenuFPoints_Click(object sender, RoutedEventArgs e)
        {
            if (curPS != null && curPS.Count > 0)
            { curPS.Clear(); DrawF(); }
        }

        private void ContextMenuCat_Click(object sender, RoutedEventArgs e)
        {
            //if (SelectedF >= 0)
            //{
            //    if (mwv!.FeatureList != null && mwv!.FeatureList.Count > 0 && SelectedF >= 0)
            //    {
            //        mwv!.FeatureList.RemoveAt(SelectedF);
            //        FeaturesList.Items.RemoveAt(SelectedF);
            //        DrawF();
            //        DrawMap();
            //    }
            //}
        }

        private void ContextMenuSharp_Click(object sender, RoutedEventArgs e)
        {
            //SelectedF = -1;
        }
        Color[] PrimaryColor => new Color[]
        {
            Color.Red,
            Color.Lime,
            Color.Purple,
            Color.Maroon,
            Color.DarkOrange,
            Color.Indigo,
            Color.Blue,
            Color.LightBlue,
            Color.LightGreen,
            Color.Cyan,
            Color.Teal,
            Color.Yellow,
            Color.Green,
            Color.Orange,
            Color.Brown,
            Color.Chocolate,
            Color.DarkSalmon,
            Color.Aquamarine,
            Color.DarkSeaGreen,
            Color.Pink,
      };
    }
}
