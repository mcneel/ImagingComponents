using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GH_IO;
using GH_IO.Serialization;
using GH_IO.Types;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace GrasshopperImagingComponent
{
  /// <summary>
  /// Base interface for all drawable elements.
  /// </summary>
  public interface IGdiGoo : IGH_Goo
  {
    /// <summary>
    /// Draw this shape onto a bitmap.
    /// </summary>
    /// <param name="graphics">Graphics object to draw with.</param>
    /// <param name="cache">Gdi cache.</param>
    void DrawShape(Graphics graphics, GdiCache cache);
  }

  /// <summary>
  /// Implementation of IGdiGoo for polygon shapes.
  /// </summary>
  public sealed class GdiShapeGoo : IGdiGoo
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
        Pen pen = cache.ParseEdge(description, out string _);
        if (pen != null)
          graphics.DrawLines(pen, polygon);
      }
    }
    private void DrawFills(Graphics graphics, GdiCache cache, PointF[] polygon)
    {
      foreach (string description in _fills)
      {
        Brush fill = cache.ParseFill(description, out string _);
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

  /// <summary>
  /// Implementation of IGdiGoo for text.
  /// </summary>
  public sealed class GdiTextGoo : IGdiGoo
  {
    #region constructors
    public GdiTextGoo()
    {
      Text = string.Empty;
      Font = string.Empty;
      Location = Point3d.Origin;
      Colour = Color.Transparent;
    }
    public GdiTextGoo(string text, string font, Point3d location, Color colour)
    {
      Text = text;
      Font = font;
      Location = location;
      Colour = colour;
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets the text to draw.
    /// </summary>
    public string Text { get; private set; }
    /// <summary>
    /// Gets the font to use.
    /// </summary>
    public string Font { get; private set; }
    /// <summary>
    /// Gets the location.
    /// </summary>
    public Point3d Location { get; private set; }
    /// <summary>
    /// Gets the colour.
    /// </summary>
    public Color Colour { get; private set; }

    public bool IsValid
    {
      get
      {
        if (string.IsNullOrWhiteSpace(Text)) return false;
        if (string.IsNullOrWhiteSpace(Font)) return false;
        if (!Location.IsValid) return false;
        return true;
      }
    }
    string IGH_Goo.IsValidWhyNot
    {
      get
      {
        if (string.IsNullOrWhiteSpace(Text)) return "No text";
        if (string.IsNullOrWhiteSpace(Font)) return "No font";
        if (!Location.IsValid) return "Invalid location";
        return null;
      }
    }
    string IGH_Goo.TypeName
    {
      get { return "GDI+ text"; }
    }
    string IGH_Goo.TypeDescription
    {
      get { return "A text entity drawn to a GDI+ bitmap."; }
    }
    #endregion

    #region casting
    IGH_Goo IGH_Goo.Duplicate()
    {
      GdiTextGoo goo = new GdiTextGoo(Text, Font, Location, Colour);
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
    public void DrawShape(Graphics graphics, GdiCache cache)
    {
      if (!IsValid) return;

      PointF point = cache.Projection.MapToBitmap(Location);
      Font font = cache.ParseFont(Font, out string _);
      if (font == null)
        font = SystemFonts.CaptionFont;

      Brush fill = new SolidBrush(Colour);
      SizeF size = graphics.MeasureString(Text, font);
      point.X -= 0.5f * size.Width;
      point.Y -= 0.5f * size.Height;

      graphics.DrawString(Text, font, fill, point);
      fill.Dispose();
    }
    #endregion

    #region deserialization
    bool GH_ISerializable.Write(GH_IWriter writer)
    {
      writer.SetString("Text", Text);
      writer.SetString("Font", Font);
      writer.SetPoint3D("Location", new GH_Point3D(Location.X, Location.Y, Location.Z));
      writer.SetDrawingColor("Colour", Colour);
      return true;
    }
    bool GH_ISerializable.Read(GH_IReader reader)
    {
      var pt = reader.GetPoint3D("Location");

      Text = reader.GetString("Text");
      Font = reader.GetString("Font");
      Location = new Point3d(pt.x, pt.y, pt.z);
      Colour = reader.GetDrawingColor("Colour");

      return true;
    }
    #endregion
  }
}