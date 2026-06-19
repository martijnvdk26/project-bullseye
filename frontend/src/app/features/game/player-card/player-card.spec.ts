import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PlayerCard } from './player-card';

describe('PlayerCard', () => {
  let component: PlayerCard;
  let fixture: ComponentFixture<PlayerCard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PlayerCard],
    }).compileComponents();

    fixture = TestBed.createComponent(PlayerCard);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
