using DataAccessLayer.DbContexts;
using DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataAccessLayer.Repositories
{
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly AppDbContext _context;

        // Tiêm DbContext thật của hệ thống vào đây thay vì dùng bộ nhớ tạm
        public SubscriptionRepository(AppDbContext context)
        {
            _context = context;
        }

        public List<SubscriptionPlan> GetAllPlans()
        {
            return _context.SubscriptionPlans.ToList();
        }

        public SubscriptionPlan? GetPlanById(int id)
        {
            return _context.SubscriptionPlans.Find(id);
        }

        public void AddPlan(SubscriptionPlan plan)
        {
            _context.SubscriptionPlans.Add(plan);
            _context.SaveChanges(); // Lưu thay đổi xuống Postgres
        }

        public void UpdatePlan(SubscriptionPlan plan)
        {
            _context.SubscriptionPlans.Update(plan);
            _context.SaveChanges();
        }

        public void DeletePlan(int id)
        {
            var plan = GetPlanById(id);
            if (plan != null)
            {
                bool hasSubscribers = _context.StudentSubscriptions.Any(s => s.SubscriptionPlanId == id);
                if (hasSubscribers)
                {
                    throw new InvalidOperationException("Không thể xóa gói dịch vụ này vì đang có người dùng đăng ký.");
                }

                _context.SubscriptionPlans.Remove(plan);
                _context.SaveChanges();
            }
        }

        public StudentSubscription? GetStudentSubscription(int userId)
        {
            // Lấy thông tin gói từ Postgres kèm theo dữ liệu bảng Plan (Include)
            var sub = _context.StudentSubscriptions
                .Include(s => s.SubscriptionPlan)
                .FirstOrDefault(s => s.UserId == userId);

            // FIX LỖI 2: Nếu TRONG DB THỰC TẾ CHƯA HỀ CÓ DÒNG NÀO thì mới cấp gói Free
            if (sub == null)
            {
                var freePlan = _context.SubscriptionPlans.FirstOrDefault(p => p.Name == "Free")
                               ?? _context.SubscriptionPlans.OrderBy(p => p.Price).FirstOrDefault()
                               ?? _context.SubscriptionPlans.FirstOrDefault(p => p.Id == 1);
                int freePlanId = freePlan?.Id ?? 1;
                sub = new StudentSubscription
                {
                    UserId = userId,
                    SubscriptionPlanId = freePlanId,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.MaxValue, // Gói Free = vĩnh viễn
                    RemainingQuestions = freePlan?.QuestionLimit ?? 5 // Mặc định gói free là 5 câu/ngày
                };

                _context.StudentSubscriptions.Add(sub);
                _context.SaveChanges();

                sub.SubscriptionPlan = freePlan!;
            }

            return sub;
        }

        public void SaveStudentSubscription(StudentSubscription sub)
        {
            var existing = _context.StudentSubscriptions.FirstOrDefault(s => s.Id == sub.Id || s.UserId == sub.UserId);
            if (existing != null)
            {
                _context.Entry(existing).CurrentValues.SetValues(sub);
            }
            else
            {
                _context.StudentSubscriptions.Add(sub);
            }
            _context.SaveChanges();
        }

        /// <summary>
        /// Cập nhật RemainingQuestions cho tất cả StudentSubscription đang dùng gói planId.
        /// Dùng khi admin muốn áp giới hạn mới ngay lập tức cho student hiện tại.
        /// </summary>
        public void UpdateStudentQuestionLimitByPlan(int planId, int newLimit)
        {
            var subs = _context.StudentSubscriptions
                .Where(s => s.SubscriptionPlanId == planId)
                .ToList();

            foreach (var sub in subs)
            {
                sub.RemainingQuestions = newLimit;
            }

            _context.SaveChanges();
        }

        public void AddTransaction(PaymentTransaction transaction)
        {
            _context.PaymentTransactions.Add(transaction);
            _context.SaveChanges();
        }

        public List<PaymentTransaction> GetAllTransactions()
        {
            return _context.PaymentTransactions
                .Include(t => t.User)
                .OrderByDescending(t => t.TransactionDate)
                .ToList();
        }

        public Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction BeginTransaction()
        {
            return _context.Database.BeginTransaction();
        }
    }
}