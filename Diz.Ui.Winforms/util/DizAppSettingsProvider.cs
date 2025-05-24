using System.ComponentModel;
using System.Runtime.CompilerServices;
using Diz.Controllers.interfaces;
using Diz.Core.util;
using DiztinGUIsh.Properties;
using JetBrains.Annotations;

namespace Diz.Ui.Winforms.util;

[UsedImplicitly]
public class DizAppSettingsProvider : IDizAppSettings
{
    private static Settings Settings => 
         Settings.Default;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string LastProjectFilename
    {
        get => Settings.LastOpenedFile;
        set
        {
            if (NotifyPropertyChangedExtensions.FieldIsEqual(Settings.LastOpenedFile, value)) 
                return;
            Settings.LastOpenedFile = value;
            OnSettingChanged();
        }
    }
    
    public bool OpenLastFileAutomatically
    {
        get => Settings.OpenLastFileAutomatically;
        set
        {
            if (NotifyPropertyChangedExtensions.FieldIsEqual(Settings.OpenLastFileAutomatically, value)) 
                return;
            Settings.OpenLastFileAutomatically = value;
            OnSettingChanged();
        }
    }
    
    public string LastOpenedFile
    {
        get => Settings.LastOpenedFile;
        set
        {
            if (NotifyPropertyChangedExtensions.FieldIsEqual(Settings.LastOpenedFile, value)) 
                return;
            Settings.LastOpenedFile = value;
            OnSettingChanged();
        }
    }

    private static void Save() => 
        Settings.Save();
    
    private void OnSettingChanged([CallerMemberName] string? propertyName = null)
    {
        Save();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}