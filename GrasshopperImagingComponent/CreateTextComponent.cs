using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace GrasshopperImagingComponent
{
  public sealed class CreateTextComponent : GH_Component
  {
    public CreateTextComponent()
      : base("Create Gdi Text", "Text", "Create a GDI+ text entity.", "Display", "Image")
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

      GdiTextGoo goo = new GdiTextGoo(text, font, location, colour);
      access.SetData(0, goo);
    }
  }
}