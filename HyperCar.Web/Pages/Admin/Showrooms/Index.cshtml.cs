using HyperCar.DAL.Entities;
using HyperCar.DAL.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HyperCar.Web.Pages.Admin.Showrooms
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;

        public IndexModel(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public List<Showroom> ShowroomList { get; set; } = new();

        [BindProperty] public string ShowroomName { get; set; } = string.Empty;
        [BindProperty] public string? ShowroomAddress { get; set; }

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            ShowroomList = await _unitOfWork.Showrooms.Query()
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        // ── Add ──
        public async Task<IActionResult> OnPostAddAsync()
        {
            if (string.IsNullOrWhiteSpace(ShowroomName))
            {
                ErrorMessage = "Tên showroom không được để trống.";
                return RedirectToPage();
            }

            var showroom = new Showroom
            {
                Name = ShowroomName.Trim(),
                Address = ShowroomAddress?.Trim(),
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            await _unitOfWork.Showrooms.AddAsync(showroom);
            await _unitOfWork.SaveChangesAsync();
            SuccessMessage = $"Đã thêm showroom \"{showroom.Name}\".";
            return RedirectToPage();
        }

        // ── Edit ──
        public async Task<IActionResult> OnPostEditAsync(int id, string name, string? address)
        {
            var showroom = await _unitOfWork.Showrooms.GetByIdAsync(id);
            if (showroom == null)
            {
                ErrorMessage = "Không tìm thấy showroom.";
                return RedirectToPage();
            }

            showroom.Name = name.Trim();
            showroom.Address = address?.Trim();
            _unitOfWork.Showrooms.Update(showroom);
            await _unitOfWork.SaveChangesAsync();
            SuccessMessage = $"Đã cập nhật showroom \"{showroom.Name}\".";
            return RedirectToPage();
        }

        // ── Toggle Active ──
        public async Task<IActionResult> OnPostToggleAsync(int id)
        {
            var showroom = await _unitOfWork.Showrooms.GetByIdAsync(id);
            if (showroom == null)
            {
                ErrorMessage = "Không tìm thấy showroom.";
                return RedirectToPage();
            }

            showroom.IsActive = !showroom.IsActive;
            _unitOfWork.Showrooms.Update(showroom);
            await _unitOfWork.SaveChangesAsync();
            SuccessMessage = showroom.IsActive
                ? $"Đã kích hoạt showroom \"{showroom.Name}\"."
                : $"Đã vô hiệu hoá showroom \"{showroom.Name}\".";
            return RedirectToPage();
        }

        // ── Delete ──
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var showroom = await _unitOfWork.Showrooms.GetByIdAsync(id);
            if (showroom == null)
            {
                ErrorMessage = "Không tìm thấy showroom.";
                return RedirectToPage();
            }

            // Check if any bookings reference this showroom
            var hasBookings = await _unitOfWork.TestDriveBookings.AnyAsync(b => b.ShowroomId == id);
            if (hasBookings)
            {
                ErrorMessage = "Không thể xóa showroom đang có lịch lái thử. Hãy vô hiệu hoá thay thế.";
                return RedirectToPage();
            }

            _unitOfWork.Showrooms.Remove(showroom);
            await _unitOfWork.SaveChangesAsync();
            SuccessMessage = $"Đã xóa showroom \"{showroom.Name}\".";
            return RedirectToPage();
        }
    }
}
