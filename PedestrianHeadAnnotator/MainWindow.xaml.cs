using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CV = OpenCvSharp.CPlusPlus;
namespace PedestrianHeadAnnotator
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {

        private ViewModel vm;
        private const int DefaultThickness = 3;
        private readonly string[] ImageExtentions = new string[] { "jpg", "png", "gif" };

        /// <summary> 補完サジェスト用．上から来たか下から来たか </summary>
        private int lastModifiedFrame = 0;

        /// <summary> ドラッグ用．ドラッグ中か否か </summary>
        private bool isDrag = false;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = vm = new ViewModel();
        }
        private bool IsImage(string filename)
        {
            string ext = System.IO.Path.GetExtension(filename).Substring(1);
            return ImageExtentions.Contains(ext);
        }
        private void btnInputImageDirectory_Click(object sender, RoutedEventArgs e)
        {
            string filename = GetFilePath();
            if (filename == "") return;
            vm.State = new NormalState();
            var files = System.IO.Directory.GetFiles(txtBoxInputImageDirectory.Text).ToList();
            if (vm.DetectedHead.Frames.Count < files.Where(IsImage).Count())
            {
                vm.DetectedHead.Frames.AddRange(
                    new DetectedHead.Frame[files.Count - vm.DetectedHead.Frames.Count]
                    .Select((f,i)=> { f = new DetectedHead.Frame() { Number = i }; return f; }));
                dudFrameNumber.Minimum = dudInterpolationBegin.Minimum = dudInterpolationEnd.Minimum = vm.DetectedHead.Frames.Min(f => f.Number);
                dudFrameNumber.Maximum = dudInterpolationBegin.Maximum = dudInterpolationEnd.Maximum = vm.DetectedHead.Frames.Max(f => f.Number);
            }
            dudFrameNumber.Value = files.IndexOf(filename);
            lastModifiedFrame = (int)dudFrameNumber.Value;
        }

        private void btnLoadAnnotation_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "*.xml | *.xml";
            if (ofd.ShowDialog() == false) { return; }
            sttCurrentStatus.Content = "アノテーションを読み込んでいます";
            try
            {
                vm.DetectedHead = DetectedHead.ReadXml(ofd.FileName);
                dudFrameNumber.Minimum = vm.DetectedHead.Frames.Min(f => f.Number);
                dudFrameNumber.Maximum = vm.DetectedHead.Frames.Max(f => f.Number);
                ResetCmbPerson();
                Draw();
                sttCurrentStatus.Content = "アノテーションの読み込みが完了しました";
            }
            catch (Exception ex)
            {
                sttCurrentStatus.Content = "アノテーション読み込みに失敗しました";
            }
        }

        private void btnSaveAnnotation_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "*.xml | *.xml";
            if (sfd.ShowDialog() == false) { return; }
            vm.DetectedHead.WriteXml(sfd.FileName);
        }

        private string GetFilePath()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = string.Join(", ", ImageExtentions.Select(ext => "*." + ext))+" | "+ string.Join(";", ImageExtentions.Select(ext => "*." + ext));
            if (ofd.ShowDialog() == true)
            {
                txtBoxInputImageDirectory.Text = System.IO.Path.GetDirectoryName(ofd.FileName);
            }
            return ofd.FileName;
        }
        private void OpenImage(string filename)
        {
            var mat = new CV.Mat(filename, LoadMode.Color);
            img.Source = WriteableBitmapConverter.ToWriteableBitmap(mat);
        }

        private void dudFrameNumber_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (vm == null || !vm.Ready) return;
            OpenImage(System.IO.Directory.GetFiles(txtBoxInputImageDirectory.Text)[(int)dudFrameNumber.Value]);
            Draw();
        }

        private void cmbPersonID_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            int idx = (sender as ComboBox).SelectedIndex;
            if (idx == comboBox.Items.Count - 1)//追加を選んだとき
            {
                int newIdx = idx == 0 ? 1 : (int)(comboBox.Items.GetItemAt(idx - 1)) + 1;
                comboBox.Items.Insert(idx, newIdx);
                comboBox.SelectedIndex = idx;
                //// todo
                //vm.State = new AddingState();
            }
            else
            {
                foreach(var el in canvas.Children)
                {
                    var fel = el as FrameworkElement;
                    bool isSelectedPerson = (int)fel.Tag == (int)(sender as ComboBox).SelectedItem;
                    if (fel is Shape) ((Shape)fel).Stroke = new SolidColorBrush(isSelectedPerson ? Colors.Red : Colors.LightGray);
                }
            }
        }

        private void ResetCmbPerson()
        {
            cmbPersonID.Items.Clear();
            foreach (var frame in vm.DetectedHead.Frames)
                foreach (var obj in frame.ObjectList.Objects)
                    if (!cmbPersonID.Items.Contains(obj.Id)) cmbPersonID.Items.Add(obj.Id);
            cmbPersonID.Items.Add("追加...");
            cmbPersonID.SelectedIndex = 0;
        }

        private void Draw()
        {
            int fNumber = (int)dudFrameNumber.Value;
            var frame = vm.DetectedHead.Frames.FirstOrDefault(i => i.Number == fNumber);
            canvas.Children.Clear();
            //全身，頭，頭部方向でそれぞれ別にループする．そうしないとID1の人の頭部よりID2の人の全身のほうがレイヤーが上になって動かせなくなる
            foreach (var obj in frame.ObjectList.Objects)
            {
                Rectangle body = new Rectangle()
                {
                    Stroke = new SolidColorBrush(obj.Id == (int)cmbPersonID.SelectedItem ? Colors.Red : Colors.LightGray),
                    Fill = new SolidColorBrush(new Color() { A = 0 }),
                    StrokeThickness = DefaultThickness,
                    Width = obj.Box.Width,
                    Height = obj.Box.Height,
                    Tag = obj.Id,
                };
                Canvas.SetLeft(body, obj.Box.XCenter - obj.Box.Width / 2);
                Canvas.SetTop(body, obj.Box.YCenter - obj.Box.Height / 2);
                canvas.Children.Add(body);
            }
            foreach (var obj in frame.ObjectList.Objects)
            {
                Rectangle head = new Rectangle()
                {
                    Stroke = new SolidColorBrush(obj.Id == (int)cmbPersonID.SelectedItem ? Colors.Red : Colors.LightGray),
                    Fill = new SolidColorBrush(new Color() { A = 0 }),
                    StrokeThickness = DefaultThickness,
                    Width = obj.Body.Head.Size * 2 * 1.5,
                    Height = obj.Body.Head.Size * 2 * 1.5,
                    Tag = obj.Id,
                };
                Canvas.SetLeft(head, obj.Body.Head.XCenter - obj.Body.Head.Size * 1.5);
                Canvas.SetTop(head, obj.Body.Head.YCenter - obj.Body.Head.Size * 1.5);
                canvas.Children.Add(head);
            }
            foreach (var obj in frame.ObjectList.Objects)
            {
                double gazeRad = obj.Body.Head.Gaze * Math.PI / 180;
                Line gaze = new Line()
                {
                    Stroke = new SolidColorBrush(obj.Id == (int)cmbPersonID.SelectedItem ? Colors.Red : Colors.LightGray),
                    StrokeThickness = DefaultThickness,
                    X1 = obj.Body.Head.XCenter + Math.Cos(gazeRad) * obj.Body.Head.Size * 0.5,
                    Y1 = obj.Body.Head.YCenter - Math.Sin(gazeRad) * obj.Body.Head.Size * 0.5,
                    X2 = obj.Body.Head.XCenter + Math.Cos(gazeRad) * obj.Body.Head.Size * 2,
                    Y2 = obj.Body.Head.YCenter - Math.Sin(gazeRad) * obj.Body.Head.Size * 2,
                    Tag= obj.Id,
                };
                gaze.MouseEnter += Line_MouseEnter;
                gaze.MouseLeave += Line_MouseLeave;
                gaze.MouseLeftButtonDown += Line_MouseLeftButtonDown;
                gaze.MouseLeftButtonUp += Line_MouseLeftButtonUp;
                gaze.MouseMove += Line_MouseMove;
                canvas.Children.Add(gaze);
            }
        }

        private void btnInterpolationCCW_Click(object sender, RoutedEventArgs e)
        {
            int beginFrame = vm.InterpolationBegin;
            int endFrame = vm.InterpolationEnd;
            double? from = vm.DetectedHead.Frames.FirstOrDefault(i=>i.Number==beginFrame)?
                .ObjectList.Objects.FirstOrDefault(i => i.Id == cmbPersonID.SelectedIndex)?.Body.Head.Gaze;
            double? to = vm.DetectedHead.Frames.FirstOrDefault(i=>i.Number==endFrame)?
                .ObjectList.Objects.FirstOrDefault(i => i.Id == cmbPersonID.SelectedIndex)?.Body.Head.Gaze;
            if (from == null) MessageBox.Show("指定した開始フレームにアノテーションがありませんでした");
            if (to == null) MessageBox.Show("指定した終了フレームにアノテーションがありませんでした");
            if (from == null || to == null) return;

            for (int fNumber = beginFrame + 1; fNumber < endFrame; fNumber++)
            {
                double rate = (double)(fNumber - beginFrame) / (endFrame - beginFrame);
                var obj = vm.DetectedHead.Frames.FirstOrDefault(i=>i.Number==fNumber)?
                    .ObjectList.Objects.FirstOrDefault(i => i.Id == cmbPersonID.SelectedIndex);
                if (obj != null) { obj.Body.Head.Gaze = (int)InterpolationCounterClockwise((double)from, (double)to, rate); }
                else {/* todo: objがないときの処理をどうするか */}
            }
        }

        private void btnInterpolationCW_Click(object sender, RoutedEventArgs e)
        {
            int beginFrame = vm.InterpolationBegin;
            int endFrame = vm.InterpolationEnd;
            double? from = vm.DetectedHead.Frames.FirstOrDefault(i => i.Number == beginFrame)?
                .ObjectList.Objects.FirstOrDefault(i => i.Id == cmbPersonID.SelectedIndex)?.Body.Head.Gaze;
            double? to = vm.DetectedHead.Frames.FirstOrDefault(i => i.Number == endFrame)?
                .ObjectList.Objects.FirstOrDefault(i => i.Id == cmbPersonID.SelectedIndex)?.Body.Head.Gaze;
            if (from == null) MessageBox.Show("指定した開始フレームにアノテーションがありませんでした");
            if (to == null) MessageBox.Show("指定した終了フレームにアノテーションがありませんでした");
            if (from == null || to == null) return;
            for (int fNumber = beginFrame + 1; fNumber < endFrame; fNumber++)
            {
                double rate = (double)(fNumber - beginFrame) / (endFrame - beginFrame);
                var obj = vm.DetectedHead.Frames.FirstOrDefault(i=>i.Number==fNumber)?
                    .ObjectList.Objects.FirstOrDefault(i => i.Id == cmbPersonID.SelectedIndex);
                if (obj != null) { obj.Body.Head.Gaze = (int)InterpolationClockwise((double)from, (double)to, rate); }
                else { /* todo: objがないときの処理をどうするか */}
            }
        }
        
        private void btnInterpolationNearest_Click(object sender, RoutedEventArgs e)
        {
            int beginFrame = vm.InterpolationBegin;
            int endFrame = vm.InterpolationEnd;
            double? from = vm.DetectedHead.Frames.FirstOrDefault(i => i.Number == beginFrame)?
                .ObjectList.Objects.FirstOrDefault(i => i.Id == cmbPersonID.SelectedIndex)?.Body.Head.Gaze;
            double? to = vm.DetectedHead.Frames.FirstOrDefault(i => i.Number == endFrame)?
                .ObjectList.Objects.FirstOrDefault(i => i.Id == cmbPersonID.SelectedIndex)?.Body.Head.Gaze;
            if (from == null) MessageBox.Show("指定した開始フレームにアノテーションがありませんでした");
            if (to == null) MessageBox.Show("指定した終了フレームにアノテーションがありませんでした");
            if (from == null || to == null) return;

            double diff = (double)to - (double)from;
            if (diff < -180 || (0 < diff && diff < 180)) btnInterpolationCCW_Click(sender, e);
            else btnInterpolationCW_Click(sender, e);
        }

        private void Line_MouseEnter(object sender, MouseEventArgs e)
        {
            var line = sender as Line;
            if(line!= null) { line.StrokeThickness = DefaultThickness * 3; }
            Mouse.OverrideCursor = Cursors.Cross;
        }
        private void Line_MouseLeave(object sender, MouseEventArgs e)
        {
            var line = sender as Line;
            if (line != null) { line.StrokeThickness = DefaultThickness; }
            if(lastModifiedFrame < (int)dudFrameNumber.Value)
            {
                vm.InterpolationBegin = lastModifiedFrame;
                vm.InterpolationEnd = (int)dudFrameNumber.Value;
            }
            else if (lastModifiedFrame > (int)dudFrameNumber.Value)
            {
                vm.InterpolationEnd = lastModifiedFrame;
                vm.InterpolationBegin = (int)dudFrameNumber.Value;
            }
            lastModifiedFrame = (int)dudFrameNumber.Value;
            Mouse.OverrideCursor = Cursors.Arrow;
        }
        private void Line_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            UIElement el = sender as UIElement;
            isDrag = true;
            el.CaptureMouse();
            if (el is FrameworkElement)
                cmbPersonID.SelectedItem = ((FrameworkElement)el).Tag;
        }

        private void Line_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDrag)
            {
                UIElement el = sender as UIElement;
                el.ReleaseMouseCapture();
                isDrag = false;
            }
        }

        private void Line_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrag)
            {
                Point pt = Mouse.GetPosition(canvas);
                var line = sender as Line;
                sttCoordinate.Content = pt.X.ToString("0.000") + "," + pt.Y.ToString("0.000");
                var obj = vm.DetectedHead.Frames.FirstOrDefault(i=>i.Number==(int)dudFrameNumber.Value)?
                    .ObjectList.Objects.FirstOrDefault(i=>i.Id==(int)line.Tag);
                if (obj == null)
                {
                    sttCurrentStatus.Content = "error 1: 不明のエラーが発生し，只今のアノテーションを中止しました";
                    return;
                }
                double gazeRad = Math.Atan2(-(pt.Y-obj.Body.Head.YCenter), pt.X-obj.Body.Head.XCenter); if (gazeRad < 0) gazeRad += Math.PI * 2;
                double gazeDeg = (gazeRad * 180 / Math.PI);
                int fNumber = (int)dudFrameNumber.Value;
                obj.Body.Head.Gaze = (int)gazeDeg;
                sttAngle.Content = gazeDeg.ToString("0.000");
                line.X1 = obj.Body.Head.XCenter + Math.Cos(gazeRad) * obj.Body.Head.Size * 0.5;
                line.Y1 = obj.Body.Head.YCenter - Math.Sin(gazeRad) * obj.Body.Head.Size * 0.5;
                line.X2 = obj.Body.Head.XCenter + Math.Cos(gazeRad) * obj.Body.Head.Size * 2;
                line.Y2 = obj.Body.Head.YCenter - Math.Sin(gazeRad) * obj.Body.Head.Size * 2;
            }
        }

        private double InterpolationCounterClockwise(double from, double to, double rate)
        {
            if (rate < 0) rate = 0;
            if (rate > 1) rate = 1;
            if (from > to) to += 360;
            return (from + (to - from) * rate) % 360;
        }
        private double InterpolationClockwise(double from, double to, double rate)
        {
            return InterpolationCounterClockwise(to, from, 1 - rate);
        }
    }
}
