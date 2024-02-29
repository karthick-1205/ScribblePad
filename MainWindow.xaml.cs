using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ScribblePad {
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window {
      Pen pen = new (Brushes.White, 2);
      PointCollection scribblePoints = new ();
      List<PointCollection> scribblePointsList = new ();
      Stack<PointCollection> scribblePointsStack = new ();
      public MainWindow () => InitializeComponent ();

      protected override void OnRender (DrawingContext dc) {
         base.OnRender (dc);
         foreach (PointCollection point in scribblePointsList) {
            for (int i = 0; i < point.Count - 1; i++) {
               dc.DrawLine (pen, point[i], point[i + 1]);
            }
         }
         for (int i = 0; i < scribblePoints.Count - 1; i++)
            dc.DrawLine (pen, scribblePoints[i], scribblePoints[i + 1]);
      }

      protected override void OnMouseLeftButtonDown (MouseButtonEventArgs e) {
         if (e.LeftButton == MouseButtonState.Pressed) {
            Point pt = e.GetPosition (this);
            scribblePoints.Add (pt);
         }
      }

      protected override void OnMouseMove (MouseEventArgs e) {
         if (e.LeftButton == MouseButtonState.Pressed) {
            Point pt = e.GetPosition (this);
            scribblePoints.Add (pt);
            InvalidateVisual ();
         }
      }

      protected override void OnMouseLeftButtonUp (MouseButtonEventArgs e) {
         if (e.LeftButton == MouseButtonState.Released) {
            Point pt = e.GetPosition (this);
            scribblePoints.Add (pt);
            scribblePointsList.Add (scribblePoints);
            scribblePoints = new ();
         }
      }

      private void Undo_Click (object sender, RoutedEventArgs e) {
         if (scribblePointsList.Count > 0) {
            var lastScribble = scribblePointsList.Last ();
            scribblePointsStack.Push (lastScribble);
            scribblePointsList.Remove (lastScribble);
            InvalidateVisual ();
         }
      }

      private void Redo_Click (object sender, RoutedEventArgs e) {
         if (scribblePointsStack.Count > 0) {
            scribblePointsList.Add (scribblePointsStack.Pop ());
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
   }
}
