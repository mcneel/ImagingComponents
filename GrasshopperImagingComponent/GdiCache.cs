using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using Rhino.Geometry;

namespace GrasshopperImagingComponent
{
  /// <summary>
  /// Provides mechanisms for parsing, caching and disposing GDI objects.
  /// Create one of these caches per bitmap, but do not reuse them.
  /// </summary>
  public sealed class GdiCache
  {
    #region caches
    private readonly SortedDictionary<string, Pen> _edges = new SortedDictionary<string, Pen>(StringComparer.Ordinal);
    private readonly SortedDictionary<string, Brush> _fills = new SortedDictionary<string, Brush>(StringComparer.Ordinal);
    private readonly SortedDictionary<string, Font> _fonts = new SortedDictionary<string, Font>(StringComparer.Ordinal);
    #endregion

    /// <summary>
    /// Create a new cache slaved to a specific projection.
    /// </summary>
    public GdiCache(BitmapProjection projection)
    {
      Projection = projection ?? throw new ArgumentNullException(nameof(projection));
    }

    #region cache access
    /// <summary>
    /// Gets the number of cached objects.
    /// </summary>
    public int CachedCount
    {
      get { return _edges.Count + _fills.Count + _fonts.Count; }
    }
    /// <summary>
    /// Clears the caches if they get too big. 
    /// </summary>
    private void ReleaseMemoryPressure()
    {
      if (CachedCount > 100)
        Clear();
    }
    /// <summary>
    /// Disposes and clears all cached objects.
    /// This method *must* be called after the last GDI object has been retrieved.
    /// </summary>
    public void Clear()
    {
      foreach (Pen pen in _edges.Values)
        pen.Dispose();
      _edges.Clear();

      foreach (Brush brush in _fills.Values)
        brush.Dispose();
      _fills.Clear();

      foreach (Font font in _fonts.Values)
        font.Dispose();
      _fonts.Clear();
    }

    public BitmapProjection Projection { get; }
    #endregion

    #region formatters
    /// <summary>
    /// Create a valid descriptor of an edge.
    /// </summary>
    public static string FormatEdge(double width, Color colour, LineCap cap, DashCap dashCap, double[] dashPattern)
    {
      List<string> fragments = new List<string>(5);

      fragments.Add(string.Format("width={0:0.##}", width));

      if (colour.A == 255)
        fragments.Add(string.Format("colour=({0},{1},{2})", colour.R, colour.G, colour.B));
      else
        fragments.Add(string.Format("colour=({0},{1},{2},{3})", colour.R, colour.G, colour.B, colour.A));

      for (int i = 0; i < _capValues.Length; i++)
        if (_capValues[i] == cap)
        {
          fragments.Add("cap=" + _capNames[i]);
          break;
        }

      if (dashPattern != null && dashPattern.Length > 0)
      {
        for (int i = 0; i < _dashCapValues.Length; i++)
          if (_dashCapValues[i] == dashCap)
          {
            fragments.Add("dashcap=" + _dashCapNames[i]);
            break;
          }
        fragments.Add(string.Format("pattern=({0})", string.Join(",", dashPattern)));
      }

      return "edge: " + string.Join(", ", fragments);
    }

