using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GH_IO;
using GH_IO.Serialization;
using GH_IO.Types;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;

namespace GrasshopperImagingComponent
{
  public sealed class GdiShapeGoo : IGH_Goo
  {
    #region fields
    private Point3d[] _points;
    private readonly List<string> _edges = new List<string>();
    private readonly List<string> _fills = new List<string>();
    #endregion

    #region constructors
    public GdiShapeGoo()
    {
      _points = new Point3d[0];
      DrawFillsBeforeEdges = true;
    }
    public GdiShapeGoo(IEnumerable<Point3d> shape, IEnumerable<string> edges, IEnumerable<string> fills)
    {
      _points = shape?.ToArray() ?? throw new ArgumentNullException(nameof(shape));
      if (edges != null) _edges.AddRange(edges);
      if (fills != null) _fills.AddRange(fills);
      DrawFillsBeforeEdges = true;
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets or sets whether fills are drawn before (i.e. underneath) edges.
    /// </summary>
    public bool DrawFillsBeforeEdges { get; set; }

    public bool IsValid
    {
      get
      {
        if (_points.Length < 2) return false;
        if (_edges.Count + _fills.Count == 0) return false;
        return true;
      }
    }
    string IGH_Goo.IsValidWhyNot
    {
      get
      {
        if (_points.Length < 2) return "No shape vertices.";
        if (_edges.Count + _fills.Count == 0) return "No shape edge or fill properties.";
        return null;
      }
    }
    string IGH_Goo.TypeName
    {
      get { return "GDI+ shape"; }
    }
    string IGH_Goo.TypeDescription
    {
      get { return "A shape drawn to a GDI+ bitmap."; }
    }
    #endregion

    #region casting
    IGH_Goo IGH_Goo.Duplicate()
    {
      GdiShapeGoo goo = new GdiShapeGoo(_points, _edges, _fills);
      goo.DrawFillsBeforeEdges = DrawFillsBeforeEdges;
      return goo;
    }
    IGH_GooProxy IGH_Goo.EmitProxy()
    {
      return null;
    }
    bool IGH_Goo.CastFrom(object source)
    {
      return false;
    }
    bool IGH_Goo.CastTo<T>(out T target)
    {
      target = default(T);
      return false;
    }
    object IGH_Goo.ScriptVariable()
    {
      return this;
    }
    #endregion

    #region drawing
    /// <summary>
    /// Draw this shape onto a bitmap.
    /// </summary>
    /// <param name="graphics">Graphics object to draw with.</param>
    /// <param name="cache">Gdi cache.</param>
    /// <param name="boundary">Bitmap boundary in world space.</param>
    /// <param name="width">Horizontal resolution.</param>
    /// <param name="height">Vertical resolution.</param>
    public void DrawShape(Graphics graphics, GdiCache cache)
    {
      if (!IsValid) return;

      PointF[] polygon = new PointF[_points.Length];
      for (int i = 0; i < _points.Length; i++)
        polygon[i] = cache.Projection.MapToBitmap(_points[i]);

      if (DrawFillsBeforeEdges)
      {
        DrawFills(graphics, cache, polygon);
        DrawEdges(graphics, cache, polygon);
      }
      else
      {
        DrawEdges(graphics, cache, polygon);
        DrawFills(graphics, cache, polygon);
      }
    }
    private void DrawEdges(Graphics graphics, GdiCache cache, PointF[] polygon)
    {
      foreach (string description in _edges)
      {
        Pen pen = cache.ParseEdge(description, out string message);
        if (pen != null)
          graphics.DrawLines(pen, polygon);
      }
    }
    private void DrawFills(Graphics graphics, GdiCache cache, PointF[] polygon)
    {
      foreach (string description in _fills)
      {
        Brush fill = cache.ParseFill(description, out string message);
        if (fill != null)
          graphics.FillPolygon(fill, polygon);
      }
    }
    #endregion

    #region deserialization
    bool GH_ISerializable.Write(GH_IWriter writer)
    {
      writer.SetInt32("PointCount", _points.Length);
      for (int i = 0; i < _points.Length; i++)
        writer.SetPoint3D("Point", i, new GH_Point3D(_points[i].X, _points[i].Y, _points[i].Z));

      writer.SetInt32("EdgeCount", _edges.Count);
      for (int i = 0; i < _edges.Count; i++)
        writer.SetString("Edge", i, _edges[i]);

      writer.SetInt32("FillCount", _fills.Count);
      for (int i = 0; i < _fills.Count; i++)
        writer.SetString("Fill", i, _fills[i]);

      writer.SetBoolean("FillsBeforeEdges", DrawFillsBeforeEdges);
      return true;
    }
    bool GH_ISerializable.Read(GH_IReader reader)
    {
      _edges.Clear();
      _fills.Clear();

      int pointCount = reader.GetInt32("PointCount");
      _points = new Point3d[pointCount];
      for (int i = 0; i < pointCount; i++)
      {
        GH_Point3D pt = reader.GetPoint3D("Point", i);
        _points[i] = new Point3d(pt.x, pt.y, pt.z);
      }

      int edgeCount = reader.GetInt32("EdgeCount");
      for (int i = 0; i < edgeCount; i++)
        _edges.Add(reader.GetString("Edge", i));

      int faceCount = reader.GetInt32("FillCount");
      for (int i = 0; i < faceCount; i++)
        _fills.Add(reader.GetString("Fill", i));

      return true;
    }
    #endregion
  }

