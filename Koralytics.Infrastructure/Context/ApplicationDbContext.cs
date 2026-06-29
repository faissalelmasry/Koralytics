using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Koralytics.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Koralytics.Infrastructure.Context
{
    public class ApplicationDbContext:IdentityDbContext<User,Role, int>
    {
    }
}
