using BussinessLayer.DTOs;
using DataAccessLayer.Models;
using Microsoft.AspNetCore.Http; // Thêm để dùng HttpContext

namespace BussinessLayer.Interfaces
{
    public interface ISubscriptionService
    {
        List<SubscriptionPlanDTO> GetAllPlans();
        SubscriptionPlanDTO? GetPlanById(int id);
        void CreatePlan(SubscriptionPlanDTO planDto);
        void UpdatePlan(SubscriptionPlanDTO planDto);

        /// <summary>
        /// Cập nhật gói và tuỳ chọn áp QuestionLimit mới cho tất cả student đang dùng gói đó.
        /// </summary>
        void UpdatePlanWithOptions(SubscriptionPlanDTO planDto, bool applyToExistingStudents);

        void DeletePlan(int id);

        StudentSubscriptionDTO? GetStudentSubscription(int userId);

        void SaveStudentSubscription(StudentSubscription sub);
        void AddTransaction(PaymentTransaction transaction);
        List<PaymentTransaction> GetAllTransactions();

        // 2 HÀM MỚI CHO VNPAY:
        string CreateVnPayPaymentUrl(int userId, int planId, HttpContext httpContext, string returnUrl);
        bool ProcessVnPayReturn(Microsoft.AspNetCore.Http.HttpRequest request);
    }
}