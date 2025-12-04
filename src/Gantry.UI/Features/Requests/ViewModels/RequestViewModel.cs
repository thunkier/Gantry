using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gantry.Core.Domain.Http;
using Gantry.Core.Domain.Settings;
using Gantry.Core.Interfaces;
using Gantry.UI.Shell.ViewModels;
using System.Linq;
using System;
using System.Net.Http;
using Gantry.Core.Domain.Collections;

namespace Gantry.UI.Features.Requests.ViewModels;

public partial class RequestViewModel : TabViewModel
{
    private readonly IHttpService _httpService;
    private readonly Gantry.Infrastructure.Services.WorkspaceService _workspaceService;
    private readonly IVariableService _variableService;

    [ObservableProperty]
    private string _url = "https://httpbin.org/get";

    [ObservableProperty]
    private string _selectedMethod = "GET";

    [ObservableProperty]
    private string _body = string.Empty;

    [ObservableProperty]
    private ResponseViewModel? _response;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<string> Methods { get; } = new()
    {
        "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS"
    };

    public ObservableCollection<HeaderViewModel> Headers { get; } = new();
    public ObservableCollection<ParamViewModel> Params { get; } = new();

    private readonly RequestItem _model;
    public RequestItem Model => _model;

    public RequestViewModel() : this(new Gantry.Infrastructure.Network.HttpService(), new Gantry.Infrastructure.Services.WorkspaceService(), new Gantry.Infrastructure.Services.VariableService(), null)
    {
        Title = "New Request";
    }

    public RequestViewModel(IHttpService httpService, Gantry.Infrastructure.Services.WorkspaceService workspaceService, IVariableService variableService, RequestItem? model = null)
    {
        _httpService = httpService;
        _workspaceService = workspaceService;
        _variableService = variableService;
        _model = model ?? new RequestItem();
        Title = _model.Name;
        if (string.IsNullOrWhiteSpace(Title)) Title = "New Request";

        Url = _model.Request.Url;
        SelectedMethod = _model.Request.Method;
        Body = _model.Request.Body;

        // Initialize collections
        foreach (var header in _model.Headers) Headers.Add(new HeaderViewModel(header));

        // Add Auto Headers
        AddAutoHeader("Host", "<calculated when request is sent>");
        AddAutoHeader("User-Agent", "Gantry/1.0"); // Gantry User Agent
        AddAutoHeader("Accept", "*/*");
        AddAutoHeader("Accept-Encoding", "gzip, deflate, br");
        AddAutoHeader("Connection", "keep-alive");
        AddAutoHeader("Gantry-Token", "<calculated when request is sent>");

        // Add empty row
        Headers.Add(new HeaderViewModel(new HeaderItem()));

        foreach (var param in _model.Params) Params.Add(new ParamViewModel(param));

        // Add empty row
        Params.Add(new ParamViewModel(new ParamItem()));

        // Load history if available
        if (_model.History != null)
        {
            foreach (var historyItem in _model.History)
            {
                History.Add(new HistoryItemViewModel(new ResponseModel
                {
                    StatusCode = historyItem.StatusCode,
                    Duration = TimeSpan.FromMilliseconds(historyItem.DurationMs),
                    Size = historyItem.Size
                })
                { Timestamp = historyItem.Timestamp });
            }
        }

        Auth = new AuthSettingsViewModel(_model.Auth);

        foreach (var v in _model.Variables)
        {
            var vm = new VariableViewModel(v);
            vm.PropertyChanged += Variable_PropertyChanged;
            Variables.Add(vm);
        }

        if (Variables.Count == 0)
        {
            AddVariable();
        }
    }

    // Settings
    public AuthSettingsViewModel Auth { get; }
    public ScriptSettings Scripts => _model.Scripts;
    public ObservableCollection<VariableViewModel> Variables { get; } = new();

    // HTTP Request Settings
    [ObservableProperty]
    private string _httpVersion = "HTTP/1.1";

    public ObservableCollection<string> HttpVersions { get; } = new() { "HTTP/1.0", "HTTP/1.1", "HTTP/2" };

    [ObservableProperty]
    private bool _enableSslCertificateVerification = true;

    [ObservableProperty]
    private bool _automaticallyFollowRedirects = true;

    [ObservableProperty]
    private bool _followOriginalHttpMethod = false;

    [ObservableProperty]
    private bool _followAuthorizationHeader = false;

    [ObservableProperty]
    private bool _removeRefererHeaderOnRedirect = false;

    [ObservableProperty]
    private bool _enableStrictHttpParser = false;

    [ObservableProperty]
    private bool _encodeUrlAutomatically = true;

    [ObservableProperty]
    private bool _disableCookieJar = false;

