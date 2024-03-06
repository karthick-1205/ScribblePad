﻿using System.Windows.Media;
namespace ScribblePad {
   public class Shapes {
      public PointCollection PointList = new ();
   }

   public class Scribble : Shapes { }

   public class Line : Shapes { }

   public class Rectangle : Shapes { }

   public class ConnectedLines : Shapes { }
}