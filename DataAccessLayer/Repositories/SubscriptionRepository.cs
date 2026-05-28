using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories
{
    public class SubscriptionRepository : ISubscriptionRepository
    {
        // Giả lập bảng dữ liệu trong DB bằng Static Lists
        private static readonly List<SubscriptionPlan> _plans = new()
        {
            new SubscriptionPlan { Id = 1, Name = "Free", Price = 0, QuestionLimit = 5, Description = "Gói mặc định cho sinh viên mới tạo tài khoản." },
            new SubscriptionPlan { Id = 2, Name = "Basic", Price = 50000, QuestionLimit = 20, Description = "Phù hợp nhu cầu ôn thi thông thường." },
            new SubscriptionPlan { Id = 3, Name = "Pro", Price = 150000, QuestionLimit = 999999, Description = "Hỏi đáp không giới hạn, hỗ trợ tài liệu nâng cao." }
        };

        private static readonly List<StudentSubscription> _studentSubs = new();
        private static readonly List<PaymentTransaction> _transactions = new();

        public List<SubscriptionPlan> GetAllPlans() => _plans;

        public SubscriptionPlan? GetPlanById(int id) => _plans.FirstOrDefault(p => p.Id == id);

        public void AddPlan(SubscriptionPlan plan)
        {
            plan.Id = _plans.Any() ? _plans.Max(p => p.Id) + 1 : 1;
            _plans.Add(plan);
        }

        public void UpdatePlan(SubscriptionPlan plan)
        {
            var existing = GetPlanById(plan.Id);
            if (existing != null)
            {
                existing.Name = plan.Name;
                existing.Price = plan.Price;
                existing.QuestionLimit = plan.QuestionLimit;
                existing.Description = plan.Description;
            }
        }

        public void DeletePlan(int id)
        {
            var plan = GetPlanById(id);
            if (plan != null) _plans.Remove(plan);
        }

        public StudentSubscription? GetStudentSubscription(int userId)
        {
            var sub = _studentSubs.FirstOrDefault(s => s.UserId == userId);
            // Nếu chưa từng mua, mặc định gói Free
            if (sub == null)
            {
                sub = new StudentSubscription
                {
                    Id = 1,
                    UserId = userId,
                    SubscriptionPlanId = 1,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddYears(1),
                    RemainingQuestions = 20,
                    SubscriptionPlan = _plans.First(p => p.Id == 1)
                };
                _studentSubs.Add(sub);
            }
            else
            {
                // Gán object Plan tương ứng để hiển thị bên ngoài View
                sub.SubscriptionPlan = GetPlanById(sub.SubscriptionPlanId)!;
            }
            return sub;
        }

        public void SaveStudentSubscription(StudentSubscription sub)
        {
            var existing = _studentSubs.FirstOrDefault(s => s.UserId == sub.UserId);
            if (existing != null)
            {
                existing.SubscriptionPlanId = sub.SubscriptionPlanId;
                existing.StartDate = sub.StartDate;
                existing.EndDate = sub.EndDate;
                existing.RemainingQuestions = sub.RemainingQuestions;
            }
            else
            {
                sub.Id = _studentSubs.Any() ? _studentSubs.Max(s => s.Id) + 1 : 1;
                _studentSubs.Add(sub);
            }
        }

        public void AddTransaction(PaymentTransaction transaction)
        {
            transaction.Id = _transactions.Any() ? _transactions.Max(t => t.Id) + 1 : 1;
            _transactions.Add(transaction);
        }
    }
}