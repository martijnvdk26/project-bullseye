import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GuestLobby } from './guest-lobby';

describe('GuestLobby', () => {
  let component: GuestLobby;
  let fixture: ComponentFixture<GuestLobby>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GuestLobby],
    }).compileComponents();

    fixture = TestBed.createComponent(GuestLobby);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
