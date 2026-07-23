import { TestBed } from '@angular/core/testing';

import { Academy } from './academy';

describe('Academy', () => {
  let service: Academy;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Academy);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
