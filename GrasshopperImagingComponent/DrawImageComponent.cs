using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;

namespace GrasshopperImagingComponent
{
  public sealed class DrawImageComponent : GH_Component
  {
    public DrawImageComponent()
      : base("Draw Image", "Draw", "Draw an image.", "Display", "Image")
    { }

    public static readonly Guid _componentId = new Guid("{E11339A0-B464-41EC-9817-3E059D91D2A8}");
    public override Guid ComponentGuid
    {
      get { return _componentId; }
    }
    public override GH_Exposure Exposure
    {
      get { return GH_Exposure.primary; }
    }
    protected override Bitmap Icon
    {
      get { return Properties.Resources.Draw_24x24; }
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      var shapeParam = new GdiShapeParameter();
      pManager.AddParameter(shapeParam, "Shapes", "S", "Shapes to draw", GH_ParamAccess.list);
      pManager.AddRectangleParameter("Size", "S", "Bitmap size and orientation", GH_ParamAccess.item,
        new Rectangle3d(Plane.WorldXY, 100, 100));
      pManager.AddNumberParameter("Factor", "F", "Image scaling factor (pixels per Rhino unit).", GH_ParamAccess.item, 1);

      pManager.AddColourParameter("Background", "B", "Background colour", GH_ParamAccess.item, Color.Transparent);
      pManager.AddBooleanParameter("Anti-alias", "A", "Anti-alias smoothing flag", GH_ParamAccess.item, true);

      var fileParam = new Param_FilePath();
      fileParam.FileFilter = "Image files (*.png)|*.png";
      pManager.AddParameter(fileParam, "File location", "L", "Save location of image", GH_ParamAccess.item);
    }
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      // does it need an output? BitmapParameter
    }
    protected override void SolveInstance(IGH_DataAccess access)
    {
      List<GdiShapeGoo> shapes = new List<GdiShapeGoo>();
      Rectangle3d boundary = Rectangle3d.Unset;
      double factor = 1.0;
      Color background = Color.Transparent;
      bool antialias = true;
      string file = null;

      access.GetDataList(0, shapes);
      if (!access.GetData(1, ref boundary)) return;
      if (!access.GetData(2, ref factor)) return;
      if (!access.GetData(3, ref background)) return;
      if (!access.GetData(4, ref antialias)) return;
      if (!access.GetData(5, ref file)) return;

      if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(file)))
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Output folder does not exist.");
        return;
      }

      if (!boundary.IsValid) return;
      boundary.MakeIncreasing();
      if (boundary.Area < 1e-12)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Bitmap boundary is too small.");
        return;
      }
      if (factor < 1e-12)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Scaling factor is too small.");
        return;
      }

      int w = (int)(boundary.Width * factor);
      int h = (int)(boundary.Height * factor);
      if (w <= 0 || h <= 0)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Bitmap boundary is too narrow.");
        return;
      }

      BitmapProjection projection = new BitmapProjection(boundary, w, h);

      Bitmap image = new Bitmap(w, h, PixelFormat.Format32bppArgb);
      Graphics graphics = Graphics.FromImage(image);
      graphics.Clear(background);
      graphics.SmoothingMode = antialias ? SmoothingMode.HighQuality : SmoothingMode.None;

      GdiCache cache = new GdiCache(projection);
      foreach (GdiShapeGoo shape in shapes)
        shape.DrawShape(graphics, cache);
      cache.Clear();
      graphics.Dispose();

      if (!string.IsNullOrWhiteSpace(file))
      {
        ImageFormat format;
        switch (System.IO.Path.GetExtension(file).ToLowerInvariant())
        {
          case ".png":
            format = ImageFormat.Png;
            break;

          case ".jpg":
          case ".jpeg":
            format = ImageFormat.Jpeg;
            break;

          case ".bmp":
            format = ImageFormat.Bmp;
            break;

          case ".tif":
          case ".tiff":
            format = ImageFormat.Tiff;
            break;

          default:
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Image extension is unknown, assuming PNG.");
            format = ImageFormat.Png;
            break;
        }

        try
        {
          image.Save(file, format);
        }
        catch (Exception e)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Image could not be saved to disk: " + e.Message);
        }
      }

      image.Dispose();
    }
  }
}