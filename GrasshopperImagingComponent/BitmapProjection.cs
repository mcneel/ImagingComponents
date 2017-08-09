using System.Drawing;
using Rhino.Geometry;

namespace GrasshopperImagingComponent
{
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
      // ReSharper disable once ImpureMethodCallOnReadonlyValueField
      _plane.ClosestParameter(point, out double u, out double v);
      u = Width * u * _uFactor;
      v = Height * v * _vFactor;
      v = Height - v;
      return new PointF((float)u, (float)v);
    }
  }
}