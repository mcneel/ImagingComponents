using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace GrasshopperImagingComponent
{
  public sealed class GhaInformation : GH_AssemblyInfo
  {
    public override string AuthorContact
    {
      get { return "www.grasshopper3d.com"; }
    }
    public override string AuthorName
    {
      get { return "David Rutten"; }
    }

    public static readonly Guid _assemblyId = new Guid("{02B1CB43-DB67-401C-AB74-86590B20292A}");
    public override Guid Id
    {
      get { return _assemblyId; }
    }

    public override string Version
    {
      get { return "1.0.2"; }
    }
    public override string Name
    {
      get { return "Imaging Library"; }
    }
    public override GH_LibraryLicense License
    {
      get { return GH_LibraryLicense.beta; }
    }
    public override Bitmap Icon
    {
      get { return Properties.Resources.Draw_24x24; }
    }
    public override string Description
    {
      get { return "Provides a collection of bitmap drawing features."; }
    }
  }
}