    /// <summary>
    /// Create a valid descriptor of a fill.
    /// </summary>
    public static string FormatFill(Color colour)
    {
      string c;
      if (colour.A == 255)
        c = string.Format("colour=({0},{1},{2})", colour.R, colour.G, colour.B);
      else
        c = string.Format("colour=({0},{1},{2},{3})", colour.R, colour.G, colour.B, colour.A);

      return "fill: " + c;
    }
    /// <summary>
    /// Create a valid descriptor of a fill.
    /// </summary>
    public static string FormatFill(Color colour1, Color colour2, Point3d point1, Point3d point2)
    {
      if (colour1 == colour2)
        return FormatFill(colour1);

      List<string> fragments = new List<string>(5);

      if (colour1.A == 255)
        fragments.Add(string.Format("colour1=({0},{1},{2})", colour1.R, colour1.G, colour1.B));
      else
        fragments.Add(string.Format("colour1=({0},{1},{2},{3})", colour1.R, colour1.G, colour1.B, colour1.A));

      if (colour2.A == 255)
        fragments.Add(string.Format("colour2=({0},{1},{2})", colour2.R, colour2.G, colour2.B));
      else
        fragments.Add(string.Format("colour2=({0},{1},{2},{3})", colour2.R, colour2.G, colour2.B, colour2.A));

      if (Math.Abs(point1.Z) < 1e-12)
        fragments.Add(string.Format("point1=({0:0.##}, {1:0.##})", point1.X, point1.Y));
      else
        fragments.Add(string.Format("point1=({0:0.##}, {1:0.##}, {2:0.##})", point1.X, point1.Y, point1.Z));

      if (Math.Abs(point2.Z) < 1e-12)
        fragments.Add(string.Format("point2=({0:0.##}, {1:0.##})", point2.X, point2.Y));
      else
        fragments.Add(string.Format("point2=({0:0.##}, {1:0.##}, {2:0.##})", point2.X, point2.Y, point2.Z));

      return "fill: " + string.Join(", ", fragments);
    }
    #endregion

    #region parsers
    /* 
     * Pen patterns look like this:
     *   edge: width=4.3, colo[u]r=(75,108,255), [cap=round], [pattern=(4,2,4,3)], [dashcap=flat]
     *   edge: width=4.3, colo[u]r=#ffab520c,    [cap=round], [pattern=(4,2,4,3)], [dashcap=flat]
     * 
     * Fill patterns look like this:
     *   fill: colo[u]r=(75,108,255)
     *   fill: colo[u]r=#ffab520c
     *   fill: colo[u]r1=(75,108,255), colo[u]r2=(255,155,0), point1=(50,80), point2=(200,80), [wrap=something]
     *   fill: colo[u]r1=#ffab520c,    colo[u]r2=(255,155,0), point1=(50,80), point2=(200,80), [wrap=something]
    */

    /// <summary>
    /// Parse a description and return the key/value pairs therein.
    /// </summary>
    /// <param name="description">Description to parse.</param>
    /// <param name="message">Error message, if any.</param>
    /// <returns>Description dictionary.</returns>
    private static Dictionary<string, string> ParseDescription(string description, out string message)
    {
      if (string.IsNullOrWhiteSpace(description))
      {
        message = "Description is blank.";
        return null;
      }

      // Replace commas inside parenthesis with semi-colons.
      {
        char[] chars = description.ToCharArray();
        int depth = 0;
        for (int i = 0; i < chars.Length; i++)
        {
          if (chars[i] == '(')
            depth++;
          else if (chars[i] == ')')
            depth--;
          else if (chars[i] == ',' && depth > 0)
            chars[i] = ';';
        }
        description = new string(chars);
      }

      int colon = description.IndexOf(":", StringComparison.Ordinal);
      if (colon < 0)
      {
        message = "Description lacks a type indicator.";
        return null;
      }

      Dictionary<string, string> dic = new Dictionary<string, string>(StringComparer.Ordinal);
      dic.Add("type", description.Substring(0, colon));
      description = description.Substring(colon + 1);

      string[] entries = description.Split(',');
      if (entries.Length == 0)
      {
        message = "Description lacks any entries.";
        return null;
      }

      foreach (string entry in entries)
      {
        string[] parts = entry.Split('=');
        if (parts.Length != 2)
        {
          message = string.Format("Entry '{0}' incorrectly formatted.", entry);
          return null;
        }

        string key = parts[0].Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
          message = string.Format("Key for '{0}' incorrectly formatted.", entry);
          return null;
        }
        key = key.Replace("color", "colour");

        string val = parts[1].Trim();
        val = val.Replace(";", ",");
        val = val.Replace("(", string.Empty);
        val = val.Replace(")", string.Empty);
        if (string.IsNullOrWhiteSpace(key))
        {
          message = string.Format("Value for '{0}' incorrectly formatted.", entry);
          return null;
        }

        if (dic.ContainsKey(key))
        {
          message = string.Format("Duplicate key '{0}' in description.", key);
          return null;
        }

        dic.Add(key, val);
      }

