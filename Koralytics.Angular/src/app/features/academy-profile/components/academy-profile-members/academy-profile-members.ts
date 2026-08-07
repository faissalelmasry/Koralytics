import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AcademyMemberResponseDto } from '../../../../../core/interfaces/academy.models';
import { EmptyStateComponent } from '../../../../../shared/components/empty-state/empty-state';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';

@Component({
  selector: 'app-academy-profile-members',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe, EmptyStateComponent, ScrollRevealDirective],
  templateUrl: './academy-profile-members.html',
  styleUrls: ['./academy-profile-members.css']
})
export class AcademyProfileMembersComponent {
  @Input() members: AcademyMemberResponseDto[] = [];
  @Input() selectedMemberRole: string = 'All';
  @Input() memberSearchQuery: string = '';

  @Output() roleChange = new EventEmitter<string>();
  @Output() searchChange = new EventEmitter<string>();

  private router = inject(Router);

  setMemberRole(role: string): void {
    this.selectedMemberRole = role;
    this.roleChange.emit(role);
  }

  onSearchChange(): void {
    this.searchChange.emit(this.memberSearchQuery);
  }

  get filteredMembers(): AcademyMemberResponseDto[] {
    return this.members.filter(m => {
      const matchesRole = this.selectedMemberRole === 'All' ||
        m.role?.toLowerCase() === this.selectedMemberRole.toLowerCase();
      const matchesSearch = !this.memberSearchQuery ||
        m.fullName?.toLowerCase().includes(this.memberSearchQuery.toLowerCase()) ||
        m.position?.toLowerCase().includes(this.memberSearchQuery.toLowerCase());
      return matchesRole && matchesSearch;
    });
  }

  viewMemberProfile(id: number | undefined, role: string | undefined): void {
    if (!id) return;
    if (role?.toLowerCase() === 'coach') {
      this.router.navigate(['/coach/profile', id]);
    } else {
      this.router.navigate(['/player/profile', id]);
    }
  }
}
