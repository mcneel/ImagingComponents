using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino;
using Rhino.Geometry;

namespace GrasshopperImagingComponent
{
  public sealed class CreateTextComponent : GH_Component
  {
    public CreateTextComponent()
      : base("Create Text", "Text", "Create a GDI+ text entity.", "Display", "Image")
    { }

    public static readonly Guid _componentId = new Guid("{0BE4294F-13FA-4179-9209-83B7994CAFC1}");
    public override Guid ComponentGuid
    {
      get { return _componentId; }
    }
    public override GH_Exposure Exposure
    {
      get { return GH_Exposure.secondary | GH_Exposure.obscure; }
    }
    protected override Bitmap Icon
    {
      get { return Properties.Resources.Text_24x24; }
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("Text", "T", "Text content", GH_ParamAccess.item);
      pManager.AddTextParameter("Font", "F", "Text font", GH_ParamAccess.item);
      pManager.AddPointParameter("Location", "L", "Text location", GH_ParamAccess.item);
      pManager.AddColourParameter("Colour", "C", "Text colour", GH_ParamAccess.item, Color.Black);
    }
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new GdiGooParameter(), "Text", "T", "GDI+ Text", GH_ParamAccess.item);
    }
    protected override void SolveInstance(IGH_DataAccess access)
    {
      string text = null;
      string font = null;
      Color colour = Color.Black;
      Point3d location = Point3d.Origin;

      if (!access.GetData(0, ref text)) return;
      if (!access.GetData(1, ref font)) return;
      if (!access.GetData(2, ref location)) return;
      if (!access.GetData(3, ref colour)) return;

      GdiTextGoo goo = new GdiTextGoo(text, font, colour, location, 0, 0, 0);
      access.SetData(0, goo);
    }
  }
  public sealed class CreateAlignedTextComponent : GH_Component
  {
    public CreateAlignedTextComponent()
      : base("Create Aligned Text", "AlignText", "Create an aligned GDI+ text entity.", "Display", "Image")
    { }

    public static readonly Guid _componentId = new Guid("{E73AD333-17CE-4D41-9BF2-713BF7FC2B48}");
    public override Guid ComponentGuid
    {
      get { return _componentId; }
    }
    public override GH_Exposure Exposure
    {
      get { return GH_Exposure.secondary | GH_Exposure.obscure; }
    }
    protected override Bitmap Icon
    {
      get { return Properties.Resources.TextAligned_24x24; }
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("Text", "T", "Text content", GH_ParamAccess.item);
      pManager.AddTextParameter("Font", "F", "Text font", GH_ParamAccess.item);
      pManager.AddPointParameter("Location", "L", "Text location", GH_ParamAccess.item);
      pManager.AddColourParameter("Colour", "C", "Text colour", GH_ParamAccess.item, Color.Black);
      pManager.AddNumberParameter("Horizontal", "H", "Normalised horizontal offset", GH_ParamAccess.item, 0.0);
      pManager.AddNumberParameter("Vertical", "V", "Normalised vertical offset", GH_ParamAccess.item, 0.0);
      pManager.AddAngleParameter("Angle", "A", "Rotation angle.", GH_ParamAccess.item, 0.0);

      Param_Number angle = pManager[6] as Param_Number;
      if (angle != null)
        angle.UseDegrees = true;
    }
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new GdiGooParameter(), "Text", "T", "GDI+ Text", GH_ParamAccess.item);
    }
    protected override void SolveInstance(IGH_DataAccess access)
    {
      string text = null;
      string font = null;
      Color colour = Color.Black;
      Point3d location = Point3d.Origin;
      double horizontal = 0.0;
      double vertical = 0.0;
      double rotation = 0.0;

      if (!access.GetData(0, ref text)) return;
      if (!access.GetData(1, ref font)) return;
      if (!access.GetData(2, ref location)) return;
      if (!access.GetData(3, ref colour)) return;
      if (!access.GetData(4, ref horizontal)) return;
      if (!access.GetData(5, ref vertical)) return;
      if (!access.GetData(6, ref rotation)) return;

      Param_Number param = (Param_Number)Params.Input[6];
      if (!param.UseDegrees)
        rotation = RhinoMath.ToDegrees(rotation);
      
      GdiTextGoo goo = new GdiTextGoo(text, font, colour, location, horizontal, vertical, rotation);
      access.SetData(0, goo);
    }
  }
}