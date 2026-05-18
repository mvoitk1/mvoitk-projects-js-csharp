using App.DTO.v1.Admin;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApp.Areas.Admin.ViewModels;

public class OrderDetailViewModel
{
    public required AdminOrderDto Order { get; init; }
    public SelectList Statuses { get; init; } = new(Array.Empty<object>());
}
