using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories
{
    public interface ISubscriptionRepository
    {
        // Quản lý Gói (Admin CRUD / Student View)
        List<SubscriptionPlan> GetAllPlans();
        SubscriptionPlan? GetPlanById(int id);
        void AddPlan(SubscriptionPlan plan);
        void UpdatePlan(SubscriptionPlan plan);
        void DeletePlan(int id);

        // Quản lý Gói của Sinh viên
        StudentSubscription? GetStudentSubscription(int userId);
        void SaveStudentSubscription(StudentSubscription sub);

        // Ghi nhận giao dịch
        void AddTransaction(PaymentTransaction transaction);
        List<PaymentTransaction> GetAllTransactions();
    }
}