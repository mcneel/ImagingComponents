using System;
using System.Collections.Generic;
using Grasshopper.Kernel;

namespace GrasshopperImagingComponent
{
  public sealed class GdiGooParameter : GH_PersistentParam<IGdiGoo>
  {
    public GdiGooParameter()
      : base("Gdi geometry", "GdiGeo", "Store shapes/geometry used in drawing images.", "Display", "Image")
    { }
    public GdiGooParameter(GH_InstanceDescription nTag) : base(nTag) { }
    public GdiGooParameter(string name, string nickname, string description, string category, string subcategory)
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
    //protected override Bitmap Icon
    //{
    //  get { return base.Icon; }
    //}

    protected override GH_GetterResult Prompt_Singular(ref IGdiGoo value)
    {
      return GH_GetterResult.cancel;
    }
    protected override GH_GetterResult Prompt_Plural(ref List<IGdiGoo> values)
    {
      return GH_GetterResult.cancel;
    }

    protected override IGdiGoo InstantiateT()
    {
      return new GdiShapeGoo();
    }
  }
}