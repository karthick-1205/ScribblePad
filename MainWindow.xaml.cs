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
      public MainWindow () => InitializeComponent ();

      #region AddShapes ---------------------------------------------
      /// <summary>Adds a scribble shape to the drawing based on the provided coordinates</summary>
      private void AddScribble (string[] values) {
         mShapes = new Scribble ();
         for (int i = 1; i < values.Length - 1; i += 2) {
            double x = double.Parse (values[i]);
            double y = double.Parse (values[i + 1]);
            mShapes.mPointList.Add (new Point (x, y));
         }
         mShapesList.Add (mShapes);
      }

      /// <summary>Adds a line shape to the drawing based on the provided coordinates</summary>
      private void AddLine (double x1, double y1, double x2, double y2) {
         mShapes = new Line ();
         mShapes.mPointList.Add (new Point (x1, y1));
         mShapes.mPointList.Add (new Point (x2, y2));
         mShapesList.Add (mShapes);
      }

      /// <summary>Adds a connected line shape to the drawing based on the provided coordinates</summary>
      private void AddConnectedLines (double x1, double y1, double x2, double y2) {
         mShapes = new ConnectedLines ();
         mShapes.mPointList.Add (new Point (x1, y1));
         mShapes.mPointList.Add (new Point (x2, y2));
         mShapesList.Add (mShapes);
      }

      /// <summary>Adds a rectangle shape to the drawing based on the provided coordinates</summary>
      private void AddRectangle (double x1, double y1, double x2, double y2) {
         mShapes = new Rectangle ();
         mShapes.mPointList.Add (new Point (x1, y1));
         mShapes.mPointList.Add (new Point (x2, y2));
         mShapesList.Add (mShapes);
      }
      #endregion

      #region Drawings ----------------------------------------------
      /// <summary>To start the drawing and collect the start point</summary>
      protected override void OnMouseLeftButtonDown (MouseButtonEventArgs e) {
         if (e.LeftButton == MouseButtonState.Pressed) {
            Point pt = e.GetPosition (this);
            switch (mSelectedItem.Name) {
               case "scribble":
                  mShapes = new Scribble ();
                  mShapes.mPointList.Add (pt);
                  mShapesList.Add (mShapes);
                  break;
               case "line":
                  mShapes = new Line ();
                  mShapes.mPointList.Add (pt);
                  mShapes.mPointList.Add (pt);
                  mShapesList.Add (mShapes);
                  break;
               case "rect":
                  mShapes = new Rectangle ();
                  mShapes.mPointList.Add (pt);
                  mShapes.mPointList.Add (pt);
                  mShapesList.Add (mShapes);
                  break;
               case "cline":
                  mShapes = new ConnectedLines ();
                  mShapes.mPointList.Add (pt);
                  mShapes.mPointList.Add (pt);
                  mShapesList.Add (mShapes);
                  break;
            }
         }
      }

      /// <summary>Update state of drawing and collect current point</summary>
      protected override void OnMouseMove (MouseEventArgs e) {
         if (e.LeftButton == MouseButtonState.Pressed) {
            Point pt = e.GetPosition (this);
            if (mShapes is Scribble) {
               if (mShapes.mPointList.Count > 0) mShapes.mPointList.Add (pt);
            } else {
               if (mShapes.mPointList.Count > 0) mShapes.mPointList[^1] = pt;
            }
            mShapesList[^1] = mShapes;
            InvalidateVisual ();
         }
      }

      /// <summary>Add drawing to the list when mouse is released</summary>
      protected override void OnMouseLeftButtonUp (MouseButtonEventArgs e) {
         if (e.LeftButton == MouseButtonState.Released) {
            if (mShapes == null || mShapes.mPointList.Count == 0) return;
            Point pt = e.GetPosition (this);
            mShapes.mPointList[^1] = pt;
            mShapesList[^1] = mShapes;
            InvalidateVisual ();
         }
      }
      #endregion

      #region Load --------------------------------------------------
      ///<summary>Loads a shape from a text file </summary>
      private void OnOpenTextClicked (object sender, RoutedEventArgs e) {
         OpenFileDialog openText = new ();
         mShapesList.Clear ();
         mShapesStack.Clear ();
         string text;
         if (openText.ShowDialog () == true) {
            using StreamReader sr = new (openText.FileName);
            string extension = Path.GetExtension (openText.FileName);
            if (extension != ".txt") {
               MessageBox.Show ("Unsupported file format");
               mInvalidFile = true;
            }
            if (mInvalidFile) { OnOpenTextClicked (sender, e); }
            while ((text = sr.ReadLine ()!) != null) {
               text = text.TrimEnd (',');
               Shapes (text);
            }
         }
         InvalidateVisual ();
      }

      ///<summary>Loads a shape from a binary file</summary>
      private void OnOpenBinaryClicked (object sender, RoutedEventArgs e) {
         OpenFileDialog openBinary = new ();
         if (openBinary.ShowDialog () == true) {
            BinaryReader br = new (File.Open (openBinary.FileName, FileMode.Open));
            string extension = Path.GetExtension (openBinary.FileName);
            if (extension != ".bin") {
               MessageBox.Show ("Unsupported file format");
               mInvalidFile = true;
            }
            if (mInvalidFile) { OnOpenBinaryClicked (sender, e); }
            int shapeCount = br.ReadInt32 ();
            for (int i = 0; i < shapeCount; i++) {
               int num = br.ReadInt32 ();
               switch (num) {
                  case 1: mShapes = new Scribble (); break;
                  case 2: mShapes = new Line (); break;
                  case 3: mShapes = new Rectangle (); break;
                  case 4: mShapes = new ConnectedLines (); break;
               }
               int pointsCount = br.ReadInt32 ();
               for (int j = 0; j < pointsCount; j++) {
                  double x = br.ReadDouble ();
                  double y = br.ReadDouble ();
                  mShapes.mPointList.Add (new Point (x, y));
               }
               mShapesList.Add (mShapes);
            }
            InvalidateVisual ();
         }
      }
      #endregion

      #region Save --------------------------------------------------
      ///<summary>Saves a shape to a text file</summary>
      private void OnSaveTextClicked (object sender, RoutedEventArgs e) {
         SaveFileDialog saveText = new () {
            FileName = "Untitled.txt",
            Filter = "Text files (*.txt)|*.txt"
         };
         if (saveText.ShowDialog () == true) {
            using StreamWriter sw = new (saveText.FileName);
            foreach (var s in mShapesList) {
               var t = s.ToString ();
               string[] parts = t!.Split ('.');
               t = parts[1];
               sw.Write (t);
               foreach (var v in s.mPointList) {
                  sw.Write (",");
                  sw.Write (v.ToString ());
               }
               sw.Write (",");
               sw.WriteLine ();
            }
         }
      }

      ///<summary>Saves a shape to a binary file</summary>
      private void OnSaveBinaryClicked (object sender, RoutedEventArgs e) {
         SaveFileDialog saveBinary = new () {
            FileName = "Untitled.bin",
            Filter = "Binary files (*.bin)|*.bin"
         };
         if (saveBinary.ShowDialog () == true) {
            using BinaryWriter bw = new (File.Open (saveBinary.FileName, FileMode.Create));
            bw.Write (mShapesList.Count);
            foreach (var shape in mShapesList) {
               switch (shape) {
                  case Scribble: bw.Write (1); break;
                  case Line: bw.Write (2); break;
                  case Rectangle: bw.Write (3); break;
                  case ConnectedLines: bw.Write (4); break;
               }
               bw.Write (shape.mPointList.Count);
               foreach (Point pt in shape.mPointList) {
                  bw.Write (pt.X);
                  bw.Write (pt.Y);
               }
            }
         }
      }
      #endregion

      #region SelectShapes ------------------------------------------
      ///<summary>Sets the selected item to scribble</summary>
      private void OnScribbleClicked (object sender, RoutedEventArgs e) => mSelectedItem.Name = "scribble";

      ///<summary>Sets the selected item to line</summary>
      private void OnLineClicked (object sender, RoutedEventArgs e) => mSelectedItem.Name = "line";

      ///<summary>Sets the selected item to rect</summary>
      private void OnRectClicked (object sender, RoutedEventArgs e) => mSelectedItem.Name = "rect";

      ///<summary>Sets the selected item to cline</summary>
      private void OnClineClicked (object sender, RoutedEventArgs e) => mSelectedItem.Name = "cline";

      #endregion

      #region Shapes ------------------------------------------------
      /// <summary>Parses a line of text representing a shape and adds it to the drawing</summary>
      private void Shapes (string line) {
         string[] values = line.Split (',');
         double x1 = double.Parse (values[1]);
         double y1 = double.Parse (values[2]);
         double x2 = double.Parse (values[3]);
         double y2 = double.Parse (values[4]);
         switch (line[0]) {
            case 'S':
               AddScribble (values);
               break;
            case 'L':
               AddLine (x1, y1, x2, y2);
               break;
            case 'C':
               AddConnectedLines (x1, y1, x2, y2);
               break;
            case 'R':
               AddRectangle (x1, y1, x2, y2);
               break;
            default:
               // Handle unsupported shape or invalid input
               break;
         }
      }
      #endregion

      #region Undo-Redo ---------------------------------------------
      ///<summary>Performs an undo action</summary>
      private void OnUndoClicked (object sender, RoutedEventArgs e) {
         if (mShapesList.Count > 0) {
            mShapesStack.Push (mShapesList.Last ());
            mShapesList.RemoveAt (mShapesList.Count - 1);
            InvalidateVisual ();
         }
      }

      ///<summary>Performs a redo action</summary>
      private void OnRedoClicked (object sender, RoutedEventArgs e) {
         if (mShapesStack.Count > 0) {
            mShapesList.Add (mShapesStack.Pop ());
            InvalidateVisual ();
         }
      }
      #endregion

      /// <summary>To render the drawing on the display</summary>
      protected override void OnRender (DrawingContext dc) {
         base.OnRender (dc);
         foreach (var shape in mShapesList) {
            switch (shape) {
               case Scribble scribble:
                  for (int i = 0; i < scribble.mPointList.Count - 1; i++) dc.DrawLine (pen, scribble.mPointList[i], scribble.mPointList[i + 1]);
                  break;
               case Line line:
                  dc.DrawLine (pen, line.mPointList[0], line.mPointList[1]);
                  break;
               case Rectangle rect:
                  Point pt1 = rect.mPointList[0];
                  Point pt2 = rect.mPointList[1];
                  dc.DrawRectangle (null, pen, new (pt1, pt2));
                  break;
               case ConnectedLines clines:
                  for (int i = 0; i < clines.mPointList.Count - 1; i++) dc.DrawLine (pen, clines.mPointList[i], clines.mPointList[i + 1]);
                  break;
            }
         }
      }

      #region Private Data ------------------------------------------
      Pen pen = new (Brushes.White, 2);
      Shapes mShapes = new ();
      Stack<Shapes> mShapesStack = new ();
      List<Shapes> mShapesList = new ();
      ToggleButton mSelectedItem = new ();
      bool mInvalidFile;
      #endregion
   }
}
