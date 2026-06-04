using BussinessLayer.DTOs;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BussinessLayer.Services
{
    /// <summary>
    /// Service implementation cho Subject management business logic
    /// </summary>
    public class SubjectService : ISubjectService
    {
        private readonly ISubjectRepository _subjectRepository;
        private readonly IUserRepository _userRepository;

        public SubjectService(ISubjectRepository subjectRepository, IUserRepository userRepository)
        {
            _subjectRepository = subjectRepository;
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<SubjectDto>> GetAllSubjectsAsync(bool includeDeleted = false)
        {
            var subjects = await _subjectRepository.GetAllAsync(includeDeleted);
            return subjects.Select(MapToDto);
        }

        public async Task<IEnumerable<SubjectDto>> GetSubjectsByLecturerIdAsync(int lecturerId, bool includeDeleted = false)
        {
            var subjects = await _subjectRepository.GetByLecturerIdAsync(lecturerId, includeDeleted);
            return subjects.Select(MapToDto);
        }

        public async Task<SubjectDto?> GetSubjectByIdAsync(int id, bool includeDeleted = false)
        {
            var subject = await _subjectRepository.GetByIdAsync(id, includeDeleted);
            return subject == null ? null : MapToDto(subject);
        }

        public async Task<SubjectDto?> GetSubjectWithChaptersAsync(int id, bool includeDeleted = false)
        {
            var subject = await _subjectRepository.GetByIdWithChaptersAsync(id, includeDeleted);
            return subject == null ? null : MapToDtoWithChapters(subject);
        }

        public async Task<(bool Success, string Message, SubjectDto? Subject)> CreateSubjectAsync(CreateSubjectDto dto, int userId)
        {
            // Check if SubjectCode exists (including deleted ones)
            var existingSubject = await _subjectRepository.GetBySubjectCodeAsync(dto.SubjectCode, includeDeleted: true);
            
            if (existingSubject != null)
            {
                if (existingSubject.IsDeleted)
                {
                    // Restore the deleted subject instead of creating new one
                    existingSubject.SubjectName = dto.SubjectName;
                    existingSubject.Description = dto.Description;
                    existingSubject.IsDeleted = false;
                    existingSubject.UpdatedDate = DateTime.UtcNow;
                    existingSubject.UpdatedByUserId = userId;
                    
                    var restoredSubject = await _subjectRepository.UpdateAsync(existingSubject);
                    return (true, $"Subject code '{dto.SubjectCode}' was previously deleted and has been restored with new information.", MapToDto(restoredSubject));
                }
                else
                {
                    // Subject exists and is not deleted
                    return (false, $"Subject code '{dto.SubjectCode}' already exists.", null);
                }
            }

            try
            {
                var subject = new Subject
                {
                    SubjectCode = dto.SubjectCode,
                    SubjectName = dto.SubjectName,
                    Description = dto.Description,
                    CreatedDate = DateTime.UtcNow,
                    CreatedByUserId = userId,
                    IsDeleted = false
                };

                var createdSubject = await _subjectRepository.CreateAsync(subject);
                return (true, "Subject created successfully.", MapToDto(createdSubject));
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate key") == true || 
                                               ex.InnerException?.Message.Contains("IX_Subjects_SubjectCode") == true)
            {
                return (false, $"Subject code '{dto.SubjectCode}' already exists.", null);
            }
            catch (Exception ex)
            {
                return (false, $"An error occurred while creating the subject: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, SubjectDto? Subject)> UpdateSubjectAsync(UpdateSubjectDto dto, int userId)
        {
            var existingSubject = await _subjectRepository.GetByIdAsync(dto.Id);
            if (existingSubject == null)
            {
                return (false, "Subject not found.", null);
            }

            // Validate duplicate SubjectCode (excluding current subject)
            if (await _subjectRepository.SubjectCodeExistsAsync(dto.SubjectCode, dto.Id))
            {
                return (false, $"Subject code '{dto.SubjectCode}' already exists.", null);
            }

            try
            {
                existingSubject.SubjectCode = dto.SubjectCode;
                existingSubject.SubjectName = dto.SubjectName;
                existingSubject.Description = dto.Description;
                existingSubject.UpdatedDate = DateTime.UtcNow;
                existingSubject.UpdatedByUserId = userId;

                var updatedSubject = await _subjectRepository.UpdateAsync(existingSubject);
                return (true, "Subject updated successfully.", MapToDto(updatedSubject));
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate key") == true || 
                                               ex.InnerException?.Message.Contains("IX_Subjects_SubjectCode") == true)
            {
                return (false, $"Subject code '{dto.SubjectCode}' already exists.", null);
            }
            catch (Exception ex)
            {
                return (false, $"An error occurred while updating the subject: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> SoftDeleteSubjectAsync(int id)
        {
            var subject = await _subjectRepository.GetByIdAsync(id);
            if (subject == null)
            {
                return (false, "Subject not found.");
            }

            // Không check HasChapters nữa - cho phép xóa Subject có chapters (cascade delete)
            var result = await _subjectRepository.SoftDeleteAsync(id);
            return result
                ? (true, "Subject deleted successfully.")
                : (false, "Failed to delete subject.");
        }

        public async Task<(bool Success, string Message)> RestoreSubjectAsync(int id)
        {
            var subject = await _subjectRepository.GetByIdAsync(id, includeDeleted: true);
            if (subject == null)
            {
                return (false, "Subject not found.");
            }

            if (!subject.IsDeleted)
            {
                return (false, "Subject is not deleted.");
            }

            var result = await _subjectRepository.RestoreAsync(id);
            return result
                ? (true, "Subject restored successfully.")
                : (false, "Failed to restore subject.");
        }

        public async Task<bool> IsLecturerAssignedToSubjectAsync(int subjectId, int lecturerId)
        {
            return await _subjectRepository.IsLecturerAssignedToSubjectAsync(subjectId, lecturerId);
        }

        public async Task AssignLecturerAsync(int subjectId, int lecturerId)
        {
            await _subjectRepository.AssignLecturerAsync(subjectId, lecturerId);
        }

        public async Task ClearLecturerAssignmentsAsync(int subjectId)
        {
            // Get all assigned lecturers and unassign them
            var assignedLecturers = await GetAssignedLecturersAsync(subjectId);
            foreach (var lecturer in assignedLecturers)
            {
                await _subjectRepository.UnassignLecturerAsync(subjectId, lecturer.Id);
            }
        }

        public async Task<IEnumerable<UserDto>> GetAllLecturersAsync()
        {
            // Get all users with RoleId = 2 (Lecturer)
            var lecturers = await _userRepository.GetUsersByRoleIdAsync(2);
            return lecturers.Select(l => new UserDto
            {
                Id = l.Id,
                Username = l.Username,
                RoleName = l.Role?.RoleName ?? "Lecturer"
            });
        }

        public async Task<IEnumerable<UserDto>> GetAssignedLecturersAsync(int subjectId)
        {
            // Get all lecturers assigned to a subject
            var lecturers = await _subjectRepository.GetAssignedLecturersAsync(subjectId);
            return lecturers.Select(l => new UserDto
            {
                Id = l.Id,
                Username = l.Username,
                RoleName = l.Role?.RoleName ?? "Lecturer"
            });
        }

        /// <summary>
        /// Map Subject entity to SubjectDto
        /// </summary>
        private SubjectDto MapToDto(Subject subject)
        {
            return new SubjectDto
            {
                Id = subject.Id,
                SubjectCode = subject.SubjectCode,
                SubjectName = subject.SubjectName,
                Description = subject.Description,
                CreatedDate = subject.CreatedDate
            };
        }

        /// <summary>
        /// Map Subject entity with Chapters to SubjectDto
        /// </summary>
        private SubjectDto MapToDtoWithChapters(Subject subject)
        {
            return new SubjectDto
            {
                Id = subject.Id,
                SubjectCode = subject.SubjectCode,
                SubjectName = subject.SubjectName,
                Description = subject.Description,
                CreatedDate = subject.CreatedDate,
                Chapters = subject.Chapters?
                    .Where(c => !c.IsDeleted)
                    .OrderBy(c => c.ChapterNumber)
                    .Select(c => new ChapterDto
                    {
                        Id = c.Id,
                        ChapterNumber = c.ChapterNumber,
                        ChapterTitle = c.ChapterTitle,
                        Description = c.Description,
                        SubjectId = c.SubjectId,
                        CreatedDate = c.CreatedDate,
                        IsDeleted = c.IsDeleted
                    })
                    .ToList()
            };
        }
    }
}
