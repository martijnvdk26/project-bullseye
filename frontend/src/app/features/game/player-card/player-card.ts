import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-player-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './player-card.html',
  styleUrl: './player-card.css',
})
export class PlayerCardComponent {
  // Receives the player object from the parent container
  @Input() player!: any;

  getCheckoutHint(score: number): string {
    // Returns an empty string if the score is not within checkout range
    if (score > 170) return '';

    // Contains the checkout combinations for critical scores
    const hints: { [key: number]: string } = {
      170: 'T20 - T20 - BULL',
      167: 'T20 - T19 - BULL',
      164: 'T20 - T18 - BULL',
      160: 'T20 - T20 - D20',
      150: 'T20 - T18 - D18',
      140: 'T20 - T20 - D10',
      120: 'T20 - 20 - D20',
      100: 'T20 - D20',
      60: '20 - D20',
      40: 'D20',
      32: 'D16',
      16: 'D8',
    };

    // Returns the calculated hint or a generic string for remaining scores
    return hints[score] || 'Under 170!';
  }
}