      message = null;
      return dic;
    }
    /// <summary>
    /// Try and parse a colour description.
    /// </summary>
    private static bool TryParseColour(string text, out Color colour)
    {
      colour = Color.Transparent;

      if (text.StartsWith("#"))
      {
        colour = ColorTranslator.FromHtml(text);
        return true;
      }

      int[] values = ToIntegerArray(text);
      if (values.Length == 3 || values.Length == 4)
      {
        for (int i = 0; i < values.Length; i++)
        {
          if (values[i] < 0) return false;
          if (values[i] > 255) return false;
        }

        if (values.Length == 3)
          colour = Color.FromArgb(values[0], values[1], values[2]);
        else
          colour = Color.FromArgb(values[3], values[0], values[1], values[2]);
        return true;
      }

      return false;
    }
    /// <summary>
    /// Try and parse a point description.
    /// </summary>
    private static bool TryParsePoint(string text, out Point3d point)
    {
      point = Point3d.Origin;

      float[] values = ToFloatArray(text);
      if (values.Length == 2)
      {
        point = new Point3d(values[0], values[1], 0);
        return true;
      }
      if (values.Length == 3)
      {
        point = new Point3d(values[0], values[1], values[2]);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Convert a list of textual values to an integer array.
    /// </summary>
    private static int[] ToIntegerArray(string list)
    {
      if (string.IsNullOrWhiteSpace(list)) return null;
      string[] fragments = list.Split(',');

      int[] values = new int[fragments.Length];
      for (int i = 0; i < fragments.Length; i++)
        if (int.TryParse(fragments[i], out int value))
          values[i] = value;
        else
          return null;

      return values;
    }
    /// <summary>
    /// Convert a list of textual values to a float array.
    /// </summary>
    private static float[] ToFloatArray(string list)
    {
      if (string.IsNullOrWhiteSpace(list)) return null;
      string[] fragments = list.Split(',');

      float[] values = new float[fragments.Length];
      for (int i = 0; i < fragments.Length; i++)
        if (float.TryParse(fragments[i], out float value))
          values[i] = value;
        else
          return null;

      return values;
    }
    /// <summary>
    /// Parse a set of named constants.
    /// </summary>
    private static T ParseName<T>(string key, string[] keys, T[] values, T defaultValue)
    {
      if (string.IsNullOrWhiteSpace(key)) return defaultValue;
      for (int i = 0; i < keys.Length; i++)
        if (string.Equals(key, keys[i], StringComparison.OrdinalIgnoreCase))
          return values[i];
      return defaultValue;
    }

    /// <summary>
    /// Parse a piece of string and return the Pen it describes. 
    /// Do *not* dispose of the object returned by this method and
    /// do *not* cache it.
    /// </summary>
    /// <param name="description">Pen description.</param>
    /// <param name="message">If parsing is not successful, the error message will be returned here.</param>
    /// <returns>Pen on successful parse, or null.</returns>
    public Pen ParseEdge(string description, out string message)
    {
      description = description.ToLowerInvariant();
      message = null;

      if (_edges.TryGetValue(description, out Pen edge))
        return edge;

      Dictionary<string, string> dic = ParseDescription(description, out message);
      if (dic == null)
        return null;

      // Validate the existence of specific keys.
      if (!dic.ContainsKey("type"))
      {
        message = "Descriptor does not contain a type indicator.";
        return null;
      }
      if (!dic.ContainsKey("width"))
      {
        message = "Pen description does not contain a width value.";
        return null;
      }
      if (!dic.ContainsKey("colour"))
      {
        message = "Pen description does not contain a colour value.";
        return null;
      }

      // Validate the values of specific keys. 
      if (!string.Equals(dic["type"], "edge", StringComparison.Ordinal))
      {
        message = "This is not an edge descriptor.";
        return null;
      }

      // Extract values.
      if (!float.TryParse(dic["width"], out float width))
      {
        message = "Width value is not a valid number.";
        return null;
      }

      if (!TryParseColour(dic["colour"], out Color colour))
      {
        message = "Colour value is not valid.";
        return null;
      }

      LineCap lineCap = LineCap.Round;
      if (dic.TryGetValue("cap", out string cap))
        lineCap = ParseName(cap, _capNames, _capValues, lineCap);

      DashCap dashCap = DashCap.Flat;
      if (dic.TryGetValue("dashcap", out string dashcap))
        dashCap = ParseName(dashcap, _dashCapNames, _dashCapValues, dashCap);

      float[] pattern = null;
      if (dic.TryGetValue("pattern", out string dashes))
      {
        pattern = ToFloatArray(dashes);
        if (pattern == null)
        {
          message = "Invalid dash pattern.";
          return null;
        }
      }

      edge = new Pen(colour, width)
      {
        StartCap = lineCap,
        EndCap = lineCap,
        DashCap = dashCap,
        LineJoin = LineJoin.Round
      };

      if (pattern != null && pattern.Length > 0)
        edge.DashPattern = pattern;

      ReleaseMemoryPressure();
      _edges.Add(description, edge);
      return edge;
    }

    public static readonly string[] _capNames = { "flat", "round", "square", "sharp", "dot", "box", "arrow" };
    public static readonly LineCap[] _capValues = { LineCap.Flat, LineCap.Round, LineCap.Square, LineCap.Triangle, LineCap.RoundAnchor, LineCap.SquareAnchor, LineCap.ArrowAnchor };
    public static readonly string[] _dashCapNames = { "flat", "round", "sharp" };
    public static readonly DashCap[] _dashCapValues = { DashCap.Flat, DashCap.Round, DashCap.Triangle };

    /// <summary>
    /// Parse a piece of string and return the Brush it describes. 
    /// Do *not* dispose of the object returned by this method and
    /// do *not* cache it.
    /// </summary>
    /// <param name="description">Brush description.</param>
    /// <param name="message">If parsing is not successful, the error message will be returned here.</param>
    /// <returns>Brush on successful parse, or null.</returns>
    public Brush ParseFill(string description, out string message)
    {
      description = description.ToLowerInvariant();
      message = null;

      if (_fills.TryGetValue(description, out Brush fill))
        return fill;

      Dictionary<string, string> dic = ParseDescription(description, out message);
      if (dic == null)
        return null;

      // Validate the existence of specific keys.
      if (!dic.ContainsKey("type"))
      {
        message = "Descriptor does not contain a type indicator.";
        return null;
      }

      if (dic.TryGetValue("colour", out string colour))
      {
        if (TryParseColour(colour, out Color solidColour))
        {
          ReleaseMemoryPressure();
          fill = new SolidBrush(solidColour);
          _fills.Add(description, fill);
          return fill;
        }
        message = "Invalid colour field";
        return null;
      }

      dic.TryGetValue("colour1", out string colour1);
      dic.TryGetValue("colour2", out string colour2);
      dic.TryGetValue("point1", out string point1);
      dic.TryGetValue("point2", out string point2);

      if (string.IsNullOrWhiteSpace(colour1) ||
          string.IsNullOrWhiteSpace(colour2) ||
          string.IsNullOrWhiteSpace(point1) ||
          string.IsNullOrWhiteSpace(point2))
      {
        message = "Invalid gradient";
        return null;
      }

      if (!TryParseColour(colour1, out Color linearColour1))
      {
        message = "Invalid colour1 entry.";
        return null;
      }
      if (!TryParseColour(colour2, out Color linearColour2))
      {
        message = "Invalid colour2 entry.";
        return null;
      }
      if (!TryParsePoint(point1, out Point3d linearPoint1))
      {
        message = "Invalid point1 entry.";
        return null;
      }
      if (!TryParsePoint(point2, out Point3d linearPoint2))
      {
        message = "Invalid point2 entry.";
        return null;
      }

      PointF p1 = Projection.MapToBitmap(linearPoint1);
      PointF p2 = Projection.MapToBitmap(linearPoint2);

      LinearGradientBrush gradient =
        new LinearGradientBrush(p1, p2, linearColour1, linearColour2)
        {
          GammaCorrection = true,
          WrapMode = WrapMode.TileFlipXY
        };

      fill = gradient;

      ReleaseMemoryPressure();
      _fills.Add(description, fill);
      return fill;
    }
    #endregion
  }
}