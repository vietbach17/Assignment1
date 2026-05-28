using DataAccessLayer.Models;
using DataAccessLayer.Repositories;

namespace BussinessLayer.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ISubscriptionRepository _repository;

        public SubscriptionService(ISubscriptionRepository repository)
        {
            _repository = repository;
        }

        public List<SubscriptionPlan> GetAllPlans() => _repository.GetAllPlans();

        public SubscriptionPlan? GetPlanById(int id) => _repository.GetPlanById(id);

        public void CreatePlan(SubscriptionPlan plan) => _repository.AddPlan(plan);

        public void UpdatePlan(SubscriptionPlan plan) => _repository.UpdatePlan(plan);

        public void DeletePlan(int id) => _repository.DeletePlan(id);

        // 1. Thêm dấu ? vào kiểu trả về StudentSubscription? để đồng bộ với Repository
        public StudentSubscription? GetStudentSubscription(int userId) => _repository.GetStudentSubscription(userId);

        public bool PurchasePlan(int userId, int planId)
        {
            var plan = _repository.GetPlanById(planId);
            if (plan == null) return false;

            // Lấy thông tin gói hiện tại (có thể null nếu có lỗi hệ thống)
            var currentSub = _repository.GetStudentSubscription(userId);

            // Kiểm tra an toàn trước khi xử lý gán dữ liệu
            if (currentSub == null) return false;

            // 1. Tạo bản ghi giao dịch thành công
            var transaction = new PaymentTransaction
            {
                UserId = userId,
                SubscriptionPlanId = planId,
                Amount = plan.Price,
                TransactionDate = DateTime.Now,
                Status = "Success"
            };
            _repository.AddTransaction(transaction);

            // 2. Cập nhật hoặc gia hạn gói cho Sinh viên công khai sau khi kiểm tra null
            currentSub.SubscriptionPlanId = plan.Id;
            currentSub.StartDate = DateTime.Now;
            currentSub.EndDate = DateTime.Now.AddMonths(1);
            currentSub.RemainingQuestions = plan.QuestionLimit;

            _repository.SaveStudentSubscription(currentSub);
            return true;
        }
    }
}