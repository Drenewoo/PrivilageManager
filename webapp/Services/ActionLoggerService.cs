using System;
using webapp.Data;
using webapp.Models;

namespace webapp.Services
{
    public class ActionLoggerService
    {
        private readonly AppDbContext _context;

        public ActionLoggerService(AppDbContext context)
        {
            _context = context;
        }

        public void Log(int? userId, int? userPermissionId, int? deviceId, int? applicationId, int actionId, int actionHistoryId)
        {
            var history = new ActionHistory()
            {

                UserId = userId,
                UserPermissionId = userPermissionId,
                DeviceId = deviceId,
                ApplicationId = applicationId,
                ActionId = actionId,
                Date = DateTime.Now,
                ActionHistoryId = actionHistoryId,
            };
            _context.ActionHistories.Add(history);
            _context.SaveChanges();
        }
    }
}