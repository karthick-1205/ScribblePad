using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ScribblePad {
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window {
      Pen pen = new (Brushes.White, 2);
      Shapes mShapes = new ();
      List<PointCollection> scribblePointsList = new ();
      Stack<Shapes> shapesStack = new ();
      List<Shapes> shapesList = new ();
      ToggleButton selectedItem = new ();

      public MainWindow () => InitializeComponent ();

      protected override void OnRender (DrawingContext dc) {
         base.OnRender (dc);
         foreach (var shape in shapesList) {
            switch (shape) {
               case Scribble scribble:
                  for (int i = 0; i < scribble.PointList.Count - 1; i++) dc.DrawLine (pen, scribble.PointList[i], scribble.PointList[i + 1]);
                  break;
               case Line line:
                  dc.DrawLine (pen, line.PointList[0], line.PointList[1]);
                  break;
               case Rectangle rect:
                  Point pt1 = rect.PointList[0];
                  Point pt2 = rect.PointList[1];
                  dc.DrawRectangle (null, pen, new (pt1, pt2));
                  break;
               case ConnectedLines clines:
                  for (int i = 0; i < clines.PointList.Count - 1; i++) dc.DrawLine (pen, clines.PointList[i], clines.PointList[i + 1]);
                  break;
            }
         }
      }

      protected override void OnMouseLeftButtonDown (MouseButtonEventArgs e) {
         if (e.LeftButton == MouseButtonState.Pressed) {
            Point pt = e.GetPosition (this);
            switch (selectedItem.Name) {
               case "scribble":
                  mShapes = new Scribble ();
                  mShapes.PointList.Add (pt);
                  shapesList.Add (mShapes);
                  break;
               case "line":
                  mShapes = new Line ();
                  mShapes.PointList.Add (pt);
                  mShapes.PointList.Add (pt);
                  shapesList.Add (mShapes);
                  break;
               case "rect":
                  mShapes = new Rectangle ();
                  mShapes.PointList.Add (pt);
                  mShapes.PointList.Add (pt);
                  shapesList.Add (mShapes);
                  break;
               case "cline":
                  mShapes = new ConnectedLines ();
                  mShapes.PointList.Add (pt);
                  mShapes.PointList.Add (pt);
                  shapesList.Add (mShapes);
                  break;
            }
         }
      }

      protected override void OnMouseMove (MouseEventArgs e) {
         if (e.LeftButton == MouseButtonState.Pressed) {
            Point pt = e.GetPosition (this);
            if (mShapes is Scribble) {
               if (mShapes.PointList.Count > 0) mShapes.PointList.Add (pt);
            } else {
               if (mShapes.PointList.Count > 0) mShapes.PointList[^1] = pt;
            }
            shapesList[^1] = mShapes;
            InvalidateVisual ();
         }
      }

      protected override void OnMouseLeftButtonUp (MouseButtonEventArgs e) {
         if (e.LeftButton == MouseButtonState.Released) {
            if (mShapes == null || mShapes.PointList.Count == 0) return;
            Point pt = e.GetPosition (this);
            mShapes.PointList[^1] = pt;
            shapesList[^1] = mShapes;
            InvalidateVisual ();
         }
      }

      private void Undo_Click (object sender, RoutedEventArgs e) {
         if (shapesList.Count > 0) {
            shapesStack.Push (shapesList.Last ());
            shapesList.RemoveAt (shapesList.Count - 1);
            InvalidateVisual ();
         }
      }

      private void Redo_Click (object sender, RoutedEventArgs e) {
         if (shapesStack.Count > 0) {
            shapesList.Add (shapesStack.Pop ());
            InvalidateVisual ();
         }
      }

      private void SaveText_Click (object sender, RoutedEventArgs e) {
         SaveFileDialog saveText = new () {
            FileName = "Untitled.txt",
            Filter = "Text files (*.txt)|*.txt"
         };
         if (saveText.ShowDialog () == true) {
            using StreamWriter sw = new (saveText.FileName);
            foreach (var s in shapesList) {
               var t = s.ToString ();
               string[] parts = t!.Split ('.');
               t = parts[1];
               sw.Write (t);
               foreach (var v in s.PointList) {
                  sw.Write (",");
                  sw.Write (v.ToString ());
               }
               sw.Write (",");
               sw.WriteLine ();
            }
         }
      }

      private void OpenText_Click (object sender, RoutedEventArgs e) {
         OpenFileDialog openText = new ();
         shapesList.Clear ();
         shapesStack.Clear ();
         string str;
         if (openText.ShowDialog () == true) {
            using StreamReader sr = new (openText.FileName);
            while ((str = sr.ReadLine ()!) != null) {
               str = str.TrimEnd (',');
               if (str.StartsWith ("S")) {
                  string[] values = str.Split (',');
                  mShapes = new Scribble ();
                  for (int i = 1; i < values.Length - 1; i += 2) {
                     double x1 = double.Parse (values[i]);
                     double y1 = double.Parse (values[i + 1]);
                     mShapes.PointList.Add (new Point (x1, y1));
                  }
                  shapesList.Add (mShapes);
               } else if (str.StartsWith ("L")) {
                  string[] values = str.Split (',');
                  double x1 = double.Parse (values[1]);
                  double y1 = double.Parse (values[2]);
                  double x2 = double.Parse (values[3]);
                  double y2 = double.Parse (values[4]);
                  mShapes = new Line ();
                  mShapes.PointList.Add (new Point (x1, y1));
                  mShapes.PointList.Add (new Point (x2, y2));
                  shapesList.Add (mShapes);
               } else if (str.StartsWith ("C")) {
                  string[] values = str.Split (',');
                  double x1 = double.Parse (values[1]);
                  double y1 = double.Parse (values[2]);
                  double x2 = double.Parse (values[3]);
                  double y2 = double.Parse (values[4]);
                  mShapes = new ConnectedLines ();
                  mShapes.PointList.Add (new Point (x1, y1));
                  mShapes.PointList.Add (new Point (x2, y2));
                  shapesList.Add (mShapes);
               } else if (str.StartsWith ("R")) {
                  string[] values = str.Split (',');
                  double x1 = double.Parse (values[1]);
                  double y1 = double.Parse (values[2]);
                  double x2 = double.Parse (values[3]);
                  double y2 = double.Parse (values[4]);
                  mShapes = new Rectangle ();
                  mShapes.PointList.Add (new Point (x1, y1));
                  mShapes.PointList.Add (new Point (x2, y2));
                  shapesList.Add (mShapes);
               }
            }
         }
         InvalidateVisual ();
      }

      private void SaveBinary_Click (object sender, RoutedEventArgs e) {
         SaveFileDialog saveBinary = new () {
            FileName = "Untitled.bin",
            Filter = "Binary files (*.bin)|*.bin"
         };
         if (saveBinary.ShowDialog () == true) {
            BinaryWriter bw = new (File.Open (saveBinary.FileName, FileMode.Create));
            bw.Write (scribblePointsList.Count);
            foreach (var ptList in scribblePointsList) {
               bw.Write (ptList.Count);
               foreach (var pt in ptList) {
                  bw.Write (pt.X);
                  bw.Write (pt.Y);
               }
            }
         }
      }

      private void OpenBinary_Click (object sender, RoutedEventArgs e) {
         OpenFileDialog openBinary = new ();
         if (openBinary.ShowDialog () == true) {
            BinaryReader br = new (File.Open (openBinary.FileName, FileMode.Open));
            int scribbleCount = br.ReadInt32 ();
            for (int i = 0; i < scribbleCount; i++) {
               int pointsCount = br.ReadInt32 ();
               PointCollection points = new PointCollection ();
               for (int j = 0; j < pointsCount; j++) {
                  double x = br.ReadDouble ();
                  double y = br.ReadDouble ();
                  points.Add (new Point (x, y));
               }
               scribblePointsList.Add (points);
            }
            InvalidateVisual ();
         }
      }
      private void Scribble_Click (object sender, RoutedEventArgs e) => selectedItem.Name = "scribble";

      private void Line_Click (object sender, RoutedEventArgs e) => selectedItem.Name = "line";

      private void Rect_Click (object sender, RoutedEventArgs e) => selectedItem.Name = "rect";

      private void Cline_Click (object sender, RoutedEventArgs e) => selectedItem.Name = "cline";
   }
}
