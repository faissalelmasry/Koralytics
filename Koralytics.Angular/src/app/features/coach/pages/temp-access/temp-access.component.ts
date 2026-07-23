import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CoachAccessService } from '../../../../../core/services/coach/coach-access.service';
import { GrantTempAccessDto, TempAccessDto } from '../../../../../core/interfaces/coach.interfaces';

@Component({
  selector: 'app-temp-access',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './temp-access.component.html',
  styleUrls: ['./temp-access.component.css']
})
export class TempAccessComponent implements OnInit {
  private accessService = inject(CoachAccessService);

  activeGrants = signal<TempAccessDto[]>([]);
  loading = signal(false);
  error = signal('');
  
  // Grant Form
  newGrant: GrantTempAccessDto = {
    grantedToUserId: 0,
    accessLevel: 'ReadOnly',
    expiresAt: ''
  };
  
  // Default to tomorrow for UI simplicity
  defaultExpiry = new Date(Date.now() + 86400000).toISOString().split('T')[0];
  
  submitting = signal(false);
  grantError = signal('');
  successMsg = signal('');

  ngOnInit(): void {
    this.newGrant.expiresAt = this.defaultExpiry;
    this.loadActiveGrants();
  }

  loadActiveGrants(): void {
    this.loading.set(true);
    this.error.set('');
    this.accessService.getActiveGrants().subscribe({
      next: (data) => {
        this.activeGrants.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err?.error?.message || 'Failed to load access grants.');
        this.loading.set(false);
      }
    });
  }

  grantAccess(): void {
    if (!this.newGrant.grantedToUserId || !this.newGrant.expiresAt) return;
    
    this.submitting.set(true);
    this.grantError.set('');
    this.successMsg.set('');

    // Ensure it's passed as ISO string
    const dtoToSubmit = { ...this.newGrant };
    dtoToSubmit.expiresAt = new Date(dtoToSubmit.expiresAt).toISOString();

    this.accessService.grantTempAccess(dtoToSubmit).subscribe({
      next: (grantedAccess) => {
        this.activeGrants.update(list => [grantedAccess, ...list]);
        this.newGrant.grantedToUserId = 0;
        this.newGrant.accessLevel = 'ReadOnly';
        this.newGrant.expiresAt = this.defaultExpiry;
        
        this.successMsg.set('Access granted successfully.');
        this.submitting.set(false);
        setTimeout(() => this.successMsg.set(''), 3000);
      },
      error: (err) => {
        this.grantError.set(err?.error?.message || 'Failed to grant access.');
        this.submitting.set(false);
      }
    });
  }

  revokeAccess(accessId: number): void {
    if (!confirm('Are you sure you want to revoke this access immediately?')) return;
    
    this.accessService.revokeTempAccess(accessId).subscribe({
      next: (revokedAccess) => {
        // Remove from list or update status
        this.activeGrants.update(list => list.filter(g => g.id !== accessId));
      },
      error: (err) => {
        alert(err?.error?.message || 'Failed to revoke access.');
      }
    });
  }
}
