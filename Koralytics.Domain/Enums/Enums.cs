using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Koralytics.Domain.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MatchType
    {
        Friendly = 1,
        Tournament = 2,
        Session = 3
    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MatchFormat
    {
        FiveSide = 5,
        SevenSide = 7,
        ElevenSide = 11
    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MatchStatus
    {
        Scheduled = 1,
        Live = 2,
        Completed = 3,
        Cancelled = 4
    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DrillMode
    {
        Manual = 1,
        SuccessOrMissed = 2
    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DifficultyLevel
    {
        Beginner = 1,
        Intermediate = 2,
        Advanced = 3
    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AvailabilityStatus
    {
        Available = 1,
        Injured = 2,
        Resting = 3,
        Suspended = 4
    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TournamentStructure
    {
        Knockout = 1,
        GroupAndKnockout = 2,
        League = 3
    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TournamentStatus
    {
        Draft = 1,
        Registration = 2,
        InProgress = 3,
        Completed = 4,
        Cancelled = 5
    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SubscriptionStatus
    {
        Paid = 1,
        Unpaid = 2,
        Grace = 3
    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PlayerAcademyStatus
    {
        Active = 1,
        Loaned = 2,
        Transferred = 3
    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AcademyStatus
    {
        Active = 1,
        Suspended = 2,
        Inactive = 3
    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AcademyRequestStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3
    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TournamentTeamStatus
    {
        Invited = 1,
        Accepted = 2,
        Rejected = 3
    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SessionType
    {
        Regular = 1,
        PreSeason = 2,
        SessionMatch = 3
    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
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
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PreferredFoot
    {
        Right = 1,
        Left = 2,
        Both = 3
    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AIReportType
    {
        Match = 1,
        Tournament = 2,
        Season = 3
    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AnnouncementTargetType
    {
        All = 1,
        Team = 2,
        AgeGroup = 3,
        Role = 4
    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TempAccessStatus
    {
        Active = 1,
        Revoked = 2,
        Expired = 3
    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RoleAuditAction
    {
        Assigned = 1,
        Removed = 2,
        Modified = 3
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AcademyBadgeType
    {
        Verified = 1,
        TopPerformer = 2,
        Premium = 3
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
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
