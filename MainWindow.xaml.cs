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
      Shapes mShapes = null;
      PointCollection scribblePoints = new ();
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
                  dc.DrawLine (pen, line.PointList[0], line.PointList[^1]);
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
            if (mShapes is Scribble) mShapes.PointList.Add (pt);
            else mShapes.PointList[^1] = pt;
            shapesList[^1] = mShapes;
            InvalidateVisual ();
         }
      }

      protected override void OnMouseLeftButtonUp (MouseButtonEventArgs e) {
         if (e.LeftButton == MouseButtonState.Released) {
            Point pt = e.GetPosition (this);
            if (mShapes is Scribble) mShapes.PointList.Add (pt);
            else mShapes.PointList[^1] = pt;
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
         SaveFileDialog saveText = new ();
         if (saveText.ShowDialog () == true) {
            using StreamWriter sw = new (saveText.FileName);
            foreach (var ptList in scribblePointsList) {
               foreach (var pt in ptList) { sw.WriteLine (pt.ToString ()); }
               sw.WriteLine ("end");
            }
         }
      }

      private void OpenText_Click (object sender, RoutedEventArgs e) {
         OpenFileDialog openText = new ();
         string str;
         if (openText.ShowDialog () == true) {
            using StreamReader sr = new (openText.FileName);
            while ((str = sr.ReadLine ()) != null) {
               if (str == "end") {
                  scribblePointsList.Add (new PointCollection (scribblePoints));
                  scribblePoints = new PointCollection ();
               } else {
                  string[] pt = str.Split (',');
                  double x = double.Parse (pt[0]);
                  double y = double.Parse (pt[1]);
                  scribblePoints.Add (new Point (x, y));
               }
            }
         }
         InvalidateVisual ();
      }

      private void SaveBinary_Click (object sender, RoutedEventArgs e) {
         SaveFileDialog saveBinary = new ();
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
