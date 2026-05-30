using BussinessLayer.Interfaces;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories;
using Microsoft.AspNetCore.Http;
using VNPAY;
using VNPAY.Models;
using VNPAY.Models.Enums;
//using VNPAY.NET;
//using VNPAY.NET.Models;
//using VNPAY.NET.Models.Enums;

namespace BussinessLayer.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ISubscriptionRepository _repository;
        private readonly IVnpayClient _vnpayClient;

        // Tiêm IVnpayClient được cấu hình từ hệ thống vào đây
        public SubscriptionService(ISubscriptionRepository repository, IVnpayClient vnpayClient)
        {
            _repository = repository;
            _vnpayClient = vnpayClient;
        }

        public List<SubscriptionPlan> GetAllPlans() => _repository.GetAllPlans();
        public SubscriptionPlan? GetPlanById(int id) => _repository.GetPlanById(id);
        public void CreatePlan(SubscriptionPlan plan) => _repository.AddPlan(plan);
        public void UpdatePlan(SubscriptionPlan plan) => _repository.UpdatePlan(plan);
        public void DeletePlan(int id) => _repository.DeletePlan(id);
        public StudentSubscription? GetStudentSubscription(int userId) => _repository.GetStudentSubscription(userId);

        // HÀM 1: TẠO URL THANH TOÁN SANG VNPAY (Theo Cách 2 - chi tiết của tài liệu)
        public string CreateVnPayPaymentUrl(int userId, int planId, HttpContext httpContext, string returnUrl)
        {
            var plan = _repository.GetPlanById(planId);
            if (plan == null) return string.Empty;

            // Đưa thông tin UserId và PlanId vào phần mô tả (Description) để khi nhận callback ta trích xuất lại
            // Định dạng: "PAY_USER_[userId]_PLAN_[planId]"
            string description = $"PAY_USER_{userId}_PLAN_{planId}";

            var request = new VnpayPaymentRequest
            {
                Money = (double)plan.Price, // Thư viện dùng kiểu double cho tiền tệ
                Description = description,
                BankCode = BankCode.ANY, // Chấp nhận tất cả ngân hàng liên kết
                Language = DisplayLanguage.Vietnamese
            };

            // Tạo link thanh toán thông qua thư viện
            var paymentUrlInfo = _vnpayClient.CreatePaymentUrl(request);
            return paymentUrlInfo.Url;
        }

        // HÀM 2: XỬ LÝ KẾT QUẢ KHI VNPAY ĐIỀU HƯỚNG VỀ (Hàm CallbackUrl)
        public bool ProcessVnPayReturn(HttpRequest request)
        {
            try
            {
                // Thư viện tự động đọc các tham số từ Request URL và check chữ ký bảo mật
                var paymentResult = _vnpayClient.GetPaymentResult(request);

                // Dựa vào file cấu hình của thư viện, ta kiểm tra xem giao dịch thành công không
                // Nếu thư viện không ném ra Exception nghĩa là chữ ký (HashSecret) hợp lệ
                if (paymentResult != null)
                {
                    // Lấy lại chuỗi Description để phân tách thông tin
                    string description = paymentResult.Description; // "PAY_USER_3_PLAN_2"
                    var parts = description.Split('_');

                    int userId = int.Parse(parts[2]);
                    int planId = int.Parse(parts[4]);

                    var plan = _repository.GetPlanById(planId);
                    if (plan != null)
                    {
                        // 1. Ghi nhận lịch sử giao dịch thành công vào Repo bộ nhớ tạm
                        var transaction = new PaymentTransaction
                        {
                            UserId = userId,
                            SubscriptionPlanId = planId,
                            Amount = plan.Price,
                            TransactionDate = DateTime.Now,
                            Status = "Success"
                        };
                        _repository.AddTransaction(transaction);

                        // 2. Cập nhật gói cho Sinh viên
                        var currentSub = _repository.GetStudentSubscription(userId);
                        if (currentSub != null)
                        {
                            currentSub.SubscriptionPlanId = plan.Id;
                            currentSub.StartDate = DateTime.Now;
                            currentSub.EndDate = DateTime.Now.AddMonths(1);
                            currentSub.RemainingQuestions = plan.QuestionLimit;

                            _repository.SaveStudentSubscription(currentSub);
                        }
                        return true;
                    }
                }
                return false;
            }
            catch (Exception)
            {
                // Nếu lỗi chữ ký hash hoặc lỗi parse chuỗi, trả về false luôn
                return false;
            }
        }

        public void SaveStudentSubscription(StudentSubscription sub)
        {
            _repository.SaveStudentSubscription(sub);
        }

        public void AddTransaction(PaymentTransaction transaction)
        {
            _repository.AddTransaction(transaction);
        }
    }
}