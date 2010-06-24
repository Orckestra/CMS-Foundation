using System.Collections.Generic;
using System.Drawing;
using System.Globalization;


namespace Composite.ResourceSystem.Plugins.ResourceProvider
{
	public interface IIconResourceProvider : IResourceProvider
	{
        IEnumerable<string> GetIconNames();

        Bitmap GetIcon(string name, IconSize iconSize, CultureInfo cultureInfo);        
	}
}
