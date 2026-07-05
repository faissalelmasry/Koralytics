using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Koralytics.Domain.Entities.Identity
{
    public class User : IdentityUser<int>, IAuditable
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? ProfileImageUrl { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? UpdatedById { get ; set ; }
        public User? UpdatedByUser { get; set; }
        public int? CreatedById { get; set; }
        public User? CreatedByUser { get; set; }
        DateTime? IAuditable.UpdatedAt { get; set; }
    }
}
