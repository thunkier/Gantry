using CommunityToolkit.Mvvm.ComponentModel;
using Gantry.Core.Domain.Http;
using System;

namespace Gantry.UI.Features.Requests.ViewModels;

public partial class HistoryItemViewModel : ObservableObject
{
    public ResponseModel Model { get; }

    public int StatusCode => Model.StatusCode;
    public string Duration => $"{Model.Duration.TotalMilliseconds:F0} ms";
    public string Size => $"{Model.Size} bytes";
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool IsSuccess => Model.IsSuccess;

    public HistoryItemViewModel(ResponseModel model)
    {
        Model = model;
    }
}