    [ObservableProperty]
    private bool _useServerCipherSuiteDuringHandshake = false;

    [ObservableProperty]
    private int _maximumNumberOfRedirects = 10;

    [ObservableProperty]
    private ObservableCollection<TlsProtocol> _disabledTlsProtocols = new();

    public ObservableCollection<TlsProtocol> AvailableTlsProtocols { get; } = new()
    {
        new TlsProtocol("SSL 2.0", false),
        new TlsProtocol("SSL 3.0", false),
        new TlsProtocol("TLS 1.0", false),
        new TlsProtocol("TLS 1.1", false),
        new TlsProtocol("TLS 1.2", true),
        new TlsProtocol("TLS 1.3", true)
    };

    [ObservableProperty]
    private string _cipherSuiteSelection = "Default";

    public ObservableCollection<string> CipherSuites { get; } = new()
    {
        "Default",
        "Modern (TLS 1.2+)",
        "Intermediate",
        "Old (Compatibility)",
        "Custom"
    };

    // Helper for Auth Types
    public System.Collections.Generic.IEnumerable<AuthType> AuthTypes =>
        System.Enum.GetValues<AuthType>();

    [RelayCommand]
    private void AddVariable()
    {
        var variable = new Variable { Key = "", Value = "" };
        var vm = new VariableViewModel(variable);
        vm.PropertyChanged += Variable_PropertyChanged;
        _model.Variables.Add(variable);
        Variables.Add(vm);
    }

    [RelayCommand]
    private void RemoveVariable(VariableViewModel variable)
    {
        if (variable != null)
        {
            variable.PropertyChanged -= Variable_PropertyChanged;
            _model.Variables.Remove(variable.Model);
            Variables.Remove(variable);

            // Ensure there's always at least one empty row
            if (Variables.Count == 0)
            {
                AddVariable();
            }
        }
    }

