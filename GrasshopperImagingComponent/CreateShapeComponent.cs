using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace GrasshopperImagingComponent
{
  public sealed class CreateShapeComponent : GH_Component
  {
    public CreateShapeComponent()
      : base("Create Gdi Shape", "Shape", "Create a GDI+ shape.", "Display", "Image")
    { }

    public static readonly Guid _componentId = new Guid("{BB034582-210C-460A-A052-028ABFD86CD9}");
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
      get { return Properties.Resources.Shape_24x24; }
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddCurveParameter("Geometry", "G", "Shape geometry", GH_ParamAccess.item);
      pManager.AddTextParameter("Edge", "E", "Optional edge description", GH_ParamAccess.item);
      pManager.AddTextParameter("Fill", "F", "Optional fill description", GH_ParamAccess.item);

      pManager[1].Optional = true;
      pManager[2].Optional = true;
    }
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new GdiShapeParameter(), "Shape", "S", "GDI+ Shape", GH_ParamAccess.item);
    }
    protected override void SolveInstance(IGH_DataAccess access)
    {
      Curve curve = null;
      string edge = null;
      string fill = null;

      if (!access.GetData(0, ref curve)) return;
      access.GetData(1, ref edge);
      access.GetData(2, ref fill);

      if (string.IsNullOrWhiteSpace(edge) && string.IsNullOrWhiteSpace(fill))
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Either an edge or a fill is required.");
        return;
      }

      Polyline polyline;
      if (!curve.TryGetPolyline(out polyline))
      {
        curve = curve.ToPolyline(0.1, RhinoMath.ToRadians(0.2), 0.01, 100);
        curve.TryGetPolyline(out polyline);
      }
      if (polyline == null)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Curve could not be converted to polyline.");
        return;
      }

      List<string> edges = new List<string>();
      List<string> fills = new List<string>();

      if (!string.IsNullOrWhiteSpace(edge)) edges.Add(edge);
      if (!string.IsNullOrWhiteSpace(fill)) fills.Add(fill);

      GdiShapeGoo goo = new GdiShapeGoo(polyline, edges, fills)
      {
        DrawFillsBeforeEdges = true
      };
      access.SetData(0, goo);
    }
  }

  public sealed class CreateShapeComplexComponent : GH_Component
  {
    public CreateShapeComplexComponent()
      : base("Create Gdi Complex", "Complex", "Create a complex GDI+ shape.", "Display", "Image")
    { }

    public static readonly Guid _componentId = new Guid("{D223B7BE-057B-4189-B4EB-04F36885F448}");
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
      get { return Properties.Resources.Complex_24x24; }
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddCurveParameter("Geometry", "G", "Shape geometry", GH_ParamAccess.item);
      pManager.AddTextParameter("Edges", "Es", "Optional list of edge descriptions", GH_ParamAccess.list);
      pManager.AddTextParameter("Fills", "Fs", "Optional list of fill descriptions", GH_ParamAccess.list);
      pManager.AddBooleanParameter("Order", "O", "Draw fills before edges", GH_ParamAccess.item, true);

      pManager[1].Optional = true;
      pManager[2].Optional = true;
    }
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new GdiShapeParameter(), "Shape", "S", "GDI+ Shape", GH_ParamAccess.item);
    }
    protected override void SolveInstance(IGH_DataAccess access)
    {
      Curve curve = null;
      List<string> edges = new List<string>();
      List<string> fills = new List<string>();
      bool order = true;

      if (!access.GetData(0, ref curve)) return;
      if (!access.GetData(3, ref order)) return;

      access.GetDataList(1, edges);
      access.GetDataList(2, fills);

      for (int i = edges.Count -1 ; i >= 0; i--)
        if (string.IsNullOrWhiteSpace(edges[i]))
          edges.RemoveAt(i);

      for (int i = fills.Count - 1; i >= 0; i--)
        if (string.IsNullOrWhiteSpace(fills[i]))
          fills.RemoveAt(i);

      if (edges.Count == 0 && fills.Count == 0)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "At least a single edge or fill is required.");
        return;
      }

      Polyline polyline;
      if (!curve.TryGetPolyline(out polyline))
      {
        curve = curve.ToPolyline(0.1, RhinoMath.ToRadians(0.2), 0.01, 100);
        curve.TryGetPolyline(out polyline);
      }
      if (polyline == null)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Curve could not be converted to polyline.");
        return;
      }

      GdiShapeGoo goo = new GdiShapeGoo(polyline, edges, fills)
      {
        DrawFillsBeforeEdges = order
      };
      access.SetData(0, goo);
    }
  }
}