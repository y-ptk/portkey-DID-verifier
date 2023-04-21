using Volo.Abp.Localization;
using Volo.Abp.Settings;

namespace CAVerifierServer.Settings;

public class CAVerifierServerSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(CAVerifierServerSettings.MySetting1));
    }
}
