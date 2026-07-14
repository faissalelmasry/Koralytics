using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.DTOs.Scouter
{
    public class PlayerProfileViewAnalyticsDto
    {
        public int TotalViewsCount { get; set; }
        public List<ProfileViewerDetailDto> RecentViews { get; set; } = new();
    }
}
