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

    /// <summary>
    /// Gets the projection associated with this cache.
    /// </summary>
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
      fragments.Add("colour=" + ExtensionMethods.FormatColour(colour));

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
      return "fill: " + "colour=" + ExtensionMethods.FormatColour(colour);
    }
    /// <summary>
    /// Create a valid descriptor of a fill.
    /// </summary>
    public static string FormatFill(Color colour1, Color colour2, Point3d point1, Point3d point2)
    {
      if (colour1 == colour2)
        return FormatFill(colour1);

      string[] fragments = new string[6];
      fragments[0] = "colour1=" + ExtensionMethods.FormatColour(colour1);
      fragments[1] = "colour2=" + ExtensionMethods.FormatColour(colour2);
      fragments[2] = "point1=" + ExtensionMethods.FormatPoint(point1);
      fragments[3] = "point2=" + ExtensionMethods.FormatPoint(point2);
      fragments[4] = "flip=yes";
      fragments[5] = "gamma=no";
      return "fill: " + string.Join(", ", fragments);
    }
    /// <summary>
    /// Create a valid descriptor of a font.
    /// </summary>
    /// <param name="typeface">Typeface name.</param>
    /// <param name="size">Point size.</param>
    /// <param name="bold">True if bold (assuming available in typeface).</param>
    /// <param name="italic">True if italic (assuming available in typeface).</param>
    public static string FormatFont(string typeface, float size, bool bold, bool italic)
    {
      string[] fragments = new string[4];
      fragments[0] = "typeface=" + typeface;
      fragments[1] = "size=" + string.Format("{0:0.##}", size);
      fragments[2] = "bold=" + (bold ? "yes" : "no");
      fragments[3] = "italic=" + (italic ? "yes" : "no");
      return "font: " + string.Join(", ", fragments);
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

      Dictionary<string, string> dic = ExtensionMethods.ParseDescription(description, out message);
      if (dic == null)
        return null;

      if (!string.Equals(dic["type"], "edge", StringComparison.Ordinal))
      {
        message = "This is not an edge descriptor.";
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

      if (!float.TryParse(dic["width"], out float width))
      {
        message = "Width value is not a valid number.";
        return null;
      }
      if (!ExtensionMethods.TryParseColour(dic["colour"], out Color colour))
      {
        message = "Colour value is not valid.";
        return null;
      }

      LineCap lineCap = LineCap.Round;
      if (dic.TryGetValue("cap", out string cap))
        lineCap = ExtensionMethods.ParseNamedValues(cap, _capNames, _capValues, lineCap);

      DashCap dashCap = DashCap.Flat;
      if (dic.TryGetValue("dashcap", out string dashcap))
        dashCap = ExtensionMethods.ParseNamedValues(dashcap, _dashCapNames, _dashCapValues, dashCap);

      float[] pattern = null;
      if (dic.TryGetValue("pattern", out string dashes))
      {
        pattern = ExtensionMethods.ToFloatArray(dashes);
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

      Dictionary<string, string> dic = ExtensionMethods.ParseDescription(description, out message);
      if (dic == null)
        return null;

      if (!dic["type"].Equals("fill", StringComparison.OrdinalIgnoreCase))
      {
        message = "Descriptor is not a fill type.";
        return null;
      }

      // Solid fills.
      if (dic.TryGetValue("colour", out string colour))
      {
        if (ExtensionMethods.TryParseColour(colour, out Color solidColour))
        {
          ReleaseMemoryPressure();
          fill = new SolidBrush(solidColour);
          _fills.Add(description, fill);
          return fill;
        }
        message = "Invalid colour field";
        return null;
      }

      // Gradient fills.
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

      if (!ExtensionMethods.TryParseColour(colour1, out Color linearColour1))
      {
        message = "Invalid colour1 entry.";
        return null;
      }
      if (!ExtensionMethods.TryParseColour(colour2, out Color linearColour2))
      {
        message = "Invalid colour2 entry.";
        return null;
      }
      if (!ExtensionMethods.TryParsePoint(point1, out Point3d linearPoint1))
      {
        message = "Invalid point1 entry.";
        return null;
      }
      if (!ExtensionMethods.TryParsePoint(point2, out Point3d linearPoint2))
      {
        message = "Invalid point2 entry.";
        return null;
      }

      PointF p1 = Projection.MapToBitmap(linearPoint1);
      PointF p2 = Projection.MapToBitmap(linearPoint2);

      LinearGradientBrush gradient = new LinearGradientBrush(p1, p2, linearColour1, linearColour2)
      {
        GammaCorrection = dic.GetYesNo("gamma", true),
        WrapMode = dic.GetYesNo("flip", true) ? WrapMode.TileFlipXY : WrapMode.Tile
      };
      fill = gradient;

      ReleaseMemoryPressure();
      _fills.Add(description, fill);
      return fill;
    }

    /// <summary>
    /// Parse a piece of string and return the Font it describes. 
    /// Do *not* dispose of the object returned by this method and
    /// do *not* cache it.
    /// </summary>
    /// <param name="description">Font description.</param>
    /// <param name="message">If parsing is not successful, the error message will be returned here.</param>
    /// <returns>Font on successful parse, or null.</returns>
    public Font ParseFont(string description, out string message)
    {
      description = description.ToLowerInvariant();
      message = null;

      if (_fonts.TryGetValue(description, out Font font))
        return font;

      Dictionary<string, string> dic = ExtensionMethods.ParseDescription(description, out message);
      if (dic == null)
        return null;

      if (!dic["type"].Equals("font", StringComparison.OrdinalIgnoreCase))
      {
        message = "Descriptor is not a font type.";
        return null;
      }
      if (!dic.ContainsKey("typeface"))
      {
        message = "Descriptor does not contain a typeface entry.";
        return null;
      }
      if (!dic.ContainsKey("size"))
      {
        message = "Descriptor does not contain a size entry.";
        return null;
      }

      FontFamily fontFamily = FontFamily.GenericSansSerif;
      if (dic.TryGetValue("typeface", out string typeface))
        foreach (FontFamily family in FontFamily.Families)
          if (family.Name.Equals(typeface, StringComparison.OrdinalIgnoreCase))
          {
            fontFamily = family;
            break;
          }

      float size = 1.0f;
      if (dic.TryGetValue("size", out string sizeValue))
      {
        if (!float.TryParse(sizeValue, out size))
        {
          message = "Size entry is incorrectly formatted: " + sizeValue;
          return null;
        }
        if (size < 1e-4)
        {
          message = "Size entry is too small.";
          return null;
        }
      }

      float emSize = size; // TODO: scale based on projection.

      bool bold = dic.GetYesNo("bold", false);
      bool italic = dic.GetYesNo("italic", false);

      FontStyle style = FontStyle.Regular;
      if (bold) style = FontStyle.Bold;
      if (italic) style = style | FontStyle.Italic;
      
      font = new Font(fontFamily, emSize, style);
      
      ReleaseMemoryPressure();
      _fonts.Add(description, font);
      return font;
    }
    #endregion
  }

  /// <summary>
  /// Some useful extension and static methods.
  /// </summary>
  public static class ExtensionMethods
  {
    #region formatters
    /// <summary>
    /// Format a colour.
    /// </summary>
    /// <param name="colour">Colour to format.</param>
    /// <returns>Parsable string representation.</returns>
    public static string FormatColour(Color colour)
    {
      if (colour.A == 255)
        return string.Format("({0},{1},{2})", colour.R, colour.G, colour.B);
      return string.Format("({0},{1},{2},{3})", colour.R, colour.G, colour.B, colour.A);
    }
    /// <summary>
    /// Format a point.
    /// </summary>
    /// <param name="point">Point to format.</param>
    /// <returns>Parsable string representation.</returns>
    public static string FormatPoint(Point3d point)
    {
      if (Math.Abs(point.Z) < 1e-4)
        return string.Format("({0:0.####},{1:0.####})", point.X, point.Y);
      return string.Format("({0:0.####},{1:0.####},{2:0.####})", point.X, point.Y, point.Z);
    }
    #endregion

    #region parsers
    /// <summary>
    /// Parse a description and return the key/value pairs therein.
    /// </summary>
    /// <param name="description">Description to parse.</param>
    /// <param name="message">Error message, if any.</param>
    /// <returns>Description dictionary.</returns>
    public static Dictionary<string, string> ParseDescription(string description, out string message)
    {
      if (string.IsNullOrWhiteSpace(description))
      {
        message = "Description is blank.";
        return null;
      }

      // Replace commas *inside* parenthesis with semi-colons.
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
    public static bool TryParseColour(string text, out Color colour)
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
    public static bool TryParsePoint(string text, out Point3d point)
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
    public static int[] ToIntegerArray(string list)
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
    public static float[] ToFloatArray(string list)
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
    /// Parse a set of named values.
    /// </summary>
    public static T ParseNamedValues<T>(string key, string[] keys, T[] values, T defaultValue)
    {
      if (string.IsNullOrWhiteSpace(key)) return defaultValue;
      for (int i = 0; i < keys.Length; i++)
        if (string.Equals(key, keys[i], StringComparison.OrdinalIgnoreCase))
          return values[i];
      return defaultValue;
    }
    #endregion

    #region getters
    /// <summary>
    /// Try and parse a yes/no flag in a dictionary.
    /// </summary>
    /// <param name="dictionary">Dictionary to operate on.</param>
    /// <param name="key">Name of key.</param>
    /// <param name="defaultValue">Default value in case the flag doesn't exist or is garbled.</param>
    /// <returns>Boolean value.</returns>
    public static bool GetYesNo(this Dictionary<string, string> dictionary, string key, bool defaultValue)
    {
      if (dictionary.TryGetValue(key, out string value))
        if (!string.IsNullOrWhiteSpace(value))
        {
          if (value.StartsWith("y") || value.StartsWith("Y")) return true;
          if (value.StartsWith("n") || value.StartsWith("N")) return false;
        }

      return defaultValue;
    }

    /// <summary>
    /// Try and parse a colour field in a dictionary.
    /// </summary>
    /// <param name="dictionary">Dictionary to operate on.</param>
    /// <param name="key">Name of key.</param>
    /// <param name="defaultValue">Default value in case the flag doesn't exist or is garbled.</param>
    /// <returns>Colour.</returns>
    public static Color GetColour(this Dictionary<string, string> dictionary, string key, Color defaultValue)
    {
      if (dictionary.TryGetValue(key, out string value))
        if (!string.IsNullOrWhiteSpace(value))
          if (TryParseColour(value, out Color colour))
            return colour;

      return defaultValue;
    }
    /// <summary>
    /// Try and parse a point field in a dictionary.
    /// </summary>
    /// <param name="dictionary">Dictionary to operate on.</param>
    /// <param name="key">Name of key.</param>
    /// <param name="defaultValue">Default value in case the flag doesn't exist or is garbled.</param>
    /// <returns>Point.</returns>
    public static Point3d GetPoint(this Dictionary<string, string> dictionary, string key, Point3d defaultValue)
    {
      if (dictionary.TryGetValue(key, out string value))
        if (!string.IsNullOrWhiteSpace(value))
          if (TryParsePoint(value, out Point3d point))
            return point;

      return defaultValue;
    }
    #endregion
  }
}