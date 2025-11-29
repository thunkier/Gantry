using CommunityToolkit.Mvvm.ComponentModel;
using Gantry.Core.Domain.Http;

namespace Gantry.UI.Features.Requests.ViewModels;

public partial class ResponseViewModel : ObservableObject
{
    private readonly ResponseModel _model;

    public int StatusCode => _model.StatusCode;
    public string Body => _model.Body;
    public string Duration => $"{_model.Duration.TotalMilliseconds:F0} ms";
    public string Size => $"{_model.Size} bytes";
    public bool IsSuccess => _model.IsSuccess;

    public System.Collections.ObjectModel.ObservableCollection<HeaderViewModel> Headers { get; } = new();

    public ResponseViewModel(ResponseModel model)
    {
        _model = model;
        foreach (var header in model.Headers)
        {
            Headers.Add(new HeaderViewModel(new Gantry.Core.Domain.Collections.HeaderItem
            {
                Key = header.Key,
                Value = header.Value,
                IsActive = true
            }));
        }
    }
}
