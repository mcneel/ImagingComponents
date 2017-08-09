using System;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace GrasshopperImagingComponent
{
  public sealed class CreateGdiSolidFillComponent : GH_Component
  {
    public CreateGdiSolidFillComponent()
      : base("Create Gdi Fill", "Fill", "Create a GDI+ solid fill description.", "Display", "Image")
    { }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddColourParameter("Colour", "C", "Colour of fill.", GH_ParamAccess.item, Color.Black);
    }
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddTextParameter("Fill", "F", "Fill description", GH_ParamAccess.item);
    }
    protected override void SolveInstance(IGH_DataAccess access)
    {
      Color colour = Color.Black;
      if (!access.GetData(0, ref colour)) return;

      access.SetData(0, GdiCache.FormatFill(colour));
    }

    public static readonly Guid _componentId = new Guid("{94196AD1-DD6E-4583-9F57-0359B314C894}");
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
      get { return Properties.Resources.SolidFill_24x24; }
    }
  }

  public sealed class CreateGdiGradientFillComponent : GH_Component
  {
    public CreateGdiGradientFillComponent()
      : base("Create Gdi Gradient", "Gradient", "Create a GDI+ gradient fill description.", "Display", "Image")
    { }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddColourParameter("Colour 1", "C1", "Start colour of gradient.", GH_ParamAccess.item, Color.Crimson);
      pManager.AddColourParameter("Colour 2", "C2", "End colour of gradient.", GH_ParamAccess.item, Color.Teal);
      pManager.AddPointParameter("Point 1", "P1", "Start point of gradient", GH_ParamAccess.item, new Point3d(0, 0, 0));
      pManager.AddPointParameter("Point 2", "P2", "End point of gradient", GH_ParamAccess.item, new Point3d(100, 100, 0));
    }
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddTextParameter("Fill", "F", "Fill description", GH_ParamAccess.item);
    }
    protected override void SolveInstance(IGH_DataAccess access)
    {
      Color colour1 = Color.Black;
      Color colour2 = Color.Black;
      Point3d point1 = Point3d.Origin;
      Point3d point2 = Point3d.Origin;

      if (!access.GetData(0, ref colour1)) return;
      if (!access.GetData(1, ref colour2)) return;
      if (!access.GetData(2, ref point1)) return;
      if (!access.GetData(3, ref point2)) return;

      access.SetData(0, GdiCache.FormatFill(colour1, colour2, point1, point2));
    }

    public static readonly Guid _componentId = new Guid("{30666B7B-C761-4AEA-A7EC-5425B2FA191B}");
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
      get { return Properties.Resources.GradientFill_24x24; }
    }
  }
}