using Microsoft.AspNetCore.Mvc;
using ToDoList_FS.Model;

namespace ToDoList_FS.Controllers
{
    [Route("api/holiday")]
    [ApiController]
    public class HolidayController : BaseAPIController
    {
        private readonly MongoDBService _mongoDBService;

        public HolidayController(MongoDBService mongoDBService)
        {
            _mongoDBService = mongoDBService;
        }

        [HttpGet("get/{userId}")]
        public async Task<IActionResult> GetHolidays(string userId)
        {
            var holidays = await _mongoDBService.GetHolidaysAsync(userId);
            return SuccessResult(holidays);
        }

        [HttpGet("detail/{userId}/{holidayId}")]
        public async Task<IActionResult> GetHolidayById(string userId, string holidayId)
        {
            var holiday = await _mongoDBService.GetHolidayByIdAsync(holidayId, userId);
            if (holiday == null)
                return ErrorResult("Holiday not found");

            return SuccessResult(holiday);
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddHoliday([FromBody] HolidayDTO holidayDto)
        {
            if (string.IsNullOrEmpty(holidayDto.UserId))
                return ErrorResult("UserId is required");

            await _mongoDBService.CreateHolidayAsync(holidayDto, holidayDto.UserId);
            return SuccessResult("Thêm ngày nghỉ thành công");
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateHoliday(string id, [FromBody] HolidayDTO holidayDto)
        {
            if (string.IsNullOrEmpty(holidayDto.UserId))
                return ErrorResult("UserId is required");

            var success = await _mongoDBService.UpdateHolidayAsync(id, holidayDto.UserId, holidayDto);
            if (!success)
                return ErrorResult("Holiday not found");

            return SuccessResult("Cập nhật ngày nghỉ thành công");
        }

        [HttpDelete("delete/{userId}/{id}")]
        public async Task<IActionResult> DeleteHoliday(string userId, string id)
        {
            var success = await _mongoDBService.DeleteHolidayAsync(id, userId);
            if (!success)
                return ErrorResult("Holiday not found");

            return SuccessResult("Xóa ngày nghỉ thành công");
        }
    }
} 