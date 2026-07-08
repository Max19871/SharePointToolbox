using MaterialSkin;
using MaterialSkin.Controls;

namespace SharePointToolbox.UI;

public partial class MainMaterialForm : MaterialForm
{
    public MainMaterialForm()
    {
        InitializeComponent();
        ConfigureMaterialSkin();
    }

    private void ConfigureMaterialSkin()
    {
        var manager = MaterialSkinManager.Instance;
        manager.AddFormToManage(this);
        manager.Theme = MaterialSkinManager.Themes.LIGHT;
        manager.ColorScheme = new ColorScheme(
            Primary.BlueGrey800,
            Primary.BlueGrey900,
            Primary.BlueGrey500,
            Accent.LightBlue200,
            TextShade.WHITE);
    }
}
