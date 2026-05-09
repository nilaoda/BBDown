using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BBDown.GUI.Services;

namespace BBDown.GUI.ViewModels;

public class LoginViewModel : INotifyPropertyChanged
{
    private string _accessToken = "";
    private string _cookie = "";
    private string _loginStatus = "";

    public string AccessToken
    {
        get => _accessToken;
        set
        {
            if (_accessToken != value)
            {
                _accessToken = value;
                OnPropertyChanged();
            }
        }
    }

    public string Cookie
    {
        get => _cookie;
        set
        {
            if (_cookie != value)
            {
                _cookie = value;
                OnPropertyChanged();
            }
        }
    }

    public string LoginStatus
    {
        get => _loginStatus;
        set
        {
            if (_loginStatus != value)
            {
                _loginStatus = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand SaveTokensCommand { get; }
    public ICommand RefreshQrWebCommand { get; }
    public ICommand RefreshQrTvCommand { get; }

    public LoginViewModel()
    {
        // Load existing tokens from config
        AccessToken = ConfigService.Instance.Config.AccessToken;
        Cookie = ConfigService.Instance.Config.Cookie;

        SaveTokensCommand = new RelayCommand(_ => SaveTokens());
        RefreshQrWebCommand = new RelayCommand(_ => GenerateQrCode("web"));
        RefreshQrTvCommand = new RelayCommand(_ => GenerateQrCode("tv"));
    }

    private void SaveTokens()
    {
        ConfigService.Instance.Config.AccessToken = AccessToken;
        ConfigService.Instance.Config.Cookie = Cookie;
        ConfigService.Instance.Save();

        LoginStatus = "已保存";
        OnPropertyChanged(nameof(LoginStatus));
    }

    private void GenerateQrCode(string type)
    {
        LoginStatus = $"正在生成{type}端二维码...";
        OnPropertyChanged(nameof(LoginStatus));

        // TODO: integrate BBDown QR code login logic
        // BBDown.Program already has login methods using QRCoder
        // For now: placeholder that sets status
        System.Diagnostics.Debug.WriteLine($"Generate QR code for {type}");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
