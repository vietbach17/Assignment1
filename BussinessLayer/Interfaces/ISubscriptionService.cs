using DataAccessLayer.Models;
using Microsoft.AspNetCore.Http; // Thêm để dùng HttpContext

namespace BussinessLayer.Interfaces
{
    public interface ISubscriptionService
    {
        List<SubscriptionPlan> GetAllPlans();
        SubscriptionPlan? GetPlanById(int id);
        void CreatePlan(SubscriptionPlan plan);
        void UpdatePlan(SubscriptionPlan plan);
        void DeletePlan(int id);

        StudentSubscription? GetStudentSubscription(int userId);

        void SaveStudentSubscription(StudentSubscription sub);
        void AddTransaction(PaymentTransaction transaction);

        // 2 HÀM MỚI CHO VNPAY:
        string CreateVnPayPaymentUrl(int userId, int planId, HttpContext httpContext, string returnUrl);
        bool ProcessVnPayReturn(Microsoft.AspNetCore.Http.HttpRequest request);
    }
}