    private void Variable_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is VariableViewModel vm && Variables.LastOrDefault() == vm)
        {
            if (!string.IsNullOrEmpty(vm.Key) || !string.IsNullOrEmpty(vm.Value))
            {
                AddVariable();
            }
        }
    }

    public ObservableCollection<HistoryItemViewModel> History { get; } = new();

    [ObservableProperty]
    private bool _isHistoryVisible = true;

    [RelayCommand]
    private void ToggleHistory()
    {
        IsHistoryVisible = !IsHistoryVisible;
    }

    [ObservableProperty]
    private HistoryItemViewModel? _selectedHistoryItem;

    partial void OnSelectedHistoryItemChanged(HistoryItemViewModel? value)
    {
        if (value != null)
        {
            Response = new ResponseViewModel(value.Model);
        }
    }

    [RelayCommand]
    private void Save()
    {
        // Update model from VM properties
        _model.Request.Url = Url;
        _model.Request.Method = SelectedMethod;
        _model.Request.Body = Body;

        _model.Headers.Clear();
        foreach (var h in Headers)
        {
            if (!string.IsNullOrEmpty(h.Key)) _model.Headers.Add(h.ToModel());
        }

        _model.Params.Clear();
        foreach (var p in Params)
        {
            if (!string.IsNullOrEmpty(p.Key)) _model.Params.Add(p.ToModel());
        }

        // Save via repository
        _workspaceService.Repository.SaveRequest(_model);
    }

    [RelayCommand]
    private async Task SendRequestAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        try
        {
            // Resolve variables
            var resolvedUrl = _variableService.ResolveVariables(Url, _model);
            var resolvedBody = _variableService.ResolveVariables(Body, _model);

            var request = new RequestModel
            {
                Url = resolvedUrl,
                Method = SelectedMethod,
                Body = resolvedBody,

                // Collect headers (exclude empty rows and auto headers) and resolve variables
                Headers = Headers
                    .Where(h => !string.IsNullOrWhiteSpace(h.Key) && !h.IsAuto)
                    .ToDictionary(
                        h => _variableService.ResolveVariables(h.Key, _model),
                        h => _variableService.ResolveVariables(h.Value, _model)),

                // Auth configuration with resolved variables
                Auth = Auth.Type != AuthType.None
                    ? new AuthConfig
                    {
                        Type = Auth.Type,
                        Username = _variableService.ResolveVariables(Auth.Username, _model),
                        Password = _variableService.ResolveVariables(Auth.Password, _model),
                        Token = _variableService.ResolveVariables(Auth.Token, _model)
                    }
                    : null,

                // HTTP Settings
                HttpVersion = HttpVersion,
                EnableSslCertificateVerification = EnableSslCertificateVerification,
                AutomaticallyFollowRedirects = AutomaticallyFollowRedirects,
                FollowOriginalHttpMethod = FollowOriginalHttpMethod,
                FollowAuthorizationHeader = FollowAuthorizationHeader,
                RemoveRefererHeaderOnRedirect = RemoveRefererHeaderOnRedirect,
                EnableStrictHttpParser = EnableStrictHttpParser,
                EncodeUrlAutomatically = EncodeUrlAutomatically,
                DisableCookieJar = DisableCookieJar,
                UseServerCipherSuiteDuringHandshake = UseServerCipherSuiteDuringHandshake,
                MaximumNumberOfRedirects = MaximumNumberOfRedirects,
                DisabledTlsProtocols = AvailableTlsProtocols
                    .Where(p => !p.IsEnabled)
                    .Select(p => p.Name)
                    .ToList(),
                CipherSuiteSelection = CipherSuiteSelection
            };

            var responseModel = await _httpService.SendRequestAsync(request);
            var historyItem = new HistoryItemViewModel(responseModel);
            History.Insert(0, historyItem);
            Response = new ResponseViewModel(responseModel);

            // Add to model history
            _model.History.Add(new RequestHistoryItem
            {
                Timestamp = DateTime.Now,
                StatusCode = responseModel.StatusCode,
                DurationMs = (long)responseModel.Duration.TotalMilliseconds,
                Size = responseModel.Size
            });

            // Auto-save history
            Save();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveResponse()
    {
        if (Response == null || string.IsNullOrEmpty(Response.Body)) return;

        // Determine directory
        string directory = string.Empty;
        if (!string.IsNullOrEmpty(_model.Path) && _model.Path.EndsWith(".req"))
        {
            directory = _model.Path;
        }
        else if (!string.IsNullOrEmpty(_model.Path))
        {
            directory = System.IO.Path.GetDirectoryName(_model.Path) ?? "";
        }
        else
        {
            var parent = _model.Parent;
            while (parent != null)
            {
                if (parent is Collection c && !string.IsNullOrEmpty(c.Path))
                {
                    directory = c.Path; // Collection path is usually the folder
                    break;
                }
                parent = parent.Parent;
            }
            // If still empty, use a default fallback
            if (string.IsNullOrEmpty(directory))
            {
                 directory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Gantry", "SavedResponses");
            }
        }

        if (!System.IO.Directory.Exists(directory)) System.IO.Directory.CreateDirectory(directory);

        var filePath = System.IO.Path.Combine(directory, "responses.json");
        List<SavedResponse> responses = new();

        // Read existing if file exists
        if (System.IO.File.Exists(filePath))
        {
            try
            {
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var existing = System.Text.Json.JsonSerializer.Deserialize<List<SavedResponse>>(json, new System.Text.Json.JsonSerializerOptions 
                { 
                    TypeInfoResolver = Gantry.Infrastructure.Serialization.WorkspaceJsonContext.Default 
                });
                if (existing != null) responses.AddRange(existing);
            }
            catch
            {
                // Ignore error, overwrite or append to empty
            }
        }

        // Add new response
        responses.Add(new SavedResponse
        {
            Name = $"{_model.Name} - {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            StatusCode = Response.StatusCode,
            Body = Response.Body,
            Timestamp = DateTime.Now,
            DurationMs = (long)(_model.History.LastOrDefault()?.DurationMs ?? 0),
            Size = _model.History.LastOrDefault()?.Size ?? 0,
            Headers = Response.Headers.ToDictionary(h => h.Key, h => h.Value)
        });

        // Write back
        var options = new System.Text.Json.JsonSerializerOptions 
        { 
            WriteIndented = true,
            TypeInfoResolver = Gantry.Infrastructure.Serialization.WorkspaceJsonContext.Default 
        };
        var newJson = System.Text.Json.JsonSerializer.Serialize(responses, options);
        await System.IO.File.WriteAllTextAsync(filePath, newJson);
    }

    [RelayCommand]
    private void SelectHistoryItem(HistoryItemViewModel? item)
    {
        if (item != null)
        {
            Response = new ResponseViewModel(item.Model);
        }
    }
    private void AddAutoHeader(string key, string value)
    {
        if (!Headers.Any(h => h.Key.Equals(key, StringComparison.OrdinalIgnoreCase)))
        {
            Headers.Add(new HeaderViewModel(new HeaderItem { Key = key, Value = value })
            {
                IsAuto = true,
                IsReadOnly = true
            });
        }
    }
}

// Helper class for TLS Protocol selection
public partial class TlsProtocol : ObservableObject
{
    public string Name { get; set; }

    [ObservableProperty]
    private bool _isEnabled;

    public TlsProtocol(string name, bool isEnabled)
    {
        Name = name;
        IsEnabled = isEnabled;
    }
}