import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { TeamResponseDto } from '../../../../../core/interfaces/academy.models';
import { EmptyStateComponent } from '../../../../../shared/components/empty-state/empty-state';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';

@Component({
  selector: 'app-academy-profile-teams',
  standalone: true,
  imports: [CommonModule, TranslatePipe, EmptyStateComponent, ScrollRevealDirective],
  templateUrl: './academy-profile-teams.html',
  styleUrls: ['./academy-profile-teams.css']
})
export class AcademyProfileTeamsComponent {
  @Input() teams: TeamResponseDto[] = [];
  @Input() expandedTeams: Set<number> = new Set<number>();
  
  private router = inject(Router);

  toggleTeamExpand(teamId: number): void {
    if (this.expandedTeams.has(teamId)) {
      this.expandedTeams.delete(teamId);
    } else {
      this.expandedTeams.add(teamId);
    }
  }

  isTeamExpanded(teamId: number): boolean {
    return this.expandedTeams.has(teamId);
  }

  viewMemberProfile(id: number | undefined, role: string = 'Player'): void {
    if (!id) return;
    if (role?.toLowerCase() === 'coach') {
      this.router.navigate(['/coach/profile', id]);
    } else {
      this.router.navigate(['/player/profile', id]);
    }
  }
}
