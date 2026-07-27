using System;
using System.Collections.Generic;

namespace Koralytics.Application.DTOs.SystemAdmin
{
    public class UserListRequestDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? RoleFilter { get; set; }
        public bool? IsDeletedFilter { get; set; }
    }

    public class UserSummaryDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public string? ProfileImageUrl { get; set; }
    }

    public class UserDetailDto : UserSummaryDto
    {
        public bool EmailConfirmed { get; set; }
        public string? GoogleId { get; set; }
        public int? AcademyId { get; set; }
        public string? AcademyName { get; set; }
    }

    public class UserListResponseDto
    {
        public List<UserSummaryDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }

    public class UpdateUserRolesDto
    {
        public List<string> Roles { get; set; } = new();
    }

    public class UpdateUserStatusDto
    {
        public bool IsDeleted { get; set; }
    }
}
