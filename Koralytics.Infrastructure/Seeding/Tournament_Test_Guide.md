# Tournament Testing Guide

## Seeded Tournaments Overview

| ID | Name | Format | Structure | Status | Teams | Purpose |
|----|------|--------|-----------|--------|-------|---------|
| 8001 | Koralytics U18 Champions Cup | 11-side | Group+Knockout | **Completed** | 4 | Read-only: bracket, teams, hall-of-fame |
| 8002 | Koralytics Registration Test Cup | 11-side | Knockout | **Registration** | 1 (Elite Strikers) | Test invite/accept, squad, seeding, draw |
| 8003 | Koralytics Knockout Challenge | 11-side | Knockout | **Draft** | 8 | Full knockout flow |
| 8004 | Koralytics Group Cup | 11-side | Group+Knockout | **Draft** | 8 | Full group+knockout flow |
| 8005 | Koralytics League Championship | 11-side | League | **Draft** | 4 | League round-robin flow |
| 8006 | Koralytics Two-Leg Cup | 11-side | Knockout | **Draft** | 4 | Two-leg aggregate flow |
| 8007 | Koralytics 7-Side Showdown | **7-side** | Knockout | **Draft** | 4 | Alternative format |
| 8008 | Koralytics Two-Leg Group Cup | 11-side | Group+Knockout | **Draft** | 8 | Group+KO + two legs |

## Quick API Test Sequence

### Step 1: Read Completed Tournament
```
GET /api/Tournament
GET /api/Tournament/8001
GET /api/Tournament/8001/bracket
GET /api/Tournament/8001/teams
GET /api/Tournament/8001/hall-of-fame
```

### Step 2: Invite & Accept Academies (use tournament 8002)
```
POST /api/Tournament/8002/invite/2002    → Future Legends
POST /api/Tournament/8002/invite/2003    → Golden Boot
POST /api/Tournament/8002/invite/2004    → Nile Stars
PUT  /api/Tournament/8002/accept/2001    → Elite Strikers accepts
PUT  /api/Tournament/8002/accept/2002    → Future Legends accepts
PUT  /api/Tournament/8002/accept/2003    → Golden Boot accepts
PUT  /api/Tournament/8002/accept/2004    → Nile Stars accepts
```

### Step 3: Register Squads
```
POST /api/Tournament/8002/squad/5001
Body: [6001,6002,6003,6004,6005,6006,6007,6008,6009,6010,6011]

POST /api/Tournament/8002/squad/5002
Body: [6101,6102,6103,6104,6105,6106,6107,6108,6109,6110,6111]

POST /api/Tournament/8002/squad/5003
Body: [6201,6202,6203,6204,6205,6206,6207,6208,6209,6210,6211]

POST /api/Tournament/8002/squad/5004
Body: [6301,6302,6303,6304,6305,6306,6307,6308,6309,6310,6311]
```

### Step 4: Seeding & Draw
```
POST /api/Tournament/8002/seeding
POST /api/Tournament/8002/draw
GET  /api/Tournament/8002/bracket
GET  /api/Tournament/8002/teams
```

### Step 5: Create Tournament
```
POST /api/Tournament
Body: {
  "name": "My Test Tournament",
  "format": 11,
  "structure": 1,
  "ageGroupId": 3001,
  "hasTwoLegs": false,
  "startDate": "2026-08-01",
  "endDate": "2026-08-10"
}
```

### Step 6: Status Management
```
PUT /api/Tournament/8002/status
Body: 3    → InProgress

PUT /api/Tournament/8002/status
Body: 4    → Completed
```

## Entity Relationships

```
Tournament
  ├── TournamentTeams (invited/accepted teams)
  │     ├── TournamentGroupTeams (group assignments)
  │     ├── TournamentStandings (points table)
  │     ├── TournamentSquads (registered players)
  │     ├── HomeFixtures (as home)
  │     └── AwayFixtures (as away)
  ├── TournamentGroups (group stage)
  │     ├── TournamentGroupTeams
  │     ├── TournamentStandings
  │     └── TournamentFixtures
  ├── TournamentRounds (knockout stage)
  │     └── TournamentFixtures
  ├── TournamentSquads
  └── TournamentHallOfFames

TournamentFixture
  ├── GroupId → TournamentGroup (group stage)
  ├── RoundId → TournamentRound (knockout)
  ├── HomeTeamId → TournamentTeam
  ├── AwayTeamId → TournamentTeam
  └── WinnerTeamId → TournamentTeam (nullable)
```

## Enum Values

### TournamentStatus
- 1 = Draft
- 2 = Registration
- 3 = InProgress
- 4 = Completed
- 5 = Cancelled

### TournamentStructure
- 1 = Knockout
- 2 = GroupAndKnockout
- 3 = League

### TournamentTeamStatus
- 1 = Invited
- 2 = Accepted
- 3 = Rejected

### MatchStatus
- 1 = Scheduled
- 2 = Live
- 3 = Completed
- 4 = Cancelled

### MatchFormat
- 5 = FiveSide
- 7 = SevenSide
- 11 = ElevenSide

