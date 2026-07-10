using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.Drill
{
    public class CategoryPerformanceDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal AverageScore { get; set; }
    }
}
