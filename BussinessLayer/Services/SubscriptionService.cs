using BussinessLayer.DTOs;
using BussinessLayer.Interfaces;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
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

        public List<SubscriptionPlanDTO> GetAllPlans()
        {
            // 1. Lấy danh sách gốc từ DB/Repo (Dạng Entity Model)
            var entities = _repository.GetAllPlans(); 

            // 2. Map thủ công toàn bộ danh sách sang DTO bằng Select của LINQ
            var dtoList = entities.Select(plan => new SubscriptionPlanDTO
            {
                Id = plan.Id,
                Name = plan.Name,
                Description = plan.Description,
                Price = plan.Price,
                QuestionLimit = plan.QuestionLimit
            }).ToList();

            return dtoList;
        }

        public SubscriptionPlanDTO? GetPlanById(int id)
        {
            var plan = _repository.GetPlanById(id); 
            if (plan == null) return null;

            return new SubscriptionPlanDTO
            {
                Id = plan.Id,
                Name = plan.Name,
                Description = plan.Description,
                Price = plan.Price,
                QuestionLimit = plan.QuestionLimit
            };
        }
        public void CreatePlan(SubscriptionPlanDTO planDto)
        {
            var plan = new SubscriptionPlan
            {
                Name = planDto.Name,
                Description = planDto.Description,
                Price = planDto.Price,
                QuestionLimit = planDto.QuestionLimit
            };

            _repository.AddPlan(plan);
        }
        public void UpdatePlan(SubscriptionPlanDTO planDto)
        {
            var existingPlan = _repository.GetPlanById(planDto.Id);
            if (existingPlan != null)
            {
                existingPlan.Name = planDto.Name;
                existingPlan.Description = planDto.Description;
                existingPlan.Price = planDto.Price;
                existingPlan.QuestionLimit = planDto.QuestionLimit;

                _repository.UpdatePlan(existingPlan);
            }
        }

        public void UpdatePlanWithOptions(SubscriptionPlanDTO planDto, bool applyToExistingStudents)
        {
            var existingPlan = _repository.GetPlanById(planDto.Id);
            if (existingPlan == null) return;

            existingPlan.Name = planDto.Name;
            existingPlan.Description = planDto.Description;
            existingPlan.Price = planDto.Price;
            existingPlan.QuestionLimit = planDto.QuestionLimit;

            _repository.UpdatePlan(existingPlan);

            // Nếu admin chọn áp dụng giới hạn mới ngay cho tất cả student đang dùng gói này
            if (applyToExistingStudents)
            {
                _repository.UpdateStudentQuestionLimitByPlan(planDto.Id, planDto.QuestionLimit);
            }
        }
        public void DeletePlan(int id) => _repository.DeletePlan(id);
        public StudentSubscriptionDTO? GetStudentSubscription(int userId)
        {
            var sub = _repository.GetStudentSubscription(userId);


            if (sub == null) return null;

            // Mapping sang DTO trước khi trả về cho Controller
            return new StudentSubscriptionDTO
            {
                Id = sub.Id,
                UserId = sub.UserId,
                SubscriptionPlanId = sub.SubscriptionPlanId,
                PlanName = sub.SubscriptionPlan?.Name ?? "N/A",
                PlanDescription = sub.SubscriptionPlan?.Description,
                StartDate = sub.StartDate,
                EndDate = sub.EndDate,
                RemainingQuestions = sub.RemainingQuestions,
                DailyResetTime = sub.DailyResetTime,
                QuestionLimit = sub.SubscriptionPlan?.QuestionLimit ?? 0
            };
        }

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
                            TransactionDate = DateTime.UtcNow,
                            Status = "Success"
                        };
                        _repository.AddTransaction(transaction);

                        // 2. Cập nhật gói cho Sinh viên
                        var currentSub = _repository.GetStudentSubscription(userId);
                        if (currentSub != null)
                        {
                            currentSub.SubscriptionPlanId = plan.Id;
                            currentSub.StartDate = DateTime.UtcNow;
                            currentSub.EndDate = plan.Id == 1 ? DateTime.MaxValue : DateTime.UtcNow.AddMonths(1);
                            currentSub.RemainingQuestions = plan.QuestionLimit;
                            currentSub.DailyResetTime = null; // Reset chu kỳ daily khi đổi gói

                            _repository.SaveStudentSubscription(currentSub);
                        }
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                // Ghi log chi tiết lỗi ra console của Web Server
                Console.WriteLine("================ VNPAY EXCEPTION ================");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("=================================================");
                
                // Rethrow để Controller có thể bắt được và hiện ra giao diện cho dễ debug
                throw;
            }
        }

        public void SaveStudentSubscription(StudentSubscriptionDTO subDto)
        {
            var existing = _repository.GetStudentSubscription(subDto.UserId);
            if (existing != null)
            {
                existing.SubscriptionPlanId = subDto.SubscriptionPlanId;
                existing.StartDate = subDto.StartDate;
                existing.EndDate = subDto.EndDate;
                existing.RemainingQuestions = subDto.RemainingQuestions;
                existing.DailyResetTime = subDto.DailyResetTime;
                _repository.SaveStudentSubscription(existing);
            }
            else
            {
                var sub = new StudentSubscription
                {
                    UserId = subDto.UserId,
                    SubscriptionPlanId = subDto.SubscriptionPlanId,
                    StartDate = subDto.StartDate,
                    EndDate = subDto.EndDate,
                    RemainingQuestions = subDto.RemainingQuestions,
                    DailyResetTime = subDto.DailyResetTime
                };
                _repository.SaveStudentSubscription(sub);
            }
        }

        public void AddTransaction(PaymentTransactionDTO transactionDto)
        {
            var transaction = new PaymentTransaction
            {
                UserId = transactionDto.UserId,
                SubscriptionPlanId = transactionDto.SubscriptionPlanId,
                Amount = transactionDto.Amount,
                TransactionDate = transactionDto.TransactionDate,
                Status = transactionDto.Status
            };
            _repository.AddTransaction(transaction);
        }

        public List<PaymentTransactionDTO> GetAllTransactions()
        {
            var entities = _repository.GetAllTransactions();
            return entities.Select(t => new PaymentTransactionDTO
            {
                Id = t.Id,
                UserId = t.UserId,
                Username = t.User?.Username,
                SubscriptionPlanId = t.SubscriptionPlanId,
                Amount = t.Amount,
                TransactionDate = t.TransactionDate,
                Status = t.Status
            }).ToList();
        }
    }
}