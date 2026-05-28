using DataAccessLayer.Models;

namespace BussinessLayer.Services
{
    public interface ISubscriptionService
    {
        List<SubscriptionPlan> GetAllPlans();
        SubscriptionPlan? GetPlanById(int id);
        void CreatePlan(SubscriptionPlan plan);
        void UpdatePlan(SubscriptionPlan plan);
        void DeletePlan(int id);

        StudentSubscription? GetStudentSubscription(int userId);
        bool PurchasePlan(int userId, int planId); // Logic thanh toán giả lập
    }
}