  public sealed class BitmapProjection
  {
    private readonly Plane _plane;
    private readonly double _uFactor;
    private readonly double _vFactor;

    /// <summary>
    /// Create a new projection.
    /// </summary>
    /// <param name="boundary">Bitmap boundary.</param>
    /// <param name="width">Horizontal resolution.</param>
    /// <param name="height">Vertical resolution.</param>
    public BitmapProjection(Rectangle3d boundary, int width, int height)
    {
      Boundary = boundary;
      Width = width;
      Height = height;

      _plane = boundary.Plane;
      _plane.Origin = boundary.Corner(0);

      _uFactor = 1.0 / boundary.Width;
      _vFactor = 1.0 / boundary.Height;
    }

    /// <summary>
    /// Gets the boundary of the bitmap in world space.
    /// </summary>
    public Rectangle3d Boundary { get; }
    /// <summary>
    /// Gets the number of pixels along the width of the bitmap.
    /// </summary>
    public int Width { get; }
    /// <summary>
    /// Gets the number of pixels along the height of the bitmap.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Map a point from world space to bitmap space.
    /// </summary>
    /// <param name="point">Point in world space.</param>
    public PointF MapToBitmap(Point3d point)
    {
      _plane.ClosestParameter(point, out double u, out double v);
      u = Width * u * _uFactor;
      v = Height * v * _vFactor;
      v = Height - v;
      return new PointF((float)u, (float)v);
    }
  }

  public sealed class GdiShapeParameter : GH_PersistentParam<GdiShapeGoo>
  {
    public GdiShapeParameter()
      : base("Gdi geometry", "GdiGeo", "Store shapes/geometry used in drawing images.", "Display", "Image")
    { }
    public GdiShapeParameter(GH_InstanceDescription nTag) : base(nTag) { }
    public GdiShapeParameter(GH_InstanceDescription nTag, bool bIsListParam) : base(nTag, bIsListParam) { }
    public GdiShapeParameter(string name, string nickname, string description, string category, string subcategory)
      : base(name, nickname, description, category, subcategory) { }

    public static readonly Guid _componentId = new Guid("{84BC010B-3D24-4A42-A262-F6B7AA1EDAEE}");
    public override Guid ComponentGuid
    {
      get { return _componentId; }
    }
    public override GH_Exposure Exposure
    {
      get { return GH_Exposure.hidden; }
    }
    protected override Bitmap Icon
    {
      get { return base.Icon; }
    }

    protected override GH_GetterResult Prompt_Singular(ref GdiShapeGoo value)
    {
      return GH_GetterResult.cancel;
    }
    protected override GH_GetterResult Prompt_Plural(ref List<GdiShapeGoo> values)
    {
      return GH_GetterResult.cancel;
    }

    protected override GdiShapeGoo InstantiateT()
    {
      return new GdiShapeGoo();
    }
  }
}