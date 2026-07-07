using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Domain.Enums
{
    public enum MatchType
    {
        Friendly = 1,
        Tournament = 2,
        Session = 3
    }
    public enum MatchFormat
    {
        FiveSide = 5,
        SevenSide = 7,
        ElevenSide = 11
    }
    public enum MatchStatus
    {
        Scheduled = 1,
        Live = 2,
        Completed = 3,
        Cancelled = 4
    }
    public enum DrillMode
    {
        Manual = 1,
        SuccessOrMissed = 2
    }
    public enum DifficultyLevel
    {
        Beginner = 1,
        Intermediate = 2,
        Advanced = 3
    }
    public enum AvailabilityStatus
    {
        Available = 1,
        Injured = 2,
        Resting = 3,
        Suspended = 4
    }
    public enum TournamentStructure
    {
        Knockout = 1,
        GroupAndKnockout = 2,
        League = 3
    }
    public enum TournamentStatus
    {
        Draft = 1,
        Registration = 2,
        InProgress = 3,
        Completed = 4,
        Cancelled = 5
    }
    public enum SubscriptionStatus
    {
        Paid = 1,
        Unpaid = 2,
        Grace = 3
    }
    public enum PlayerAcademyStatus
    {
        Active = 1,
        Loaned = 2,
        Transferred = 3
    }
    public enum AcademyStatus
    {
        Active = 1,
        Suspended = 2,
        Inactive = 3
    }
    public enum AcademyRequestStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3
    }
    public enum TournamentTeamStatus
    {
        Invited = 1,
        Accepted = 2,
        Rejected = 3
    }
    public enum SessionType
    {
        Regular = 1,
        PreSeason = 2,
        SessionMatch = 3
    }
    public enum MatchEventType
    {
        Goal = 1,
        YellowCard = 2,
        RedCard = 3,
        Substitution = 4,
        OwnGoal = 5,
        PenaltyScored = 6,
        PenaltyMissed = 7,
        CleanSheet = 8
    }
    public enum PreferredFoot
    {
        Right = 1,
        Left = 2,
        Both = 3
    }
    public enum AIReportType
    {
        Match = 1,
        Tournament = 2,
        Season = 3
    }
    public enum AnnouncementTargetType
    {
        All = 1,
        Team = 2,
        AgeGroup = 3,
        Role = 4
    }
    public enum TempAccessStatus
    {
        Active = 1,
        Revoked = 2,
        Expired = 3
    }
    public enum RoleAuditAction
    {
        Assigned = 1,
        Removed = 2,
        Modified = 3
    }

    public enum AcademyBadgeType
    {
        Verified = 1,
        TopPerformer = 2,
        Premium = 3
    }

    public enum TransferClassification
    {
        Elite = 1,
        Trainable = 2,
        Natural = 3,
        NeedsWork = 4,
        Developing = 5,
        InsufficientData=0
    }

}
