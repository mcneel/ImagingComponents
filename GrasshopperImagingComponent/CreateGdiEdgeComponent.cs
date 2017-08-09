using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace GrasshopperImagingComponent
{
  public sealed class CreateGdiEdgeComponent : GH_Component
  {
    public CreateGdiEdgeComponent()
      : base("Create Gdi Edge", "Edge", "Create a GDI+ edge description.", "Display", "Image")
    { }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddNumberParameter("Width", "W", "Width of edge (in pixels).", GH_ParamAccess.item, 2);
      pManager.AddColourParameter("Colour", "C", "Colour of edge.", GH_ParamAccess.item, Color.Black);
      pManager.AddIntegerParameter("End Cap", "E", "End cap type", GH_ParamAccess.item, 0);
      pManager.AddIntegerParameter("Dash Cap", "D", "Dash cap type", GH_ParamAccess.item, 0);
      pManager.AddNumberParameter("Dash pattern", "P", "Dash pattern as a collection of dash+gap lengths.",
        GH_ParamAccess.list);

      pManager[4].Optional = true;

      Param_Integer cap0 = pManager[2] as Param_Integer;
      if (cap0 != null)
        for (int i = 0; i < GdiCache._capNames.Length; i++)
          cap0.AddNamedValue(GdiCache._capNames[i], (int)GdiCache._capValues[i]);

      Param_Integer cap1 = pManager[3] as Param_Integer;
      if (cap1 != null)
        for (int i = 0; i < GdiCache._dashCapNames.Length; i++)
          cap1.AddNamedValue(GdiCache._dashCapNames[i], (int)GdiCache._dashCapValues[i]);
    }
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddTextParameter("Edge", "E", "Edge description", GH_ParamAccess.item);
    }
    protected override void SolveInstance(IGH_DataAccess access)
    {
      double width = 0.0;
      Color colour = Color.Black;
      int lineCap = 0;
      int dashCap = 0;

      if (!access.GetData(0, ref width)) return;
      if (!access.GetData(1, ref colour)) return;
      if (!access.GetData(2, ref lineCap)) return;
      if (!access.GetData(3, ref dashCap)) return;

      List<double> pattern = new List<double>();
      access.GetDataList(4, pattern);

      LineCap actualLineCap = LineCap.Round;
      foreach (var cap in GdiCache._capValues)
        if (lineCap == (int)cap)
          actualLineCap = cap;

      DashCap actualDashCap = DashCap.Flat;
      foreach (var cap in GdiCache._dashCapValues)
        if (dashCap == (int)cap)
          actualDashCap = cap;

      access.SetData(0, GdiCache.FormatEdge((float)width, colour, actualLineCap, actualDashCap, pattern.ToArray()));
    }

    public static readonly Guid _componentId = new Guid("{E913D12D-F9D6-4319-97C1-8CB2120655E8}");
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
      get { return Properties.Resources.Edge_24x24; }
    }
  }

  public sealed class CreateGdiFontComponent : GH_Component
  {
    public CreateGdiFontComponent()
      : base("Create Gdi Font", "Font", "Create a GDI+ font description.", "Display", "Image")
    { }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("Typeface", "T", "Typeface name.", GH_ParamAccess.item, FontFamily.GenericSansSerif.Name);
      pManager.AddNumberParameter("Size", "S", "Font size (in Rhino units).", GH_ParamAccess.item, 1);
      pManager.AddBooleanParameter("Bold", "B", "Bold state", GH_ParamAccess.item, false);
      pManager.AddBooleanParameter("Italic", "I", "Italic state", GH_ParamAccess.item, false);
    }
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddTextParameter("Font", "F", "Font description", GH_ParamAccess.item);
    }
    protected override void SolveInstance(IGH_DataAccess access)
    {
      string name = string.Empty;
      double size = 0.0;
      bool bold = false;
      bool italic = false;

      if (!access.GetData(0, ref name)) return;
      if (!access.GetData(1, ref size)) return;
      if (!access.GetData(2, ref bold)) return;
      if (!access.GetData(3, ref italic)) return;

      access.SetData(0, GdiCache.FormatFont(name, (float)size, bold, italic));
    }

    public static readonly Guid _componentId = new Guid("{5F4D11BA-C072-4C6B-84F3-C7B9757C86E4}");
    public override Guid ComponentGuid
    {
      get { return _componentId; }
    }
    public override GH_Exposure Exposure
    {
      get { return GH_Exposure.primary | GH_Exposure.obscure; }
    }
    protected override Bitmap Icon
    {
      get { return Properties.Resources.Font_24x24; }
    }
  }
}