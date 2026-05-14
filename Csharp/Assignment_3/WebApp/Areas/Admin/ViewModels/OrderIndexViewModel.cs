using App.DTO.v1.Admin;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApp.Areas.Admin.ViewModels;

public class OrderIndexViewModel
{
    public IEnumerable<AdminOrderDto> Orders { get; init; } = [];
    public SelectList Statuses { get; init; } = new(Array.Empty<object>());
    public string? StatusFilter { get; init; }
}
